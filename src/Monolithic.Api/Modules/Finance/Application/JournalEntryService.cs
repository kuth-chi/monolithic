using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Implements all GL journal-entry operations following DRY, single-responsibility principles.
/// 
/// Key invariants enforced by this service:
///  1. Σ DebitAmount = Σ CreditAmount before posting.
///  2. Minimum 2 lines per entry.
///  3. Each line must have exactly one non-zero side (debit XOR credit).
///  4. All accounts must be active, detail (non-header) accounts of the same business.
///  5. Posted entries are immutable; reversals generate new entries with full audit trail.
///  6. Entry numbers are sequential and deterministic per business per year.
/// </summary>
public sealed class JournalEntryService(
    ApplicationDbContext context,
    TimeProvider timeProvider) : IJournalEntryService
{
    // ── Query ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<JournalEntryDto>> GetPagedAsync(
        JournalEntryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<JournalEntryDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<JournalEntryDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entry = await context.JournalEntries
            .AsNoTracking()
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Include(e => e.AuditLogs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entry is null ? null : MapToDto(entry);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public async Task<JournalEntryDto> CreateAsync(
        CreateJournalEntryRequest request,
        Guid createdByUserId,
        string createdByDisplayName,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await ValidateBusinessExistsAsync(request.BusinessId, cancellationToken);
        await ValidateAccountsAsync(request.BusinessId, request.Lines.Select(l => l.AccountId), cancellationToken);
        ValidateLineAmounts(request.Lines);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            EntryNumber = await GenerateEntryNumberAsync(request.BusinessId, now.Year, cancellationToken),
            FiscalPeriod = request.TransactionDate.ToString("yyyy-MM"),
            TransactionDate = request.TransactionDate,
            Description = request.Description.Trim(),
            Status = JournalEntryStatus.Draft,
            SourceType = request.SourceType,
            SourceDocumentReference = request.SourceDocumentReference?.Trim(),
            SourceDocumentId = request.SourceDocumentId,
            CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
            ExchangeRate = request.ExchangeRate,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now
        };

        entry.Lines = BuildLines(entry.Id, request.Lines, request.ExchangeRate);

        entry.AuditLogs.Add(new JournalEntryAuditLog
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            UserId = createdByUserId,
            UserDisplayName = createdByDisplayName,
            Action = JournalEntryAuditAction.Created,
            OccurredAtUtc = now
        });

        await context.JournalEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await LoadFullEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<JournalEntryDto> PostAsync(
        Guid id,
        PostJournalEntryRequest request,
        Guid postedByUserId,
        string postedByDisplayName,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var entry = await context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry '{id}' not found.");

        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft entries can be posted. Current status: {entry.Status}.");

        if (entry.Lines.Count < 2)
            throw new InvalidOperationException("A journal entry must have at least 2 lines.");

        var totalDebits = entry.Lines.Sum(l => l.DebitAmountBase);
        var totalCredits = entry.Lines.Sum(l => l.CreditAmountBase);

        if (totalDebits != totalCredits)
            throw new InvalidOperationException(
                $"Entry is not balanced. Total debits ({totalDebits:N2}) ≠ Total credits ({totalCredits:N2}).");

        entry.Status = JournalEntryStatus.Posted;
        entry.TotalDebits = totalDebits;
        entry.TotalCredits = totalCredits;
        entry.PostedByUserId = postedByUserId;
        entry.PostedAtUtc = now;
        entry.ModifiedAtUtc = now;

        context.JournalEntryAuditLogs.Add(new JournalEntryAuditLog
        {
            Id = Guid.NewGuid(),
            JournalEntryId = entry.Id,
            UserId = postedByUserId,
            UserDisplayName = postedByDisplayName,
            Action = JournalEntryAuditAction.Posted,
            OccurredAtUtc = now,
            Notes = request.Notes
        });

        await context.SaveChangesAsync(cancellationToken);

        return await LoadFullEntryAsync(entry.Id, cancellationToken);
    }

    public async Task<JournalEntryDto> ReverseAsync(
        Guid id,
        ReverseJournalEntryRequest request,
        Guid reversedByUserId,
        string reversedByDisplayName,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var reversalDate = request.ReversalDate ?? DateOnly.FromDateTime(now.DateTime);

        var original = await context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry '{id}' not found.");

        if (original.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException(
                $"Only Posted entries can be reversed. Current status: {original.Status}.");

        if (original.ReversedByEntryId.HasValue)
            throw new InvalidOperationException(
                $"Entry '{id}' has already been reversed by entry '{original.ReversedByEntryId}'.");

        // Build reversal entry (mirror image — debits ↔ credits)
        var reversal = new JournalEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = original.BusinessId,
            EntryNumber = await GenerateEntryNumberAsync(original.BusinessId, now.Year, cancellationToken),
            FiscalPeriod = reversalDate.ToString("yyyy-MM"),
            TransactionDate = reversalDate,
            Description = $"Reversal of {original.EntryNumber}: {request.Reason.Trim()}",
            Status = JournalEntryStatus.Reversal,
            SourceType = original.SourceType,
            SourceDocumentReference = original.SourceDocumentReference,
            SourceDocumentId = original.SourceDocumentId,
            ReversalOfEntryId = original.Id,
            CurrencyCode = original.CurrencyCode,
            ExchangeRate = original.ExchangeRate,
            CreatedByUserId = reversedByUserId,
            CreatedAtUtc = now,
            PostedByUserId = reversedByUserId,
            PostedAtUtc = now
        };

        // Mirror lines: swap debit/credit amounts
        reversal.Lines = original.Lines
            .Select((l, idx) => new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = reversal.Id,
                AccountId = l.AccountId,
                LineNumber = idx + 1,
                DebitAmount = l.CreditAmount,
                CreditAmount = l.DebitAmount,
                DebitAmountBase = l.CreditAmountBase,
                CreditAmountBase = l.DebitAmountBase,
                CostCenter = l.CostCenter,
                ProjectCode = l.ProjectCode,
                LineDescription = l.LineDescription
            })
            .ToList();

        reversal.TotalDebits = reversal.Lines.Sum(l => l.DebitAmountBase);
        reversal.TotalCredits = reversal.Lines.Sum(l => l.CreditAmountBase);

        reversal.AuditLogs.Add(new JournalEntryAuditLog
        {
            Id = Guid.NewGuid(),
            JournalEntryId = reversal.Id,
            UserId = reversedByUserId,
            UserDisplayName = reversedByDisplayName,
            Action = JournalEntryAuditAction.Reversed,
            OccurredAtUtc = now,
            Notes = request.Reason
        });

        // Mark original as reversed
        original.Status = JournalEntryStatus.Reversed;
        original.ReversedByEntryId = reversal.Id;
        original.ReversedByUserId = reversedByUserId;
        original.ReversedAtUtc = now;
        original.ModifiedAtUtc = now;

        // Audit on original
        context.JournalEntryAuditLogs.Add(new JournalEntryAuditLog
        {
            Id = Guid.NewGuid(),
            JournalEntryId = original.Id,
            UserId = reversedByUserId,
            UserDisplayName = reversedByDisplayName,
            Action = JournalEntryAuditAction.Reversed,
            OccurredAtUtc = now,
            Notes = $"Reversed by entry {reversal.EntryNumber}. Reason: {request.Reason}"
        });

        await context.JournalEntries.AddAsync(reversal, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await LoadFullEntryAsync(reversal.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntryAuditLogDto>> GetAuditLogsAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var logs = await context.JournalEntryAuditLogs
            .AsNoTracking()
            .Where(l => l.JournalEntryId == journalEntryId)
            .OrderBy(l => l.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        return logs.Select(MapAuditLogToDto).ToList();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private IQueryable<JournalEntry> BuildBaseQuery(JournalEntryFilter filter)
    {
        var q = context.JournalEntries
            .AsNoTracking()
            .Where(e => e.BusinessId == filter.BusinessId);

        if (!string.IsNullOrWhiteSpace(filter.FiscalPeriod))
            q = q.Where(e => e.FiscalPeriod == filter.FiscalPeriod);

        if (filter.TransactionDateFrom.HasValue)
            q = q.Where(e => e.TransactionDate >= filter.TransactionDateFrom.Value);

        if (filter.TransactionDateTo.HasValue)
            q = q.Where(e => e.TransactionDate <= filter.TransactionDateTo.Value);

        if (filter.Status.HasValue)
            q = q.Where(e => e.Status == filter.Status.Value);

        if (filter.SourceType.HasValue)
            q = q.Where(e => e.SourceType == filter.SourceType.Value);

        if (filter.AccountId.HasValue)
            q = q.Where(e => e.Lines.Any(l => l.AccountId == filter.AccountId.Value));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            q = q.Where(e =>
                e.EntryNumber.ToLower().Contains(term) ||
                e.Description.ToLower().Contains(term) ||
                (e.SourceDocumentReference != null && e.SourceDocumentReference.ToLower().Contains(term)));
        }

        return q;
    }

    private async Task ValidateBusinessExistsAsync(Guid businessId, CancellationToken ct)
    {
        if (!await context.Businesses.AnyAsync(b => b.Id == businessId, ct))
            throw new InvalidOperationException($"Business '{businessId}' not found.");
    }

    private async Task ValidateAccountsAsync(
        Guid businessId,
        IEnumerable<Guid> accountIds,
        CancellationToken ct)
    {
        var ids = accountIds.Distinct().ToList();

        var accounts = await context.ChartOfAccounts
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.BusinessId, a.IsActive, a.IsHeaderAccount })
            .ToListAsync(ct);

        var notFound = ids.Except(accounts.Select(a => a.Id)).ToList();
        if (notFound.Count > 0)
            throw new InvalidOperationException(
                $"Account(s) not found: {string.Join(", ", notFound)}");

        var wrongBusiness = accounts.Where(a => a.BusinessId != businessId).ToList();
        if (wrongBusiness.Count > 0)
            throw new InvalidOperationException(
                $"Account(s) do not belong to business '{businessId}': {string.Join(", ", wrongBusiness.Select(a => a.Id))}");

        var inactive = accounts.Where(a => !a.IsActive).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException(
                $"Account(s) are inactive and cannot receive postings: {string.Join(", ", inactive.Select(a => a.Id))}");

        var headers = accounts.Where(a => a.IsHeaderAccount).ToList();
        if (headers.Count > 0)
            throw new InvalidOperationException(
                $"Account(s) are header/group accounts and cannot receive direct postings: {string.Join(", ", headers.Select(a => a.Id))}");
    }

    private static void ValidateLineAmounts(IEnumerable<CreateJournalEntryLineRequest> lines)
    {
        foreach (var (line, index) in lines.Select((l, i) => (l, i + 1)))
        {
            var hasDebit = line.DebitAmount > 0;
            var hasCredit = line.CreditAmount > 0;

            if (!hasDebit && !hasCredit)
                throw new InvalidOperationException(
                    $"Line {index}: Both DebitAmount and CreditAmount are zero.");

            if (hasDebit && hasCredit)
                throw new InvalidOperationException(
                    $"Line {index}: A line cannot have both DebitAmount and CreditAmount > 0.");

            if (line.DebitAmount < 0 || line.CreditAmount < 0)
                throw new InvalidOperationException(
                    $"Line {index}: Amounts cannot be negative.");
        }
    }

    private static List<JournalEntryLine> BuildLines(
        Guid entryId,
        IEnumerable<CreateJournalEntryLineRequest> lineRequests,
        decimal exchangeRate)
        => lineRequests
            .Select((req, idx) => new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                JournalEntryId = entryId,
                AccountId = req.AccountId,
                LineNumber = idx + 1,
                DebitAmount = req.DebitAmount,
                CreditAmount = req.CreditAmount,
                DebitAmountBase = req.DebitAmount * exchangeRate,
                CreditAmountBase = req.CreditAmount * exchangeRate,
                CostCenter = req.CostCenter?.Trim(),
                ProjectCode = req.ProjectCode?.Trim(),
                LineDescription = req.LineDescription?.Trim()
            })
            .ToList();

    /// <summary>
    /// Generates a deterministic, sequential entry number per business per year.
    /// Format: JE-{YYYY}-{NNNNN}  e.g. "JE-2026-00001"
    /// </summary>
    private async Task<string> GenerateEntryNumberAsync(Guid businessId, int year, CancellationToken ct)
    {
        var prefix = $"JE-{year}-";

        var lastNumber = await context.JournalEntries
            .Where(e => e.BusinessId == businessId && e.EntryNumber.StartsWith(prefix))
            .Select(e => e.EntryNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        var nextSeq = 1;
        if (lastNumber is not null && int.TryParse(lastNumber[prefix.Length..], out var lastSeq))
            nextSeq = lastSeq + 1;

        return $"{prefix}{nextSeq:D5}";
    }

    private async Task<JournalEntryDto> LoadFullEntryAsync(Guid id, CancellationToken ct)
    {
        var entry = await context.JournalEntries
            .AsNoTracking()
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Include(e => e.AuditLogs)
            .AsSplitQuery()
            .FirstAsync(e => e.Id == id, ct);

        return MapToDto(entry);
    }

    // ── Mappers (static for purity & testability) ────────────────────────────

    private static JournalEntryDto MapToDto(JournalEntry e) => new()
    {
        Id = e.Id,
        BusinessId = e.BusinessId,
        EntryNumber = e.EntryNumber,
        FiscalPeriod = e.FiscalPeriod,
        TransactionDate = e.TransactionDate,
        Description = e.Description,
        Status = e.Status,
        SourceType = e.SourceType,
        SourceDocumentReference = e.SourceDocumentReference,
        SourceDocumentId = e.SourceDocumentId,
        ReversalOfEntryId = e.ReversalOfEntryId,
        ReversedByEntryId = e.ReversedByEntryId,
        CurrencyCode = e.CurrencyCode,
        ExchangeRate = e.ExchangeRate,
        TotalDebits = e.TotalDebits,
        TotalCredits = e.TotalCredits,
        CreatedByUserId = e.CreatedByUserId,
        CreatedAtUtc = e.CreatedAtUtc,
        PostedByUserId = e.PostedByUserId,
        PostedAtUtc = e.PostedAtUtc,
        ReversedByUserId = e.ReversedByUserId,
        ReversedAtUtc = e.ReversedAtUtc,
        Lines = e.Lines
            .OrderBy(l => l.LineNumber)
            .Select(MapLineToDto)
            .ToList(),
        AuditLogs = e.AuditLogs
            .OrderBy(a => a.OccurredAtUtc)
            .Select(MapAuditLogToDto)
            .ToList()
    };

    private static JournalEntryLineDto MapLineToDto(JournalEntryLine l) => new()
    {
        Id = l.Id,
        JournalEntryId = l.JournalEntryId,
        AccountId = l.AccountId,
        AccountLabel = l.Account is null
            ? string.Empty
            : $"{l.Account.AccountNumber} – {l.Account.Name}",
        LineNumber = l.LineNumber,
        DebitAmount = l.DebitAmount,
        CreditAmount = l.CreditAmount,
        DebitAmountBase = l.DebitAmountBase,
        CreditAmountBase = l.CreditAmountBase,
        CostCenter = l.CostCenter,
        ProjectCode = l.ProjectCode,
        LineDescription = l.LineDescription
    };

    private static JournalEntryAuditLogDto MapAuditLogToDto(JournalEntryAuditLog a) => new()
    {
        Id = a.Id,
        JournalEntryId = a.JournalEntryId,
        UserId = a.UserId,
        UserDisplayName = a.UserDisplayName,
        Action = a.Action,
        OccurredAtUtc = a.OccurredAtUtc,
        Notes = a.Notes
    };
}

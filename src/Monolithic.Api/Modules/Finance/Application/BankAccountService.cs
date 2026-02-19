using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Finance.Application;

public sealed class BankAccountService(ApplicationDbContext context) : IBankAccountService
{
    public async Task<IReadOnlyCollection<BankAccountDto>> GetAllAsync(
        Guid? businessId = null,
        Guid? vendorId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOwnerFilter(businessId, vendorId, customerId);

        IQueryable<BankAccountBase> query = context.BankAccounts.AsNoTracking();

        if (businessId.HasValue)
        {
            query = context.BusinessBankAccounts
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId.Value);
        }
        else if (vendorId.HasValue)
        {
            query = context.VendorBankAccounts
                .AsNoTracking()
                .Where(x => x.VendorId == vendorId.Value);
        }
        else if (customerId.HasValue)
        {
            query = context.CustomerBankAccounts
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId.Value);
        }

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<BankAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<BankAccountDto> CreateForBusinessAsync(CreateBusinessBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var businessExists = await context.Businesses.AnyAsync(x => x.Id == request.BusinessId, cancellationToken);
        if (!businessExists)
        {
            throw new InvalidOperationException("Business not found.");
        }

        var duplicate = await context.BusinessBankAccounts.AnyAsync(
            x => x.BusinessId == request.BusinessId && x.AccountNumber == request.AccountNumber,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Business bank account number already exists.");
        }

        var entity = new BusinessBankAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            AccountName = request.AccountName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            BankName = request.BankName.Trim(),
            BranchName = request.BranchName.Trim(),
            SwiftCode = request.SwiftCode.Trim(),
            RoutingNumber = request.RoutingNumber.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            IsPrimary = request.IsPrimary,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.BusinessBankAccounts.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<BankAccountDto> CreateForVendorAsync(CreateVendorBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var vendorExists = await context.Vendors.AnyAsync(x => x.Id == request.VendorId, cancellationToken);
        if (!vendorExists)
        {
            throw new InvalidOperationException("Vendor not found.");
        }

        var duplicate = await context.VendorBankAccounts.AnyAsync(
            x => x.VendorId == request.VendorId && x.AccountNumber == request.AccountNumber,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Vendor bank account number already exists.");
        }

        var entity = new VendorBankAccount
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            AccountName = request.AccountName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            BankName = request.BankName.Trim(),
            BranchName = request.BranchName.Trim(),
            SwiftCode = request.SwiftCode.Trim(),
            RoutingNumber = request.RoutingNumber.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            IsPrimary = request.IsPrimary,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.VendorBankAccounts.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<BankAccountDto> CreateForCustomerAsync(CreateCustomerBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var duplicate = await context.CustomerBankAccounts.AnyAsync(
            x => x.CustomerId == request.CustomerId && x.AccountNumber == request.AccountNumber,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Customer bank account number already exists.");
        }

        var entity = new CustomerBankAccount
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            AccountName = request.AccountName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            BankName = request.BankName.Trim(),
            BranchName = request.BranchName.Trim(),
            SwiftCode = request.SwiftCode.Trim(),
            RoutingNumber = request.RoutingNumber.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            IsPrimary = request.IsPrimary,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.CustomerBankAccounts.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<BankAccountDto?> UpdateAsync(Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.AccountName = request.AccountName.Trim();
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.BankName = request.BankName.Trim();
        entity.BranchName = request.BranchName.Trim();
        entity.SwiftCode = request.SwiftCode.Trim();
        entity.RoutingNumber = request.RoutingNumber.Trim();
        entity.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        context.BankAccounts.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static void ValidateOwnerFilter(Guid? businessId, Guid? vendorId, Guid? customerId)
    {
        var filterCount = 0;

        if (businessId.HasValue)
        {
            filterCount++;
        }

        if (vendorId.HasValue)
        {
            filterCount++;
        }

        if (customerId.HasValue)
        {
            filterCount++;
        }

        if (filterCount > 1)
        {
            throw new InvalidOperationException("Filter by exactly one owner type at a time.");
        }
    }

    private static BankAccountDto MapToDto(BankAccountBase entity)
    {
        return entity switch
        {
            BusinessBankAccount business => new BankAccountDto
            {
                Id = business.Id,
                OwnerType = "Business",
                BusinessId = business.BusinessId,
                AccountName = business.AccountName,
                AccountNumber = business.AccountNumber,
                BankName = business.BankName,
                BranchName = business.BranchName,
                SwiftCode = business.SwiftCode,
                RoutingNumber = business.RoutingNumber,
                CurrencyCode = business.CurrencyCode,
                IsPrimary = business.IsPrimary,
                IsActive = business.IsActive,
                CreatedAtUtc = business.CreatedAtUtc,
                ModifiedAtUtc = business.ModifiedAtUtc
            },
            VendorBankAccount vendor => new BankAccountDto
            {
                Id = vendor.Id,
                OwnerType = "Vendor",
                VendorId = vendor.VendorId,
                AccountName = vendor.AccountName,
                AccountNumber = vendor.AccountNumber,
                BankName = vendor.BankName,
                BranchName = vendor.BranchName,
                SwiftCode = vendor.SwiftCode,
                RoutingNumber = vendor.RoutingNumber,
                CurrencyCode = vendor.CurrencyCode,
                IsPrimary = vendor.IsPrimary,
                IsActive = vendor.IsActive,
                CreatedAtUtc = vendor.CreatedAtUtc,
                ModifiedAtUtc = vendor.ModifiedAtUtc
            },
            CustomerBankAccount customer => new BankAccountDto
            {
                Id = customer.Id,
                OwnerType = "Customer",
                CustomerId = customer.CustomerId,
                AccountName = customer.AccountName,
                AccountNumber = customer.AccountNumber,
                BankName = customer.BankName,
                BranchName = customer.BranchName,
                SwiftCode = customer.SwiftCode,
                RoutingNumber = customer.RoutingNumber,
                CurrencyCode = customer.CurrencyCode,
                IsPrimary = customer.IsPrimary,
                IsActive = customer.IsActive,
                CreatedAtUtc = customer.CreatedAtUtc,
                ModifiedAtUtc = customer.ModifiedAtUtc
            },
            _ => throw new InvalidOperationException("Unknown bank account type.")
        };
    }
}

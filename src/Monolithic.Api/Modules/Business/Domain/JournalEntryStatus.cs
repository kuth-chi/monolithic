namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Lifecycle state of a GL journal entry.
/// Once <see cref="Posted"/>, the entry is immutable; reversals create new entries.
/// </summary>
public enum JournalEntryStatus
{
    /// <summary>Entry is being drafted — not yet balanced or posted.</summary>
    Draft = 0,

    /// <summary>Entry is balanced and permanently posted to the GL.</summary>
    Posted = 1,

    /// <summary>Entry has been reversed by a subsequent reversal entry.</summary>
    Reversed = 2,

    /// <summary>Entry is itself a reversal of another posted entry.</summary>
    Reversal = 3
}

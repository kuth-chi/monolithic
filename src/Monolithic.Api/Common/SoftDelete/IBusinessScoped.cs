namespace Monolithic.Api.Common.SoftDelete;

/// <summary>
/// Marks an entity as belonging to a specific business/tenant.
/// Used by <see cref="BackgroundServices.SoftDeletePurgeService"/> to apply
/// per-business retention rules when purging soft-deleted records.
/// </summary>
public interface IBusinessScoped
{
    Guid BusinessId { get; }
}

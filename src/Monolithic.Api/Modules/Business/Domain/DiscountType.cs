namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// How a discount is applied — either a flat currency amount or a percentage.
/// </summary>
public enum DiscountType
{
    None = 0,
    Amount = 1,
    Percentage = 2
}

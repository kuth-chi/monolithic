namespace Monolithic.Api.Modules.Platform.Templates.Domain;

/// <summary>
/// Determines the visibility and override hierarchy for a template.
///
/// Resolution order (most specific wins):
///   User → Business → System
///
/// A business-level template overrides the system default for that business.
/// A user-level template overrides both for that user.
/// </summary>
public enum TemplateScope
{
    /// <summary>Shipped by the platform / module. Visible to all businesses.</summary>
    System = 0,

    /// <summary>Overrides the system template for a specific business.</summary>
    Business = 1,

    /// <summary>Personal override for a specific user within a business.</summary>
    User = 2,
}

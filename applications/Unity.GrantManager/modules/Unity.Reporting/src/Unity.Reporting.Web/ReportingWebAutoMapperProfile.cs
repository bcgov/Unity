using AutoMapper;

namespace Unity.Reporting.Web;

/// <summary>
/// AutoMapper configuration profile for Unity.Reporting Web module defining mappings between domain objects and view models.
/// Currently intentionally left blank as no specific web-layer object mappings are required,
/// but serves as the registration point for any future web-specific AutoMapper configurations
/// such as domain entities to view models or DTOs to presentation models.
/// </summary>
public class ReportingWebAutoMapperProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the ReportingWebAutoMapperProfile.
    /// Currently intentionally left blank as no web-layer specific mappings are needed,
    /// but can be extended to include view model transformations as requirements evolve.
    /// </summary>
    public ReportingWebAutoMapperProfile()
    {
        // Intentionally left blank
    }
}

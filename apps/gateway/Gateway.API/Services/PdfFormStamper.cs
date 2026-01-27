using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// STUB: PDF form stamper that returns an empty byte array.
/// Production implementation will use iText to stamp PA data onto PDF templates.
/// </summary>
/// <remarks>
/// The iText NuGet dependency is kept for future production use.
/// </remarks>
public sealed class PdfFormStamper : IPdfFormStamper
{
    private readonly ILogger<PdfFormStamper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfFormStamper"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public PdfFormStamper(ILogger<PdfFormStamper> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<byte[]> StampFormAsync(
        PAFormData formData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "STUB: PDF stamping requested for patient {PatientName}",
            formData.PatientName);

        // STUB: Return empty array for now
        // Production will use iText to:
        // 1. Load PDF template from assets
        // 2. Stamp form fields using formData.FieldMappings
        // 3. Flatten and return the stamped PDF bytes
        return Task.FromResult(Array.Empty<byte>());
    }
}

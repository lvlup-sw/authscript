using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Stamps prior authorization form data onto PDF templates.
/// </summary>
public interface IPdfFormStamper
{
    /// <summary>
    /// Stamps the PA form data onto a PDF template and returns the completed form.
    /// </summary>
    /// <param name="formData">The prior authorization form data with field mappings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stamped PDF as a byte array.</returns>
    Task<byte[]> StampFormAsync(
        PAFormData formData,
        CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Http;

namespace Eventix.Share.Commerce;

/// <summary>
/// Request model for scan-image endpoint.
/// Wrapping IFormFile + EventId into a class is required for Swagger to
/// correctly generate the multipart/form-data schema.
/// </summary>
public class ScanImageRequest
{
    public Guid EventId { get; set; }
    public IFormFile? QrImage { get; set; }
}

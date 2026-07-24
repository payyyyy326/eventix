using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Controllers;
using Eventix.Infrastructure.QrCode;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Constants;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.CommerceModule.Controllers;

[Route("api/checkin")]
[Authorize(Roles = "Organizer,Admin")]
public class CheckInController : BaseApiController
{
    private const long MaxQrImageSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageTypes =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/bmp"
    ];

    private readonly ICommerceService _service;
    private readonly IQrCodeImageDecoder _qrCodeDecoder;

    public CheckInController(
        ICommerceService service,
        IQrCodeImageDecoder qrCodeDecoder)
    {
        _service = service;
        _qrCodeDecoder = qrCodeDecoder;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole(SystemConstants.RoleConstants.ADMIN);

    [HttpPost("scan")]
    public async Task<ActionResult<ApiResponseModel<CheckInResponse>>> Scan(
        CheckInRequest request) =>
        SuccessResponse(
            SystemSuccess.SUCCESS,
            await _service.CheckInAsync(request, UserId, IsAdmin));

    [HttpPost("scan-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxQrImageSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxQrImageSize)]
    public async Task<ActionResult<ApiResponseModel<CheckInResponse>>> ScanImage(
        [FromForm] ScanImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty)
            throw new BadRequestException("Event ID không hợp lệ.");
        if (request.QrImage == null || request.QrImage.Length == 0)
            throw new BadRequestException("Vui lòng chọn ảnh QR.");
        if (request.QrImage.Length > MaxQrImageSize)
            throw new BadRequestException("Ảnh QR không được vượt quá 5 MB.");
        if (!AllowedImageTypes.Contains(request.QrImage.ContentType.ToLowerInvariant()))
            throw new BadRequestException(
                "Chỉ hỗ trợ ảnh PNG, JPG, WEBP hoặc BMP.");

        await using var imageStream = request.QrImage.OpenReadStream();
        var qrToken = await _qrCodeDecoder.DecodeAsync(
            imageStream,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(qrToken))
            throw new BadRequestException(
                "Không đọc được mã QR trong ảnh. Hãy chọn ảnh rõ nét hơn.");

        var result = await _service.CheckInAsync(
            new CheckInRequest
            {
                EventId = request.EventId,
                QrToken = qrToken.Trim()
            },
            UserId,
            IsAdmin);

        return SuccessResponse(SystemSuccess.SUCCESS, result);
    }

    [HttpGet("event/{eventId:guid}/stats")]
    public async Task<ActionResult<ApiResponseModel<CheckInStatsResponse>>> Stats(
        Guid eventId) =>
        SuccessResponse(
            SystemSuccess.SUCCESS,
            await _service.GetCheckInStatsAsync(eventId, UserId, IsAdmin));
}
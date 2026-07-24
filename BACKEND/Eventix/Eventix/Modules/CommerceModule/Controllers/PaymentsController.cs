using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.CommerceModule.Controllers;

[Route("api/payments")]
[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly ICommerceService _service;
    public PaymentsController(ICommerceService service) => _service = service;

    [HttpPost("demo/complete")]
    public async Task<ActionResult<ApiResponseModel<PaymentResponse>>> Complete(
        DemoPaymentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return SuccessResponse(SystemSuccess.SUCCESS,
            await _service.CompleteDemoPaymentAsync(request.OrderId, userId));
    }
}

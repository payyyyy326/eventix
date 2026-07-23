using System.Security.Claims;
using Eventix.Common.Constants.SystemData;
using Eventix.Controllers;
using Eventix.Modules.CommerceModule.Interfaces;
using Eventix.Share.Commerce;
using Eventix.Share.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventix.Modules.CommerceModule.Controllers;

[Route("api/orders")]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly ICommerceService _service;
    public OrdersController(ICommerceService service) => _service = service;
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<ApiResponseModel<OrderResponse>>> Create(
        CreateOrderRequest request) =>
        SuccessResponse(SystemSuccess.SUCCESS,
            await _service.CreateOrderAsync(request.ReservationIds, UserId));

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponseModel<List<OrderResponse>>>> My() =>
        SuccessResponse(SystemSuccess.SUCCESS, await _service.GetMyOrdersAsync(UserId));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseModel<OrderResponse>>> Detail(Guid id) =>
        SuccessResponse(SystemSuccess.SUCCESS, await _service.GetOrderAsync(id, UserId));
}

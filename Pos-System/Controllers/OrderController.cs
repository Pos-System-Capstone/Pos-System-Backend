using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Menus;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Request.Products;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.User;
using Pos_System.API.Services.Implements;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Models;

namespace Pos_System.API.Controllers
{
    public class OrderController : BaseController<OrderController>
    {
        private readonly IOrderService _orderService;

        public OrderController(ILogger<OrderController> logger, IOrderService orderService) : base(logger)
        {
            _orderService = orderService;
        }

        [CustomAuthorize(RoleEnum.Staff)]
        [HttpPost(ApiEndPointConstant.Order.OrdersEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewOrder(Guid storeId, CreateNewOrderRequest createNewOrderRequest)
        {
            Guid newOrderIdResponse = await _orderService.CreateNewOrder(storeId, createNewOrderRequest);
            if (newOrderIdResponse == Guid.Empty)
            {
                _logger.LogInformation($"Create order failed");
                return BadRequest(MessageConstant.Order.CreateOrderFailedMessage);
            }

            _logger.LogInformation($"Create order successfully");
            return Ok(newOrderIdResponse);
        }

        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.User, RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Order.OrderEndPoint)]
        [ProducesResponseType(typeof(GetOrderDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderDetail(Guid storeId, Guid id)
        {
            var response = await _orderService.GetOrderDetail(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.User, RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Order.OrderEndPoints)]
        [ProducesResponseType(typeof(GetOrderDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderDetail(Guid id)
        {
            var response = await _orderService.GetOrderDetail(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.BrandAdmin, RoleEnum.BrandManager,
            RoleEnum.User)]
        [HttpPatch(ApiEndPointConstant.Order.OrderEndPoints)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateOrder(Guid id,
            UpdateOrderRequest updateOrderRequest)
        {
            var response = await _orderService.UpdateOrder(id, updateOrderRequest);
            return Ok(response);
        }
        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.User)]
        [HttpPatch(ApiEndPointConstant.Order.OrderEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateOrderPayment(Guid storeId, Guid id,
            UpdateOrderRequest updateOrderRequest)
        {
            var response = await _orderService.UpdateOrder(id, updateOrderRequest);
            return Ok(response);
        }

        [HttpPost("orders/{id}/payment")]
        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.User)]
        [ProducesResponseType(typeof(PaymentOrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePaymentOrder(Guid id, [FromBody] PaymentOrderRequest req)

        {
            var response = await _orderService.UpdatePaymentOrder(id, req);
            return Ok(response);
        }

        [HttpPost("orders/prepare")]
        [ProducesResponseType(typeof(PrepareOrderRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareOrder(
            [FromBody] PrepareOrderRequest prepareOrderRequest)
        {
            var response = await _orderService.PrepareOrder(prepareOrderRequest);
            return Ok(response);
        }

        [HttpPost("orders/create")]
        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager, RoleEnum.User)]
        [ProducesResponseType(typeof(PrepareOrderRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceOrder([FromBody] PrepareOrderRequest orderReq)
        {
            var response = await _orderService.PlaceStoreOrder(orderReq);
            return Ok(response);
        }

        [HttpPost("orders/{id}/checkout")]
        [CustomAuthorize(RoleEnum.Staff, RoleEnum.StoreManager)]
        [ProducesResponseType(typeof(PrepareOrderRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceOrder(Guid id, [FromQuery] Guid storeId,
            [FromBody] UpdateOrderRequest updateOrderRequest)
        {
            var response = await _orderService.CheckoutOrder(storeId, id, updateOrderRequest);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Order.NewUserOrderEndPoint)]
        [ProducesResponseType(typeof(NewUserOrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> FindNewUserOrder(Guid storeId)
        {
            var response = await _orderService.GetNewUserOrderInStore(storeId);
            return Ok(response);
        }
    }
}
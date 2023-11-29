using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IOrderService
    {
        public Task<Guid> CreateNewOrder(Guid storeId, CreateNewOrderRequest createNewOrderRequest);
        public Task<Guid> UpdateOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest);
        public Task<GetOrderDetailResponse> GetOrderDetail(Guid storeId, Guid orderId);

        public Task<IPaginate<ViewOrdersResponse>> GetOrdersInStore(Guid storeId, int page, int size,
            DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? orderStatus);

        public Task<List<GetPromotionResponse>> GetPromotion(Guid storeId);

        Task<PaymentOrderResponse> UpdatePaymentOrder(Guid orderId, PaymentOrderRequest req);
        Task<IPaginate<ViewOrdersResponse>> GetListOrderByUserId(Guid userId, OrderStatus status, int page, int size);
        Task<GetOrderDetailResponse> GetOrderDetailUser(Guid orderId);
        Task<PrepareOrderRequest> PrepareOrder(PrepareOrderRequest orderReq);
        Task<Guid> PlaceStoreOrder(PrepareOrderRequest createNewOrderRequest);
        Task<Guid> CheckoutOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest);
    }
}
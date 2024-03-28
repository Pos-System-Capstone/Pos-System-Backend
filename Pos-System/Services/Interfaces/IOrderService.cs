using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IOrderService
    {
        public Task<Guid> CreateNewOrder(Guid storeId, CreateNewOrderRequest createNewOrderRequest);
        public Task<Guid> UpdateOrder(Guid orderId, UpdateOrderRequest updateOrderRequest);
        public Task<GetOrderDetailResponse> GetOrderDetail(Guid orderId);

        public Task<IPaginate<ViewOrdersResponse>> GetOrdersInStore(Guid storeId, int page, int size,
            DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? orderStatus,
            PaymentTypeEnum? paymentType, string? invoiceID);

        public Task<List<GetPromotionResponse>> GetPromotion(Guid storeId);

        Task<PaymentOrderResponse> UpdatePaymentOrder(Guid orderId, PaymentOrderRequest req);
        Task<IPaginate<ViewOrdersResponse>> GetListOrderByUserId(Guid userId, OrderStatus status, int page, int size);
        Task<GetOrderDetailResponse> GetOrderDetailUser(Guid orderId);
        Task<PrepareOrderRequest> PrepareOrder(PrepareOrderRequest orderReq);
        Task<Guid> PlaceStoreOrder(PrepareOrderRequest createNewOrderRequest);
        Task<Guid> CheckoutOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest);
        public Task<NewUserOrderResponse> GetNewUserOrderInStore(Guid storeId);

        Task<bool> CreateOrderHistory(Guid orderId, OrderStatus fromStatus, OrderStatus toStatus, Guid? changeBy
        );
    }
}
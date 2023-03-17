﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;
using PaymentType = Pos_System.Domain.Models.PaymentType;

namespace Pos_System.API.Services.Implements
{
    public class OrderService : BaseService<OrderService>, IOrderService
    {
        public const double VAT_PERCENT = 0.1;
        public const double VAT_STANDARD = 1.1 * VAT_PERCENT;
        public OrderService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<Guid> CreateNewOrder(Guid storeId, CreateNewOrderRequest createNewOrderRequest)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = DateTime.Now;
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x => 
	            x.StoreId.Equals(storeId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);

            if (currentUserSession == null) throw new BadHttpRequestException(MessageConstant.Order.UserNotInSessionMessage);
            if (createNewOrderRequest.ProductsList.Count() < 1) throw new BadHttpRequestException(MessageConstant.Order.NoProductsInOrderMessage);

            string newInvoiceId = store.Code + currentTimeStamp;
            double SystemDiscountAmount = 0;
            int defaultGuest = 1;
            
            double VATAmount = createNewOrderRequest.FinalAmount / VAT_STANDARD;

            Order newOrder = new Order()
            {
                Id = Guid.NewGuid(),
                CheckInPerson = currentUser.Id,
                CheckInDate = currentTime,
                CheckOutDate = currentTime,
                InvoiceId = newInvoiceId,
                TotalAmount = createNewOrderRequest.TotalAmount,
                Discount = createNewOrderRequest.DiscountAmount,
                FinalAmount = createNewOrderRequest.FinalAmount,
                Vat = VAT_PERCENT,
                Vatamount = VATAmount,
                OrderType = createNewOrderRequest.OrderType.GetDescriptionFromEnum(),
                NumberOfGuest = defaultGuest,
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id
            };

            List<OrderDetail> orderDetails = new List<OrderDetail>();
            createNewOrderRequest.ProductsList.ForEach(product =>
            {
                double totalProductAmount = product.SellingPrice * product.Quantity;
                double finalProductAmount = totalProductAmount - SystemDiscountAmount;
                Guid masterOrderDetailId = Guid.NewGuid();
                orderDetails.Add(new OrderDetail()
                {
                    Id = masterOrderDetailId,
                    MenuProductId = product.ProductInMenuId,
                    OrderId = newOrder.Id,
                    Quantity = product.Quantity,
                    SellingPrice = product.SellingPrice,
                    TotalAmount = totalProductAmount,
                    Discount = SystemDiscountAmount,
                    FinalAmount = finalProductAmount,
                    Notes = product.Note
                });
                if (product.Extras.Count > 0)
                {
                    product.Extras.ForEach(extra =>
                    {
                        double totalProductExtraAmount = extra.SellingPrice * extra.Quantity;
                        double finalProductExtraAmount = totalProductExtraAmount - SystemDiscountAmount;
                        orderDetails.Add(new OrderDetail()
                        {
                            Id = Guid.NewGuid(),
                            MenuProductId = extra.ProductInMenuId,
                            OrderId = newOrder.Id,
                            Quantity = extra.Quantity,
                            SellingPrice = extra.SellingPrice,
                            TotalAmount = totalProductExtraAmount,
                            Discount = SystemDiscountAmount,
                            FinalAmount = finalProductExtraAmount,
                            MasterOrderDetailId = masterOrderDetailId,
                        });
                    });
                }
            });

            currentUserSession.NumberOfOrders++;

            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            _unitOfWork.GetRepository<Session>().UpdateAsync(currentUserSession);
            await _unitOfWork.CommitAsync();

            return newOrder.Id;
        }

        public async Task<GetOrderDetailResponse> GetOrderDetail(Guid storeId, Guid orderId)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            if (orderId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Order.EmptyOrderIdMessage);

            Store store = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId));
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            GetOrderDetailResponse orderDetailResponse = new GetOrderDetailResponse();
            orderDetailResponse.OrderId = order.Id;
            orderDetailResponse.InvoiceId = order.InvoiceId;
            orderDetailResponse.TotalAmount = order.TotalAmount;
            orderDetailResponse.FinalAmount = order.FinalAmount;
            orderDetailResponse.Vat = order.Vat;
            orderDetailResponse.VatAmount = order.Vatamount;
            orderDetailResponse.Discount = order.Discount;
            orderDetailResponse.OrderStatus = EnumUtil.ParseEnum<OrderStatus>(order.Status);
            orderDetailResponse.OrderType = EnumUtil.ParseEnum<OrderType>(order.OrderType);
            orderDetailResponse.CheckInDate = order.CheckInDate;

            orderDetailResponse.ProductList = (List<OrderProductDetailResponse>)await _unitOfWork.GetRepository<OrderDetail>().GetListAsync(
                selector: x => new OrderProductDetailResponse()
                {
                    ProductInMenuId = x.MenuProductId,
                    OrderDetailId = x.Id,
                    SellingPrice = x.SellingPrice,
                    Quantity = x.Quantity,
                    Name = x.MenuProduct.Product.Name,
                    TotalAmount = x.TotalAmount,
                    FinalAmount = x.FinalAmount,
                    Discount = x.Discount,
                    Note = x.Notes,
                },
                predicate: x => x.OrderId.Equals(orderId) && x.MasterOrderDetailId == null,
                include: x => x.Include(x => x.MenuProduct).ThenInclude(menuProduct => menuProduct.Product));

            if (orderDetailResponse.ProductList.Count > 0)
            {
                foreach (OrderProductDetailResponse masterProduct in orderDetailResponse.ProductList)
                {
                    masterProduct.Extras = (List<OrderProductExtraDetailResponse>)await _unitOfWork.GetRepository<OrderDetail>().GetListAsync(selector: extra => new OrderProductExtraDetailResponse()
                    {
                        ProductInMenuId = extra.MenuProductId,
                        SellingPrice = extra.SellingPrice,
                        Quantity = extra.Quantity,
                        TotalAmount = extra.TotalAmount,
                        FinalAmount = extra.FinalAmount,
                        Discount = extra.Discount,
                        Name = extra.MenuProduct.Product.Name,
                    },
                    predicate: extra => extra.OrderId.Equals(orderId) && extra.MasterOrderDetailId != null && extra.MasterOrderDetailId.Equals(masterProduct.OrderDetailId),
                    include: x => x.Include(x => x.MenuProduct).ThenInclude(menuProduct => menuProduct.Product));
                }
            }

            orderDetailResponse.Payment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(
                selector: payment => new OrderPaymentResponse()
                {
                    Id = payment.Id,
                    PaymentTypeId = payment.PaymentTypeId,
                    PaymentType = EnumUtil.ParseEnum<OrderPaymentType>(payment.PaymentType.Name),
                    PicUrl = payment.PaymentType.PicUrl,
                    PaidAmount = payment.Amount
                },
                predicate: payment => payment.OrderId.Equals(orderId),
                include: payment => payment.Include(payment => payment.PaymentType)
                );

            return orderDetailResponse;
        }

        public async Task<Guid> UpdateOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = DateTime.Now;
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
                x.StoreId.Equals(storeId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);

            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId));

            if (currentUserSession == null) throw new BadHttpRequestException(MessageConstant.Order.UserNotInSessionMessage);
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            order.CheckOutDate = currentTime;
            order.Status = updateOrderRequest.Status.GetDescriptionFromEnum();

            if (updateOrderRequest.Status.Equals(OrderStatus.CANCELED))
            {
                currentUserSession.NumberOfOrders--;
            }

            if(updateOrderRequest.Status.Equals(OrderStatus.PAID) && updateOrderRequest.Payment != null)
            {
                PaymentType paymentType = await _unitOfWork.GetRepository<PaymentType>().SingleOrDefaultAsync(predicate: x =>
                x.Name.Equals(updateOrderRequest.Payment.GetDescriptionFromEnum()));

                if (paymentType == null) throw new BadHttpRequestException("Payment not found!");

                Payment currentPayment = await _unitOfWork.GetRepository<Payment>().SingleOrDefaultAsync(predicate: x => x.OrderId.Equals(orderId));

                if (currentPayment == null)
                {

                    Payment newPaymentRequest = new Payment()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Amount = order.FinalAmount,
                        PaymentTypeId = paymentType.Id
                    };
                    currentUserSession.TotalAmount += order.TotalAmount;
                    currentUserSession.TotalFinalAmount += order.FinalAmount;
                    currentUserSession.TotalChangeCash -= order.FinalAmount;
                    currentUserSession.TotalDiscountAmount += order.Discount;

                    await _unitOfWork.GetRepository<Payment>().InsertAsync(newPaymentRequest);
                }
                else
                {
                    currentPayment.Amount = order.FinalAmount;
                    currentPayment.PaymentTypeId = paymentType.Id;
                    _unitOfWork.GetRepository<Payment>().UpdateAsync(currentPayment);
                }

            }

            _unitOfWork.GetRepository<Session>().UpdateAsync(currentUserSession);
            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();

            return order.Id;
        }
    }
}

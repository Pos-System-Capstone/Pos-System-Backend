using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Extensions;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.CheckoutOrder;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Response.CheckoutOrderResponse;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class OrderService : BaseService<OrderService>, IOrderService
    {
        private const double VatPercent = 0.1;
        private const double VatStandard = 1.1;
        private readonly IUserService _userService;

        public OrderService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, IUserService userService) : base(unitOfWork, logger, mapper,
            httpContextAccessor)
        {
            _userService = userService;
        }

        public async Task<Guid> CreateNewOrder(Guid storeId, CreateNewOrderRequest createNewOrderRequest)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
                x.StoreId.Equals(storeId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);

            if (currentUserSession == null)
                throw new BadHttpRequestException(MessageConstant.Order.UserNotInSessionMessage);
            if (createNewOrderRequest.ProductsList.Count() < 1)
                throw new BadHttpRequestException(MessageConstant.Order.NoProductsInOrderMessage);

            string newInvoiceId = store.Code + currentTimeStamp;
            int defaultGuest = 1;

            double vatAmount = (createNewOrderRequest.FinalAmount / VatStandard) * VatPercent;

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
                Vat = VatPercent,
                Vatamount = vatAmount,
                OrderType = createNewOrderRequest.OrderType.GetDescriptionFromEnum(),
                NumberOfGuest = defaultGuest,
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = PaymentTypeEnum.CASH.GetDescriptionFromEnum()
            };


            List<OrderDetail> orderDetails = new List<OrderDetail>();
            createNewOrderRequest.ProductsList.ForEach(product =>
            {
                double totalProductAmount = product.SellingPrice * product.Quantity;
                double finalProductAmount = totalProductAmount - product.Discount;
                Guid masterOrderDetailId = Guid.NewGuid();
                orderDetails.Add(new OrderDetail()
                {
                    Id = masterOrderDetailId,
                    MenuProductId = product.ProductInMenuId,
                    OrderId = newOrder.Id,
                    Quantity = product.Quantity,
                    SellingPrice = product.SellingPrice,
                    TotalAmount = totalProductAmount,
                    Discount = product.Discount,
                    FinalAmount = finalProductAmount,
                    Notes = product.Note
                });
                if (product.Extras.Count > 0)
                {
                    product.Extras.ForEach(extra =>
                    {
                        double totalProductExtraAmount = extra.SellingPrice * extra.Quantity;
                        double finalProductExtraAmount = totalProductExtraAmount - extra.Discount;
                        orderDetails.Add(new OrderDetail()
                        {
                            Id = Guid.NewGuid(),
                            MenuProductId = extra.ProductInMenuId,
                            OrderId = newOrder.Id,
                            Quantity = extra.Quantity,
                            SellingPrice = extra.SellingPrice,
                            TotalAmount = totalProductExtraAmount,
                            Discount = extra.Discount,
                            FinalAmount = finalProductExtraAmount,
                            MasterOrderDetailId = masterOrderDetailId,
                        });
                    });
                }
            });
            if (createNewOrderRequest.PromotionList.Any())
            {
                List<PromotionOrderMapping> promotionMappingList = new List<PromotionOrderMapping>();
                createNewOrderRequest.PromotionList.ForEach(orderPromotion =>
                {
                    promotionMappingList.Add(new PromotionOrderMapping()
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = orderPromotion.PromotionId,
                        OrderId = newOrder.Id,
                        Quantity = orderPromotion.Quantity,
                        DiscountAmount = orderPromotion.DiscountAmount
                    });
                });
                await _unitOfWork.GetRepository<PromotionOrderMapping>().InsertRangeAsync(promotionMappingList);
            }

            currentUserSession.NumberOfOrders++;
            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            _unitOfWork.GetRepository<Session>().UpdateAsync(currentUserSession);


            await _unitOfWork.CommitAsync();

            return newOrder.Id;
        }

        public async Task<GetOrderDetailResponse> GetOrderDetail(Guid orderId)
        {
            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(orderId),
                include: x =>
                    x.Include(p => p.PromotionOrderMappings).ThenInclude(a => a.Promotion).Include(o => o.OrderSource)
                        .Include(o => o.Session).ThenInclude(s => s.Store)
            );
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            GetOrderDetailResponse orderDetailResponse = new GetOrderDetailResponse();
            orderDetailResponse.OrderId = order.Id;
            orderDetailResponse.InvoiceId = order.InvoiceId;
            orderDetailResponse.TotalAmount = order.TotalAmount;
            orderDetailResponse.FinalAmount = order.FinalAmount;
            orderDetailResponse.StoreName = order.Session.Store.Name;
            orderDetailResponse.Vat = order.Vat;
            orderDetailResponse.VatAmount = order.Vatamount;
            orderDetailResponse.Discount = order.Discount;
            orderDetailResponse.CustomerNumber = order.NumberOfGuest;
            orderDetailResponse.Notes = order.Note;
            orderDetailResponse.OrderStatus = EnumUtil.ParseEnum<OrderStatus>(order.Status);
            orderDetailResponse.OrderType = EnumUtil.ParseEnum<OrderType>(order.OrderType);
            orderDetailResponse.PaymentType = string.IsNullOrEmpty(order.PaymentType)
                ? PaymentTypeEnum.CASH
                : EnumUtil.ParseEnum<PaymentTypeEnum>(order.PaymentType);
            orderDetailResponse.CheckInDate = order.CheckInDate;

            if (order.PromotionOrderMappings.Any())
            {
                orderDetailResponse.PromotionList = (List<OrderPromotionResponse>) await _unitOfWork
                    .GetRepository<PromotionOrderMapping>().GetListAsync(
                        selector: x => new OrderPromotionResponse()
                        {
                            PromotionId = x.PromotionId,
                            PromotionName = x.Promotion.Name,
                            DiscountAmount = x.DiscountAmount ?? 0,
                            Quantity = x.Quantity ?? 1,
                            EffectType = x.EffectType
                        },
                        predicate: x => x.OrderId.Equals(orderId),
                        include: x => x.Include(x => x.Promotion));
            }

            if (order.OrderSource != null)
            {
                if (order.OrderSource.UserId == null)
                {
                    orderDetailResponse.CustomerInfo = new OrderUserResponse()
                    {
                        Name = order.OrderSource.Name,
                        Phone = order.OrderSource.Phone,
                        Address = order.OrderSource.Address,
                        CustomerType = order.OrderSource.UserType,
                        DeliTime = order.OrderSource.Note,
                        PaymentStatus =
                            EnumUtil.ParseEnum<PaymentStatusEnum>(order.OrderSource.PaymentStatus ?? "PENDING"),
                        DeliStatus = EnumUtil.ParseEnum<OrderSourceStatus>(order.OrderSource.Status)
                    };
                }
                else
                {
                    orderDetailResponse.CustomerInfo = await _unitOfWork
                        .GetRepository<User>().SingleOrDefaultAsync(
                            selector: x => new OrderUserResponse()
                            {
                                Id = x.Id,
                                Name = x.FullName,
                                Phone = x.PhoneNumber,
                                Address = order.OrderSource.Address,
                                CustomerType = order.OrderSource.UserType,
                                DeliTime = order.OrderSource.Note,
                                PaymentStatus =
                                    EnumUtil.ParseEnum<PaymentStatusEnum>(order.OrderSource.PaymentStatus ?? "PENDING"),
                                DeliStatus = EnumUtil.ParseEnum<OrderSourceStatus>(order.OrderSource.Status)
                            },
                            predicate: x => x.Id.Equals(order.OrderSource.UserId)
                        );
                }
            }

            orderDetailResponse.ProductList = (List<OrderProductDetailResponse>) await _unitOfWork
                .GetRepository<OrderDetail>().GetListAsync(
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
                    masterProduct.Extras = (List<OrderProductExtraDetailResponse>) await _unitOfWork
                        .GetRepository<OrderDetail>().GetListAsync(selector: extra =>
                                new OrderProductExtraDetailResponse()
                                {
                                    ProductInMenuId = extra.MenuProductId,
                                    SellingPrice = extra.SellingPrice,
                                    Quantity = extra.Quantity,
                                    TotalAmount = extra.TotalAmount,
                                    FinalAmount = extra.FinalAmount,
                                    Discount = extra.Discount,
                                    Name = extra.MenuProduct.Product.Name,
                                },
                            predicate: extra =>
                                extra.OrderId.Equals(orderId) && extra.MasterOrderDetailId != null &&
                                extra.MasterOrderDetailId.Equals(masterProduct.OrderDetailId),
                            include: x =>
                                x.Include(x => x.MenuProduct).ThenInclude(menuProduct => menuProduct.Product));
                }
            }

            return orderDetailResponse;
        }

        public async Task<IPaginate<ViewOrdersResponse>> GetOrdersInStore(Guid storeId, int page, int size,
            DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? status,
            PaymentTypeEnum? paymentType)
        {
            RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Guid currentUserStoreId = Guid.Parse(GetStoreIdFromJwt());
            if (currentUserStoreId != storeId)
                throw new BadHttpRequestException(MessageConstant.Store.GetStoreOrdersUnAuthorized);
            IPaginate<ViewOrdersResponse> ordersResponse = await _unitOfWork.GetRepository<Order>().GetPagingListAsync(
                selector: x => new ViewOrdersResponse
                {
                    Id = x.Id,
                    InvoiceId = x.InvoiceId,
                    StaffName = x.CheckInPersonNavigation.Name,
                    StartDate = x.CheckInDate,
                    EndDate = x.CheckOutDate,
                    FinalAmount = x.FinalAmount,
                    OrderDate = x.CheckOutDate.Date,
                    OrderType = EnumUtil.ParseEnum<OrderType>(x.OrderType),
                    Status = EnumUtil.ParseEnum<OrderStatus>(x.Status),
                    PaymentType = string.IsNullOrEmpty(x.PaymentType)
                        ? PaymentTypeEnum.CASH
                        : EnumUtil.ParseEnum<PaymentTypeEnum>(x.PaymentType),
                    CustomerName = x.OrderSource != null ? x.OrderSource.Name : null,
                    Phone = x.OrderSource != null ? x.OrderSource.Phone : null,
                    Address = x.OrderSource != null ? x.OrderSource.Address : null,
                    StoreName = x.Session.Store.Name,
                    PaymentStatus = x.OrderSource != null && x.OrderSource.PaymentStatus != null
                        ? EnumUtil.ParseEnum<PaymentStatusEnum>(x.OrderSource.PaymentStatus)
                        : null,
                },
                predicate: BuildGetOrdersInStoreQuery(storeId, startDate, endDate, orderType, status, paymentType),
                include: x => x.Include(s => s.OrderSource).Include(v => v.Session).ThenInclude(p => p.Store),
                orderBy: x =>
                    x.OrderByDescending(x => x.CheckInDate),
                page: page,
                size: size
            );
            return ordersResponse;
        }

        private Expression<Func<Order, bool>> BuildGetOrdersInStoreQuery(Guid storeId, DateTime? startDate,
            DateTime? endDate, OrderType? orderType, OrderStatus? status, PaymentTypeEnum? paymentType)
        {
            Expression<Func<Order, bool>> filterQuery = p => p.Session.StoreId.Equals(storeId);
            if (startDate != null && endDate == null)
            {
                filterQuery = filterQuery.AndAlso(p =>
                    p.CheckInDate >= startDate && p.CheckInDate <= startDate.Value.AddDays(1));
            }
            else if (startDate != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.CheckInDate >= startDate);
            }

            if (startDate != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.CheckInDate <= startDate.Value.AddDays(1));
            }

            if (orderType != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.OrderType.Equals(orderType.GetDescriptionFromEnum()));
            }

            if (status != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.Status.Equals(status.GetDescriptionFromEnum()));
            }

            if (paymentType != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.PaymentType.Equals(paymentType.GetDescriptionFromEnum()));
            }

            return filterQuery;
        }

        public async Task<Guid> UpdateOrder(Guid orderId, UpdateOrderRequest updateOrderRequest)
        {
            string currentUserName = GetUsernameFromJwt();
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            Order order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId),
                    include: x => x.Include(p => p.PromotionOrderMappings));
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            order.CheckInPerson = currentUser.Id;
            order.CheckOutDate = currentTime;
            order.PaymentType = updateOrderRequest.PaymentType != null
                ? updateOrderRequest.PaymentType.GetDescriptionFromEnum()
                : order.PaymentType;
            order.Status = updateOrderRequest.Status != null
                ? updateOrderRequest.Status.GetDescriptionFromEnum()
                : order.Status;
            if (updateOrderRequest.DeliStatus != null)
            {
                if (order.OrderSourceId != null)
                {
                    OrderUser orderUser = await _unitOfWork.GetRepository<OrderUser>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId)
                        );
                    orderUser.Status = updateOrderRequest.DeliStatus.GetDescriptionFromEnum();
                    _unitOfWork.GetRepository<OrderUser>().UpdateAsync(orderUser);
                }
            }

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();
            return order.Id;
        }

        public async Task<Guid> CheckoutOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            string currentUserName = GetUsernameFromJwt();
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            Order order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId),
                    include: x => x.Include(p => p.PromotionOrderMappings));
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            order.PaymentType = updateOrderRequest.PaymentType.GetDescriptionFromEnum();
            order.Status = updateOrderRequest.Status.GetDescriptionFromEnum();
            if (updateOrderRequest.GuestNumber.HasValue)
            {
                order.NumberOfGuest = updateOrderRequest.GuestNumber;
            }

            if (order.PromotionOrderMappings.Any() && updateOrderRequest.Status.Equals(OrderStatus.PAID))
            {
                List<Transaction> transactions = new List<Transaction>();
                CheckOutPointifyRequest checkOutPointify = new CheckOutPointifyRequest()
                {
                    StoreCode = store.Code,
                    ListEffect = new List<ListEffect>(),
                    Discount = order.Discount,
                    FinalAmount = order.FinalAmount
                };
                if (order.OrderSourceId != null)
                {
                    OrderUser orderUser = await _unitOfWork.GetRepository<OrderUser>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId));
                    if (orderUser.UserId != null && orderUser.UserType == "USER")
                    {
                        checkOutPointify.UserId = orderUser.UserId;
                    }

                    orderUser.Status = OrderSourceStatus.DELIVERING.GetDescriptionFromEnum();
                    _unitOfWork.GetRepository<OrderUser>().UpdateAsync(orderUser);
                }

                foreach (var promotionOrder in order.PromotionOrderMappings)
                {
                    checkOutPointify.ListEffect.Add(new ListEffect()
                    {
                        PromotionId = promotionOrder.PromotionId,
                        EffectType = promotionOrder.EffectType,
                        Amount = promotionOrder.DiscountAmount ?? 0
                    });
                    if (promotionOrder.EffectType is "GET_POINT")
                    {
                        Transaction newTransaction = new Transaction()
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = currentTime,
                            Amount = (decimal) (promotionOrder.DiscountAmount ?? 0),
                            BrandId = store.BrandId,
                            Currency = "Point",
                            IsIncrease = true,
                            OrderId = orderId,
                            UserId = checkOutPointify.UserId, Status = "SUCCESS",
                            Type = TransactionTypeEnum.GET_POINT.GetDescriptionFromEnum()
                        };
                        transactions.Add(newTransaction);
                        checkOutPointify.BonusPoint = promotionOrder.DiscountAmount ?? 0;
                    }

                    if (promotionOrder.VoucherCode != null)
                    {
                        checkOutPointify.VoucherCode = promotionOrder.VoucherCode;
                    }
                }

                checkOutPointify.InvoiceId = order.InvoiceId;

                string url = "https://api-pointify.reso.vn/api/promotions/check-out-promotion";
                var response = await CallApiUtils.CallApiEndpoint(url, checkOutPointify);
                // if (!response.StatusCode.Equals(HttpStatusCode.OK))
                // {
                //     throw new BadHttpRequestException("Cập nhật khuyến mãi đơn hàng thất bại");
                // }

                await _unitOfWork.GetRepository<Transaction>().InsertRangeAsync(transactions);
            }


            order.CheckInPerson = currentUser.Id;
            order.CheckOutDate = currentTime;

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();
            return order.Id;
        }

        public async Task<List<GetPromotionResponse>> GetPromotion(Guid storeId)
        {
            RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Guid currentUserStoreId = Guid.Parse(GetStoreIdFromJwt());
            if (currentUserStoreId != storeId)
                throw new BadHttpRequestException(MessageConstant.Store.GetStoreOrdersUnAuthorized);
            Guid userBrandId = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(currentUserStoreId));

            DateTime currentSEATime = TimeUtils.GetCurrentSEATime();
            DateFilter currentDay = DateTimeHelper.GetDateFromDateTime(currentSEATime);
            TimeOnly currentTime = TimeOnly.FromDateTime(currentSEATime);


            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            List<GetPromotionResponse> responese = (List<GetPromotionResponse>) await _unitOfWork
                .GetRepository<Promotion>().GetListAsync(
                    selector: x => new GetPromotionResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        Description = x.Description,
                        Type = EnumUtil.ParseEnum<PromotionEnum>(x.Type),
                        MaxDiscount = x.MaxDiscount,
                        MinConditionAmount = x.MinConditionAmount,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        StartTime = DateTimeHelper.ConvertIntToTimeOnly(x.StartTime ?? 0),
                        EndTime = DateTimeHelper.ConvertIntToTimeOnly(x.EndTime ?? 1439),
                        DateFilters = DateTimeHelper.GetDatesFromDateFilter(x.DayFilter ?? 127),
                        IsAvailable = false,
                        ListProductApply = x.PromotionProductMappings.Select(x => new ProductApply(x.ProductId))
                            .ToList(),
                        Status = x.Status
                    },
                    include: x => x.Include(product => product.PromotionProductMappings),
                    predicate:
                    x => x.BrandId.Equals(userBrandId) &&
                         x.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum()),
                    orderBy: x => x.OrderBy(x => x.Name)
                );
            foreach (var promotionResponse in responese)
            {
                //Find the menu available days and time
                if (promotionResponse.DateFilters.Contains(currentDay) && currentTime <= promotionResponse.EndTime &&
                    currentTime >= promotionResponse.StartTime)
                    promotionResponse.IsAvailable = true;
            }


            return responese;
        }

        public async Task<PaymentOrderResponse> UpdatePaymentOrder(Guid orderId, PaymentOrderRequest req)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId),
                    include: s => s.Include(x => x.Session).ThenInclude(s => s.Store));
            var brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(order.Session.Store.BrandId));
            if (brand == null)
            {
                throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            }

            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            if (order.Status.Equals(OrderStatus.PAID.GetDescriptionFromEnum()))
            {
                throw new BadHttpRequestException(MessageConstant.Order.OrderCompleteBefore);
            }

            if (order.OrderSourceId != null)
            {
                var orderUser = await _unitOfWork.GetRepository<OrderUser>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId)
                    );
                if (orderUser.PaymentStatus != null &&
                    orderUser.PaymentStatus.Equals(PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum()))
                {
                    PaymentOrderResponse response = new PaymentOrderResponse()
                    {
                        OrderId = order.Id,
                        PaymentType = req.PaymentType,
                        Status = PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum()
                    };
                    return response;
                }
            }


            var paymentOrderResponse = new PaymentOrderResponse()
            {
                OrderId = order.Id,
                PaymentType = req.PaymentType,
                Status = PaymentStatusEnum.FAIL.GetDescriptionFromEnum()
            };
            switch (EnumUtil.ParseEnum<PaymentTypeEnum>(req.PaymentType))
            {
                case PaymentTypeEnum.POINTIFY:
                {
                    var user = await _userService.ScanUser(req.Code);
                    if (user.BrandId == order.Session.Store.BrandId)
                    {
                        MemberActionRequest request = new MemberActionRequest()
                        {
                            ApiKey = order.Session.Store.BrandId,
                            Amount = order.FinalAmount,
                            Description = order.InvoiceId,
                            MembershipId = user.Id,
                            MemberActionType = MemberActionType.PAYMENT.GetDescriptionFromEnum()
                        };
                        var url = "https://api-pointify.reso.vn/api/member-action";
                        var response = await CallApiUtils.CallApiEndpoint(url, request);
                        if (!response.StatusCode.Equals(HttpStatusCode.OK))
                        {
                            paymentOrderResponse.Message = "Thanh toán thất bại";
                            return paymentOrderResponse;
                        }

                        var actionResponse =
                            JsonConvert.DeserializeObject<MemberActionResponse>(response.Content
                                .ReadAsStringAsync().Result);
                        if (actionResponse != null &&
                            actionResponse.Status.Equals(MemberActionStatus.SUCCESS.GetDescriptionFromEnum()))
                        {
                            brand.BrandBalance += order.FinalAmount;
                            var listTrans = new List<Transaction>
                            {
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = order.Session.Store.BrandId,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    UserId = user.Id,
                                    OrderId = order.Id,
                                    IsIncrease = false,
                                    Type = TransactionTypeEnum.PAYMENT.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Thanh toán đơn hàng " + order.InvoiceId
                                },
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = order.Session.Store.BrandId,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    OrderId = order.Id,
                                    IsIncrease = true,
                                    Type = TransactionTypeEnum.BRAND_BALANCE.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Cộng tiền vào tài khoản danh thu " + brand.Name
                                }
                            };

                            if (order.OrderSourceId != null)
                            {
                                var orderUser = await _unitOfWork.GetRepository<OrderUser>()
                                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId)
                                    );
                                orderUser.PaymentStatus = PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum();
                                _unitOfWork.GetRepository<OrderUser>().UpdateAsync(orderUser);
                            }

                            await _unitOfWork.GetRepository<Transaction>()
                                .InsertRangeAsync(listTrans);
                            order.PaymentType = req.PaymentType.GetDescriptionFromEnum();
                            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                            _unitOfWork.GetRepository<Brand>().UpdateAsync(brand);
                            paymentOrderResponse.Message = "Thanh toán đơn hàng" + actionResponse.Description;
                            paymentOrderResponse.Status = PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum();
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            paymentOrderResponse.Message = actionResponse?.Description;
                        }
                    }
                    else
                    {
                        var brandPartner = await _unitOfWork.GetRepository<BrandPartner>()
                            .SingleOrDefaultAsync(predicate: x =>
                                x.MasterBrandId.Equals(order.Session.Store.BrandId) &&
                                x.BrandPartnerId.Equals(user.BrandId)
                            );
                        if (brandPartner == null)
                        {
                            throw new BadHttpRequestException(MessageConstant.Brand.BrandPartnerNotFound);
                        }

                        var partner = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                            predicate: x => x.Id.Equals(brandPartner.BrandPartnerId));
                        if (partner == null)
                        {
                            throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
                        }

                        MemberActionRequest request = new MemberActionRequest()
                        {
                            ApiKey = partner.Id,
                            Amount = order.FinalAmount,
                            Description = order.InvoiceId,
                            MembershipId = user.Id,
                            MemberActionType = MemberActionType.PAYMENT.GetDescriptionFromEnum()
                        };
                        var url = "https://api-pointify.reso.vn/api/member-action";
                        var response = await CallApiUtils.CallApiEndpoint(url, request);
                        if (!response.StatusCode.Equals(HttpStatusCode.OK))
                        {
                            paymentOrderResponse.Message = "Thanh toán thất bại";
                            return paymentOrderResponse;
                        }

                        var actionResponse =
                            JsonConvert.DeserializeObject<MemberActionResponse>(response.Content
                                .ReadAsStringAsync().Result);
                        if (actionResponse != null &&
                            actionResponse.Status.Equals(MemberActionStatus.SUCCESS.GetDescriptionFromEnum()))
                        {
                            partner.BrandBalance += order.FinalAmount;
                            brand.BrandBalance += order.FinalAmount;
                            brandPartner.DebtBalance -= order.FinalAmount;

                            var listTrans = new List<Transaction>
                            {
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = partner.Id,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    UserId = user.Id,
                                    OrderId = order.Id,
                                    IsIncrease = false,
                                    Type = TransactionTypeEnum.PAYMENT.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Thanh toán đơn hàng " + order.InvoiceId
                                },
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = partner.Id,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    OrderId = order.Id,
                                    IsIncrease = true,
                                    Type = TransactionTypeEnum.BRAND_BALANCE.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Cộng tiền vào tài khoản danh thu " + partner.Name
                                },
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = brand.Id,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    BrandPartnerId = partner.Id,
                                    OrderId = order.Id,
                                    IsIncrease = false,
                                    Type = TransactionTypeEnum.DEPT.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Trừ tiền ví dư nợ " + partner.Name
                                },
                                new()
                                {
                                    Id = Guid.NewGuid(),
                                    BrandId = brand.Id,
                                    TransactionJson = response.Content
                                        .ReadAsStringAsync().Result,
                                    Amount = (decimal) order.FinalAmount,
                                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                                    OrderId = order.Id,
                                    IsIncrease = true,
                                    Type = TransactionTypeEnum.BRAND_BALANCE.GetDescriptionFromEnum(),
                                    Currency = "đ",
                                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                                    Description = "Cộng tiền vào tài khoản danh thu " + brand.Name
                                },
                            };

                            await _unitOfWork.GetRepository<Transaction>().InsertRangeAsync(listTrans);
                            order.PaymentType = req.PaymentType.GetDescriptionFromEnum();
                            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                            paymentOrderResponse.Message = "Thanh toán đơn hàng " + actionResponse.Description;
                            paymentOrderResponse.Status = PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum();
                            brandPartner.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            _unitOfWork.GetRepository<BrandPartner>().UpdateAsync(brandPartner);
                            _unitOfWork.GetRepository<Brand>().UpdateAsync(brand);
                            _unitOfWork.GetRepository<Brand>().UpdateAsync(partner);
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            paymentOrderResponse.Message = actionResponse?.Description;
                        }
                    }
                }
                    break;
                case PaymentTypeEnum.CASH:
                    break;
                case PaymentTypeEnum.MOMO:
                    break;
                case PaymentTypeEnum.BANKING:
                    break;
                case PaymentTypeEnum.VISA:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return paymentOrderResponse;
        }

        public async Task<IPaginate<ViewOrdersResponse>> GetListOrderByUserId(Guid userId, OrderStatus status, int page,
            int size)
        {
            //lấy ra danh sách order của user
            IPaginate<ViewOrdersResponse> orders = await _unitOfWork.GetRepository<Order>().GetPagingListAsync(
                selector: x => new ViewOrdersResponse
                {
                    Id = x.Id,
                    InvoiceId = x.InvoiceId,
                    StaffName = x.CheckInPersonNavigation.Name,
                    StartDate = x.CheckInDate,
                    EndDate = x.CheckOutDate,
                    FinalAmount = x.FinalAmount,
                    OrderType = EnumUtil.ParseEnum<OrderType>(x.OrderType),
                    Status = EnumUtil.ParseEnum<OrderStatus>(x.Status),
                    PaymentType = string.IsNullOrEmpty(x.PaymentType)
                        ? PaymentTypeEnum.CASH
                        : EnumUtil.ParseEnum<PaymentTypeEnum>(x.PaymentType),
                    CustomerName = x.OrderSource != null ? x.OrderSource.Name : null,
                    Phone = x.OrderSource != null ? x.OrderSource.Phone : null,
                    Address = x.OrderSource != null ? x.OrderSource.Address : null,
                    StoreName = x.Session.Store.Name,
                    StorePic = x.Session.Store.Brand.PicUrl,
                    PaymentStatus = x.OrderSource != null && x.OrderSource.PaymentStatus != null
                        ? EnumUtil.ParseEnum<PaymentStatusEnum>(x.OrderSource.PaymentStatus)
                        : null,
                },
                include: x => x.Include(s => s.OrderSource).Include(v => v.Session).ThenInclude(p => p.Store),
                predicate:
                x =>
                    x.OrderSource.UserId.Equals(userId),
                orderBy:
                x => x.OrderByDescending(x => x.CheckInDate),
                page:
                page,
                size:
                size
            );
            return orders;
        }

        public async Task<GetOrderDetailResponse> GetOrderDetailUser(Guid orderId)
        {
            if (orderId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Order.EmptyOrderIdMessage);

            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(orderId),
                include: x =>
                    x.Include(p => p.PromotionOrderMappings).ThenInclude(a => a.Promotion)
                        .Include(o => o.OrderSource).Include(o => o.Session).ThenInclude(s => s.Store)
            );
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);

            GetOrderDetailResponse orderDetailResponse = new GetOrderDetailResponse();
            orderDetailResponse.OrderId = order.Id;
            orderDetailResponse.InvoiceId = order.InvoiceId;
            orderDetailResponse.TotalAmount = order.TotalAmount;
            orderDetailResponse.StoreName = order.Session.Store.Name;
            orderDetailResponse.FinalAmount = order.FinalAmount;
            orderDetailResponse.Vat = order.Vat;
            orderDetailResponse.VatAmount = order.Vatamount;
            orderDetailResponse.Discount = order.Discount;
            orderDetailResponse.Notes = order.Note;
            orderDetailResponse.OrderStatus = EnumUtil.ParseEnum<OrderStatus>(order.Status);
            orderDetailResponse.OrderType = EnumUtil.ParseEnum<OrderType>(order.OrderType);
            orderDetailResponse.PaymentType = string.IsNullOrEmpty(order.PaymentType)
                ? PaymentTypeEnum.CASH
                : EnumUtil.ParseEnum<PaymentTypeEnum>(order.PaymentType);
            orderDetailResponse.CheckInDate = order.CheckInDate;

            if (order.PromotionOrderMappings.Any())
            {
                orderDetailResponse.PromotionList = (List<OrderPromotionResponse>) await _unitOfWork
                    .GetRepository<PromotionOrderMapping>().GetListAsync(
                        selector: x => new OrderPromotionResponse()
                        {
                            PromotionId = x.PromotionId,
                            PromotionName = x.Promotion.Name,
                            DiscountAmount = x.DiscountAmount ?? 0,
                            Quantity = x.Quantity ?? 1,
                            EffectType = x.EffectType
                        },
                        predicate: x => x.OrderId.Equals(orderId),
                        include: x => x.Include(x => x.Promotion));
            }

            if (order.OrderSource != null)
            {
                if (order.OrderSource.UserId == null)
                {
                    orderDetailResponse.CustomerInfo = new OrderUserResponse()
                    {
                        Name = order.OrderSource.Name,
                        Phone = order.OrderSource.Phone,
                        Address = order.OrderSource.Address,
                        CustomerType = order.OrderSource.UserType,
                        DeliTime = order.OrderSource.Note,
                        PaymentStatus =
                            EnumUtil.ParseEnum<PaymentStatusEnum>(order.OrderSource.PaymentStatus ?? "PENDING"),
                        DeliStatus = EnumUtil.ParseEnum<OrderSourceStatus>(order.OrderSource.Status)
                    };
                }
                else
                {
                    orderDetailResponse.CustomerInfo = await _unitOfWork
                        .GetRepository<User>().SingleOrDefaultAsync(
                            selector: x => new OrderUserResponse()
                            {
                                Id = x.Id,
                                Name = x.FullName,
                                Phone = x.PhoneNumber,
                                Address = order.OrderSource.Address,
                                DeliTime = order.OrderSource.Note,
                                CustomerType = order.OrderSource.UserType,
                                DeliStatus = EnumUtil.ParseEnum<OrderSourceStatus>(order.OrderSource.Status),
                                PaymentStatus =
                                    EnumUtil.ParseEnum<PaymentStatusEnum>(order.OrderSource.PaymentStatus ?? "PENDING"),
                            },
                            predicate: x => x.Id.Equals(order.OrderSource.UserId)
                        );
                }
            }

            orderDetailResponse.ProductList = (List<OrderProductDetailResponse>) await _unitOfWork
                .GetRepository<OrderDetail>().GetListAsync(
                    selector:
                    x => new OrderProductDetailResponse()
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
                        PicUrl = x.MenuProduct.Product.PicUrl
                    },
                    predicate:
                    x => x.OrderId.Equals(orderId) && x.MasterOrderDetailId == null,
                    include:
                    x => x.Include(x => x.MenuProduct).ThenInclude(menuProduct => menuProduct.Product));

            if (orderDetailResponse.ProductList.Count > 0)
            {
                foreach (OrderProductDetailResponse masterProduct in orderDetailResponse.ProductList)
                {
                    masterProduct.Extras = (List<OrderProductExtraDetailResponse>) await _unitOfWork
                        .GetRepository<OrderDetail>().GetListAsync(selector: extra =>
                                new OrderProductExtraDetailResponse()
                                {
                                    ProductInMenuId = extra.MenuProductId,
                                    SellingPrice = extra.SellingPrice,
                                    Quantity = extra.Quantity,
                                    TotalAmount = extra.TotalAmount,
                                    FinalAmount = extra.FinalAmount,
                                    Discount = extra.Discount,
                                    Name = extra.MenuProduct.Product.Name,
                                },
                            predicate: extra =>
                                extra.OrderId.Equals(orderId) && extra.MasterOrderDetailId != null &&
                                extra.MasterOrderDetailId.Equals(masterProduct.OrderDetailId),
                            include: x =>
                                x.Include(x => x.MenuProduct).ThenInclude(menuProduct => menuProduct.Product));
                }
            }

            return orderDetailResponse;
        }

        public async Task<PrepareOrderRequest> PrepareOrder(PrepareOrderRequest orderReq)
        {
            if (orderReq.CustomerId == null && orderReq.PromotionCode == null && orderReq.VoucherCode == null)
                return orderReq;
            //tìm store từ req
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderReq.StoreId)
                                                      && x.Status.Equals(StoreStatus.Active.GetDescriptionFromEnum()));
            //tìm brand từ store đã tìm dc
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x =>
                x.Id.Equals(store.BrandId)
                && x.Status.Equals(BrandStatus.Active.GetDescriptionFromEnum()));
            //khởi tạo cái orderInfo
            CustomerOrderInfo customerOrderInfo = new CustomerOrderInfo
            {
                CartItems = new List<Item>(),
                Vouchers = new List<CouponCode>(),
                ApiKey = brand.Id.ToString(),
                Id = store.Code,
                BookingDate = DateTime.Now,
                Amount = (decimal) orderReq.TotalAmount,
                ShippingFee = (decimal) orderReq.ShippingFee
            };
            foreach (var menuPro in orderReq.ProductList)
            {
                //tìm menuProduct từ req

                customerOrderInfo.CartItems.Add(new Item()
                {
                    ProductCode = menuPro.Code,
                    CategoryCode = menuPro.CategoryCode,
                    ProductName = menuPro.Name,
                    UnitPrice = (decimal) menuPro.SellingPrice,
                    Quantity = menuPro.Quantity,
                    SubTotal = (decimal) menuPro.TotalAmount,
                    Discount = (decimal) menuPro.Discount,
                    DiscountFromOrder = 0,
                    Total = (decimal) menuPro.FinalAmount,
                    UrlImg = null
                });
            }


            CouponCode voucher = new CouponCode()
            {
                PromotionCode = orderReq.PromotionCode ?? null,
                VoucherCode = orderReq.VoucherCode ?? null
            };
            customerOrderInfo.Vouchers.Add(voucher);

            customerOrderInfo.Attributes = new OrderAttribute()
            {
                SalesMode = 7,
                PaymentMethod = 63,
                StoreInfo = new StoreInfo()
                {
                    StoreCode = store.Code,
                    BrandCode = brand.BrandCode,
                    Applier = "3"
                }
            };
            //tìm user từ req
            if (orderReq.CustomerId != null)
            {
                User user = await _unitOfWork.GetRepository<User>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderReq.CustomerId));
                if (user == null)
                {
                    throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
                }

                orderReq.CustomerName = user.FullName;
                orderReq.CustomerPhone = user.PhoneNumber;

                customerOrderInfo.Users = new Users()
                {
                    MembershipId = user.Id,
                };
            }
            else
            {
                customerOrderInfo.Users = null;
            }

            //call api check promotion
            //sửa lại localhost thành domain của api
            string url = "https://api-pointify.reso.vn/api/promotions/check-promotion";
            var response = await CallApiUtils.CallApiEndpoint(url, customerOrderInfo);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                //lấy value dc response từ api
                //CheckoutOrderResponse responseContent = (CheckoutOrderResponse)await CallApiUtils.GenerateObjectFromResponse(response);
                CheckoutOrderResponse? responseContent = new CheckoutOrderResponse();
                responseContent =
                    JsonConvert.DeserializeObject<CheckoutOrderResponse>(
                        response.Content.ReadAsStringAsync().Result);
                if (responseContent != null)
                {
                    orderReq.FinalAmount = (double) (responseContent.Order.FinalAmount ?? 0);
                    orderReq.DiscountAmount = (double) ((responseContent.Order.Discount ?? 0) +
                                                        (responseContent.Order.DiscountOrderDetail ?? 0));
                    orderReq.BonusPoint = (double) (responseContent.Order.BonusPoint ?? 0);
                    orderReq.PromotionList = new List<PromotionPrepare>();
                    if (responseContent.Order.Effects != null)
                    {
                        foreach (var promotionInOrder in responseContent.Order.Effects.Select(effect =>
                                     new PromotionPrepare()
                                     {
                                         PromotionId = effect.PromotionId,
                                         Name = effect.PromotionName,
                                         Code = effect.Prop.Code ?? "",
                                         DiscountAmount = (double) (effect.Prop.Value),
                                         EffectType = effect.EffectType
                                     }))
                        {
                            orderReq.PromotionList?.Add(promotionInOrder);
                        }
                    }

                    for (var i = 0; i < orderReq.ProductList.Count; i++)
                    {
                        orderReq.ProductList[i].Discount =
                            (double) responseContent.Order.CustomerOrderInfo.CartItems[i].Discount;
                        orderReq.ProductList[i].FinalAmount =
                            (double) responseContent.Order.CustomerOrderInfo.CartItems[i].Total;
                        orderReq.ProductList[i].PromotionCodeApplied =
                            responseContent.Order.CustomerOrderInfo.CartItems[i].PromotionCodeApply;
                    }

                    orderReq.Message = responseContent.Message;
                }
            }

            return orderReq;
        }

        public async Task<Guid> PlaceStoreOrder(PrepareOrderRequest createNewOrderRequest)
        {
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(createNewOrderRequest.StoreId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
                x.StoreId.Equals(createNewOrderRequest.StoreId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);
            if (currentUserSession == null)
                throw new BadHttpRequestException(MessageConstant.Order.CanNotCreateOrderInThisTime);
            if (!createNewOrderRequest.ProductList.Any())
                throw new BadHttpRequestException(MessageConstant.Order.NoProductsInOrderMessage);

            string newInvoiceId = store.Code + currentTimeStamp;
            int defaultGuest = 1;

            double vatAmount = (createNewOrderRequest.FinalAmount / VatStandard) * VatPercent;

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
                Vat = VatPercent,
                Vatamount = vatAmount,
                OrderType = createNewOrderRequest.OrderType.GetDescriptionFromEnum(),
                NumberOfGuest = createNewOrderRequest.CustomerNumber,
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = createNewOrderRequest.PaymentType.GetDescriptionFromEnum(),
                Note = createNewOrderRequest.Notes
            };


            List<OrderDetail> orderDetails = new List<OrderDetail>();
            List<PromotionOrderMapping> promotionMappingList = new List<PromotionOrderMapping>();
            List<PromotionPrepare> promotionInProductToRemove = new List<PromotionPrepare>();
            createNewOrderRequest.ProductList.ForEach(product =>
            {
                Guid masterOrderDetailId = Guid.NewGuid();
                orderDetails.Add(new OrderDetail()
                {
                    Id = masterOrderDetailId,
                    MenuProductId = product.ProductInMenuId,
                    OrderId = newOrder.Id,
                    Quantity = product.Quantity,
                    SellingPrice = product.SellingPrice,
                    TotalAmount = product.TotalAmount,
                    Discount = product.Discount,
                    FinalAmount = product.FinalAmount,
                    Notes = product.Note
                });
                if (product.Extras.Count > 0)
                {
                    product.Extras.ForEach(extra =>
                    {
                        orderDetails.Add(new OrderDetail()
                        {
                            Id = Guid.NewGuid(),
                            MenuProductId = extra.ProductInMenuId,
                            OrderId = newOrder.Id,
                            Quantity = extra.Quantity,
                            SellingPrice = extra.SellingPrice,
                            TotalAmount = extra.TotalAmount,
                            Discount = 0,
                            FinalAmount = extra.TotalAmount,
                            MasterOrderDetailId = masterOrderDetailId,
                        });
                    });
                }

                if (product.PromotionCodeApplied != null)
                {
                    if (createNewOrderRequest.PromotionList != null)
                    {
                        PromotionPrepare promotionPrepare = createNewOrderRequest.PromotionList.SingleOrDefault(
                            x => x.Code.Equals(product.PromotionCodeApplied));
                        promotionMappingList.Add(new PromotionOrderMapping()
                        {
                            Id = Guid.NewGuid(),
                            PromotionId = promotionPrepare.PromotionId ?? Guid.NewGuid(),
                            OrderId = newOrder.Id,
                            Quantity = 1,
                            DiscountAmount = product.Discount,
                            OrderDetailId = masterOrderDetailId,
                            EffectType = promotionPrepare.EffectType,
                            VoucherCode = createNewOrderRequest.VoucherCode
                        });
                        if (!promotionInProductToRemove.Contains(promotionPrepare))
                        {
                            promotionInProductToRemove.Add(promotionPrepare);
                        }
                    }
                }
            });


            if (createNewOrderRequest.PromotionList != null && createNewOrderRequest.PromotionList.Any())
            {
                createNewOrderRequest.PromotionList.ForEach(orderPromotion =>
                {
                    if (!promotionInProductToRemove.Contains(orderPromotion))
                    {
                        promotionMappingList.Add(new PromotionOrderMapping()
                        {
                            Id = Guid.NewGuid(),
                            PromotionId = orderPromotion.PromotionId ?? Guid.NewGuid(),
                            OrderId = newOrder.Id,
                            Quantity = 1,
                            DiscountAmount = orderPromotion.DiscountAmount,
                            EffectType = orderPromotion.EffectType,
                            VoucherCode = createNewOrderRequest.VoucherCode
                        });
                    }
                });
            }

            if (promotionMappingList.Any())
            {
                await _unitOfWork.GetRepository<PromotionOrderMapping>().InsertRangeAsync(promotionMappingList);
            }


            if (createNewOrderRequest.CustomerId != null)
            {
                OrderUser orderSource = new OrderUser()
                {
                    Id = Guid.NewGuid(),
                    UserType = "USER",
                    UserId = createNewOrderRequest.CustomerId,
                    Address = createNewOrderRequest.DeliveryAddress,
                    Name = createNewOrderRequest.CustomerName,
                    Phone = createNewOrderRequest.CustomerPhone,
                    CreatedAt = currentTime,
                    Status = OrderSourceStatus.PENDING.GetDescriptionFromEnum(),
                    CompletedAt = currentTime
                };
                newOrder.OrderSourceId = orderSource.Id;
                await _unitOfWork.GetRepository<OrderUser>().InsertAsync(orderSource);
            }
            else if (createNewOrderRequest is
                     {CustomerName: not null, CustomerPhone: not null, DeliveryAddress: not null})
            {
                OrderUser orderSource = new OrderUser()
                {
                    Id = Guid.NewGuid(),
                    UserType = "GUEST",
                    Address = createNewOrderRequest.DeliveryAddress,
                    Name = createNewOrderRequest.CustomerName,
                    Phone = createNewOrderRequest.CustomerPhone,
                    CreatedAt = currentTime,
                    Status = OrderSourceStatus.PENDING.GetDescriptionFromEnum()
                };
                newOrder.OrderSourceId = orderSource.Id;
                await _unitOfWork.GetRepository<OrderUser>().InsertAsync(orderSource);
            }

            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            await _unitOfWork.CommitAsync();
            return newOrder.Id;
        }

        public async Task<NewUserOrderResponse> GetNewUserOrderInStore(Guid storeId
        )
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            var orders = (List<Order>) await _unitOfWork.GetRepository<Order>().GetListAsync(
                predicate: x =>
                    x.Status.Equals(OrderStatus.PENDING.GetDescriptionFromEnum()) && x.OrderSourceId.HasValue &&
                    x.CheckInDate.Date.Equals(currentTime.Date) &&
                    x.Session.StoreId.Equals(storeId),
                include:
                x => x.Include(order => order.Session).Include(order => order.CheckInPersonNavigation)
            );
            NewUserOrderResponse response = new NewUserOrderResponse()
            {
                TotalOrder = orders.Count,
                TotalOrderPickUp = orders
                    .Count(o => o.OrderType != null && o.OrderType.Equals(OrderType.EAT_IN.GetDescriptionFromEnum())),
                TotalOrderDeli = orders
                    .Count(o => o.OrderType != null && o.OrderType.Equals(OrderType.DELIVERY.GetDescriptionFromEnum()))
            };
            return response;
        }
    }
}
﻿using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Extensions;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Request.CheckoutOrder;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.CheckoutOrderResponse;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;
using PromotionInOrder = Pos_System.API.Payload.Request.User.PromotionInOrder;

namespace Pos_System.API.Services.Implements
{
    public class OrderService : BaseService<OrderService>, IOrderService
    {
        public const double VAT_PERCENT = 0.1;
        public const double VAT_STANDARD = 1.1;

        public OrderService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<OrderService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
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
            double SystemDiscountAmount = 0;
            int defaultGuest = 1;

            double VATAmount = (createNewOrderRequest.FinalAmount / VAT_STANDARD) * VAT_PERCENT;

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

        public async Task<GetOrderDetailResponse> GetOrderDetail(Guid storeId, Guid orderId)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            if (orderId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Order.EmptyOrderIdMessage);

            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            Order order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(orderId),
                include: x =>
                    x.Include(p => p.PromotionOrderMappings).ThenInclude(a => a.Promotion).Include(o => o.OrderSource)
            );
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
            orderDetailResponse.PaymentType = string.IsNullOrEmpty(order.PaymentType)
                ? PaymentTypeEnum.CASH
                : EnumUtil.ParseEnum<PaymentTypeEnum>(order.PaymentType);
            orderDetailResponse.CheckInDate = order.CheckInDate;
            if (order.OrderSourceId != null)
            {
                OrderUser orderUser = await _unitOfWork.GetRepository<OrderUser>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(order.OrderSourceId)
                );
                orderDetailResponse.CustomerId = orderUser.UserId;
                orderDetailResponse.CustomerName = orderUser.Name;
                orderDetailResponse.CustomerPhone = orderUser.Phone;
                orderDetailResponse.CustomerType = orderUser.UserType;
                orderDetailResponse.DeliAddress = orderUser.Address;
                orderDetailResponse.DeliStatus = EnumUtil.ParseEnum<OrderSourceStatus>(orderUser.Status);
            }
            if (order.PromotionOrderMappings.Any())
            {
                orderDetailResponse.PromotionList = (List<OrderPromotionResponse>)await _unitOfWork
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
                        Address = order.OrderSource.Address
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
                                Address = order.OrderSource.Address
                            },
                            predicate: x => x.Id.Equals(order.OrderSource.UserId)
                        );
                }
            }

            orderDetailResponse.ProductList = (List<OrderProductDetailResponse>)await _unitOfWork
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
                    masterProduct.Extras = (List<OrderProductExtraDetailResponse>)await _unitOfWork
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
            DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? status)
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
                    OrderType = EnumUtil.ParseEnum<OrderType>(x.OrderType),
                    Status = EnumUtil.ParseEnum<OrderStatus>(x.Status),
                    PaymentType = string.IsNullOrEmpty(x.PaymentType)
                        ? PaymentTypeEnum.CASH
                        : EnumUtil.ParseEnum<PaymentTypeEnum>(x.PaymentType),
                },
                predicate: BuildGetOrdersInStoreQuery(storeId, startDate, endDate, orderType, status),
                include: x => x.Include(order => order.Session).Include(order => order.CheckInPersonNavigation),
                orderBy: x =>
                    userRole == RoleEnum.Staff
                        ? x.OrderByDescending(x => x.CheckInDate)
                        : x.OrderByDescending(x => x.InvoiceId),
                page: page,
                size: size
            );
            return ordersResponse;
        }

        private Expression<Func<Order, bool>> BuildGetOrdersInStoreQuery(Guid storeId, DateTime? startDate,
            DateTime? endDate, OrderType? orderType, OrderStatus? status)
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

            if (endDate != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.CheckInDate <= endDate);
            }

            if (orderType != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.OrderType.Equals(orderType.GetDescriptionFromEnum()));
            }

            if (status != null)
            {
                filterQuery = filterQuery.AndAlso(p => p.Status.Equals(status.GetDescriptionFromEnum()));
            }

            return filterQuery;
        }

        public async Task<Guid> UpdateOrder(Guid storeId, Guid orderId, UpdateOrderRequest updateOrderRequest)
        {
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
                x.StoreId.Equals(storeId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);

            Order order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId));
            if (currentUserSession == null)
                throw new BadHttpRequestException(MessageConstant.Order.UserNotInSessionMessage);
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            if (updateOrderRequest.Status.Equals(OrderStatus.CANCELED))
            {
                currentUserSession.NumberOfOrders--;
            }

            if (updateOrderRequest.Status.Equals(OrderStatus.PAID))
            {
                currentUserSession.TotalAmount += order.TotalAmount;
                currentUserSession.TotalFinalAmount += order.FinalAmount;
                currentUserSession.TotalDiscountAmount += order.Discount;
                if (updateOrderRequest.PaymentType != null)
                {
                    if (updateOrderRequest.PaymentType.Equals(PaymentTypeEnum.CASH))
                    {
                        currentUserSession.TotalChangeCash += order.FinalAmount;
                    }
                }
            }

            order.CheckInPerson = currentUser.Id;
            order.CheckOutDate = currentTime;
            order.PaymentType = updateOrderRequest.PaymentType.GetDescriptionFromEnum();
            order.Status = updateOrderRequest.Status.GetDescriptionFromEnum();
            _unitOfWork.GetRepository<Session>().UpdateAsync(currentUserSession);
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
            List<GetPromotionResponse> responese = (List<GetPromotionResponse>)await _unitOfWork
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

        public async Task<Payment> UpdatePaymentOrder(Guid orderId, PaymentOrderRequest req)
        {
            Order order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId));
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            Payment newPayment = new Payment()
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.FinalAmount,
                CurrencyCode = "VND",
                PayTime = TimeUtils.GetCurrentSEATime(),
                Type = req.PaymentType.GetDescriptionFromEnum(),
                Status = req.Status.GetDescriptionFromEnum()
            };
            //if(payment.Type.Equals(PaymentTypeEnum.POINTIFY_WALLET.GetDescriptionFromEnum()))
            //{
            //    payment.Status = OrderStatus.PAID.GetDescriptionFromEnum();
            //    _unitOfWork.GetRepository<Payment>().UpdateAsync(payment);
            //    await _unitOfWork.CommitAsync();
            //    var data = new { 

            //    };
            //}
            //else
            //{
            //    order.Status = OrderStatus.PENDING.GetDescriptionFromEnum();
            //}
            await _unitOfWork.GetRepository<Payment>().InsertAsync(newPayment);
            await _unitOfWork.CommitAsync();
            return newPayment;
        }

        public async Task<IPaginate<ViewOrdersResponse>> GetListOrderByUserId(Guid userId, OrderStatus status, int page,
            int size)
        {
            //lấy ra danh sách order của user
            List<Order> orders = (List<Order>)await _unitOfWork.GetRepository<Order>().GetListAsync(
                predicate: x => x.OrderSource.Id.Equals(userId));
            return orders;
        }

        // payment
        public async Task<CheckoutOrderRequest> CheckOutOrderAndPayment(
            CreateUserOrderRequest createNewUserOrderRequest,
            PaymentTypeEnum typePayment)
        {
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(createNewUserOrderRequest.StoreId)
                                                      && x.Status.Equals(StoreStatus.Active.GetDescriptionFromEnum()));
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x =>
                x.Id.Equals(store.BrandId)
                && x.Status.Equals(BrandStatus.Active.GetDescriptionFromEnum()));
            string url = $"https://localhost:44367/api/transaction/check-out?brandId={brand.Id}";
            if (typePayment == PaymentTypeEnum.POINTIFY_WALLET)
            {
                CheckoutOrderResponse response = await checkPromotionOrder(createNewUserOrderRequest);
                if (response != null)
                {
                    var checkOutOrder = await CallApiUtils.CallApiEndpoint(url, response.Order);
                    if (checkOutOrder.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        //CheckoutOrderRequest responseContent = (CheckoutOrderRequest)await CallApiUtils.GenerateObjectFromResponse(checkOutOrder);
                        CheckoutOrderRequest responseContent = new CheckoutOrderRequest();
                        responseContent =
                            JsonConvert.DeserializeObject<CheckoutOrderRequest>(checkOutOrder.Content
                                .ReadAsStringAsync().Result);
                        foreach (var item in responseContent.Effects)
                        {
                            if (item.EffectType.Equals("GET_POINT"))
                            {
                                Transaction transactionGetPoint = new Transaction()
                                {
                                    Id = Guid.NewGuid(),
                                    TransactionJson = JsonConvert.SerializeObject(responseContent),
                                    InsDate = DateTime.Now,
                                    UpsDate = DateTime.Now,
                                    PromotionId = item.PromotionId,
                                    BrandId = brand.Id,
                                    Amount = (decimal)responseContent.BonusPoint,
                                    Currency = "POINT",
                                    IsIncrease = true,
                                    Type = "GET_POINT",
                                };
                                await _unitOfWork.GetRepository<Transaction>().InsertAsync(transactionGetPoint);
                                await _unitOfWork.CommitAsync();
                            }
                            else
                            {
                                Transaction transaction = new Transaction()
                                {
                                    Id = Guid.NewGuid(),
                                    TransactionJson = JsonConvert.SerializeObject(responseContent),
                                    InsDate = DateTime.Now,
                                    UpsDate = DateTime.Now,
                                    PromotionId = item.PromotionId,
                                    BrandId = brand.Id,
                                    Amount = (decimal)responseContent.FinalAmount,
                                    Currency = "VND",
                                    IsIncrease = false,
                                    Type = typePayment.GetDescriptionFromEnum(),
                                };
                                await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                                await _unitOfWork.CommitAsync();
                            }
                        }

                        return responseContent;
                    }
                }
            }

            return null;
        }

        private async Task<CheckoutOrderResponse> checkPromotionOrder(CreateUserOrderRequest orderReq)
        {
            //tìm store từ req
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderReq.StoreId)
                                                      && x.Status.Equals(StoreStatus.Active.GetDescriptionFromEnum()));
            //tìm brand từ store đã tìm dc
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x =>
                x.Id.Equals(store.BrandId)
                && x.Status.Equals(BrandStatus.Active.GetDescriptionFromEnum()));
            //khởi tạo cái orderInfo
            CustomerOrderInfo customerOrderInfo = new CustomerOrderInfo()
            {
                CartItems = new List<Item>(),
                Vouchers = new List<CouponCode>(),
            };
            //tìm thấy store và brand
            customerOrderInfo.ApiKey = brand.Id.ToString();
            customerOrderInfo.Id = store.Code;
            customerOrderInfo.BookingDate = DateTime.Now;
            //tìm product từ req
            MenuProduct menPro = new MenuProduct();
            Product product = new Product();
            Category category = new Category();
            foreach (var menuPro in orderReq.ProductsList)
            {
                //tìm menuProduct từ req
                menPro = await _unitOfWork.GetRepository<MenuProduct>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(menuPro.ProductInMenuId)
                                                          && x.Status.Equals(
                                                              ProductStatus.Active.GetDescriptionFromEnum()));
                //tìm product từ menuProduct
                product = await _unitOfWork.GetRepository<Product>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(menPro.ProductId)
                                                          && x.Status.Equals(
                                                              ProductStatus.Active.GetDescriptionFromEnum()));
                //tìm category từ product
                category = await _unitOfWork.GetRepository<Category>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(product.CategoryId)
                                                          && x.Status.Equals(
                                                              ProductStatus.Active.GetDescriptionFromEnum()));
                customerOrderInfo.CartItems.Add(new Item()
                {
                    ProductCode = product.Code,
                    CategoryCode = category.Code,
                    ProductName = product.Name,
                    UnitPrice = (decimal)product.SellingPrice,
                    Quantity = menuPro.Quantity,
                    SubTotal = (decimal)(product.SellingPrice * menuPro.Quantity),
                    Discount = (decimal)menuPro.Discount,
                    DiscountFromOrder = 0,
                    Total = (decimal)((product.SellingPrice * menuPro.Quantity) -
                                       (product.SellingPrice * menuPro.Quantity * menuPro.Discount)),
                    UrlImg = product.PicUrl
                });
            }

            foreach (var promotion in orderReq.PromotionList)
            {
                //tìm promotion
                Promotion promo = await _unitOfWork.GetRepository<Promotion>()
                    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(promotion.PromotionId)
                                                          //sửa lại cái promotion status
                                                          && x.Status.Equals(PromotionStatus.Deactive
                                                              .GetDescriptionFromEnum()));
                customerOrderInfo.Vouchers.Add(new CouponCode()
                {
                    PromotionCode = promo.Code,
                });
            }

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
            User user = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderReq.UserId));
            customerOrderInfo.Users = new Users()
            {
                MembershipId = user.Id,
            };
            customerOrderInfo.Amount = (decimal)orderReq.FinalAmount;
            customerOrderInfo.ShippingFee = (decimal)orderReq.DiscountAmount;
            //call api check promotion
            //sửa lại localhost thành domain của api
            string url = "https://localhost:44367/api/promotions/check-promotion";
            var response = await CallApiUtils.CallApiEndpoint(url, customerOrderInfo);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                //lấy value dc response từ api
                //CheckoutOrderResponse responseContent = (CheckoutOrderResponse)await CallApiUtils.GenerateObjectFromResponse(response);
                CheckoutOrderResponse responseContent = new CheckoutOrderResponse();
                responseContent =
                    JsonConvert.DeserializeObject<CheckoutOrderResponse>(response.Content.ReadAsStringAsync().Result);
                return responseContent;
            }

            return null;
        }


        public async Task<PrepareOrderRequest> PrepareOrder(PrepareOrderRequest orderReq)
        {
            //tìm store từ req
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderReq.StoreId)
                                                      && x.Status.Equals(StoreStatus.Active.GetDescriptionFromEnum()));
            //tìm brand từ store đã tìm dc
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x =>
                x.Id.Equals(store.BrandId)
                && x.Status.Equals(BrandStatus.Active.GetDescriptionFromEnum()));
            //khởi tạo cái orderInfo
            CustomerOrderInfo customerOrderInfo = new CustomerOrderInfo()
            {
                CartItems = new List<Item>(),
                Vouchers = new List<CouponCode>(),
            };
            //tìm thấy store và brand
            customerOrderInfo.ApiKey = brand.Id.ToString();
            customerOrderInfo.Id = store.Code;
            customerOrderInfo.BookingDate = DateTime.Now;

            foreach (var menuPro in orderReq.ProductList)
            {
                //tìm menuProduct từ req

                customerOrderInfo.CartItems.Add(new Item()
                {
                    ProductCode = menuPro.Code,
                    CategoryCode = menuPro.CategoryCode,
                    ProductName = menuPro.Name,
                    UnitPrice = (decimal)menuPro.SellingPrice,
                    Quantity = menuPro.Quantity,
                    SubTotal = (decimal)menuPro.TotalAmount,
                    Discount = (decimal)menuPro.Discount,
                    DiscountFromOrder = 0,
                    Total = (decimal)menuPro.FinalAmount,
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

            customerOrderInfo.Amount = (decimal)orderReq.TotalAmount;
            customerOrderInfo.ShippingFee = (decimal)orderReq.ShippingFee;
            //call api check promotion
            //sửa lại localhost thành domain của api
            string url = "https://api-pointify.reso.vn/api/promotions/check-promotion";
            var response = await CallApiUtils.CallApiEndpoint(url, customerOrderInfo);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                //lấy value dc response từ api
                //CheckoutOrderResponse responseContent = (CheckoutOrderResponse)await CallApiUtils.GenerateObjectFromResponse(response);
                CheckoutOrderResponse responseContent = new CheckoutOrderResponse();
                responseContent =
                    JsonConvert.DeserializeObject<CheckoutOrderResponse>(response.Content.ReadAsStringAsync().Result);
                orderReq.FinalAmount = (double)(responseContent.Order.FinalAmount ?? 0);
                orderReq.DiscountAmount = (double)((responseContent.Order.Discount ?? 0) +
                                                    (responseContent.Order.DiscountOrderDetail ?? 0));
                orderReq.BonusPoint = (double)(responseContent.Order.BonusPoint ?? 0);
                orderReq.PromotionList = new List<PromotionPrepare>();
                foreach (var promotionInOrder in responseContent.Order.Effects.Select(effect => new PromotionPrepare()
                {
                    PromotionId = effect.PromotionId,
                    Name = effect.PromotionName,
                    Code = effect.Prop.Code ?? "",
                    DiscountAmount = (double)(effect.Prop.Value),
                    EffectType = effect.EffectType
                }))
                {
                    orderReq.PromotionList?.Add(promotionInOrder);
                }

                foreach (var cartItem in responseContent.Order.CustomerOrderInfo.CartItems)
                {
                    if (cartItem.PromotionCodeApply == null) continue;
                    foreach (var t in orderReq.ProductList)
                    {
                        t.Discount = (double)cartItem.Discount;
                        t.FinalAmount = (double)cartItem.Total;
                        t.PromotionCodeApplied = cartItem.PromotionCodeApply;
                    }
                }

                orderReq.Message = responseContent.Message;
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

            double vatAmount = (createNewOrderRequest.FinalAmount / VAT_STANDARD) * VAT_PERCENT;

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
                Vatamount = vatAmount,
                OrderType = createNewOrderRequest.OrderType.GetDescriptionFromEnum(),
                NumberOfGuest = defaultGuest,
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = createNewOrderRequest.PaymentType.GetDescriptionFromEnum()
            };


            List<OrderDetail> orderDetails = new List<OrderDetail>();
            List<PromotionOrderMapping> promotionMappingList = new List<PromotionOrderMapping>();
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
                            EffectType = promotionPrepare.EffectType
                        });
                        createNewOrderRequest.PromotionList.Remove(promotionPrepare);
                    }
                }
            });
            if (createNewOrderRequest.PromotionList != null && createNewOrderRequest.PromotionList.Any())
            {
                createNewOrderRequest.PromotionList.ForEach(orderPromotion =>
                {
                    promotionMappingList.Add(new PromotionOrderMapping()
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = orderPromotion.PromotionId ?? Guid.NewGuid(),
                        OrderId = newOrder.Id,
                        Quantity = 1,
                        DiscountAmount = orderPromotion.DiscountAmount,
                        EffectType = orderPromotion.EffectType
                    });
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
            { CustomerName: not null, CustomerPhone: not null, DeliveryAddress: not null })
            {
                OrderUser orderSource = new OrderUser()
                {
                    Id = Guid.NewGuid(),
                    UserType = "GUEST",
                    UserId = Guid.Parse("6CFADCDC-25C2-4F0C-8335-5B45698B2375"),
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
    }
}
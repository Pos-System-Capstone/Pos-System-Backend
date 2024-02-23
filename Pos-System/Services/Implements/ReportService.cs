using System;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Response.Report;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class ReportService : BaseService<ReportService>, IReportService
    {
        private readonly IMenuService _menuService;
        public readonly IOrderService _orderService;

        public ReportService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<ReportService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, IMenuService menuService, IOrderService orderService) : base(
            unitOfWork, logger, mapper, httpContextAccessor)
        {
            _menuService = menuService;
            _orderService = orderService;
        }

        public async Task<GetStoreEndDayReport> GetStoreEndDayReport(string? storeCode, string? brandCode,
            DateTime? startDate,
            DateTime? endDate)
        {
            //RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());

            //Check Brand Code is existed.
            if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandCodeNotFoundMessage);

            //Check StoreCode is existed

            // Store store = await _unitOfWork.GetRepository<Store>()
            //     .SingleOrDefaultAsync(predicate: x => x.BrandId.Equals(brand.Id));
            // if (storeCode == null || store == null)
            //     throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreCodeMessage);

            #region ~~~~~~~~OldCode~~~~~~~~

            //if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            //Guid currentUserStoreId = Guid.Parse(GetStoreIdFromJwt());
            //if (currentUserStoreId != storeId)
            //    throw new BadHttpRequestException(MessageConstant.Store.GetStoreOrdersUnAuthorized);

            //Guid userBrandId = await _unitOfWork.GetRepository<Store>()
            //   .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(currentUserStoreId));

            #endregion

            GetStoreEndDayReport report = new GetStoreEndDayReport();

            List<Category> categories = (List<Category>) await _unitOfWork.GetRepository<Category>().GetListAsync(
                predicate: x => x.BrandId.Equals(brand.Id)
            );
            List<Promotion> promotions = (List<Promotion>) await _unitOfWork.GetRepository<Promotion>().GetListAsync(
                predicate: x => x.BrandId.Equals(brand.Id)
            );
            List<Order> orders = (List<Order>) await _unitOfWork.GetRepository<Order>().GetListAsync(
                include: x =>
                    x.Include(order => order.OrderDetails).ThenInclude(x => x.MenuProduct).ThenInclude(x => x.Product)
                        .Include(x => x.PromotionOrderMappings).Include(order => order.Session)
                        .ThenInclude(store => store.Store).ThenInclude(brand => brand.Brand),
                predicate: p =>
                    (storeCode == null
                        ? p.Session.Store.Brand.BrandCode.Equals(brandCode)
                        : p.Session.Store.Code.Equals(storeCode)) && p.CheckInDate >= startDate &&
                    p.CheckInDate <= endDate.Value.AddDays(1) &&
                    p.Status.Equals(OrderStatus.PAID.GetDescriptionFromEnum()),
                orderBy: x => x.OrderByDescending(x => x.CheckInDate)
            );
            report.TotalProductDiscount = 0;
            report.TotalProduct = 0;
            report.TotalAmountSizeL = 0;
            report.TotalAmountSizeM = 0;
            report.TotalAmountSizeS = 0;
            report.TotalSizeL = 0;
            report.TotalSizeM = 0;
            report.TotalSizeS = 0;
            report.TotalPromotionUsed = 0;
            for (int i = 6; i < 24; i++)
            {
                report.TimeLine.Add(i);
                report.TotalAmountTimeLine.Add(0);
                report.TotalOrderTimeLine.Add(0);
            }

            foreach (var category in categories)
            {
                report.CategoryReports.Add(new CategoryReport(category.Id, category.Name, 0, 0, 0, 0,
                    new List<ProductReport>()));
            }

            foreach (var promotion in promotions)
            {
                report.PromotionReports.Add(new PromotionReport(promotion.Id, promotion.Name, 0, 0));
            }


            foreach (var item in orders)
            {
                report.TotalAmount += item.TotalAmount;
                report.TotalDiscount += item.Discount;
                report.VatAmount += item.Vatamount;
                report.FinalAmount += item.FinalAmount;
                report.TotalOrder++;
                if (item.PromotionOrderMappings.Any())
                {
                    foreach (var promotionInOrder in item.PromotionOrderMappings)
                    {
                        report.TotalPromotionUsed += (promotionInOrder.Quantity ?? 1);
                        var idx = report.PromotionReports.FindIndex(element =>
                            element.Id.Equals(promotionInOrder.PromotionId));
                        if (idx != -1)
                        {
                            report.PromotionReports[idx].Quantity += (promotionInOrder.Quantity ?? 1);
                            report.PromotionReports[idx].TotalDiscount +=
                                (promotionInOrder.DiscountAmount ?? item.Discount);
                        }
                    }
                }

                if (item.OrderType == OrderType.EAT_IN.GetDescriptionFromEnum())
                {
                    report.InStoreAmount += item.FinalAmount;
                    report.TotalOrderInStore++;
                }
                else if (item.OrderType == OrderType.TAKE_AWAY.GetDescriptionFromEnum())
                {
                    report.TakeAwayAmount += item.FinalAmount;
                    report.TotalOrderTakeAway++;
                }
                else
                {
                    report.DeliAmount += item.FinalAmount;
                    report.TotalOrderDeli++;
                }

                ;
                if (item.PaymentType == PaymentTypeEnum.VISA.GetDescriptionFromEnum())
                {
                    report.VisaAmount += item.FinalAmount;
                    report.TotalVisa++;
                }
                else if (item.PaymentType == PaymentTypeEnum.MOMO.GetDescriptionFromEnum())
                {
                    report.MomoAmount += item.FinalAmount;
                    report.TotalMomo++;
                }
                else if (item.PaymentType == PaymentTypeEnum.BANKING.GetDescriptionFromEnum())
                {
                    report.BankingAmount += item.FinalAmount;
                    report.TotalBanking++;
                }
                else
                {
                    report.CashAmount += item.FinalAmount;
                    report.TotalCash++;
                }

                ;
                foreach (var cateReport in report.CategoryReports)
                {
                    foreach (var orderDetail in item.OrderDetails)
                    {
                        if (cateReport.Id.Equals(orderDetail.MenuProduct.Product.CategoryId))
                        {
                            var exitProduct = cateReport.ProductReports.FindIndex(element =>
                                element.Id.Equals(orderDetail.MenuProduct.ProductId));
                            if (exitProduct == -1)
                            {
                                cateReport.ProductReports.Add(new ProductReport(orderDetail.MenuProduct.Product.Id,
                                    orderDetail.MenuProduct.Product.Name, orderDetail.Quantity,
                                    orderDetail.MenuProduct.Product.SellingPrice, orderDetail.TotalAmount,
                                    orderDetail.Discount, orderDetail.FinalAmount));
                                cateReport.TotalProduct += orderDetail.Quantity;
                                cateReport.TotalAmount += orderDetail.TotalAmount;
                                cateReport.TotalDiscount += orderDetail.Discount;
                                cateReport.FinalAmount += orderDetail.FinalAmount;
                                report.ProductCosAmount += orderDetail.MenuProduct.Product.HistoricalPrice *
                                                           orderDetail.Quantity;
                                report.TotalProductDiscount += orderDetail.Discount;
                                report.TotalProduct += orderDetail.Quantity;
                                if (!string.IsNullOrEmpty(orderDetail.MenuProduct.Product.Size))
                                {
                                    if (orderDetail.MenuProduct.Product.Size.Equals(
                                            ProductSize.S.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeS += orderDetail.Quantity;
                                        report.TotalAmountSizeS += orderDetail.FinalAmount;
                                    }
                                    else if (orderDetail.MenuProduct.Product.Size.Equals(
                                                 ProductSize.M.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeM += orderDetail.Quantity;
                                        report.TotalAmountSizeM += orderDetail.FinalAmount;
                                    }
                                    else
                                    {
                                        report.TotalSizeL += orderDetail.Quantity;
                                        report.TotalAmountSizeL += orderDetail.FinalAmount;
                                    }
                                }
                            }
                            else
                            {
                                cateReport.ProductReports[exitProduct].Quantity += orderDetail.Quantity;
                                cateReport.ProductReports[exitProduct].TotalAmount += orderDetail.TotalAmount;
                                cateReport.ProductReports[exitProduct].TotalDiscount += orderDetail.Discount;
                                cateReport.ProductReports[exitProduct].FinalAmount += orderDetail.FinalAmount;
                                cateReport.TotalProduct += orderDetail.Quantity;
                                cateReport.TotalAmount += orderDetail.TotalAmount;
                                cateReport.TotalDiscount += orderDetail.Discount;
                                cateReport.FinalAmount += orderDetail.FinalAmount;
                                report.ProductCosAmount += orderDetail.MenuProduct.Product.HistoricalPrice *
                                                           orderDetail.Quantity;
                                report.TotalProductDiscount += orderDetail.Discount;
                                report.TotalProduct += orderDetail.Quantity;
                                if (!string.IsNullOrEmpty(orderDetail.MenuProduct.Product.Size))
                                {
                                    if (orderDetail.MenuProduct.Product.Size.Equals(
                                            ProductSize.S.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeS += orderDetail.Quantity;
                                        report.TotalAmountSizeS += orderDetail.FinalAmount;
                                    }
                                    else if (orderDetail.MenuProduct.Product.Size.Equals(
                                                 ProductSize.M.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeM += orderDetail.Quantity;
                                        report.TotalAmountSizeM += orderDetail.FinalAmount;
                                    }
                                    else
                                    {
                                        report.TotalSizeL += orderDetail.Quantity;
                                        report.TotalAmountSizeL += orderDetail.FinalAmount;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 6; i < 24; i++)
                {
                    if (i == item.CheckOutDate.Hour)
                    {
                        report.TotalOrderTimeLine[i - 6]++;
                        report.TotalAmountTimeLine[i - 6] += item.FinalAmount;
                    }
                }
            }

            report.TotalPromotionDiscount = report.TotalDiscount - report.TotalProductDiscount;
            if (report.TotalOrder == 0)
            {
                report.AverageBill = 0;
            }
            else
            {
                report.AverageBill = report.FinalAmount / report.TotalOrder;
            }

            report.TotalRevenue = report.FinalAmount - report.ProductCosAmount;
            return report;
        }

        public async Task<SessionReport> GetSessionReportDetail(Guid sessionId)
        {
            Guid userStoreId = Guid.Parse(GetStoreIdFromJwt());
            if (sessionId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.Session.EmptySessionIdMessage);

            Session session = await _unitOfWork.GetRepository<Session>()
                .SingleOrDefaultAsync(predicate: x => x.StoreId.Equals(userStoreId) && x.Id.Equals(sessionId));
            if (session == null) throw new BadHttpRequestException(MessageConstant.Session.SessionNotFoundMessage);


            List<Order> orders = (List<Order>) await _unitOfWork.GetRepository<Order>().GetListAsync(
                include: x => x.Include(order => order.Session),
                predicate: p =>
                    p.SessionId.Equals(sessionId) && p.Status.Equals(OrderStatus.PAID.GetDescriptionFromEnum())
            );
            SessionReport report = new SessionReport(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            foreach (var item in orders)
            {
                report.TotalAmount += item.TotalAmount;
                report.TotalDiscount += item.Discount;
                report.FinalAmount += item.FinalAmount;
                report.TotalOrder++;
                if (item.PaymentType == PaymentTypeEnum.VISA.GetDescriptionFromEnum())
                {
                    report.VisaAmount += item.FinalAmount;
                    report.TotalVisa++;
                }
                else if (item.PaymentType == PaymentTypeEnum.MOMO.GetDescriptionFromEnum())
                {
                    report.MomoAmount += item.FinalAmount;
                    report.TotalMomo++;
                }
                else if (item.PaymentType == PaymentTypeEnum.BANKING.GetDescriptionFromEnum())
                {
                    report.BankingAmount += item.FinalAmount;
                    report.TotalBanking++;
                }
                else if (item.PaymentType == PaymentTypeEnum.POINTIFY.GetDescriptionFromEnum())
                {
                    report.PointifyAmount += item.FinalAmount;
                    report.TotalPointify++;
                }
                else if (item.PaymentType == PaymentTypeEnum.CASH.GetDescriptionFromEnum())
                {
                    report.CashAmount += item.FinalAmount;
                    report.TotalCash++;
                }
            }

            return report;
        }


        public async Task<GetStoreEndDayReport> GetStoreEndDayReport(Guid storeId, DateTime? startDate,
            DateTime? endDate)
        {
            RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
            if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Guid currentUserStoreId = Guid.Parse(GetStoreIdFromJwt());
            if (currentUserStoreId != storeId)
                throw new BadHttpRequestException(MessageConstant.Store.GetStoreOrdersUnAuthorized);

            Guid userBrandId = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(currentUserStoreId));

            GetStoreEndDayReport report = new GetStoreEndDayReport();

            List<Category> categories = (List<Category>) await _unitOfWork.GetRepository<Category>().GetListAsync(
                predicate: x => x.BrandId.Equals(userBrandId)
            );
            List<Promotion> promotions = (List<Promotion>) await _unitOfWork.GetRepository<Promotion>().GetListAsync(
                predicate: x => x.BrandId.Equals(userBrandId)
            );
            List<Order> orders = (List<Order>) await _unitOfWork.GetRepository<Order>().GetListAsync(
                include: x =>
                    x.Include(order => order.OrderDetails).ThenInclude(x => x.MenuProduct).ThenInclude(x => x.Product)
                        .Include(x => x.PromotionOrderMappings).Include(order => order.Session),
                predicate: p =>
                    p.Session.StoreId.Equals(storeId) && p.CheckInDate >= startDate &&
                    p.CheckInDate <= endDate.Value.AddDays(1) &&
                    p.Status.Equals(OrderStatus.PAID.GetDescriptionFromEnum()),
                orderBy: x => x.OrderByDescending(x => x.CheckInDate)
            );

            report.TotalProductDiscount = 0;
            report.TotalProduct = 0;
            report.TotalAmountSizeL = 0;
            report.TotalAmountSizeM = 0;
            report.TotalAmountSizeS = 0;
            report.TotalSizeL = 0;
            report.TotalSizeM = 0;
            report.TotalSizeS = 0;
            report.TotalPromotionUsed = 0;
            for (int i = 6; i < 24; i++)
            {
                report.TimeLine.Add(i);
                report.TotalAmountTimeLine.Add(0);
                report.TotalOrderTimeLine.Add(0);
            }

            foreach (var category in categories)
            {
                report.CategoryReports.Add(new CategoryReport(category.Id, category.Name, 0, 0, 0, 0,
                    new List<ProductReport>()));
            }

            foreach (var promotion in promotions)
            {
                report.PromotionReports.Add(new PromotionReport(promotion.Id, promotion.Name, 0, 0));
            }


            foreach (var item in orders)
            {
                report.TotalAmount += item.TotalAmount;
                report.TotalDiscount += item.Discount;
                report.VatAmount += item.Vatamount;
                report.FinalAmount += item.FinalAmount;
                report.TotalOrder++;
                if (item.PromotionOrderMappings.Any())
                {
                    foreach (var promotionInOrder in item.PromotionOrderMappings)
                    {
                        report.TotalPromotionUsed += (promotionInOrder.Quantity ?? 1);
                        var idx = report.PromotionReports.FindIndex(element =>
                            element.Id.Equals(promotionInOrder.PromotionId));
                        if (idx != -1)
                        {
                            report.PromotionReports[idx].Quantity += (promotionInOrder.Quantity ?? 1);
                            report.PromotionReports[idx].TotalDiscount +=
                                (promotionInOrder.DiscountAmount ?? item.Discount);
                        }
                    }
                }

                if (item.OrderType == OrderType.EAT_IN.GetDescriptionFromEnum())
                {
                    report.InStoreAmount += item.FinalAmount;
                    report.TotalOrderInStore++;
                }
                else if (item.OrderType == OrderType.TAKE_AWAY.GetDescriptionFromEnum())
                {
                    report.TakeAwayAmount += item.FinalAmount;
                    report.TotalOrderTakeAway++;
                }
                else if (item.OrderType == OrderType.TOP_UP.GetDescriptionFromEnum())
                {
                    report.TopUpAmount += item.FinalAmount;
                    report.TotalOrderTopUp++;
                }
                else if (item.OrderType == OrderType.DELIVERY.GetDescriptionFromEnum())
                {
                    report.DeliAmount += item.FinalAmount;
                    report.TotalOrderDeli++;
                }


                if (item.PaymentType == PaymentTypeEnum.VISA.GetDescriptionFromEnum())
                {
                    report.VisaAmount += item.FinalAmount;
                    report.TotalVisa++;
                }
                else if (item.PaymentType == PaymentTypeEnum.POINTIFY.GetDescriptionFromEnum())
                {
                    report.PointifyAmount += item.FinalAmount;
                    report.TotalPointify++;
                }
                else if (item.PaymentType == PaymentTypeEnum.MOMO.GetDescriptionFromEnum())
                {
                    report.MomoAmount += item.FinalAmount;
                    report.TotalMomo++;
                }
                else if (item.PaymentType == PaymentTypeEnum.BANKING.GetDescriptionFromEnum())
                {
                    report.BankingAmount += item.FinalAmount;
                    report.TotalBanking++;
                }
                else
                {
                    report.CashAmount += item.FinalAmount;
                    report.TotalCash++;
                }

                ;
                foreach (var cateReport in report.CategoryReports)
                {
                    foreach (var orderDetail in item.OrderDetails)
                    {
                        if (cateReport.Id.Equals(orderDetail.MenuProduct.Product.CategoryId))
                        {
                            var exitProduct = cateReport.ProductReports.FindIndex(element =>
                                element.Id.Equals(orderDetail.MenuProduct.ProductId));
                            if (exitProduct == -1)
                            {
                                cateReport.ProductReports.Add(new ProductReport(orderDetail.MenuProduct.Product.Id,
                                    orderDetail.MenuProduct.Product.Name, orderDetail.Quantity,
                                    orderDetail.MenuProduct.Product.SellingPrice, orderDetail.TotalAmount,
                                    orderDetail.Discount, orderDetail.FinalAmount));
                                cateReport.TotalProduct += orderDetail.Quantity;
                                cateReport.TotalAmount += orderDetail.TotalAmount;
                                cateReport.TotalDiscount += orderDetail.Discount;
                                cateReport.FinalAmount += orderDetail.FinalAmount;
                                report.ProductCosAmount += orderDetail.MenuProduct.Product.HistoricalPrice *
                                                           orderDetail.Quantity;
                                report.TotalProductDiscount += orderDetail.Discount;
                                report.TotalProduct += orderDetail.Quantity;
                                if (!string.IsNullOrEmpty(orderDetail.MenuProduct.Product.Size))
                                {
                                    if (orderDetail.MenuProduct.Product.Size.Equals(
                                            ProductSize.S.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeS += orderDetail.Quantity;
                                        report.TotalAmountSizeS += orderDetail.FinalAmount;
                                    }
                                    else if (orderDetail.MenuProduct.Product.Size.Equals(
                                                 ProductSize.M.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeM += orderDetail.Quantity;
                                        report.TotalAmountSizeM += orderDetail.FinalAmount;
                                    }
                                    else
                                    {
                                        report.TotalSizeL += orderDetail.Quantity;
                                        report.TotalAmountSizeL += orderDetail.FinalAmount;
                                    }
                                }
                            }
                            else
                            {
                                cateReport.ProductReports[exitProduct].Quantity += orderDetail.Quantity;
                                cateReport.ProductReports[exitProduct].TotalAmount += orderDetail.TotalAmount;
                                cateReport.ProductReports[exitProduct].TotalDiscount += orderDetail.Discount;
                                cateReport.ProductReports[exitProduct].FinalAmount += orderDetail.FinalAmount;
                                cateReport.TotalProduct += orderDetail.Quantity;
                                cateReport.TotalAmount += orderDetail.TotalAmount;
                                cateReport.TotalDiscount += orderDetail.Discount;
                                cateReport.FinalAmount += orderDetail.FinalAmount;
                                report.ProductCosAmount += orderDetail.MenuProduct.Product.HistoricalPrice *
                                                           orderDetail.Quantity;
                                report.TotalProductDiscount += orderDetail.Discount;
                                report.TotalProduct += orderDetail.Quantity;
                                if (!string.IsNullOrEmpty(orderDetail.MenuProduct.Product.Size))
                                {
                                    if (orderDetail.MenuProduct.Product.Size.Equals(
                                            ProductSize.S.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeS += orderDetail.Quantity;
                                        report.TotalAmountSizeS += orderDetail.FinalAmount;
                                    }
                                    else if (orderDetail.MenuProduct.Product.Size.Equals(
                                                 ProductSize.M.GetDescriptionFromEnum()))
                                    {
                                        report.TotalSizeM += orderDetail.Quantity;
                                        report.TotalAmountSizeM += orderDetail.FinalAmount;
                                    }
                                    else
                                    {
                                        report.TotalSizeL += orderDetail.Quantity;
                                        report.TotalAmountSizeL += orderDetail.FinalAmount;
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 6; i < 24; i++)
                {
                    if (i == item.CheckOutDate.Hour)
                    {
                        report.TotalOrderTimeLine[i - 6]++;
                        report.TotalAmountTimeLine[i - 6] += item.FinalAmount;
                    }
                }
            }

            report.TotalPromotionDiscount = report.TotalDiscount - report.TotalProductDiscount;
            if (report.TotalOrder == 0)
            {
                report.AverageBill = 0;
            }
            else
            {
                report.AverageBill = report.FinalAmount / report.TotalOrder;
            }

            report.TotalRevenue = report.FinalAmount - report.ProductCosAmount;
            return report;
        }

        public async Task<FileStreamResult> DownloadStoreReport(Guid storeId, DateTime? startDate, DateTime? endDate)
        {
            Store store = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(storeId)
            );
            GetStoreEndDayReport report = await GetStoreEndDayReport(storeId, startDate, endDate);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using ExcelPackage package = new ExcelPackage();
            ExcelWorksheet ws = package.Workbook.Worksheets.Add(
                $"{startDate:dd/MM/yyyy}-{startDate:dd/MM/yyyy}");
            List<string> listRow2Title = new List<string>()
            {
                "Danh mục",
                "Tên sản phẩm",
                "Đơn giá",
                "Số lượng",
                "Thành tiền",
                "Tổng cộng"
            };
            Dictionary<int, int> walletPositions = new Dictionary<int, int>();
            int initialRow = 1; //A
            int initialCol = 1; //1

            //Row 2
            initialCol = 1;
            for (int i = 0; i < listRow2Title.Count; i++)
            {
                ws.Cells[initialRow, initialCol].Value = listRow2Title[i];
                ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
                ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
                ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                initialCol++;
            }

            initialRow++;
            initialCol = 1;
            foreach (var t in report.CategoryReports)
            {
                foreach (var t1 in t.ProductReports)
                {
                    ws.Cells[initialRow, initialCol].Value = t.Name;
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = t1.Name;
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = t1.SellingPrice;
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = t1.Quantity;
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = t1.TotalAmount;
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = t1.FinalAmount;
                    initialCol = 1;
                    ++initialRow;
                }
            }

            ws.Cells[initialRow, 4].Value = "Tổng SP: " + report.TotalProduct;
            ws.Cells[initialRow, 4].Style.Font.Size = 13;
            ws.Cells[initialRow, 4].Style.Font.Bold = true;
            ws.Cells[initialRow, 6].Value = "Trước giảm(1): " + report.TotalAmount;
            ws.Cells[initialRow, 6].Style.Font.Size = 13;
            ws.Cells[initialRow, 6].Style.Font.Bold = true;
            ws.Cells[initialRow, 7].Value = "Giảm giá(2): " + report.TotalDiscount;
            ws.Cells[initialRow, 7].Style.Font.Size = 13;
            ws.Cells[initialRow, 7].Style.Font.Bold = true;
            ws.Cells[initialRow, 8].Value = "Sau giảm(3)=(1)-(2): " + report.TotalDiscount;
            ws.Cells[initialRow, 8].Style.Font.Size = 13;
            ws.Cells[initialRow, 8].Style.Font.Bold = true;
            ++initialRow;
            ws.Cells[initialRow, 7].Value = "Chi phí SP (4): " + report.ProductCosAmount;
            ws.Cells[initialRow, 7].Style.Font.Size = 13;
            ws.Cells[initialRow, 7].Style.Font.Bold = true;
            ws.Cells[initialRow, 8].Value = "Lợi nhuận (5)= (3)-(4): " + report.TotalRevenue;
            ws.Cells[initialRow, 8].Style.Font.Size = 13;
            ws.Cells[initialRow, 8].Style.Font.Bold = true;
            ++initialRow;
            ++initialRow;
            ws.Cells[initialRow, 1].Value = "Đơn tại quán: " + report.TotalOrderInStore;
            ws.Cells[initialRow, 1].Style.Font.Bold = true;
            ws.Cells[initialRow, 1].Style.Font.Size = 13;
            ws.Cells[initialRow, 2].Value = "Đơn mang đi: " + report.TotalOrderTakeAway;
            ws.Cells[initialRow, 2].Style.Font.Bold = true;
            ws.Cells[initialRow, 2].Style.Font.Size = 13;
            ws.Cells[initialRow, 3].Value = "Đơn giao hàng: " + report.TotalOrderDeli;
            ws.Cells[initialRow, 3].Style.Font.Bold = true;
            ws.Cells[initialRow, 3].Style.Font.Size = 13;
            ws.Cells[initialRow, 5].Value = "Doanh thu tại quán: " + report.InStoreAmount;
            ws.Cells[initialRow, 5].Style.Font.Bold = true;
            ws.Cells[initialRow, 5].Style.Font.Size = 13;
            ws.Cells[initialRow, 6].Value = "Doanh thu mang di: " + report.TakeAwayAmount;
            ws.Cells[initialRow, 6].Style.Font.Bold = true;
            ws.Cells[initialRow, 6].Style.Font.Size = 13;
            ws.Cells[initialRow, 7].Value = "Doanh thu giao hàng: " + report.DeliAmount;
            ws.Cells[initialRow, 7].Style.Font.Bold = true;
            ws.Cells[initialRow, 7].Style.Font.Size = 13;
            ++initialRow;
            ws.Cells[initialRow, 1].Value = "Đơn tiền mặt: " + report.TotalCash;
            ws.Cells[initialRow, 1].Style.Font.Bold = true;
            ws.Cells[initialRow, 1].Style.Font.Size = 13;
            ws.Cells[initialRow, 2].Value = "Đơn MOMO: " + report.TotalMomo;
            ws.Cells[initialRow, 2].Style.Font.Bold = true;
            ws.Cells[initialRow, 2].Style.Font.Size = 13;
            ws.Cells[initialRow, 3].Value = "Đơn Chuyển khoản: " + report.TotalBanking;
            ws.Cells[initialRow, 3].Style.Font.Bold = true;
            ws.Cells[initialRow, 3].Style.Font.Size = 13;
            ws.Cells[initialRow, 5].Value = "Doanh thu tiền mặt: " + report.CashAmount;
            ws.Cells[initialRow, 5].Style.Font.Bold = true;
            ws.Cells[initialRow, 5].Style.Font.Size = 13;
            ws.Cells[initialRow, 6].Value = "Doanh thu MOMO: " + report.MomoAmount;
            ws.Cells[initialRow, 6].Style.Font.Bold = true;
            ws.Cells[initialRow, 6].Style.Font.Size = 13;
            ws.Cells[initialRow, 7].Value = "Doanh thu chuyển khoản: " + report.BankingAmount;
            ws.Cells[initialRow, 7].Style.Font.Bold = true;
            ws.Cells[initialRow, 7].Style.Font.Size = 13;

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream(await package.GetAsByteArrayAsync());

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName =
                    $"Report_{store.Name}_{startDate:dd/MM/yyyy}-{startDate:dd/MM/yyyy}.xlsx" // Specify the desired file name
            };
        }
    }
}
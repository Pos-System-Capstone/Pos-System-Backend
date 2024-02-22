using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Extensions;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response.Brands;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements;

public class BrandService : BaseService<BrandService>, IBrandService
{
    public BrandService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<BrandService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
    }

    public async Task<CreateNewBrandResponse> CreateNewBrand(CreateNewBrandRequest newBrandRequest)
    {
        _logger.LogInformation($"Create new brand with {newBrandRequest.Name}");
        Brand newBrand = _mapper.Map<Brand>(newBrandRequest);
        newBrand.Status = BrandStatus.Active.GetDescriptionFromEnum();
        newBrand.Id = Guid.NewGuid();
        await _unitOfWork.GetRepository<Brand>().InsertAsync(newBrand);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
        CreateNewBrandResponse createNewBrandResponse = null;
        if (isSuccessful)
        {
            createNewBrandResponse = _mapper.Map<CreateNewBrandResponse>(newBrand);
        }

        return createNewBrandResponse;
    }

    public async Task<IPaginate<GetBrandResponse>> GetBrands(string? searchBrandName, int page, int size)
    {
        searchBrandName = searchBrandName?.Trim().ToLower();
        IPaginate<GetBrandResponse> brands = await _unitOfWork.GetRepository<Brand>().GetPagingListAsync(
            selector: x => new GetBrandResponse(x.Id, x.Name, x.Email, x.Address, x.Phone, x.PicUrl,
                EnumUtil.ParseEnum<BrandStatus>(x.Status), x.Stores.Count),
            predicate: string.IsNullOrEmpty(searchBrandName)
                ? x => true
                : x => x.Name.ToLower().Contains(searchBrandName),
            include: x => x.Include(x => x.Stores),
            page: page,
            size: size);
        return brands;
    }

    public async Task<GetBrandResponse> GetBrandById(Guid brandId)
    {
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        GetBrandResponse brandResponse = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            selector: x => new GetBrandResponse(x.Id, x.Name, x.Email, x.Address, x.Phone, x.PicUrl,
                EnumUtil.ParseEnum<BrandStatus>(x.Status), x.Stores.Count),
            predicate: x => x.Id.Equals(brandId),
            include: x => x.Include(x => x.Stores)
        );
        return brandResponse;
    }

    public async Task<bool> UpdateBrandInformation(Guid brandId, UpdateBrandRequest updateBrandRequest)
    {
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>()
            .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
        _logger.LogInformation($"Start update brand {brandId}");
        updateBrandRequest.TrimString();
        brand.Name = string.IsNullOrEmpty(updateBrandRequest.Name) ? brand.Name : updateBrandRequest.Name;
        brand.Email = string.IsNullOrEmpty(updateBrandRequest.Email) ? brand.Email : updateBrandRequest.Email;
        brand.Address = string.IsNullOrEmpty(updateBrandRequest.Address) ? brand.Address : updateBrandRequest.Address;
        brand.Phone = string.IsNullOrEmpty(updateBrandRequest.Phone) ? brand.Phone : updateBrandRequest.Phone;
        brand.PicUrl = string.IsNullOrEmpty(updateBrandRequest.PicUrl) ? brand.PicUrl : updateBrandRequest.PicUrl;
        _unitOfWork.GetRepository<Brand>().UpdateAsync(brand);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
        return isSuccessful;
    }

    public async Task<GetMenuDetailForStaffResponse> GetMenus(string? brandCode)
    {
        if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>()
            .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

        Menu menu = await _unitOfWork.GetRepository<Menu>()
            .SingleOrDefaultAsync(predicate: x => x.BrandId.Equals(brand.Id) && x.Priority == 0);


        //Filter Menu, make sure it return correct menu in specific time
        List<MenuStore> allMenuAvailable = (List<MenuStore>) await _unitOfWork.GetRepository<MenuStore>()
            .GetListAsync(predicate: x => x.MenuId.Equals(menu.Id),
                include: x => x
                    .Include(x => x.Menu)
                    .Include(x => x.Store).ThenInclude(x => x.Brand)
            );
        if (!allMenuAvailable.Any()) throw new BadHttpRequestException(MessageConstant.Menu.NoMenusFoundMessage);

        DateTime currentSEATime = TimeUtils.GetCurrentSEATime();
        DateFilter currentDay = DateTimeHelper.GetDateFromDateTime(currentSEATime);
        TimeOnly currentTime = TimeOnly.FromDateTime(currentSEATime);

        GetMenuDetailForStaffResponse menuOfStore = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
            selector: x => new GetMenuDetailForStaffResponse(
                x.Id,
                x.BrandId,
                x.Code,
                x.Priority,
                true,
                x.DateFilter,
                x.StartTime,
                x.EndTime),
            predicate: x => x.Id.Equals(menu.Id) && x.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum()));

        menuOfStore.ProductsInMenu = (List<ProductDataForStaff>) await _unitOfWork.GetRepository<MenuProduct>()
            .GetListAsync(
                selector: x => new ProductDataForStaff
                (
                    x.ProductId,
                    x.Product.Code,
                    x.Product.Name,
                    x.SellingPrice,
                    x.Product.PicUrl,
                    x.Product.Status,
                    x.HistoricalPrice,
                    x.DiscountPrice,
                    x.Product.Description,
                    x.Product.DisplayOrder,
                    x.Product.Size,
                    x.Product.Type,
                    x.Product.ParentProductId,
                    x.Product.BrandId,
                    x.Product.CategoryId,
                    (List<Guid>) x.Product.CollectionProducts.Select(x => x.CollectionId),
                    (List<Guid>) x.Product.Category.ExtraCategoryProductCategories.Select(x => x.ExtraCategoryId),
                    x.Id //This is the menuProductId in response body
                ),
                predicate: x =>
                    x.MenuId.Equals(menu.Id) &&
                    x.Status.Equals(MenuProductStatus.Active.GetDescriptionFromEnum()),
                include: menuProduct => menuProduct
                    .Include(menuProduct => menuProduct.Product)
                    .ThenInclude(product => product.CollectionProducts)
                    .Include(menuProduct => menuProduct.Product)
                    .ThenInclude(product => product.Category)
                    .ThenInclude(category => category.ExtraCategoryProductCategories)
            );

        menuOfStore.CollectionsOfBrand = (List<CollectionOfBrand>) await _unitOfWork.GetRepository<Collection>()
            .GetListAsync(selector: x => new CollectionOfBrand(
                    x.Id,
                    x.Name,
                    x.Code,
                    x.PicUrl,
                    x.Description
                ),
                predicate: x =>
                    x.BrandId.Equals(brand.Id) && x.Status == CollectionStatus.Active.GetDescriptionFromEnum());

        menuOfStore.CategoriesOfBrand = (List<CategoryOfBrand>) await _unitOfWork.GetRepository<Category>()
            .GetListAsync(selector: x => new CategoryOfBrand(
                    x.Id,
                    x.Code,
                    x.Name,
                    EnumUtil.ParseEnum<CategoryType>(x.Type),
                    (List<Guid>) x.ExtraCategoryProductCategories.Select(e => e.ExtraCategoryId),
                    x.DisplayOrder,
                    x.Description,
                    x.PicUrl
                ),
                predicate: x =>
                    x.BrandId.Equals(brand.Id) && x.Status.Equals(CategoryStatus.Active.GetDescriptionFromEnum()));

        //Use to filter which productInGroups is added to menu
        List<Guid> productIdsInMenu = menuOfStore.ProductsInMenu.Select(x => x.Id).ToList();

        menuOfStore.groupProductInMenus = (List<GroupProductInMenu>) await _unitOfWork.GetRepository<GroupProduct>()
            .GetListAsync(
                x => new GroupProductInMenu
                {
                    Id = x.Id,
                    ComboProductId = (Guid) x.ComboProductId,
                    Name = x.Name,
                    CombinationMode = EnumUtil.ParseEnum<GroupCombinationMode>(x.CombinationMode),
                    Priority = x.Priority,
                    Quantity = x.Quantity,
                    Status = EnumUtil.ParseEnum<GroupProductStatus>(x.Status),
                },
                predicate: x =>
                    x.ComboProduct.BrandId.Equals(brand.Id) &&
                    x.Status.Equals(GroupProductStatus.Active.GetDescriptionFromEnum()),
                include: x => x.Include(x => x.ComboProduct)
            );

        menuOfStore.productInGroupList = (List<ProductsInGroupResponse>) await _unitOfWork
            .GetRepository<ProductInGroup>().GetListAsync(
                selector: x => new ProductsInGroupResponse
                {
                    Id = x.Id,
                    GroupProductId = x.GroupProductId,
                    ProductId = x.ProductId,
                    Priority = x.Priority,
                    AdditionalPrice = x.AdditionalPrice,
                    Min = x.Min,
                    Max = x.Max,
                    Quantity = x.Quantity,
                    Status = EnumUtil.ParseEnum<ProductInGroupStatus>(x.Status)
                },
                predicate: x => x.Product.BrandId.Equals(brand.Id)
                                && productIdsInMenu.Contains(x.ProductId)
                                && x.Status.Equals(ProductInGroupStatus.Active.GetDescriptionFromEnum())
                                && x.Product.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum()),
                include: x => x.Include(x => x.Product)
            );

        foreach (GroupProductInMenu groupProduct in menuOfStore.groupProductInMenus)
        {
            groupProduct.ProductsInGroupIds = (List<Guid>) menuOfStore.productInGroupList
                .Where(x => x.GroupProductId.Equals(groupProduct.Id))
                .Select(x => x.Id).ToList();
        }

        return menuOfStore;
    }

    public async Task<IPaginate<ViewOrdersResponse>> GetOrderInBrand(Guid brandId, int page, int size,
        DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? status, PaymentTypeEnum? paymentType)
    {
        RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        Guid currentUserBrandId = Guid.Parse(GetBrandIdFromJwt());
        if (currentUserBrandId != brandId)
            throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
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
            predicate: BuildGetOrdersInBrandQuery(brandId, startDate, endDate, orderType, status, paymentType),
            include: x => x.Include(s => s.OrderSource).Include(v => v.Session).ThenInclude(p => p.Store),
            orderBy: x =>
                x.OrderByDescending(x => x.CheckInDate),
            page: page,
            size: size
        );
        return ordersResponse;
    }

    private Expression<Func<Order, bool>> BuildGetOrdersInBrandQuery(Guid brandId, DateTime? startDate,
        DateTime? endDate, OrderType? orderType, OrderStatus? status, PaymentTypeEnum? paymentType)
    {
        Expression<Func<Order, bool>> filterQuery = p => p.Session.Store.BrandId.Equals(brandId);
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
}
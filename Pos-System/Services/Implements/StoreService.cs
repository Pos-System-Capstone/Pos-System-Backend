using System.Runtime.Intrinsics.X86;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Request.Stores;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements;

public class StoreService : BaseService<StoreService>, IStoreService
{
    private readonly IMenuService _menuService;

    public StoreService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<StoreService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor, IMenuService menuService) : base(unitOfWork, logger, mapper,
        httpContextAccessor)
    {
        _menuService = menuService;
    }

    public async Task<IPaginate<GetStoreResponse>> GetStoresInBrand(Guid brandId, string? searchShortName, int page,
        int size)
    {
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        searchShortName = searchShortName?.Trim().ToLower();
        IPaginate<GetStoreResponse> storesInBrandResponse = await _unitOfWork.GetRepository<Store>().GetPagingListAsync(
            selector: x => new GetStoreResponse(x.Id, x.BrandId, x.Name, x.ShortName, x.Email, x.Address, x.Status,
                x.WifiName, x.WifiPassword),
            predicate: string.IsNullOrEmpty(searchShortName)
                ? x => x.BrandId.Equals(brandId)
                : x => x.BrandId.Equals(brandId) && x.ShortName.ToLower().Contains(searchShortName),
            orderBy: x => x.OrderBy(x => x.ShortName),
            page: page,
            size: size
        );
        return storesInBrandResponse;
    }

    public async Task<GetStoreDetailResponse> GetStoreById(Guid storeId)
    {
        if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
        GetStoreDetailResponse storeDetailResponse = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
            selector: x => new GetStoreDetailResponse(x.Id, x.BrandId, x.Name, x.Name, x.Email, x.Address, x.Status,
                x.Phone, x.Code, x.Brand.PicUrl, x.WifiName, x.WifiPassword),
            include: x => x.Include(x => x.Brand),
            predicate: x => x.Id.Equals(storeId)
        );
        return storeDetailResponse;
    }

    public async Task<CreateNewStoreResponse> CreateNewStore(CreateNewStoreRequest newStoreRequest)
    {
        Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
        _logger.LogInformation($"Create new store with {newStoreRequest.Name}");
        Store newStore = _mapper.Map<Store>(newStoreRequest);
        newStore.Status = StoreStatus.Active.GetDescriptionFromEnum();
        newStore.Id = Guid.NewGuid();
        newStore.BrandId = userBrandId;
        HasBaseMenuResponse hasBaseMenu = await _menuService.CheckHasBaseMenuInBrand(userBrandId);
        if (hasBaseMenu.HasBaseMenu)
        {
            Menu brandBaseMenu = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
                predicate: menu => menu.BrandId.Equals(userBrandId) && menu.Priority == 0
            );
            MenuStore newMenuStore = new MenuStore()
            {
                Id = Guid.NewGuid(),
                MenuId = brandBaseMenu.Id,
                StoreId = newStore.Id
            };

            List<MenuStore> newMenuStores = new List<MenuStore>
            {
                newMenuStore
            };
            newStore.MenuStores = newMenuStores;
        }

        await _unitOfWork.GetRepository<Store>().InsertAsync(newStore);
        bool isSuccessfull = await _unitOfWork.CommitAsync() > 0;
        CreateNewStoreResponse createNewStoreResponse = null;
        if (isSuccessfull)
        {
            createNewStoreResponse = _mapper.Map<CreateNewStoreResponse>(newStore);
        }

        return createNewStoreResponse;
    }

    public async Task<IPaginate<GetStoreEmployeesResponse>> GetStoreEmployees(Guid storeId, string? searchUserName,
        int page, int size)
    {
        if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
        IPaginate<GetStoreEmployeesResponse> storeEmployees = new Paginate<GetStoreEmployeesResponse>();
        searchUserName = searchUserName?.Trim().ToLower();

        storeEmployees = await _unitOfWork.GetRepository<Account>().GetPagingListAsync(
            selector: x => new GetStoreEmployeesResponse()
            {
                Id = x.Id,
                Name = x.Name,
                Status = EnumUtil.ParseEnum<AccountStatus>(x.Status),
                Role = EnumUtil.ParseEnum<RoleEnum>(x.Role.Name),
                Username = x.Username,
            },
            predicate: string.IsNullOrEmpty(searchUserName)
                ? x => x.StoreAccount != null && x.StoreAccount.StoreId.Equals(storeId)
                : x => x.StoreAccount != null && x.StoreAccount.StoreId.Equals(storeId) &&
                       x.Username.ToLower().Contains(searchUserName),
            orderBy: x => x.OrderBy(x => x.Username),
            include: x => x.Include(x => x.StoreAccount).Include(x => x.Role),
            page: page,
            size: size);

        return storeEmployees;
    }

    public async Task<bool> UpdateStoreInformation(Guid storeId, UpdateStoreRequest updateStoreRequest)
    {
        if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
        Store storeForUpdate = await _unitOfWork.GetRepository<Store>()
            .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
        if (storeForUpdate == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
        _logger.LogInformation($"Start update store {storeId}");

        updateStoreRequest.TrimString();
        storeForUpdate.Name = string.IsNullOrEmpty(updateStoreRequest.Name)
            ? storeForUpdate.Name
            : updateStoreRequest.Name;
        storeForUpdate.ShortName = string.IsNullOrEmpty(updateStoreRequest.ShortName)
            ? storeForUpdate.ShortName
            : updateStoreRequest.ShortName;
        storeForUpdate.Email = string.IsNullOrEmpty(updateStoreRequest.Email)
            ? storeForUpdate.Email
            : updateStoreRequest.Email;
        storeForUpdate.Phone = string.IsNullOrEmpty(updateStoreRequest.Phone)
            ? storeForUpdate.Phone
            : updateStoreRequest.Phone;
        storeForUpdate.Code = string.IsNullOrEmpty(updateStoreRequest.Code)
            ? storeForUpdate.Code
            : updateStoreRequest.Code;
        storeForUpdate.Address = string.IsNullOrEmpty(updateStoreRequest.Address)
            ? storeForUpdate.Address
            : updateStoreRequest.Address;
        storeForUpdate.WifiName = string.IsNullOrEmpty(updateStoreRequest.WifiName)
            ? storeForUpdate.WifiName
            : updateStoreRequest.WifiName;
        storeForUpdate.WifiPassword = string.IsNullOrEmpty(updateStoreRequest.WifiPassword)
            ? storeForUpdate.WifiPassword
            : updateStoreRequest.WifiPassword;

        _unitOfWork.GetRepository<Store>().UpdateAsync(storeForUpdate);
        bool isSuccesful = await _unitOfWork.CommitAsync() > 0;

        return isSuccesful;
    }

    public async Task<GetMenuDetailForStaffResponse> GetMenuDetailForStaff()
    {
        Guid userStoreId = Guid.Parse(GetStoreIdFromJwt());
        if (userStoreId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Menu.MissingStoreIdMessage);

        //Filter Menu, make sure it return correct menu in specific time
        List<MenuStore> allMenuAvailable = (List<MenuStore>)await _unitOfWork.GetRepository<MenuStore>()
            .GetListAsync(predicate: x => x.StoreId.Equals(userStoreId)
                                          && x.Menu.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum())
                                          && x.Store.Brand.Status.Equals(BrandStatus.Active.GetDescriptionFromEnum()),
                include: x => x
                    .Include(x => x.Menu)
                    .Include(x => x.Store).ThenInclude(x => x.Brand)
            );
        if (!allMenuAvailable.Any()) throw new BadHttpRequestException(MessageConstant.Menu.NoMenusFoundMessage);

        DateTime currentSEATime = TimeUtils.GetCurrentSEATime();
        DateFilter currentDay = DateTimeHelper.GetDateFromDateTime(currentSEATime);
        TimeOnly currentTime = TimeOnly.FromDateTime(currentSEATime);

        List<MenuStore> menusAvailableInDay = new List<MenuStore>();
        foreach (var menu in allMenuAvailable)
        {
            //Find the menu available days and time
            List<DateFilter> menuAvailableDays = DateTimeHelper.GetDatesFromDateFilter(menu.Menu.DateFilter);
            TimeOnly menuStartTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.StartTime);
            TimeOnly menuEndTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.EndTime);
            if (menuAvailableDays.Contains(currentDay) && currentTime <= menuEndTime && currentTime >= menuStartTime)
                menusAvailableInDay.Add(menu);
        }

        //If there are more than 2 menus available take the highest priority one
        MenuStore menuAvailableWithHighestPriority =
            menusAvailableInDay.OrderByDescending(x => x.Menu.Priority).FirstOrDefault();
        if (menuAvailableWithHighestPriority == null)
            throw new BadHttpRequestException(MessageConstant.Menu.NoMenusAvailableMessage);
        Guid menuOfStoreId = menuAvailableWithHighestPriority.MenuId;

        Guid userBrandId = await _unitOfWork.GetRepository<Store>()
            .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(userStoreId));

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
            predicate: x => x.Id.Equals(menuOfStoreId) && x.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum()));

        menuOfStore.ProductsInMenu = (List<ProductDataForStaff>)await _unitOfWork.GetRepository<MenuProduct>()
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
                    (List<Guid>)x.Product.CollectionProducts.Select(x => x.CollectionId),
                    (List<Guid>)x.Product.Category.ExtraCategoryProductCategories.Select(x => x.ExtraCategoryId),
                    x.Id //This is the menuProductId in response body
                ),
                predicate: x =>
                    x.MenuId.Equals(menuOfStoreId) &&
                    x.Status.Equals(MenuProductStatus.Active.GetDescriptionFromEnum()),
                include: menuProduct => menuProduct
                    .Include(menuProduct => menuProduct.Product)
                    .ThenInclude(product => product.CollectionProducts)
                    .Include(menuProduct => menuProduct.Product)
                    .ThenInclude(product => product.Category)
                    .ThenInclude(category => category.ExtraCategoryProductCategories)
            );

        menuOfStore.CollectionsOfBrand = (List<CollectionOfBrand>)await _unitOfWork.GetRepository<Collection>()
            .GetListAsync(selector: x => new CollectionOfBrand(
                    x.Id,
                    x.Name,
                    x.Code,
                    x.PicUrl,
                    x.Description
                ),
                predicate: x =>
                    x.BrandId.Equals(userBrandId) && x.Status == CollectionStatus.Active.GetDescriptionFromEnum());

        menuOfStore.CategoriesOfBrand = (List<CategoryOfBrand>)await _unitOfWork.GetRepository<Category>()
            .GetListAsync(selector: x => new CategoryOfBrand(
                x.Id,
                x.Code,
                x.Name,
                EnumUtil.ParseEnum<CategoryType>(x.Type),
                x.DisplayOrder,
                x.Description,
                x.PicUrl
            ), predicate: x => x.BrandId.Equals(userBrandId));

        //Use to filter which productInGroups is added to menu
        List<Guid> productIdsInMenu = menuOfStore.ProductsInMenu.Select(x => x.Id).ToList();

        menuOfStore.groupProductInMenus = (List<GroupProductInMenu>)await _unitOfWork.GetRepository<GroupProduct>()
            .GetListAsync(
                x => new GroupProductInMenu
                {
                    Id = x.Id,
                    ComboProductId = (Guid)x.ComboProductId,
                    Name = x.Name,
                    CombinationMode = EnumUtil.ParseEnum<GroupCombinationMode>(x.CombinationMode),
                    Priority = x.Priority,
                    Quantity = x.Quantity,
                    Status = EnumUtil.ParseEnum<GroupProductStatus>(x.Status),
                },
                predicate: x =>
                    x.ComboProduct.BrandId.Equals(userBrandId) &&
                    x.Status.Equals(GroupProductStatus.Active.GetDescriptionFromEnum()),
                include: x => x.Include(x => x.ComboProduct)
            );

        menuOfStore.productInGroupList = (List<ProductsInGroupResponse>)await _unitOfWork
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
                predicate: x => x.Product.BrandId.Equals(userBrandId)
                                && productIdsInMenu.Contains(x.ProductId)
                                && x.Status.Equals(ProductInGroupStatus.Active.GetDescriptionFromEnum())
                                && x.Product.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum()),
                include: x => x.Include(x => x.Product)
            );

        foreach (GroupProductInMenu groupProduct in menuOfStore.groupProductInMenus)
        {
            groupProduct.ProductsInGroupIds = (List<Guid>)menuOfStore.productInGroupList
                .Where(x => x.GroupProductId.Equals(groupProduct.Id))
                .Select(x => x.Id).ToList();
        }

        return menuOfStore;
    }

    public async Task<GetStoreEndShiftStatisticsResponse> GetStoreEndShiftStatistics(Guid storeId, Guid sessionId)
    {
        Guid userStoreId = Guid.Parse(GetStoreIdFromJwt());
        if (userStoreId != storeId)
            throw new BadHttpRequestException(MessageConstant.Store.GetStoreSessionUnAuthorized);
        if (storeId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
        if (sessionId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Session.EmptySessionIdMessage);
        Store store = await _unitOfWork.GetRepository<Store>()
            .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
        if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
        Session session = await _unitOfWork.GetRepository<Session>()
            .SingleOrDefaultAsync(predicate: x => x.StoreId.Equals(storeId) && x.Id.Equals(sessionId));
        if (session == null) throw new BadHttpRequestException(MessageConstant.Session.SessionNotFoundMessage);

        GetStoreEndShiftStatisticsResponse result = new GetStoreEndShiftStatisticsResponse()
        {
            SessionId = session.Id,
            StartDateTime = session.StartDateTime,
            EndDateTime = session.EndDateTime,
            Name = session.Name ?? "",
            NumberOfOrders = session.NumberOfOrders,
            TotalAmount = session.TotalAmount ?? 0,
            TotalPromotion = session.TotalPromotion ?? 0,
            CurrentCashInVault = session.TotalChangeCash ?? 0,
            ProfitAmount = session.TotalFinalAmount ?? 0,
            TotalDiscountAmount = session.TotalDiscountAmount ?? 0,
        };

        return result;
    }

    public async Task<IPaginate<GetStoreResponse>> GetStoresInBrandByBrandCode(string? brandCode, string? storeCode, int page, int size)
    {
        if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>()
            .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

        Store store = await _unitOfWork.GetRepository<Store>() .SingleOrDefaultAsync(predicate: x => x.Code.Equals(storeCode));

        IPaginate<GetStoreResponse> storesInBrandResponse = null;

        if (store == null)
        {
            storesInBrandResponse = await _unitOfWork.GetRepository<Store>().GetPagingListAsync(
            selector: x => new GetStoreResponse(x.Id, x.BrandId, x.Name, x.ShortName, x.Email, x.Address, x.Status,
                x.WifiName, x.WifiPassword),
            predicate: x => x.BrandId.Equals(brand.Id),
            orderBy: x => x.OrderBy(x => x.ShortName),
            page: page,
            size: size
            );
        }
        else
        {
            storesInBrandResponse = await _unitOfWork.GetRepository<Store>().GetPagingListAsync(
            selector: x => new GetStoreResponse(x.Id, x.BrandId, x.Name, x.ShortName, x.Email, x.Address, x.Status,
                x.WifiName, x.WifiPassword),
            predicate: x => x.BrandId.Equals(brand.Id) && x.Id.Equals(store.Id),
            orderBy: x => x.OrderBy(x => x.ShortName),
            page: page,
            size: size
            );
        }

        return storesInBrandResponse;
    }
}
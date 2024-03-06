using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Menus;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class MenuService : BaseService<MenuService>, IMenuService
    {
        public MenuService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<MenuService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<Guid> CreateNewMenu(CreateNewMenuRequest createNewMenuRequest)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            _logger.LogInformation($"Create new Menu with menu code: {createNewMenuRequest.Code}");
            string currentUserName = GetUsernameFromJwt();
            DateTime now = DateTime.Now;
            Menu newMenu = new Menu()
            {
                Id = Guid.NewGuid(),
                Code = createNewMenuRequest.Code.Trim(),
                DateFilter = createNewMenuRequest.DateFilter,
                Priority = createNewMenuRequest.Priority,
                StartTime = createNewMenuRequest.StartTime,
                EndTime = createNewMenuRequest.EndTime,
                BrandId = brand.Id,
                CreatedBy = currentUserName,
                CreatedAt = now,
                Status = EnumUtil.GetDescriptionFromEnum(MenuStatus.Deactivate)
            };
            if (createNewMenuRequest.IsBaseMenu || createNewMenuRequest.Priority == 0)
            {
                HasBaseMenuResponse hasBaseMenu = await CheckHasBaseMenuInBrand(brandId);
                if (hasBaseMenu.HasBaseMenu)
                {
                    throw new BadHttpRequestException(MessageConstant.Menu.BaseMenuExistedMessage);
                }

                newMenu.Priority = 0; //Default priority of base menu is 0
                newMenu.MenuStores = new List<MenuStore>();
                IEnumerable<Guid> storesInBrand = await _unitOfWork.GetRepository<Store>()
                    .GetListAsync(
                        selector: x => x.Id,
                        predicate: x => x.BrandId.Equals(brand.Id));
                foreach (var storeGuid in storesInBrand)
                {
                    newMenu.MenuStores.Add(new MenuStore()
                    {
                        Id = Guid.NewGuid(),
                        MenuId = newMenu.Id,
                        StoreId = storeGuid
                    });
                }
            } //Create a base menu for brand that will apply for all stores

            List<MenuProduct> menuProductsToInsert = new List<MenuProduct>();
            if (createNewMenuRequest.IsUseBaseMenu)
            {
                if (createNewMenuRequest.Priority == 0)
                    throw new BadHttpRequestException(MessageConstant.Menu.CanNotUsePriorityAsBaseMenu);
                IEnumerable<MenuProduct> menuProducts = await _unitOfWork.GetRepository<MenuProduct>()
                    .GetListAsync(predicate: x => x.Menu.Priority == 0 && x.Menu.BrandId.Equals(brandId),
                        include: x => x.Include(x => x.Menu));

                menuProducts.ToList().ForEach(x => menuProductsToInsert.Add(new MenuProduct
                {
                    Id = Guid.NewGuid(),
                    Status = x.Status,
                    SellingPrice = x.SellingPrice,
                    DiscountPrice = x.DiscountPrice,
                    HistoricalPrice = x.HistoricalPrice,
                    MenuId = newMenu.Id,
                    ProductId = x.ProductId,
                    CreatedBy = currentUserName,
                    CreatedAt = now
                }));
            }

            await _unitOfWork.GetRepository<Menu>().InsertAsync(newMenu);
            await _unitOfWork.GetRepository<MenuProduct>().InsertRangeAsync(menuProductsToInsert);
            bool isSuccessfully = await _unitOfWork.CommitAsync() > 0;
            if (isSuccessfully) return newMenu.Id;
            return Guid.Empty;
        }

        public async Task<HasBaseMenuResponse> CheckHasBaseMenuInBrand(Guid brandId)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            IEnumerable<Menu> menus = await _unitOfWork.GetRepository<Menu>()
                .GetListAsync(predicate: x => x.BrandId.Equals(brand.Id));
            if (menus == null)
            {
                return new HasBaseMenuResponse()
                {
                    HasBaseMenu = false
                };
            } // brand đó chưa có menu nào

            if (menus.Any(x => x.Priority == 0))
            {
                return new HasBaseMenuResponse()
                {
                    HasBaseMenu = true
                };
            }
            else
            {
                return new HasBaseMenuResponse()
                {
                    HasBaseMenu = false
                };
            }
        }

        public async Task<IPaginate<GetMenuDetailResponse>> GetMenus(Guid brandId, string? code, int page = 1,
            int size = 10)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            code = code?.Trim();
            IPaginate<GetMenuDetailResponse> menusInBrand = await _unitOfWork.GetRepository<Menu>().GetPagingListAsync(
                selector: x => new GetMenuDetailResponse(x.Id, x.Code, x.Priority, x.DateFilter, x.StartTime, x.EndTime,
                    x.Status, x.CreatedBy, x.CreatedAt, x.UpdatedBy, x.UpdatedAt, x.MenuProducts.ToList(),
                    x.MenuStores.ToList()),
                predicate: string.IsNullOrEmpty(code)
                    ? x => x.BrandId.Equals(brandId)
                    : x => x.Code.ToLower().Equals(code) && x.BrandId.Equals(brandId),
                include: x =>
                    x.Include(x => x.MenuStores).ThenInclude(x => x.Store).Include(x => x.MenuProducts)
                        .ThenInclude(x => x.Product).ThenInclude(x => x.Category),
                page: page,
                size: size
            );
            return menusInBrand;
        }

        public async Task<GetMenuDetailResponse> GetMenuDetailInBrand(Guid menuId)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            List<MenuProduct> menuProducts = new List<MenuProduct>();

            GetMenuDetailResponse menuDetailResponse = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
                selector: menu => new GetMenuDetailResponse(menu.Id, menu.Code, menu.Priority, menu.DateFilter,
                    menu.StartTime, menu.EndTime,
                    menu.Status, menu.CreatedBy, menu.CreatedAt, menu.UpdatedBy, menu.UpdatedAt,
                    menuProducts, menu.MenuStores.ToList()),
                predicate: menu => menu.Id.Equals(menuId) && menu.BrandId.Equals(brandId),
                include: menu => menu
                    .Include(menu => menu.MenuStores)
                    .ThenInclude(menu => menu.Store)
            );

            menuProducts = (List<MenuProduct>) await _unitOfWork.GetRepository<MenuProduct>().GetListAsync(
                predicate: x =>
                    x.Status.Equals(MenuProductStatus.Active.GetDescriptionFromEnum()) && x.MenuId == menuId,
                include: x => x.Include(x => x.Product).ThenInclude(x => x.Category)
            );

            menuDetailResponse.SetProductsInMenu(menuProducts);

            if (menuDetailResponse == null)
            {
                throw new BadHttpRequestException(MessageConstant.Menu.BrandIdWithMenuIdIsNotExistedMessage);
            }

            return menuDetailResponse;
        }

        public async Task<Guid> UpdateMenuProducts(Guid menuId, UpdateMenuProductsRequest updateMenuProductsRequest)
        {
            if (menuId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Menu.EmptyMenuIdMessage);
            Menu menu = await _unitOfWork.GetRepository<Menu>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(menuId));
            if (menu == null) throw new BadHttpRequestException(MessageConstant.Menu.MenuNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            DateTime currentTime = DateTime.Now;

            List<MenuProduct> productsInMenu = (List<MenuProduct>) await _unitOfWork.GetRepository<MenuProduct>()
                .GetListAsync(predicate: x => x.MenuId.Equals(menuId));
            List<Product> currentProductsInSystem = (List<Product>) await _unitOfWork
                .GetRepository<Product>()
                .GetListAsync(predicate: x => x.BrandId.Equals(userBrandId));
            List<Guid> newProductIds = updateMenuProductsRequest.Products.Select(x => x.ProductId).ToList();
            List<Guid> oldProductIds = productsInMenu.Select(x => x.ProductId).ToList();
            (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedProductIds =
                CustomListUtil.splitIdsToAddAndRemove(oldProductIds, newProductIds);

            if (splittedProductIds.idsToAdd.Count > 0)
            {
                List<ProductToUpdate> productsToInsert = updateMenuProductsRequest.Products
                    .Where(x => splittedProductIds.idsToAdd.Contains(x.ProductId)).ToList();
                List<MenuProduct> prepareDataToInsert = new List<MenuProduct>();
                productsToInsert.ForEach(x =>
                {
                    Product referenceProductData = currentProductsInSystem.Find(y => y.Id.Equals(x.ProductId));
                    if (referenceProductData == null)
                        throw new BadHttpRequestException(MessageConstant.Menu.ProductNotInBrandMessage + x.ProductId);

                    prepareDataToInsert.Add(new MenuProduct
                    {
                        Id = Guid.NewGuid(),
                        Status = ProductStatus.Active.GetDescriptionFromEnum(),
                        SellingPrice =
                            (double) (x.SellingPrice == null ? referenceProductData.SellingPrice : x.SellingPrice),
                        DiscountPrice = (double) (x.DiscountPrice == null
                            ? referenceProductData.DiscountPrice
                            : x.DiscountPrice),
                        HistoricalPrice = referenceProductData.HistoricalPrice,
                        MenuId = menuId,
                        ProductId = x.ProductId,
                        CreatedBy = currentUserName,
                        CreatedAt = currentTime,
                    });
                });
                await _unitOfWork.GetRepository<MenuProduct>().InsertRangeAsync(prepareDataToInsert);
            }

            if (splittedProductIds.idsToKeep.Count > 0)
            {
                List<ProductToUpdate> productDataFromRequest = updateMenuProductsRequest.Products
                    .Where(x => splittedProductIds.idsToKeep.Contains(x.ProductId)).ToList();
                List<MenuProduct> productsToUpdate = productsInMenu
                    .Where(x => splittedProductIds.idsToKeep.Contains(x.ProductId)).ToList();
                productsToUpdate.ForEach(x =>
                {
                    //Get products in menu to with deactive status to check have the same productId with product is prepared to add to menu
                    List<MenuProduct> deactiveMenuProducts = productsInMenu.Where(menuProduct =>
                        menuProduct.Status == MenuProductStatus.Deactivate.ToString()).ToList();

                    List<MenuProduct> prepareDataToUpdate = new List<MenuProduct>();
                    MenuProduct deactivateMenuProductToUpdate =
                        deactiveMenuProducts.Find(deactiveMenuProduct => deactiveMenuProduct.ProductId == x.ProductId);

                    if (deactivateMenuProductToUpdate != null)
                    {
                        //Update status to active
                        deactivateMenuProductToUpdate.Status = MenuProductStatus.Active.ToString();
                        prepareDataToUpdate.Add(deactivateMenuProductToUpdate);
                    }

                    if (prepareDataToUpdate.Count > 0)
                    {
                        _unitOfWork.GetRepository<MenuProduct>().UpdateRange(prepareDataToUpdate);
                    }

                    ProductToUpdate requestProductData =
                        productDataFromRequest.Find(y => y.ProductId.Equals(x.ProductId));
                    //Get ref product data to update newest price from product
                    Product referenceProductData = currentProductsInSystem.Find(y => y.Id.Equals(x.ProductId));
                    if (referenceProductData == null)
                        throw new BadHttpRequestException(MessageConstant.Menu.ProductNotInBrandMessage + x.ProductId);
                    if (requestProductData == null) return;
                    x.SellingPrice = (double) (requestProductData.SellingPrice == null
                        ? referenceProductData.SellingPrice
                        : requestProductData.SellingPrice);
                    x.DiscountPrice = (double) (requestProductData.DiscountPrice == null
                        ? referenceProductData.DiscountPrice
                        : requestProductData.DiscountPrice);
                    x.UpdatedBy = currentUserName;
                    x.UpdatedAt = currentTime;
                });
                _unitOfWork.GetRepository<MenuProduct>().UpdateRange(productsToUpdate);
            }

            if (splittedProductIds.idsToRemove.Count > 0)
            {
                List<MenuProduct> prepareDataToRemove = (List<MenuProduct>) await _unitOfWork
                    .GetRepository<MenuProduct>().GetListAsync(
                        predicate: x => splittedProductIds.idsToRemove.Contains(x.ProductId)
                                        && x.MenuId.Equals(menuId));
                //Change status of product in menu from 'Active' to 'Deactivate' to remove product
                List<MenuProduct> finalDataToRemove = new List<MenuProduct>();
                foreach (var menuProductToChangeStatus in prepareDataToRemove)
                {
                    //Update status to deactive
                    menuProductToChangeStatus.Status = MenuProductStatus.Deactivate.ToString();
                    finalDataToRemove.Add(menuProductToChangeStatus);
                }

                _unitOfWork.GetRepository<MenuProduct>().UpdateRange(finalDataToRemove);
            }

            await _unitOfWork.CommitAsync();
            return menuId;
        }

        public async Task<IPaginate<GetProductInMenuResponse>> GetProductsInMenu(Guid menuId, string? productName,
            string? productCode, int page, int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            IPaginate<GetProductInMenuResponse> productsInMenu;

            if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(productCode))
            {
                productsInMenu = await _unitOfWork.GetRepository<MenuProduct>()
                    .GetPagingListAsync(
                        selector: product => new GetProductInMenuResponse(product.ProductId, product.Product.Name,
                            product.Product.Code, product.Product.PicUrl, product.SellingPrice,
                            product.HistoricalPrice, product.DiscountPrice, product.Product.Type),
                        predicate: product => product.MenuId.Equals(menuId) &&
                                              product.Product.BrandId.Equals(brandId) &&
                                              product.Product.Name.Contains(productName) &&
                                              product.Product.Code.Contains(productCode) &&
                                              product.Status == MenuProductStatus.Active.ToString(),
                        include: product => product.Include(product => product.Product),
                        orderBy: x => x.OrderBy(x => x.Product.Code),
                        page: page,
                        size: size
                    );
            }
            else if (!string.IsNullOrEmpty(productName))
            {
                productsInMenu = await _unitOfWork.GetRepository<MenuProduct>()
                    .GetPagingListAsync(
                        selector: product => new GetProductInMenuResponse(product.ProductId, product.Product.Name,
                            product.Product.Code, product.Product.PicUrl, product.SellingPrice,
                            product.HistoricalPrice, product.DiscountPrice, product.Product.Type),
                        predicate: product =>
                            product.MenuId.Equals(menuId) && product.Product.BrandId.Equals(brandId) &&
                            product.Product.Name.Contains(productName) &&
                            product.Status == MenuProductStatus.Active.ToString(),
                        include: product => product.Include(product => product.Product),
                        orderBy: x => x.OrderBy(x => x.Product.Code),
                        page: page,
                        size: size
                    );
            }
            else if (!string.IsNullOrEmpty(productCode))
            {
                productsInMenu = await _unitOfWork.GetRepository<MenuProduct>()
                    .GetPagingListAsync(
                        selector: product => new GetProductInMenuResponse(product.ProductId, product.Product.Name,
                            product.Product.Code, product.Product.PicUrl, product.SellingPrice,
                            product.HistoricalPrice, product.DiscountPrice, product.Product.Type),
                        predicate: product =>
                            product.MenuId.Equals(menuId) && product.Product.BrandId.Equals(brandId) &&
                            product.Product.Code.Contains(productCode) &&
                            product.Status == MenuProductStatus.Active.ToString(),
                        include: product => product.Include(product => product.Product),
                        orderBy: x => x.OrderBy(x => x.Product.Code),
                        page: page,
                        size: size
                    );
            }
            else
            {
                productsInMenu = await _unitOfWork.GetRepository<MenuProduct>()
                    .GetPagingListAsync(
                        selector: product => new GetProductInMenuResponse(product.ProductId, product.Product.Name,
                            product.Product.Code, product.Product.PicUrl, product.SellingPrice,
                            product.HistoricalPrice, product.DiscountPrice, product.Product.Type),
                        predicate: product =>
                            product.MenuId.Equals(menuId) && product.Product.BrandId.Equals(brandId) &&
                            product.Status == MenuProductStatus.Active.ToString(),
                        include: product => product.Include(product => product.Product),
                        orderBy: x => x.OrderBy(x => x.Product.Code),
                        page: page,
                        size: size
                    );
            }

            return productsInMenu;
        }

        public async Task<bool> UpdateMenuInformation(Guid menuId,
            UpdateMenuInformationRequest updateMenuInformationRequest)
        {
            Guid branId = Guid.Parse(GetBrandIdFromJwt());

            if (updateMenuInformationRequest.Priority == 0)
            {
                Menu brandBaseMenu = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
                    predicate: menu => menu.BrandId.Equals(branId) && menu.Priority == 0
                );
                if (brandBaseMenu != null)
                {
                    _logger.LogInformation($"Failed to update menu {menuId} because brand has base menu already");
                    throw new BadHttpRequestException(MessageConstant.Menu.BaseMenuIsExistedInBrandMessage);
                }
            }

            Menu currentMenu = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
                predicate: menu => menu.BrandId.Equals(branId) && menu.Id.Equals(menuId)
            );

            if (currentMenu == null)
            {
                _logger.LogInformation($"Failed to update menu {menuId} because of wrong menuId or brandId from JWT");
                throw new BadHttpRequestException(MessageConstant.Menu.BrandIdWithMenuIdIsNotExistedMessage);
            }

            if (updateMenuInformationRequest.StartTime.HasValue && updateMenuInformationRequest.EndTime.HasValue
                                                                && updateMenuInformationRequest.StartTime >
                                                                updateMenuInformationRequest.EndTime)
            {
                _logger.LogInformation($"Failed to update menu {menuId} because endtime is lower than starttime");
                throw new BadHttpRequestException(MessageConstant.Menu.EndTimeLowerThanStartTimeMessage);
            }

            if (updateMenuInformationRequest.StartTime.HasValue &&
                (updateMenuInformationRequest.StartTime > currentMenu.EndTime))
            {
                throw new BadHttpRequestException(MessageConstant.Menu
                    .StartTimeRequestBiggerThanCurrentMenuEndTimeMessage);
            }

            if (updateMenuInformationRequest.EndTime.HasValue &&
                (updateMenuInformationRequest.EndTime < currentMenu.StartTime))
            {
                throw new BadHttpRequestException(MessageConstant.Menu
                    .EndTimeRequestLowerThanCurrentMenuStartTimeMessage);
            }

            _logger.LogInformation($"Start update menu information of menu's {menuId} id");
            currentMenu.Priority = updateMenuInformationRequest.Priority ?? currentMenu.Priority;
            currentMenu.DateFilter = updateMenuInformationRequest.DateFilter ?? currentMenu.DateFilter;
            currentMenu.StartTime = updateMenuInformationRequest.StartTime ?? currentMenu.StartTime;
            currentMenu.EndTime = updateMenuInformationRequest.EndTime ?? currentMenu.EndTime;
            _unitOfWork.GetRepository<Menu>().UpdateAsync(currentMenu);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }

        public async Task<IPaginate<GetStoreDetailResponse>> GetStoresInMenu(Guid menuId, string? storeName, int page,
            int size)
        {
            if (menuId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Menu.EmptyMenuIdMessage);

            Menu currentMenuInSystem = await _unitOfWork.GetRepository<Menu>()
                .SingleOrDefaultAsync(predicate: menu => menu.Id.Equals(menuId));

            if (currentMenuInSystem == null)
                throw new BadHttpRequestException(MessageConstant.Menu.MenuNotFoundMessage);

            IPaginate<GetStoreDetailResponse> storesApplyMenu = await _unitOfWork.GetRepository<MenuStore>()
                .GetPagingListAsync(
                    selector: menuStore => new GetStoreDetailResponse(menuStore.Store.Id, menuStore.Store.BrandId,
                        menuStore.Store.Name, menuStore.Store.ShortName, menuStore.Store.Email,
                        menuStore.Store.Address,
                        menuStore.Store.Status, menuStore.Store.Phone, menuStore.Store.Code,
                        menuStore.Store.Brand.PicUrl, menuStore.Store.WifiName, menuStore.Store.WifiPassword,
                        menuStore.Store.Lat, menuStore.Store.Long),
                    predicate: string.IsNullOrEmpty(storeName)
                        ? menuStore => menuStore.MenuId.Equals(menuId)
                        : menuStore => menuStore.MenuId.Equals(menuId) &&
                                       menuStore.Store.Name.ToLower().Contains(storeName),
                    include: menuStore =>
                        menuStore.Include(menuStore => menuStore.Store).ThenInclude(store => store.Brand),
                    page: page,
                    size: size
                );
            return storesApplyMenu;
        }

        public async Task<Guid> UpdateStoresApplyMenu(Guid menuId,
            UpdateStoresApplyMenuRequest updateStoresApplyMenuRequest)
        {
            List<Guid> currentStoreIdsApplyMenu = (List<Guid>) await _unitOfWork.GetRepository<MenuStore>()
                .GetListAsync(
                    selector: menuStore => menuStore.StoreId,
                    predicate: menuStore => menuStore.MenuId.Equals(menuId)
                );

            (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedStoreIds =
                CustomListUtil.splitIdsToAddAndRemove(currentStoreIdsApplyMenu, updateStoresApplyMenuRequest.storeIds);

            if (splittedStoreIds.idsToAdd.Count > 0)
            {
                List<MenuStore> newMenuStoresToAdd = new List<MenuStore>();
                foreach (var storeId in splittedStoreIds.idsToAdd)
                {
                    MenuStore newMenuStoreToAdd = new MenuStore()
                    {
                        Id = Guid.NewGuid(),
                        StoreId = storeId,
                        MenuId = menuId
                    };
                    newMenuStoresToAdd.Add(newMenuStoreToAdd);
                }

                await _unitOfWork.GetRepository<MenuStore>().InsertRangeAsync(newMenuStoresToAdd);
            }

            if (splittedStoreIds.idsToRemove.Count > 0)
            {
                List<MenuStore> listMenuStoreToRemove = new List<MenuStore>();

                foreach (var storeIdToRemove in splittedStoreIds.idsToRemove)
                {
                    MenuStore menuStoreToRemove = await _unitOfWork.GetRepository<MenuStore>().SingleOrDefaultAsync(
                        selector: menuStore => new MenuStore()
                            {Id = menuStore.Id, MenuId = menuStore.MenuId, StoreId = menuStore.StoreId},
                        predicate: menuStore =>
                            menuStore.MenuId.Equals(menuId) && menuStore.StoreId.Equals(storeIdToRemove)
                    );
                    listMenuStoreToRemove.Add(menuStoreToRemove);
                }

                _unitOfWork.GetRepository<MenuStore>().DeleteRangeAsync(listMenuStoreToRemove);
            }

            await _unitOfWork.CommitAsync();
            return menuId;
        }

        public async Task<Guid> UpdateMenuStatus(Guid menuId, UpdateMenuStatusRequest updateMenuStatusRequest)
        {
            if (!updateMenuStatusRequest.Op.Equals("/update") || !updateMenuStatusRequest.Path.Equals("/status"))
                throw new BadHttpRequestException(MessageConstant.Menu.UpdateMenuStatusRequestWrongFormatMessage);
            bool isValidValue = Enum.TryParse(updateMenuStatusRequest.Value, out MenuStatus newStatus);
            if (!isValidValue)
                throw new BadHttpRequestException(MessageConstant.Menu.UpdateMenuStatusRequestWrongFormatMessage);

            if (menuId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Menu.EmptyMenuIdMessage);

            Menu menuForupdate = await _unitOfWork.GetRepository<Menu>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(menuId));

            if (menuForupdate == null) throw new BadHttpRequestException(MessageConstant.Menu.MenuNotFoundMessage);
            menuForupdate.Status = EnumUtil.GetDescriptionFromEnum(newStatus);
            _unitOfWork.GetRepository<Menu>().UpdateAsync(menuForupdate);
            await _unitOfWork.CommitAsync();
            return menuId;
        }
    }
}
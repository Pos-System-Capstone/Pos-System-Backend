using Pos_System.API.Payload.Request.Menus;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IMenuService
    {
        public Task<Guid> CreateNewMenu(CreateNewMenuRequest createNewMenuRequest);
        public Task<HasBaseMenuResponse> CheckHasBaseMenuInBrand(Guid brandId);
        public Task<IPaginate<GetMenuDetailResponse>> GetMenus(Guid brandId, string? code, int page = 1, int size = 10);
        public Task<Guid> UpdateMenuProducts(Guid menuId, UpdateMenuProductsRequest updateMenuProductsRequest);
        public Task<GetMenuDetailResponse> GetMenuDetailInBrand(Guid menuId);
        public Task<IPaginate<GetProductInMenuResponse>> GetProductsInMenu(Guid menuId, string? productName, string? productCode, int page, int size);
        public Task<bool> UpdateMenuInformation(Guid menuId, UpdateMenuInformationRequest updateMenuInformationRequest);
        public Task<IPaginate<GetStoreDetailResponse>> GetStoresInMenu(Guid menuId, string? storeName, int page, int size);
        public Task<Guid> UpdateStoresApplyMenu(Guid menuId, UpdateStoresApplyMenuRequest updateStoresApplyMenuRequest);
        public Task<Guid> UpdateMenuStatus(Guid menuId, UpdateMenuStatusRequest updateMenuStatusRequest);
    }
}

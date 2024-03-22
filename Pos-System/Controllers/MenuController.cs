using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Menus;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    public class MenuController : BaseController<MenuController>
    {
        private readonly IMenuService _menuService;

        public MenuController(ILogger<MenuController> logger, IMenuService menuService) : base(logger)
        {
            _menuService = menuService;
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Menu.MenusEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewMenu(CreateNewMenuRequest createNewMenuRequest)
        {
            Guid newMenuIdResponse = await _menuService.CreateNewMenu(createNewMenuRequest);
            if (newMenuIdResponse == Guid.Empty)
            {
                _logger.LogInformation($"Create menu failed with menuCode: {createNewMenuRequest.Code}");
                return BadRequest(MessageConstant.Menu.CreateNewMenuFailedMessage);
            }

            _logger.LogInformation($"Create menu successfully with menuCode: {createNewMenuRequest.Code}");
            return Ok(newMenuIdResponse);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Menu.HasBaseMenuEndPoint)]
        [ProducesResponseType(typeof(HasBaseMenuResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckHasBaseMenu(Guid id)
        {
            var response = await _menuService.CheckHasBaseMenuInBrand(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Menu.MenusInBrandEndPoint)]
        public async Task<IActionResult> GetMenusInBrand(Guid id, [FromQuery] string? code, [FromQuery] int page,
            [FromQuery] int size)
        {
            var response = await _menuService.GetMenus(id, code, page, size);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Menu.MenuProductsEndpoint)]
        public async Task<IActionResult> UpdateMenuProducts(Guid menuId,
            UpdateMenuProductsRequest updateMenuProductsRequest)
        {
            Guid response = await _menuService.UpdateMenuProducts(menuId, updateMenuProductsRequest);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Menu.MenuEndPoint)]
        [ProducesResponseType(typeof(GetMenuDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMenuDetailInBrand(Guid menuId)
        {
            var response = await _menuService.GetMenuDetailInBrand(menuId);
            _logger.LogInformation($"Get menu detail with menuId: {menuId}");
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Menu.MenuProductsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetProductInMenuResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductInMenu(Guid menuId, [FromQuery] string? name,
            [FromQuery] string? code, [FromQuery] int page, [FromQuery] int size)
        {
            var response = await _menuService.GetProductsInMenu(menuId, name, code, page, size);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin, RoleEnum.StoreManager)]
        [HttpPatch(ApiEndPointConstant.Menu.MenuEndPoint)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMenuInformation(Guid menuId,
            UpdateMenuInformationRequest updateMenuInformationRequest)
        {
            bool isUpdateSuccessful = await _menuService.UpdateMenuInformation(menuId, updateMenuInformationRequest);
            if (isUpdateSuccessful)
            {
                return Ok(MessageConstant.Menu.UpdateMenuInformationSuccessfulMessage);
            }

            return BadRequest(MessageConstant.Menu.UpdateMenuInformationFailedMessage);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Menu.MenuStoresEndPoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreDetailResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoresInMenu(Guid menuId, [FromQuery] string? name, [FromQuery] int page,
            [FromQuery] int size)
        {
            var response = await _menuService.GetStoresInMenu(menuId, name, page, size);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Menu.MenuStoresEndPoint)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMenuStoreList(Guid menuId,
            UpdateStoresApplyMenuRequest updateStoresApplyMenuRequest)
        {
            var response = await _menuService.UpdateStoresApplyMenu(menuId, updateStoresApplyMenuRequest);
            return Ok(response);
        }

        // [CustomAuthorize(RoleEnum.BrandAdmin)]
        // [HttpPatch(ApiEndPointConstant.Menu.MenuEndPoint)]
        // [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        // public async Task<IActionResult> UpdateMenuStatus(Guid menuId, UpdateMenuStatusRequest updateMenuStatusRequest)
        // {
        //     var response = await _menuService.UpdateMenuStatus(menuId, updateMenuStatusRequest);
        //     return Ok(response);
        // }
    }
}
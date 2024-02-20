using System.Net;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response;
using Pos_System.API.Payload.Response.Brands;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Implements;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class BrandController : BaseController<BrandController>
    {
        private readonly IBrandService _brandService;
        private readonly IAccountService _accountService;
        private readonly IStoreService _storeService;
        private readonly ICategoryService _categoryService;
        private readonly ICollectionService _collectionService;
        private readonly IReportService _reportService;

        public BrandController(ILogger<BrandController> logger, IBrandService brandService,
            IAccountService accountService, IStoreService storeService, ICategoryService categoryService,
            ICollectionService collectionService, IReportService reportService) : base(logger)
        {
            _brandService = brandService;
            _accountService = accountService;
            _storeService = storeService;
            _categoryService = categoryService;
            _collectionService = collectionService;
            _reportService = reportService;
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpPost(ApiEndPointConstant.Brand.BrandsEndpoint)]
        [ProducesResponseType(typeof(CreateNewBrandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNewBrand(CreateNewBrandRequest createNewBrandRequest)
        {
            CreateNewBrandResponse response = await _brandService.CreateNewBrand(createNewBrandRequest);
            if (response == null)
            {
                _logger.LogError($"Create new brand failed with {createNewBrandRequest.Name}");
                return Problem($"{MessageConstant.Brand.CreateBrandFailMessage}: {createNewBrandRequest.Name}");
            }

            _logger.LogInformation($"Create new brand successful with {createNewBrandRequest.Name}");
            return CreatedAtAction(nameof(CreateNewBrand), response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpPost(ApiEndPointConstant.Brand.BrandAccountEndpoint)]
        [ProducesResponseType(typeof(CreateNewBrandAccountResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateBrandAccount(CreateNewBrandAccountRequest createNewBrandAccountRequest)
        {
            CreateNewBrandAccountResponse response =
                await _accountService.CreateNewBrandAccount(createNewBrandAccountRequest);
            if (response == null)
            {
                _logger.LogError(
                    $"Create new brand account failed: brand {createNewBrandAccountRequest.BrandId} with account {createNewBrandAccountRequest.Username}");
                return Problem(MessageConstant.Account.CreateBrandAccountFailMessage);
            }

            _logger.LogInformation(
                $"Create brand account successfully with brand: {createNewBrandAccountRequest.BrandId}, account: {createNewBrandAccountRequest.Username}");
            return CreatedAtAction(nameof(CreateBrandAccount), response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.BrandAccountEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetAccountResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ViewBrandsAccounts(Guid id, [FromQuery] string? username,
            [FromQuery] RoleEnum? role, [FromQuery] int page, [FromQuery] int size)
        {
            var accountsInBrand = await _accountService.GetBrandAccounts(id, username, role, page, size);
            return Ok(accountsInBrand);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.BrandsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBrandResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBrands([FromQuery] string? name, [FromQuery] int page,
            [FromQuery] int size)
        {
            var brands = await _brandService.GetBrands(name, page, size);
            return Ok(brands);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Brand.BrandEndpoint)]
        [ProducesResponseType(typeof(GetBrandResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBrandById(Guid id)
        {
            var brandResponse = await _brandService.GetBrandById(id);
            return Ok(brandResponse);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandManager, RoleEnum.BrandAdmin, RoleEnum.StoreManager)]
        [HttpGet(ApiEndPointConstant.Brand.StoresInBrandEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoresInBrand(Guid id, [FromQuery] string? shortName, [FromQuery] int page,
            [FromQuery] int size)
        {
            var storesInBrandResponse = await _storeService.GetStoresInBrand(id, shortName, page, size);
            return Ok(storesInBrandResponse);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Brand.BrandEndpoint)]
        public async Task<IActionResult> UpdateBrandInformation(Guid id, UpdateBrandRequest updateBrandRequest)
        {
            bool isSuccessful = await _brandService.UpdateBrandInformation(id, updateBrandRequest);
            if (isSuccessful)
            {
                _logger.LogInformation($"Update Brand {id} information successfully");
                return Ok(MessageConstant.Brand.UpdateBrandSuccessfulMessage);
            }

            _logger.LogInformation($"Update Brand {id} information failed");
            return Ok(MessageConstant.Brand.UpdateBrandFailedMessage);
        }

        [HttpGet(ApiEndPointConstant.Brand.GetCategoriesInBrand)]
        [ProducesResponseType(typeof(IPaginate<GetStoreResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(Guid brandId, [FromQuery] string? name,
            [FromQuery] CategoryType? type,
            [FromQuery] int page,
            [FromQuery] int size)
        {
            var storesInBrandResponse = await _categoryService.GetCategories(name, type, page, size);
            return Ok(storesInBrandResponse);
        }

        [HttpGet(ApiEndPointConstant.Brand.ExportStoreEndDateReport)]
        [ProducesResponseType(typeof(GetStoreEndDayReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportStoreEndDayReport(string? storeCode, string? brandCode,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var response = await _reportService.GetStoreEndDayReport(storeCode, brandCode, startDate, endDate);
            return Ok(response);
        }


        [HttpGet(ApiEndPointConstant.Brand.BrandMenuEndpoint)]
        public async Task<IActionResult> GetMenusOfBrand([FromQuery] string? brandCode)
        {
            var response = await _brandService.GetMenus(brandCode);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Brand.StoresInBrandCodeEndpoint)]
        public async Task<IActionResult> GetStoresInBrand([FromQuery] string? brandCode, [FromQuery] int page,
            [FromQuery] int size)
        {
            var response = await _storeService.GetStoresInBrandByBrandCode(brandCode, page, size);
            return Ok(response);
        }


        [CustomAuthorize(RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Brand.BrandOrdersEndpoint)]
        [ProducesResponseType(typeof(IPaginate<ViewOrdersResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderOfBrand(Guid id, [FromQuery] int page, [FromQuery] int size,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] OrderType? orderType,
            [FromQuery] OrderStatus? status, PaymentTypeEnum? paymentType)
        {
            var response =
                await _brandService.GetOrderInBrand(id, page, size, startDate, endDate, orderType, status, paymentType);
            return Ok(response);
        }
    }
}
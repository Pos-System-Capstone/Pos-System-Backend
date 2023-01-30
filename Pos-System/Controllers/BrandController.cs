using System.Net;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response;
using Pos_System.API.Payload.Response.Brands;
using Pos_System.API.Payload.Response.Stores;
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
        public BrandController(ILogger<BrandController> logger, IBrandService brandService, IAccountService accountService, IStoreService storeService) : base(logger)
        {
            _brandService = brandService;
            _accountService = accountService;
            _storeService = storeService;
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
            CreateNewBrandAccountResponse response = await _accountService.CreateNewBrandAccount(createNewBrandAccountRequest);
            if (response == null)
            {
                _logger.LogError($"Create new brand account failed: brand {createNewBrandAccountRequest.BrandId} with account {createNewBrandAccountRequest.Username}");
                return Problem(MessageConstant.Account.CreateBrandAccountFailMessage);
            }
            _logger.LogInformation($"Create brand account successfully with brand: {createNewBrandAccountRequest.BrandId}, account: {createNewBrandAccountRequest.Username}");
            return CreatedAtAction(nameof(CreateBrandAccount), response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.BrandAccountEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetAccountResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ViewBrandsAccounts(Guid id, [FromQuery] string? searchUsername, [FromQuery] RoleEnum role, [FromQuery] int page, [FromQuery] int size)
        {

            var accountsInBrand = await _accountService.GetBrandAccounts(id, searchUsername, role, page, size);
            return Ok(accountsInBrand);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.BrandsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBrandResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBrands([FromQuery] string? searchBrandName, [FromQuery] int page,
            [FromQuery] int size)
        {
            var brands = await _brandService.GetBrands(searchBrandName, page, size);
            return Ok(brands);
        }

        [CustomAuthorize(RoleEnum.SysAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.BrandEndpoint)]
        [ProducesResponseType(typeof(GetBrandResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBrandById(Guid id)
        {
            var brandResponse = await _brandService.GetBrandById(id);
            return Ok(brandResponse);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Brand.StoresInBrandEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoresInBrand(Guid id, [FromQuery] string? searchShortName, [FromQuery] int page, [FromQuery] int size)
        {
            var storesInBrandResponse = await _storeService.GetStoresInBrand(id, searchShortName, page, size);
            return Ok(storesInBrandResponse);
        }

        [CustomAuthorize(RoleEnum.BrandManager)]
        [HttpPut(ApiEndPointConstant.Brand.BrandEndpoint)]
        public async Task<IActionResult> UpdateBrandInformation(Guid id, [FromBody] UpdateBrandRequest brandRequest)
        {
            await _brandService.UpdateBrandInformation(id, brandRequest);
            return Ok();
        }
    }
}

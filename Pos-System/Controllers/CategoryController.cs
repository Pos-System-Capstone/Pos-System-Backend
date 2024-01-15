using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Categories;
using Pos_System.API.Payload.Response.Categories;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class CategoryController : BaseController<CategoryController>
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ILogger<CategoryController> logger, ICategoryService categoryService) : base(logger)
        {
            _categoryService = categoryService;
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Category.CategoriesEndpoint)]
        [ProducesResponseType(typeof(CreateNewCategoryRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewCategory(CreateNewCategoryRequest request)
        {
            bool isSuccessful = await _categoryService.CreateNewCategory(request);
            if (!isSuccessful) return Ok(MessageConstant.Category.CreateNewCategoryFailedMessage);
            return Ok(request);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Category.CategoriesEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories([FromQuery] string? name, [FromQuery] CategoryType? type, [FromQuery] int page, [FromQuery] int size)
        {
            var response = await _categoryService.GetCategories(name, type, page, size);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Category.CategoryEndpoint)]
        [ProducesResponseType(typeof(GetCategoryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            _logger.LogInformation($"Get Category by Id: {id}");
            var response = await _categoryService.GetCategoryById(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Category.CategoryEndpoint)]
        public async Task<IActionResult> UpdateCategoryInformation(Guid id, UpdateCategoryRequest updateCategoryRequest)
        {
            bool isSuccessfuly = await _categoryService.UpdateCategory(id, updateCategoryRequest);
            if (!isSuccessfuly) return BadRequest(MessageConstant.Category.UpdateCategoryFailedMessage);
            return Ok(MessageConstant.Category.UpdateCategorySuccessfulMessage);
        }


        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Category.ExtraCategoryEndpoint)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddExtraCategoriesToNormalCategory(Guid categoryId, List<Guid> request)
        {
            bool isSuccessful = await _categoryService.AddExtraCategoriesToNormalCategory(categoryId, request);
            if (!isSuccessful) return Ok(MessageConstant.Category.UpdateExtraCategoryFailedMessage);
            return Ok(MessageConstant.Category.UpdateExtraCategorySuccessfulMessage);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Category.ExtraCategoryEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetCategoryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExtraCategoriesByCategoryId(Guid categoryId, [FromQuery] int page, [FromQuery] int size)
        {
            var response = await _categoryService.GetExtraCategoriesByCategoryId(categoryId, page, size);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Category.ProductsInCategoryEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetProductsInCategory>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductsInCategory(Guid categoryId, [FromQuery] int page, [FromQuery] int size)
        {
            var response = await _categoryService.GetProductsInCategory(categoryId, page, size);
            return Ok(response);
        }
    }
}

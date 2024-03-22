using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Products;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class ProductController : BaseController<ProductController>
    {
        private readonly IProductService _productService;

        public ProductController(ILogger<ProductController> logger, IProductService productService) : base(logger)
        {
            _productService = productService;
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Product.ProductsEndPoint)]
        [ProducesResponseType(typeof(CreateNewProductResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewProduct(CreateNewProductRequest createNewProductRequest)
        {
            _logger.LogInformation($"Start to create new product with {createNewProductRequest}");
            var response = await _productService.CreateNewProduct(createNewProductRequest);
            if (response == null)
            {
                _logger.LogInformation(
                    $"Create new product failed: {createNewProductRequest.Name}, {createNewProductRequest.Code}");
                return Ok(MessageConstant.Product.CreateNewProductFailedMessage);
            }

            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Product.ProductsEndPoint)]
        [ProducesResponseType(typeof(IPaginate<GetProductResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts([FromQuery] string? name, [FromQuery] ProductType? type,
            [FromQuery] int page, [FromQuery] int size)
        {
            var productsResponse = await _productService.GetProducts(name, type, page, size);
            return Ok(productsResponse);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Product.ProductEndPoint)]
        [ProducesResponseType(typeof(GetProductDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            _logger.LogInformation($"Get Category by Id: {id}");
            var response = await _productService.GetProductById(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Product.ProductEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProductInformation(Guid id, UpdateProductRequest updateProductRequest)
        {
            _logger.LogInformation($"Start to update product with product id: {id}");
            Guid response = await _productService.UpdateProduct(id, updateProductRequest);
            if (response == Guid.Empty)
            {
                _logger.LogInformation(
                    $"Update product failed: {updateProductRequest.Name}, {updateProductRequest.Code}");
                return Ok(MessageConstant.Product.UpdateProductFailedMessage);
            }

            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Product.ProductsInBrandEndPoint)]
        [ProducesResponseType(typeof(GetProductDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductsInBrand(Guid id)
        {
            var response = await _productService.GetProductsInBrand(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Product.GroupProductsInBrandEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewGroupProduct(Guid id,
            CreateNewGroupProductRequest createUpdateNewGroupProductRequest)
        {
            var response = await _productService.CreateNewGroupProduct(id, createUpdateNewGroupProductRequest);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Product.GroupProductInBrandEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateGroupProduct(Guid brandId, Guid id,
            UpdateGroupProductRequest updateGroupProductRequest)
        {
            var response = await _productService.UpdateGroupProduct(brandId, id, updateGroupProductRequest);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.Product.GroupProductOfComboEndPoint)]
        [ProducesResponseType(typeof(List<GetGroupProductListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGroupProductsOfCombo(Guid brandId, Guid id)
        {
            var response = await _productService.GetGroupProductListOfCombo(brandId, id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Product.ProductInGroupEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSingleProductInGroup(Guid groupProductId, Guid id,
            UpdateProductInGroupRequest updateProductInGroupRequest)
        {
            var response = await _productService.UpdateProductInGroup(groupProductId, id, updateProductInGroupRequest);
            return Ok(response);
        }


        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.ProductVariant.ProductVariantsEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewProductVariant(CreatNewProductVariantRequest createNewProductRequest)
        {
            _logger.LogInformation($"Start to create new product with {createNewProductRequest}");
            var response = await _productService.CreateNewProductVariant(createNewProductRequest);
            if (response == null)
            {
                _logger.LogInformation(
                    $"Create new product failed: {createNewProductRequest.Name}, {createNewProductRequest.Name}");
                return Ok(MessageConstant.ProductVariant.CreateNewProductVariantFailedMessage);
            }

            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.ProductVariant.ProductVariantsEndPoint)]
        [ProducesResponseType(typeof(IPaginate<VariantDetailsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductVarians([FromQuery] string? name,
            [FromQuery] int page, [FromQuery] int size)
        {
            var productsResponse = await _productService.GetProductVariants(name, page, size);
            return Ok(productsResponse);
        }


        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpGet(ApiEndPointConstant.ProductVariant.ProductVariantEndPoint)]
        [ProducesResponseType(typeof(VariantDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductVariantById(Guid id)
        {
            _logger.LogInformation($"Get pRODUCT vARIANT by Id: {id}");
            var response = await _productService.GetProductVariantByiD(id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.ProductVariant.ProductVariantEndPoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProductVariantInformation(Guid id,
            UpdateProductVariantRequest updateProductRequest)
        {
            _logger.LogInformation($"Start to update product with product id: {id}");
            Guid response = await _productService.UpdateProductVariants(id, updateProductRequest);
            if (response != Guid.Empty) return Ok(response);
            _logger.LogInformation(
                $"Update product VARIANT failed: {updateProductRequest.Name}");
            return Ok(MessageConstant.ProductVariant.UpdateProductVariantFailedMessage);
        }

        [CustomAuthorize(RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Product.VariantInProductEndpoint)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddVariantToProduct(Guid productId, List<Guid> request)
        {
            await _productService.AddVariantToProduct(productId, request);
            return Ok(MessageConstant.Category.UpdateExtraCategorySuccessfulMessage);
        }
    }
}
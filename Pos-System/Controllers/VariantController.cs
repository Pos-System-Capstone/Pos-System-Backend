using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Payload.Request.Variant;
using Pos_System.API.Payload.Request.Vsriant;
using Pos_System.API.Services.Interfaces;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class VariantController : BaseController<VariantController>
    {
        private readonly IVariantService _variantService;

        public VariantController(ILogger<VariantController> logger, IVariantService variantService) : base(logger)
        {
            _variantService = variantService;
        }

        public async Task<IActionResult> CreateNewVariant(Guid brandId, CreateNewVariantRequest createNewVariantRequest)
        {
            var response = await _variantService.CreateNewVariant(brandId, createNewVariantRequest);
            if (response == null)
            {
                return Ok(MessageConstant.Variant.CreateVariantFailedMessage);
            }
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Variant.VariantsEndpoint)]
        public async Task<IActionResult> GetListVariant(Guid brandId)

        {
            var ListVariant = await _variantService.GetListVariant(brandId);
            return Ok(ListVariant);
        }

        [HttpPatch(ApiEndPointConstant.Variant.VariantEndpoint)]
        public async Task<IActionResult> UpdateVariant(Guid id, Guid brandId, [FromBody]UpdateVariantRequest updateVariantRequest)
        {
            bool isSuccessful = await _variantService.UpdateVariant(brandId, id, updateVariantRequest);

            if (isSuccessful)
            {
                _logger.LogInformation($"Update Variant {id} information successfully");
                return Ok(MessageConstant.Variant.UpdateVariantSuccessfulMessage);
            }

            _logger.LogInformation($"Update Variant {id} information failed");
            return Ok(MessageConstant.Variant.UpdateVariantFailedMessage);
        }

        [HttpPatch(ApiEndPointConstant.Variant.RemoveVariantEndpoint)]
        public async Task<IActionResult> RemoveVariant(Guid id, Guid brandId)
        {
            bool isSuccessful = await _variantService.RemoveVariant(brandId, id);

            if (isSuccessful)
            {
                _logger.LogInformation($"Remove Variant {id} information successfully");
                return Ok(MessageConstant.Variant.RemoveVariantSuccessfulMessage);
            }

            _logger.LogInformation($"Remove Variant {id} information failed");
            return Ok(MessageConstant.Variant.RemoveVariantFailedMessage);
        }

        [HttpPost(ApiEndPointConstant.Variant.MapProductEndpoint)]
        public async Task<IActionResult> CreateProductMapping(Guid id, Guid productId, Guid brandId)
        {
            bool isSuccessful = await _variantService.CreateProductMap(id, productId, brandId);

            if (isSuccessful)
            {
                _logger.LogInformation($"Create product mapping of product {id} successfully");
                return Ok(MessageConstant.Variant.CreateProductMappingSuccessfulMessage);
            }

            _logger.LogInformation($"Create product mapping of product {id} failed");
            return Ok(MessageConstant.Variant.CreateProductMappingFailedMessage);
        }
    }
}

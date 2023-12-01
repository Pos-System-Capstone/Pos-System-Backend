using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
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

        public async Task<IActionResult> GetListVariant(Guid id, Guid brandId)
        {
            return Ok();
        }

        public async Task<IActionResult> CreateVariant(Guid id, Guid brandId)
        {
            return Ok();
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

        public async Task<IActionResult> RemoveVariant(Guid id, Guid brandId)
        {
            return Ok();
        }
    }
}

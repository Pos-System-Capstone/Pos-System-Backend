using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> UpdateVariant(Guid id, Guid brandId)
        {
            return Ok();
        }

        public async Task<IActionResult> RemoveVariant(Guid id, Guid brandId)
        {
            return Ok();
        }
    }
}

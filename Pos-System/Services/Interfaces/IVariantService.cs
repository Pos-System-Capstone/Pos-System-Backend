using Pos_System.API.Payload.Request.Variant;
using Pos_System.API.Payload.Request.Vsriant;
using Pos_System.API.Payload.Response.Variant;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IVariantService
    {
        Task<IPaginate<GetListVariantResponse>> GetListVariant(Guid brandId);

        Task<CreateNewVariantResponse> CreateNewVariant(Guid brandId, CreateNewVariantRequest createNewVariantRequest);

        Task<bool> UpdateVariant(Guid brandId, Guid variandId, UpdateVariantRequest updateVariantRequest);

        Task<bool> RemoveVariant(Guid brandId, Guid variandId);

        Task<bool> CreateProductMap(Guid variantId, Guid productId, Guid brandId);
    }
}

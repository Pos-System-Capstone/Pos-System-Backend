using Pos_System.API.Payload.Request.Vsriant;

namespace Pos_System.API.Services.Interfaces
{
    public interface IVariantService
    {
        Task<bool> UpdateVariant(Guid brandId, Guid variandId, UpdateVariantRequest updateVariantRequest);
    }
}

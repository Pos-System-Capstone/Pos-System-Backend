using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Payload.Request.Vsriant;
using Pos_System.API.Services.Interfaces;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class VariantService : BaseService<VariantService>,  IVariantService
    {
        public VariantService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<VariantService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        } 

        public async Task<bool> UpdateVariant(Guid brandId, Guid variandId, UpdateVariantRequest updateVariantRequest)
        {
            //Check validation of inputed IDs
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (variandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Variant.EmptyVariantIdMessage);
            Variant variant = await _unitOfWork.GetRepository<Variant>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(variandId));
            if (variant == null) throw new BadHttpRequestException(MessageConstant.Variant.VariantNotFoundMessage);

            //Update data
            variant.Name = string.IsNullOrEmpty(updateVariantRequest.Name) ? variant.Name : updateVariantRequest.Name;
            _unitOfWork.GetRepository<Variant>().UpdateAsync(variant);

            bool isSuccess = await _unitOfWork.CommitAsync() > 0;
            return isSuccess;
        }
    }
}

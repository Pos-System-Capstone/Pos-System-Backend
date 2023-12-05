using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Variant;
using Pos_System.API.Payload.Request.Vsriant;
using Pos_System.API.Payload.Response.Variant;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class VariantService : BaseService<VariantService>, IVariantService
    {
        public VariantService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<VariantService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<IPaginate<GetListVariantResponse>> GetListVariant(Guid brandId)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            ICollection<VariantOption> options = await _unitOfWork.GetRepository<VariantOption>().GetListAsync();

            IPaginate<GetListVariantResponse> ListVariantResponse = await _unitOfWork.GetRepository<Variant>().GetPagingListAsync
                (
                    selector: x => new GetListVariantResponse(x.Id, x.Name, x.Status, options),
                    predicate: x => x.Status.Equals(EnumUtil.GetDescriptionFromEnum(VariantStatus.Active)));
            

            return ListVariantResponse;
        }

        public async Task<CreateNewVariantResponse> CreateNewVariant(Guid brandId, CreateNewVariantRequest createNewVariantRequest)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            Variant newVariant = new Variant()
            {
                Id = Guid.NewGuid(),
                Name = createNewVariantRequest.VariantName,
                Status = EnumUtil.GetDescriptionFromEnum(VariantStatus.Active),
                BrandId = brandId,
            };
            await _unitOfWork.GetRepository<Variant>().InsertAsync(newVariant);
            _unitOfWork.Commit();
            return new CreateNewVariantResponse(newVariant.Id);
        }

        public async Task<bool> UpdateVariant(Guid brandId, Guid variantId, UpdateVariantRequest updateVariantRequest)
        {
            //Check validation of inputed IDs
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (variantId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Variant.EmptyVariantIdMessage);
            Variant variant = await _unitOfWork.GetRepository<Variant>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(variantId));
            if (variant == null) throw new BadHttpRequestException(MessageConstant.Variant.VariantNotFoundMessage);

            //Update data
            variant.Name = string.IsNullOrEmpty(updateVariantRequest.Name) ? variant.Name : updateVariantRequest.Name;
            _unitOfWork.GetRepository<Variant>().UpdateAsync(variant);

            bool isSuccess = await _unitOfWork.CommitAsync() > 0;
            return isSuccess;
        }

        public async Task<bool> RemoveVariant(Guid brandId, Guid variandId)
        {
            //Check validation of inputed IDs
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (variandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Variant.EmptyVariantIdMessage);
            Variant variant = await _unitOfWork.GetRepository<Variant>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(variandId));
            if (variant == null) throw new BadHttpRequestException(MessageConstant.Variant.VariantNotFoundMessage);

            //Update data
            variant.Status = EnumUtil.GetDescriptionFromEnum(VariantStatus.Deactive);
            _unitOfWork.GetRepository<Variant>().UpdateAsync(variant);

            bool isSuccess = await _unitOfWork.CommitAsync() > 0;
            return isSuccess;
        }

        public async Task<bool> CreateProductMap(Guid variantId, Guid productId, Guid brandId)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            if (variantId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Variant.EmptyVariantIdMessage);
            Variant variant = await _unitOfWork.GetRepository<Variant>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(variantId));
            if (variant == null) throw new BadHttpRequestException(MessageConstant.Variant.VariantNotFoundMessage);

            if (productId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Variant.EmptyVariantIdMessage);
            Product product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(productId));
            if (product == null) throw new BadHttpRequestException(MessageConstant.Product.ProductNotFoundMessage);

            VariantProductMapping map = new VariantProductMapping() 
            {
                Id = Guid.NewGuid(),
                GroupVariantId = variantId,
                ProductId = productId,
            };

            await _unitOfWork.GetRepository<VariantProductMapping>().InsertAsync(map);
            bool result = _unitOfWork.Commit() > 0;
            return result;
        }
    }
}

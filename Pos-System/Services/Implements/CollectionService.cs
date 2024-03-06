using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Collections;
using Pos_System.API.Payload.Response.Collections;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class CollectionService : BaseService<CollectionService>, ICollectionService
    {
        public CollectionService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<CollectionService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {

        }
        public async Task<GetCollectionDetailResponse> GetCollectionById(Guid collectionId, string? productName, string? productCode, int page, int size)
        {
            if (collectionId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Collection.EmptyCollectionIdMessage);

            productName = productName?.Trim();
            productCode = productCode?.Trim();

            _logger.LogInformation($"Get Collection with collection: {collectionId}");

            Collection collectionData = await _unitOfWork.GetRepository<Collection>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(collectionId));
            GetCollectionDetailResponse collectionResponse = new GetCollectionDetailResponse(
                collectionData.Id,
                collectionData.Name,
                collectionData.Code,
                EnumUtil.ParseEnum<CollectionStatus>(collectionData.Status),
                collectionData.PicUrl,
                collectionData.Description);

            if (collectionResponse == null) throw new BadHttpRequestException(MessageConstant.Collection.CollectionNotFoundMessage);

            List<Guid> productIds = (List<Guid>)await _unitOfWork.GetRepository<CollectionProduct>().GetListAsync(
                selector: x => x.ProductId,
                predicate: x => x.CollectionId.Equals(collectionId) && x.Status.Equals(CollectionStatus.Active.GetDescriptionFromEnum())
                );

            collectionResponse.brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: x => new BrandOfCollection(x.Id, x.Name, x.Email, x.Address, x.Phone, x.PicUrl, x.Status),
                predicate: x => x.Id.Equals(collectionData.BrandId));

            collectionResponse.Products = await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
                             selector: x => new ProductOfCollection(x.Id, x.Name, x.Description, x.Code, x.PicUrl, x.SellingPrice),
                             predicate: x => !string.IsNullOrWhiteSpace(productName) && !string.IsNullOrWhiteSpace(productCode) ? x.Name.ToLower().Contains(productName.ToLower())
                                                 && x.Code.ToLower().Contains(productCode.ToLower()) && productIds.Contains(x.Id) && x.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum())
                                             : !string.IsNullOrWhiteSpace(productName) ? x.Name.ToLower().Contains(productName.ToLower()) && productIds.Contains(x.Id) && x.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum())
                                             : !string.IsNullOrWhiteSpace(productCode) ? x.Code.ToLower().Contains(productCode.ToLower()) && productIds.Contains(x.Id) && x.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum())
                                             : productIds.Contains(x.Id) && x.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum()),
                             page: page,
                             size: size);

            return collectionResponse;
        }

        public async Task<bool> UpdateCollectionInformation(Guid collectionId, UpdateCollectionInformationRequest collectionInformationRequest)
        {
            if (collectionId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Collection.EmptyCollectionIdMessage);

            Collection collectionForupdate = await _unitOfWork.GetRepository<Collection>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(collectionId));

            if (collectionForupdate == null) throw new BadHttpRequestException(MessageConstant.Collection.CollectionNotFoundMessage);

            _logger.LogInformation($"Updating Collection with collection: {collectionId}");
            collectionInformationRequest.TrimString();
            collectionForupdate.Name = string.IsNullOrEmpty(collectionInformationRequest.Name) ? collectionForupdate.Name : collectionInformationRequest.Name;
            collectionForupdate.Code = string.IsNullOrEmpty(collectionInformationRequest.Code) ? collectionForupdate.Code : collectionInformationRequest.Code;
            collectionForupdate.Description = string.IsNullOrEmpty(collectionInformationRequest.Description) ? collectionForupdate.Description : collectionInformationRequest.Description;
            collectionForupdate.PicUrl = collectionInformationRequest.PicUrl;
            collectionForupdate.BrandId = (Guid)((collectionInformationRequest.brandId == Guid.Empty || collectionInformationRequest.brandId == null) ? collectionForupdate.BrandId : collectionInformationRequest.brandId);

            _unitOfWork.GetRepository<Collection>().UpdateAsync(collectionForupdate);


            if (collectionInformationRequest.ProductIds != null)
            {
                List<Guid> currentProductIds = (List<Guid>)await _unitOfWork.GetRepository<CollectionProduct>().GetListAsync(
                    selector: x => x.ProductId,
                    predicate: x => x.CollectionId.Equals(collectionId)
                    );

                List<Guid> productIdsRequest = new List<Guid>(collectionInformationRequest.ProductIds);

                (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedProductIds = CustomListUtil.splitIdsToAddAndRemove(currentProductIds, productIdsRequest);
                //Handle add and remove to database
                if (splittedProductIds.idsToAdd.Count > 0)
                {
                    List<CollectionProduct> collectionProductsToInsert = new List<CollectionProduct>();
                    splittedProductIds.idsToAdd.ForEach(id => collectionProductsToInsert.Add(new CollectionProduct
                    {
                        Id = Guid.NewGuid(),
                        CollectionId = collectionId,
                        ProductId = id,
                        Status = CollectionStatus.Active.GetDescriptionFromEnum()
                    }));
                    await _unitOfWork.GetRepository<CollectionProduct>().InsertRangeAsync(collectionProductsToInsert);
                }

                if (splittedProductIds.idsToRemove.Count > 0)
                {
                    List<CollectionProduct> collectionProductsToDelete = new List<CollectionProduct>();
                    collectionProductsToDelete = (List<CollectionProduct>)await _unitOfWork.GetRepository<CollectionProduct>()
                        .GetListAsync(predicate: x => x.CollectionId.Equals(collectionId) && splittedProductIds.idsToRemove.Contains(x.ProductId));

                    _unitOfWork.GetRepository<CollectionProduct>().DeleteRangeAsync(collectionProductsToDelete);
                }


            }
            bool isSuccesful = await _unitOfWork.CommitAsync() > 0;
            return isSuccesful;
        }

        public async Task<CreateNewCollectionResponse> CreateNewCollection(CreateNewCollectionRequest createNewCollectionRequest)
        {
            createNewCollectionRequest.TrimString();
            var brandId = Guid.Parse(GetBrandIdFromJwt());
            Collection newCollection = new Collection()
            {
                Id = Guid.NewGuid(),
                Code = createNewCollectionRequest.Code,
                Name = createNewCollectionRequest.Name,
                BrandId = brandId,
                Description = createNewCollectionRequest?.Description,
                PicUrl = createNewCollectionRequest?.PicUrl,
                Status = CollectionStatus.Active.GetDescriptionFromEnum(),

            };
            await _unitOfWork.GetRepository<Collection>().InsertAsync(newCollection);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccessful) return null;
            return new CreateNewCollectionResponse(newCollection.Id, newCollection.Name, newCollection.Code, newCollection.Status, newCollection.Description, newCollection.PicUrl, newCollection.BrandId);
        }

        public async Task<IPaginate<GetCollectionResponse>> GetCollections(string? name, int page, int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            name = name?.Trim();
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            IPaginate<GetCollectionResponse> collectionsResponse = await _unitOfWork.GetRepository<Collection>().GetPagingListAsync(
                selector: x => new GetCollectionResponse(x.Id, x.Name, x.Code, x.Status, x.Description, x.PicUrl, x.BrandId),
                predicate: string.IsNullOrEmpty(name) ? x => x.BrandId.Equals(brandId) : x => x.Name.ToLower().Contains(name.ToLower()) && x.BrandId.Equals(brandId),
                page: page,
                size: size
                );
            return collectionsResponse;
        }

        public async Task<IPaginate<GetProductResponse>> GetProductInCollection(Guid collectionId, string? name, int page, int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            _logger.LogInformation($"Get GetProductInCollection from CollectionId: {collectionId}");
            name = name?.Trim();
            List<Guid> productInCollectionIds = (List<Guid>)await _unitOfWork.GetRepository<CollectionProduct>().GetListAsync(
                 selector: x => x.ProductId,
                 predicate: x => x.CollectionId.Equals(collectionId)
                 );

            IPaginate<GetProductResponse> productsResponse =
                await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
                    selector: x => new GetProductResponse(x.Id, x.Code, x.Name, x.PicUrl, x.Status, x.Type),
                    predicate: x => string.IsNullOrEmpty(name) ? x.BrandId.Equals(brandId) && productInCollectionIds.Contains(x.Id) : x.BrandId.Equals(brandId) && productInCollectionIds.Contains(x.Id) && x.Name.Contains(name),
                    orderBy: x => x.OrderByDescending(x => x.DisplayOrder),
                    page: page,
                    size: size
                    );
            return productsResponse;
        }

        public async Task<bool> AddProductsToCollection(Guid collectionId, List<Guid> request)
        {
            _logger.LogInformation($"Add ProductsToCollection: {collectionId}");
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            List<Guid> currentProductIds = (List<Guid>)await _unitOfWork.GetRepository<CollectionProduct>().GetListAsync(
                     selector: x => x.ProductId,
                     predicate: x => x.CollectionId.Equals(collectionId)
                     );

            (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedProductIds = CustomListUtil.splitIdsToAddAndRemove(currentProductIds, request);
            //Handle add and remove to database
            if (splittedProductIds.idsToAdd.Count > 0)
            {
                List<CollectionProduct> collectionProductsToInsert = new List<CollectionProduct>();
                splittedProductIds.idsToAdd.ForEach(id => collectionProductsToInsert.Add(new CollectionProduct
                {
                    Id = Guid.NewGuid(),
                    CollectionId = collectionId,
                    ProductId = id,
                    Status = CollectionStatus.Active.GetDescriptionFromEnum()
                }));
                await _unitOfWork.GetRepository<CollectionProduct>().InsertRangeAsync(collectionProductsToInsert);
            }

            if (splittedProductIds.idsToRemove.Count > 0)
            {
                List<CollectionProduct> collectionProductsToDelete = new List<CollectionProduct>();
                collectionProductsToDelete = (List<CollectionProduct>)await _unitOfWork.GetRepository<CollectionProduct>()
                    .GetListAsync(predicate: x => x.CollectionId.Equals(collectionId) && splittedProductIds.idsToRemove.Contains(x.ProductId));

                _unitOfWork.GetRepository<CollectionProduct>().DeleteRangeAsync(collectionProductsToDelete);
            }
            bool isSuccesful = await _unitOfWork.CommitAsync() > 0;
            return isSuccesful;
        }

        public async Task<Guid> UpdateCollectionStatus(Guid collectionId, UpdateCollectionStatusRequest updateCollectionStatusRequest)
        {
            if (!updateCollectionStatusRequest.Op.Equals("/update") || !updateCollectionStatusRequest.Path.Equals("/status"))
                throw new BadHttpRequestException(MessageConstant.Account.UpdateAccountStatusRequestWrongFormatMessage);
            bool isValidValue = Enum.TryParse(updateCollectionStatusRequest.Value, out CollectionStatus newStatus);
            if (!isValidValue)
                throw new BadHttpRequestException(MessageConstant.Account.UpdateAccountStatusRequestWrongFormatMessage);

            if (collectionId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Collection.EmptyCollectionIdMessage);

            Collection collectionForupdate = await _unitOfWork.GetRepository<Collection>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(collectionId));

            if (collectionForupdate == null) throw new BadHttpRequestException(MessageConstant.Collection.CollectionNotFoundMessage);
            collectionForupdate.Status = EnumUtil.GetDescriptionFromEnum(newStatus);
            _unitOfWork.GetRepository<Collection>().UpdateAsync(collectionForupdate);
            return collectionId;
        }
    }
}

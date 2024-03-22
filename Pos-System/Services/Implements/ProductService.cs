using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Products;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class ProductService : BaseService<ProductService>, IProductService
    {
        public ProductService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<ProductService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateNewProductResponse> CreateNewProduct(CreateNewProductRequest createNewProductRequest)
        {
            _logger.LogInformation($"Start create new : {createNewProductRequest}");
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            Category category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(Guid.Parse(createNewProductRequest.CategoryId)));
            if (category == null)
            {
                throw new BadHttpRequestException(MessageConstant.Category.CategoryNotFoundMessage);
            }

            Product newProduct = new Product()
            {
                Id = Guid.NewGuid(),
                Code = createNewProductRequest.Code,
                Name = createNewProductRequest.Name,
                BrandId = brandId,
                Description = createNewProductRequest?.Description,
                PicUrl = createNewProductRequest?.PicUrl,
                Status = ProductStatus.Active.GetDescriptionFromEnum(),
                CategoryId = Guid.Parse(createNewProductRequest.CategoryId),
                Size = createNewProductRequest.Size != null
                    ? createNewProductRequest.Size.GetDescriptionFromEnum()
                    : null,
                HistoricalPrice = createNewProductRequest.HistoricalPrice,
                SellingPrice = createNewProductRequest.SellingPrice,
                DiscountPrice = (double) (createNewProductRequest.DiscountPrice == null
                    ? 0
                    : createNewProductRequest.DiscountPrice),
                DisplayOrder = createNewProductRequest.DisplayOrder,
                Type = createNewProductRequest.Type.GetDescriptionFromEnum(),
                ParentProductId = createNewProductRequest.ParentProductId != null
                    ? Guid.Parse(createNewProductRequest?.ParentProductId)
                    : null
            };
            await _unitOfWork.GetRepository<Product>().InsertAsync(newProduct);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccessful) return null;
            return new CreateNewProductResponse(newProduct.Id);
        }

        public async Task<IPaginate<GetProductResponse>> GetProducts(string? name, ProductType? type, int page,
            int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            name = name?.Trim();
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            IPaginate<GetProductResponse> productsResponse = await _unitOfWork.GetRepository<Product>()
                .GetPagingListAsync(
                    selector: x => new GetProductResponse(x.Id, x.Code, x.Name, x.PicUrl, x.SellingPrice,
                        x.DiscountPrice, x.HistoricalPrice, x.Status, x.Type),
                    predicate: string.IsNullOrEmpty(name) && (type == null)
                        ? x => x.BrandId.Equals(brandId)
                        : ((type == null)
                            ? x => x.BrandId.Equals(brandId) && x.Name.Contains(name)
                            : (string.IsNullOrEmpty(name)
                                ? x => x.BrandId.Equals(brandId) && x.Type.Equals(type.GetDescriptionFromEnum())
                                : x => x.BrandId.Equals(brandId) && x.Name.ToLower().Contains(name) &&
                                       x.Type.Equals(type.GetDescriptionFromEnum()))),
                    page: page,
                    size: size,
                    orderBy: x => x.OrderBy(x => x.Code)
                );
            return productsResponse;
        }

        public async Task<GetProductDetailsResponse> GetProductById(Guid id)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Product.EmptyProductIdMessage);
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            GetProductDetailsResponse productResponse = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                selector: x => new GetProductDetailsResponse(x.Id, x.Code, x.Name, x.SellingPrice, x.PicUrl, x.Status,
                    x.HistoricalPrice, x.DiscountPrice, x.Description, x.DisplayOrder, x.Size, x.Type,
                    x.ParentProductId, x.BrandId, x.CategoryId),
                predicate: x => x.Id.Equals(id) && x.BrandId.Equals(brandId)
            );
            if (productResponse == null)
                throw new BadHttpRequestException(MessageConstant.Product.ProductNotFoundMessage);
            return productResponse;
        }

        public async Task<Guid> UpdateProduct(Guid productId, UpdateProductRequest updateProductRequest)
        {
            _logger.LogInformation($"Start updating product: {productId}");
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            Category category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(updateProductRequest.CategoryId));
            if (category == null) throw new BadHttpRequestException(MessageConstant.Category.CategoryNotFoundMessage);

            Product updateProduct = await _unitOfWork.GetRepository<Product>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(productId));
            if (updateProduct == null)
                throw new BadHttpRequestException(MessageConstant.Product.ProductNotFoundMessage);

            updateProduct.Code = updateProductRequest.Code;
            updateProduct.Name = updateProductRequest.Name;
            updateProduct.Description = updateProductRequest.Description;
            updateProduct.PicUrl = updateProductRequest.PicUrl;
            updateProduct.CategoryId = updateProductRequest.CategoryId;
            updateProduct.Size = updateProductRequest.Size != null
                ? updateProductRequest.Size.GetDescriptionFromEnum()
                : updateProduct.Size;
            updateProduct.HistoricalPrice = updateProductRequest.HistoricalPrice;
            updateProduct.SellingPrice = updateProductRequest.SellingPrice;
            updateProduct.DiscountPrice =
                (double) (updateProductRequest.DiscountPrice == null ? 0 : updateProductRequest.DiscountPrice);
            updateProduct.DisplayOrder = updateProductRequest.DisplayOrder;
            updateProduct.Type = updateProductRequest.Type.GetDescriptionFromEnum();
            updateProduct.ParentProductId = updateProductRequest.ParentProductId;
            updateProduct.Status = string.IsNullOrEmpty(updateProductRequest.Status)
                ? updateProduct.Status
                : updateProductRequest.Status;

            _unitOfWork.GetRepository<Product>().UpdateAsync(updateProduct);
            await _unitOfWork.CommitAsync();
            return productId;
        }

        public async Task<IEnumerable<GetProductDetailsResponse>> GetProductsInBrand(Guid brandId)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId)
            );
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            IEnumerable<GetProductDetailsResponse> products = await _unitOfWork.GetRepository<Product>().GetListAsync(
                selector: x => new GetProductDetailsResponse(x.Id, x.Code, x.Name, x.SellingPrice, x.PicUrl, x.Status,
                    x.HistoricalPrice, x.DiscountPrice, x.Description, x.DisplayOrder, x.Size, x.Type,
                    x.ParentProductId, x.BrandId, x.CategoryId),
                predicate: x => x.Id.Equals(brandId) && x.BrandId.Equals(brandId),
                orderBy: x => x.OrderBy(x => x.Code)
            );
            return products;
        }

        public async Task<Guid> CreateNewGroupProduct(Guid brandId,
            CreateNewGroupProductRequest createUpdateNewGroupProductRequest)
        {
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            if (!userBrandId.Equals(brandId))
                throw new BadHttpRequestException(MessageConstant.GroupProduct.WrongComboInformationMessage);

            if (createUpdateNewGroupProductRequest.ComboProductId != null)
            {
                Product product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(createUpdateNewGroupProductRequest.ComboProductId)
                                    && x.Type.Equals(ProductType.COMBO.GetDescriptionFromEnum())
                                    && x.BrandId.Equals(userBrandId)
                );

                if (product == null)
                    throw new BadHttpRequestException(MessageConstant.GroupProduct.WrongComboInformationMessage);
            }

            GroupProduct groupProductToInsert = new GroupProduct()
            {
                Id = Guid.NewGuid(),
                ComboProductId = createUpdateNewGroupProductRequest.ComboProductId,
                Name = createUpdateNewGroupProductRequest.Name,
                CombinationMode = createUpdateNewGroupProductRequest.CombinationMode.GetDescriptionFromEnum(),
                Priority = createUpdateNewGroupProductRequest.Priority,
                Quantity = createUpdateNewGroupProductRequest.Quantity,
                Status = GroupProductStatus.Active.GetDescriptionFromEnum()
            };


            List<ProductInGroup> productInGroupsToInsert = new List<ProductInGroup>();
            if (createUpdateNewGroupProductRequest.ProductIds != null ||
                createUpdateNewGroupProductRequest.ProductIds.Count > 0)
            {
                int defaultMin = 1;
                int defaultMax = 1;
                double defaultAdditionalPrice = 0;
                int defaultPriority = 0;
                int defaultQuantity = 1;
                createUpdateNewGroupProductRequest.ProductIds.ForEach(productId =>
                    productInGroupsToInsert.Add(new ProductInGroup()
                    {
                        Id = Guid.NewGuid(),
                        GroupProductId = groupProductToInsert.Id,
                        ProductId = productId,
                        Priority = defaultPriority,
                        AdditionalPrice = defaultAdditionalPrice,
                        Min = defaultMin,
                        Max = defaultMax,
                        Quantity = defaultQuantity,
                        Status = ProductInGroupStatus.Active.GetDescriptionFromEnum()
                    }));
            }

            await _unitOfWork.GetRepository<GroupProduct>().InsertAsync(groupProductToInsert);
            await _unitOfWork.GetRepository<ProductInGroup>().InsertRangeAsync(productInGroupsToInsert);
            await _unitOfWork.CommitAsync();
            return groupProductToInsert.Id;
        }

        public async Task<Guid> UpdateGroupProduct(Guid brandId, Guid groupProductId,
            UpdateGroupProductRequest updateGroupProductRequest)
        {
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            if (!userBrandId.Equals(brandId))
                throw new BadHttpRequestException(MessageConstant.GroupProduct.WrongComboInformationMessage);

            if (updateGroupProductRequest.ComboProductId != null)
            {
                Product product = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(updateGroupProductRequest.ComboProductId)
                                    && x.Type.Equals(ProductType.COMBO.GetDescriptionFromEnum())
                                    && x.BrandId.Equals(userBrandId)
                );

                if (product == null)
                    throw new BadHttpRequestException(MessageConstant.GroupProduct.WrongComboInformationMessage);
            }

            GroupProduct groupProductToUpdate = await _unitOfWork.GetRepository<GroupProduct>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(groupProductId));

            if (groupProductToUpdate == null)
                throw new BadHttpRequestException(MessageConstant.GroupProduct.GroupProductNotFoundMessage);

            groupProductToUpdate.ComboProductId = updateGroupProductRequest.ComboProductId;
            groupProductToUpdate.Name = updateGroupProductRequest.Name;
            groupProductToUpdate.CombinationMode = updateGroupProductRequest.CombinationMode.ToString();
            groupProductToUpdate.Priority = updateGroupProductRequest.Priority;
            groupProductToUpdate.Quantity = updateGroupProductRequest.Quantity;

            _unitOfWork.GetRepository<GroupProduct>().UpdateAsync(groupProductToUpdate);

            if (updateGroupProductRequest.Products != null)
            {
                //Update Product In Group
                List<ProductInGroup> currentProductInGroup = (List<ProductInGroup>) await _unitOfWork
                    .GetRepository<ProductInGroup>()
                    .GetListAsync(predicate: x => x.GroupProductId.Equals(groupProductId));
                List<Guid> newProductIds = updateGroupProductRequest.Products.Select(x => x.Id).ToList();
                List<Guid> oldProductIds = currentProductInGroup.Select(x => x.ProductId).ToList();
                (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedProductIds =
                    CustomListUtil.splitIdsToAddAndRemove(oldProductIds, newProductIds);

                int defaultMin = 1;
                int defaultMax = 1;
                double defaultAdditionalPrice = 0;
                int defaultPriority = 0;
                int defaultQuantity = 1;

                if (splittedProductIds.idsToAdd.Count > 0)
                {
                    List<ProductInGroupRequest> productsToInsert = updateGroupProductRequest.Products
                        .Where(x => splittedProductIds.idsToAdd.Contains(x.Id)).ToList();
                    List<ProductInGroup> prepareDataToInsert = new List<ProductInGroup>();
                    productsToInsert.ForEach(x =>
                    {
                        prepareDataToInsert.Add(new ProductInGroup
                        {
                            Id = Guid.NewGuid(),
                            Status = ProductInGroupStatus.Active.GetDescriptionFromEnum(),
                            GroupProductId = groupProductId,
                            ProductId = x.Id,
                            Priority = x.Priority ?? defaultPriority,
                            AdditionalPrice = x.AdditionalPrice ?? defaultAdditionalPrice,
                            Min = x.Min ?? defaultMin,
                            Max = x.Max ?? defaultMax,
                            Quantity = x.Quantity ?? defaultQuantity,
                        });
                    });
                    await _unitOfWork.GetRepository<ProductInGroup>().InsertRangeAsync(prepareDataToInsert);
                }

                if (splittedProductIds.idsToKeep.Count > 0)
                {
                    List<ProductInGroupRequest> productDataFromRequest = updateGroupProductRequest.Products
                        .Where(x => splittedProductIds.idsToKeep.Contains(x.Id)).ToList();
                    List<ProductInGroup> productsToUpdate = currentProductInGroup
                        .Where(x => splittedProductIds.idsToKeep.Contains(x.ProductId)).ToList();

                    List<ProductInGroup> prepareDataToUpdate = new List<ProductInGroup>();
                    productsToUpdate.ForEach(x =>
                    {
                        ProductInGroupRequest requestProductData =
                            productDataFromRequest.Find(y => y.Id.Equals(x.ProductId));
                        if (requestProductData == null) return;
                        x.Priority = requestProductData.Priority ?? x.Priority;
                        x.AdditionalPrice = requestProductData.AdditionalPrice ?? x.AdditionalPrice;
                        x.Min = requestProductData.Min ?? x.Min;
                        x.Max = requestProductData.Max ?? x.Max;
                        x.Quantity = requestProductData.Quantity ?? x.Quantity;
                        //Re-actvate product status in case user wanted to re-add product in group
                        x.Status = ProductInGroupStatus.Active.GetDescriptionFromEnum();

                        prepareDataToUpdate.Add(x);
                    });
                    _unitOfWork.GetRepository<ProductInGroup>().UpdateRange(prepareDataToUpdate);
                }

                if (splittedProductIds.idsToRemove.Count > 0)
                {
                    List<ProductInGroup> prepareDataToRemove = currentProductInGroup
                        .Where(x => splittedProductIds.idsToRemove.Contains(x.ProductId)).ToList();

                    //Change status of product in group from 'Active' to 'Deactivate' to remove product
                    List<ProductInGroup> finalDataToRemove = new List<ProductInGroup>();
                    foreach (var productInGroupToChangeStatus in prepareDataToRemove)
                    {
                        //Update status to deactive
                        productInGroupToChangeStatus.Status = ProductInGroupStatus.Deactivate.GetDescriptionFromEnum();
                        finalDataToRemove.Add(productInGroupToChangeStatus);
                    }

                    _unitOfWork.GetRepository<ProductInGroup>().UpdateRange(finalDataToRemove);
                }
            }

            await _unitOfWork.CommitAsync();
            return groupProductId;
        }

        public async Task<IEnumerable<GetGroupProductListResponse>> GetGroupProductListOfCombo(Guid brandId,
            Guid productId)
        {
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            if (!userBrandId.Equals(brandId))
                throw new BadHttpRequestException(MessageConstant.GroupProduct.WrongComboInformationMessage);

            if (productId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.Product.EmptyProductIdMessage);
            Product currentProduct = await _unitOfWork.GetRepository<Product>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(productId) && x.Type.Equals(ProductType.COMBO.GetDescriptionFromEnum())
            );
            if (currentProduct == null)
                throw new BadHttpRequestException(MessageConstant.Product.ProductNotFoundMessage);

            List<GetGroupProductListResponse> response = (List<GetGroupProductListResponse>) await _unitOfWork
                .GetRepository<GroupProduct>().GetListAsync(
                    selector: x => new GetGroupProductListResponse
                    {
                        Id = x.Id,
                        ComboProductId = (Guid) x.ComboProductId,
                        Name = x.Name,
                        CombinationMode = EnumUtil.ParseEnum<GroupCombinationMode>(x.CombinationMode),
                        Priority = x.Priority,
                        Quantity = x.Quantity,
                        Status = EnumUtil.ParseEnum<GroupProductStatus>(x.Status),
                        ProductsInGroups = (List<ProductsInGroupResponse>) x.ProductInGroups.Select(productInGroup =>
                            new ProductsInGroupResponse
                            {
                                Id = productInGroup.Id,
                                GroupProductId = productInGroup.GroupProductId,
                                ProductId = productInGroup.ProductId,
                                Priority = productInGroup.Priority,
                                AdditionalPrice = productInGroup.AdditionalPrice,
                                Min = productInGroup.Min,
                                Max = productInGroup.Max,
                                Quantity = productInGroup.Quantity,
                                Status = EnumUtil.ParseEnum<ProductInGroupStatus>(productInGroup.Status)
                            })
                    },
                    predicate: x => x.ComboProductId.Equals(productId),
                    include: groupProduct => groupProduct.Include(groupProduct => groupProduct.ProductInGroups),
                    orderBy: x => x.OrderBy(x => x.Priority));

            return response;
        }

        public async Task<Guid> UpdateProductInGroup(Guid groupProductId, Guid id,
            UpdateProductInGroupRequest updateProductInGroupRequest)
        {
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            if (id == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.ProductInGroup.EmptyProductInGroupId);
            ProductInGroup currentProductInGroup = await _unitOfWork.GetRepository<ProductInGroup>()
                .SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(id) && x.Product.BrandId.Equals(userBrandId)
                );

            if (currentProductInGroup == null)
                throw new BadHttpRequestException(MessageConstant.ProductInGroup.ProductInGroupNotFound);

            currentProductInGroup.Priority = updateProductInGroupRequest.Priority ?? currentProductInGroup.Priority;
            currentProductInGroup.AdditionalPrice =
                updateProductInGroupRequest.AdditionalPrice ?? currentProductInGroup.AdditionalPrice;
            currentProductInGroup.Min = updateProductInGroupRequest?.Min ?? currentProductInGroup.Min;
            currentProductInGroup.Max = updateProductInGroupRequest.Max ?? currentProductInGroup.Max;
            currentProductInGroup.Quantity = updateProductInGroupRequest.Quantity ?? currentProductInGroup.Quantity;
            currentProductInGroup.Status = updateProductInGroupRequest.Status.GetDescriptionFromEnum() ??
                                           currentProductInGroup.Status;

            _unitOfWork.GetRepository<ProductInGroup>().UpdateAsync(currentProductInGroup);
            await _unitOfWork.CommitAsync();

            return id;
        }

        public async Task<Guid?> CreateNewProductVariant(
            CreatNewProductVariantRequest creatNewProductVariant)
        {
            _logger.LogInformation($"Start create new : {creatNewProductVariant}");
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            Variant newVariant = new Variant()
            {
                Id = Guid.NewGuid(),

                Name = creatNewProductVariant.Name,
                Value = creatNewProductVariant.Value,
                DisplayOrder = creatNewProductVariant.DisplayOrder,
                BrandId = brandId,
                Status = ProductStatus.Active.GetDescriptionFromEnum(),
            };
            await _unitOfWork.GetRepository<Variant>().InsertAsync(newVariant);
            var isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccessful) return null;
            return newVariant.Id;
        }


        public async Task<IPaginate<VariantDetailsResponse>> GetProductVariants(string? name, int page,
            int size)
        {
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            name = name?.Trim();
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            IPaginate<VariantDetailsResponse> variants = await _unitOfWork.GetRepository<Variant>()
                .GetPagingListAsync(
                    selector: x => new VariantDetailsResponse()
                    {
                        Id = x.Id,
                        DisplayOrder = x.DisplayOrder,
                        Name = x.Name,
                        Value = x.Value,
                        Status = x.Status
                    },
                    predicate: (string.IsNullOrEmpty(name)
                        ? x => x.BrandId.Equals(brandId)
                        : x => x.BrandId.Equals(brandId) && x.Name.ToLower().Contains(name)),
                    page:
                    page,
                    size:
                    size,
                    orderBy:
                    x => x.OrderBy(x => x.DisplayOrder)
                );
            return variants;
        }

        public async Task<VariantDetailsResponse> GetProductVariantByiD(Guid id)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Product.EmptyProductIdMessage);
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            var productResponse = await _unitOfWork.GetRepository<Variant>().SingleOrDefaultAsync(
                selector: x => new VariantDetailsResponse()
                {
                    Id = x.Id,
                    DisplayOrder = x.DisplayOrder,
                    Name = x.Name,
                    Value = x.Value,
                    Status = x.Status
                },
                predicate: x => x.Id.Equals(id) && x.BrandId.Equals(brandId)
            );
            if (productResponse == null)
                throw new BadHttpRequestException(MessageConstant.ProductVariant.ProductVariantNotFoundMessage);
            return productResponse;
        }

        public async Task<Guid> UpdateProductVariants(Guid productId, UpdateProductVariantRequest updateProductRequest)
        {
            _logger.LogInformation("Start updating product: {ProductId}", productId);
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);


            Variant updateProduct = await _unitOfWork.GetRepository<Variant>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(productId));
            if (updateProduct == null)
                throw new BadHttpRequestException(MessageConstant.Product.ProductNotFoundMessage);


            updateProduct.Name = updateProductRequest.Name ?? updateProduct.Name;
            updateProduct.Value = updateProductRequest.Value ?? updateProduct.Value;
            updateProduct.DisplayOrder = updateProductRequest.DisplayOrder ?? updateProduct.DisplayOrder;

            updateProduct.Status = string.IsNullOrEmpty(updateProductRequest.Status.GetDescriptionFromEnum())
                ? updateProduct.Status
                : updateProductRequest.Status.GetDescriptionFromEnum();

            _unitOfWork.GetRepository<Variant>().UpdateAsync(updateProduct);
            await _unitOfWork.CommitAsync();
            return productId;
        }

        public async Task<bool> AddVariantToProduct(Guid productId, List<Guid> request)
        {
            _logger.LogInformation($"Add Variant to Product: {productId}");
            var brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            var brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            var currentVariantIds = (List<Guid>) await _unitOfWork.GetRepository<VariantProductMapping>()
                .GetListAsync(
                    selector: x => x.VariantId,
                    predicate: x => x.ProductId.Equals(productId)
                );
            var splittedVariantIds =
                CustomListUtil.splitIdsToAddAndRemove(currentVariantIds, request);
            //Handle add and remove to database
            if (splittedVariantIds.idsToAdd.Count > 0)
            {
                var variantToInsert = new List<VariantProductMapping>();
                splittedVariantIds.idsToAdd.ForEach(id => variantToInsert.Add(new VariantProductMapping()
                {
                    Id = Guid.NewGuid(),
                    VariantId = id,
                    ProductId = productId
                }));
                await _unitOfWork.GetRepository<VariantProductMapping>().InsertRangeAsync(variantToInsert);
            }

            if (splittedVariantIds.idsToRemove.Count > 0)
            {
                var variantsToDelete = (List<VariantProductMapping>) await _unitOfWork
                    .GetRepository<VariantProductMapping>()
                    .GetListAsync(predicate: x =>
                        x.ProductId.Equals(productId) &&
                        splittedVariantIds.idsToRemove.Contains(x.VariantId));

                _unitOfWork.GetRepository<VariantProductMapping>().DeleteRangeAsync(variantsToDelete);
            }

            var isSuccesful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccesful)
            {
                throw new HttpRequestException(MessageConstant.Product.UpdateVariantToProductFail);
            }

            return isSuccesful;
        }
    }
}
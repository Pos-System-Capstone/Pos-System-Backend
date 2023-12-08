using AutoMapper;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Categories;
using Pos_System.API.Payload.Response.Categories;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements;

public class CategoryService : BaseService<CategoryService>, ICategoryService
{
    public CategoryService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<CategoryService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
    {
    }


    public async Task<bool> CreateNewCategory(CreateNewCategoryRequest request)
    {
        _logger.LogInformation($"Start create new category: {request}");
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            predicate: x => x.Id.Equals(brandId));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
        Category newCategory = new Category()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Type = EnumUtil.GetDescriptionFromEnum(request.CategoryType),
            DisplayOrder = request.DisplayOrder,
            Status = EnumUtil.GetDescriptionFromEnum(CategoryStatus.Active),
            PicUrl = request?.PicUrl,
            Description = request.Description.Trim(),
            BrandId = brandId
        };
        await _unitOfWork.GetRepository<Category>().InsertAsync(newCategory);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
        return isSuccessful;
    }

    public async Task<IPaginate<GetCategoryResponse>> GetCategories(string? name, CategoryType? type, int page,
        int size)
    {
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        _logger.LogInformation($"Get Categories from Brand: {brandId}");
        name = name?.Trim();
        IPaginate<GetCategoryResponse> categoryResponse =
            await _unitOfWork.GetRepository<Category>().GetPagingListAsync(
                selector: x => new GetCategoryResponse(x.Id, x.Code, x.Name, x.Type, x.DisplayOrder, x.Description,
                    x.Status, x.BrandId.Value, x.PicUrl),
                predicate:
                string.IsNullOrEmpty(name) && (type == null)
                    ? x => x.BrandId.Equals(brandId)
                    : ((type == null)
                        ? x => x.BrandId.Equals(brandId) && x.Name.Contains(name)
                        : (string.IsNullOrEmpty(name)
                            ? x => x.BrandId.Equals(brandId) && x.Type.Equals(type.GetDescriptionFromEnum())
                            : x => x.BrandId.Equals(brandId) && x.Name.Contains(name) &&
                                   x.Type.Equals(type.GetDescriptionFromEnum()))),
                orderBy: x => x.OrderByDescending(x => x.DisplayOrder),
                page: page,
                size: size
            );
        return categoryResponse;
    }

    public async Task<GetCategoryResponse> GetCategoryById(Guid id)
    {
        if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Category.EmptyCategoryIdMessage);
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        GetCategoryResponse categoryResponse = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
            selector: x => new GetCategoryResponse(x.Id, x.Code, x.Name, x.Type, x.DisplayOrder, x.Description,
                x.Status, x.BrandId.Value, x.PicUrl),
            predicate: x => x.Id.Equals(id) && x.BrandId.Equals(brandId)
        );
        return categoryResponse;
    }

    public async Task<bool> UpdateCategory(Guid id, UpdateCategoryRequest request)
    {
        if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Category.EmptyCategoryIdMessage);
        Category category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(
            predicate: x => x.Id.Equals(id)
        );
        if (category == null) throw new BadHttpRequestException(MessageConstant.Category.CategoryNotFoundMessage);
        _logger.LogInformation($"Start to update category {category.Id}");
        request.TrimString();
        category.Name = string.IsNullOrEmpty(request.Name) ? category.Name : request.Name;
        category.Description = string.IsNullOrEmpty(request.Description) ? category.Description : request.Description;
        category.DisplayOrder = (request.DisplayOrder ?? category.DisplayOrder);
        category.Status = string.IsNullOrEmpty(request.Status) ? category.Status : request.Status;
        _unitOfWork.GetRepository<Category>().UpdateAsync(category);
        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
        return isSuccessful;
    }

    public async Task<bool> AddExtraCategoriesToNormalCategory(Guid categoryId, List<Guid> request)
    {
        _logger.LogInformation($"Add extraCategory to Normal Categoery: {categoryId}");
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            predicate: x => x.Id.Equals(brandId));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

        List<Guid> currentExtraCategoriesId = (List<Guid>) await _unitOfWork.GetRepository<ExtraCategory>()
            .GetListAsync(
                selector: x => x.ExtraCategoryId,
                predicate: x => x.ProductCategoryId.Equals(categoryId)
            );

        (List<Guid> idsToRemove, List<Guid> idsToAdd, List<Guid> idsToKeep) splittedExtraCategoriesIds =
            CustomListUtil.splitIdsToAddAndRemove(currentExtraCategoriesId, request);
        //Handle add and remove to database
        if (splittedExtraCategoriesIds.idsToAdd.Count > 0)
        {
            List<ExtraCategory> extraCategoriesToInsert = new List<ExtraCategory>();
            splittedExtraCategoriesIds.idsToAdd.ForEach(id => extraCategoriesToInsert.Add(new ExtraCategory
            {
                Id = Guid.NewGuid(),
                ProductCategoryId = categoryId,
                ExtraCategoryId = id,
                Status = CategoryStatus.Active.GetDescriptionFromEnum()
            }));
            await _unitOfWork.GetRepository<ExtraCategory>().InsertRangeAsync(extraCategoriesToInsert);
        }

        if (splittedExtraCategoriesIds.idsToRemove.Count > 0)
        {
            List<ExtraCategory> extraCategoriesToDelete = new List<ExtraCategory>();
            extraCategoriesToDelete = (List<ExtraCategory>) await _unitOfWork.GetRepository<ExtraCategory>()
                .GetListAsync(predicate: x =>
                    x.ProductCategoryId.Equals(categoryId) &&
                    splittedExtraCategoriesIds.idsToRemove.Contains(x.ExtraCategoryId));

            _unitOfWork.GetRepository<ExtraCategory>().DeleteRangeAsync(extraCategoriesToDelete);
        }

        bool isSuccesful = await _unitOfWork.CommitAsync() > 0;
        return isSuccesful;
    }

    public async Task<IPaginate<GetCategoryResponse>> GetExtraCategoriesByCategoryId(Guid categoryId, int page,
        int size)
    {
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        _logger.LogInformation($"Get ExtraCategories from CategoryId: {categoryId}");

        List<Guid> extraCategoryIds = (List<Guid>) await _unitOfWork.GetRepository<ExtraCategory>().GetListAsync(
            selector: x => x.ExtraCategoryId,
            predicate: x =>
                x.ProductCategoryId.Equals(categoryId) &&
                x.Status.Equals(CollectionStatus.Active.GetDescriptionFromEnum())
        );

        IPaginate<GetCategoryResponse> categoryResponse =
            await _unitOfWork.GetRepository<Category>().GetPagingListAsync(
                selector: x => new GetCategoryResponse(x.Id, x.Code, x.Name, x.Type, x.DisplayOrder, x.Description,
                    x.Status, x.BrandId.Value, x.PicUrl),
                predicate:
                x => x.BrandId.Equals(brandId) && extraCategoryIds.Contains(x.Id),
                orderBy: x => x.OrderByDescending(x => x.DisplayOrder),
                page: page,
                size: size
            );
        return categoryResponse;
    }

    public async Task<IPaginate<GetProductsInCategory>> GetProductsInCategory(Guid categoryId, int page, int size)
    {
        Guid brandId = Guid.Parse(GetBrandIdFromJwt());
        if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
        Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            predicate: x => x.Id.Equals(brandId));
        if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

        IPaginate<GetProductsInCategory> response = await _unitOfWork.GetRepository<Product>().GetPagingListAsync(
            selector: x => new GetProductsInCategory
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                PicUrl = x.PicUrl,
                SellingPrice = x.SellingPrice == null ? 0 : x.SellingPrice,
                DiscountPrice = x.DiscountPrice,
                HistoricalPrice = x.HistoricalPrice,
                Status = EnumUtil.ParseEnum<ProductStatus>(x.Status),
                Type = EnumUtil.ParseEnum<ProductType>(x.Type),
            },
            predicate: x => x.CategoryId.Equals(categoryId) && x.BrandId.Equals(brandId),
            orderBy: x => x.OrderBy(x => x.Code),
            page: page,
            size: size
        );

        return response;
    }
}
using Pos_System.API.Enums;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Response.Products;
using System.Text.Json.Serialization;

namespace Pos_System.API.Payload.Response.Menus;
public class GetMenuDetailForStaffResponse
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Code { get; set; }
    public int Priority { get; set; }
    public bool IsBaseMenu { get; set; }
    public List<DateFilter> DateFilter { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    [JsonPropertyName("products")]
    public List<ProductDataForStaff> ProductsInMenu { get; set; }
    [JsonPropertyName("collections")]
    public List<CollectionOfBrand> CollectionsOfBrand { get; set; }
    [JsonPropertyName("categories")]
    public List<CategoryOfBrand> CategoriesOfBrand { get; set; }
    [JsonPropertyName("groupProducts")]
    public List<GroupProductInMenu> groupProductInMenus { get; set; }
    [JsonPropertyName("productsInGroup")]
    public List<ProductsInGroupResponse> productInGroupList { get; set; }

    public GetMenuDetailForStaffResponse(Guid id, Guid brandId, string code, int priority, bool isBaseMenu, int dateFilter, int startTime, int endTime)
    {
        Id = id;
        BrandId = brandId;
        Code = code;
        Priority = priority;
        IsBaseMenu = isBaseMenu;
        DateFilter = DateTimeHelper.GetDatesFromDateFilter(dateFilter);
        StartTime = DateTimeHelper.ConvertIntToTimeOnly(startTime);
        EndTime = DateTimeHelper.ConvertIntToTimeOnly(endTime);
    }
}

public class CategoryOfBrand
{
    public Guid Id { get; set; }
    public List<Guid>? ChildCategoryIds { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public int DisplayOrder { get; set; }
    public string Description { get; set; }
    public string PicUrl { get; set; }
    public CategoryOfBrand(Guid id, string code, string name, CategoryType categoryType,List<Guid> childCategoryIds, int displayOrder, string description, string picUrl)
    {
        Id = id;
        Code = code;
        Name = name;
        Type = categoryType;
        ChildCategoryIds = childCategoryIds;
        DisplayOrder = displayOrder;
        Description = description;
        PicUrl = picUrl;
    }
}

public class CollectionOfBrand
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string PicUrl { get; set; }
    public string Description { get; set; }
    public CollectionOfBrand(Guid id, string name, string code, string picUrl, string description)
    {
        Id = id;
        Name = name;
        Code = code;
        PicUrl = picUrl;
        Description = description;
    }
}

public class ProductDataForStaff : GetProductDetailsResponse
{
    public List<Guid> CollectionIds { get; set; } = new List<Guid>();
    public List<Guid> ExtraCategoryIds { get; set; } = new List<Guid>();
    public Guid MenuProductId { get; set; }
    public ProductDataForStaff(Guid id, string code, string name, double sellingPrice,
        string? picUrl, string status, double historicalPrice, double discountPrice,
        string? description, int displayOrder, string? size, string type, Guid? parentProductId,
        Guid brandId, Guid categoryId, List<Guid> collectionIds, List<Guid> extraCategoryIds, Guid menuProductId)
        : base(id, code, name, sellingPrice, picUrl, status, historicalPrice,
            discountPrice, description, displayOrder, size, type, parentProductId, brandId, categoryId)
    {
        CollectionIds = collectionIds;
        ExtraCategoryIds = extraCategoryIds;
        MenuProductId = menuProductId;
    }
}

public class GroupProductInMenu
{
    public Guid Id { get; set; }
    public Guid ComboProductId { get; set; }
    public string Name { get; set; }
    public GroupCombinationMode CombinationMode { get; set; }
    public int Priority { get; set; }
    public int Quantity { get; set; }
    public GroupProductStatus Status { get; set; }
    public List<Guid> ProductsInGroupIds { get; set; }
}
using Pos_System.API.Enums;
using Pos_System.API.Utils;

namespace Pos_System.API.Payload.Response.Categories;

public class GetCategoryResponse
{
	public Guid Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public CategoryType Type { get; set; }
	public int DisplayOrder { get; set; }
	public string Description { get; set; }
	public string PicUrl { get; set; }
	public CategoryStatus Status { get; set; }
	public Guid BrandId { get; set; }

	public GetCategoryResponse(Guid id, string code, string name, string type, int displayOrder, string description, string status, Guid brandId, string picUrl)
	{
		Id = id;
		Code = code;
		Name = name;
		Type = EnumUtil.ParseEnum<CategoryType>(type);
		DisplayOrder = displayOrder;
		Description = description;
		Status = EnumUtil.ParseEnum<CategoryStatus>(status);
		BrandId = brandId;
		PicUrl = picUrl;
	}
}
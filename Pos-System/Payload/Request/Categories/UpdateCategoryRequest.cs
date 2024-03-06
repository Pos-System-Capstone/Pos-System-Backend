namespace Pos_System.API.Payload.Request.Categories;

public class UpdateCategoryRequest
{
	public string? Name { get; set; }
	public int? DisplayOrder { get; set; }
	public string? Description { get; set; }
	
	public string? Status { get; set; }

	public void TrimString()
	{
		Name = Name?.Trim();
		Description = Description?.Trim();
	}
}
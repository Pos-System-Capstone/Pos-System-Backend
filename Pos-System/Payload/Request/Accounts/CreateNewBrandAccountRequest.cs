using System.ComponentModel.DataAnnotations;
using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.Brands;

public class CreateNewBrandAccountRequest
{
	[Required(ErrorMessage = "Brand Id is missing")]
	public Guid BrandId { get; set; }
	[Required(ErrorMessage = "Username is missing")]
	public string Username { get; set; }
	[Required(ErrorMessage = "Name is missing")]
	public string Name { get; set; }
	[Required(ErrorMessage = "Password is missing")]
	public string Password { get; set; }
	[Required(ErrorMessage = "Status of account is missing")]
	public AccountStatus Status { get; set; }
	[Required(ErrorMessage = "Role is missing")]
	public RoleEnum Role { get; set; }

	public CreateNewBrandAccountRequest()
	{

	}
}
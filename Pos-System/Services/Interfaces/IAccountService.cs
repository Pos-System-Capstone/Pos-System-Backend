using Pos_System.API.Enums;
using Pos_System.API.Payload.Request;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
	public interface IAccountService
	{
		Task<LoginResponse> Login(LoginRequest loginRequest);

		Task<CreateNewBrandAccountResponse> CreateNewBrandAccount(CreateNewBrandAccountRequest createNewBrandAccountRequest);

		Task<IPaginate<GetAccountResponse>> GetBrandAccounts(Guid brandId, string? searchUsername, RoleEnum role, int page, int size);

		Task<bool> UpdateAccountStatus (Guid accountId,UpdateAccountStatusRequest updateAccountStatusRequest);
	}
}

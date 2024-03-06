using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;

namespace Pos_System.API.Controllers
{
	[ApiController]
	public class AccountController : BaseController<AccountController>
	{
		private readonly IAccountService _accountService;

		public AccountController(ILogger<AccountController> logger, IAccountService accountService) : base(logger)
		{
			_accountService = accountService;
		}

		[CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.StoreManager, RoleEnum.BrandManager)]
		[HttpPatch(ApiEndPointConstant.Account.AccountEndpoint)]
		public async Task<IActionResult> UpdateAccountStatus(Guid id,[FromBody] UpdateAccountStatusRequest updateAccountStatusRequest)
		{
			var isSuccessful = await _accountService.UpdateAccountStatus(id, updateAccountStatusRequest);
			if (!isSuccessful) return Ok(MessageConstant.Account.UpdateAccountStatusFailedMessage);
			return Ok(MessageConstant.Account.UpdateAccountStatusSuccessfulMessage);
		}

		[CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager, RoleEnum.Staff, RoleEnum.StoreManager)]
		[HttpGet(ApiEndPointConstant.Account.AccountEndpoint)]
		public async Task<IActionResult> GetAccountDetail(Guid id)
		{
			var accountDetails = await _accountService.GetAccountDetail(id);
			return Ok(accountDetails);
		}
	}
}

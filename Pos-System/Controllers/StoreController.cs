using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.Accounts;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Request.Sessions;
using Pos_System.API.Payload.Request.Stores;
using Pos_System.API.Payload.Response.Accounts;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.API.Payload.Response.Sessions;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Payload.Response.User;
using Pos_System.API.Services.Implements;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class StoreController : BaseController<StoreController>
    {
        private readonly IStoreService _storeService;
        private readonly IAccountService _accountService;
        private readonly IOrderService _orderService;
        private readonly ISessionService _sessionService;
        private readonly IReportService _reportService;
        private readonly IUserService _userService;

        public StoreController(ILogger<StoreController> logger, IStoreService storeService,
            IAccountService accountService, IOrderService orderService, ISessionService sessionService,
            IReportService reportService, IUserService userService) : base(logger)
        {
            _storeService = storeService;
            _accountService = accountService;
            _orderService = orderService;
            _sessionService = sessionService;
            _reportService = reportService;
            _userService = userService;
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager, RoleEnum.StoreManager,
            RoleEnum.Staff, RoleEnum.User)]
        [HttpGet(ApiEndPointConstant.Store.StoreEndpoint)]
        [ProducesResponseType(typeof(GetStoreDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            var storeResponse = await _storeService.GetStoreById(id);
            return Ok(storeResponse);
        }

        [CustomAuthorize(RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Store.StoresEndpoint)]
        [ProducesResponseType(typeof(CreateNewStoreResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNewStore(CreateNewStoreRequest createNewStoreRequest)
        {
            CreateNewStoreResponse response = await _storeService.CreateNewStore(createNewStoreRequest);
            if (response == null)
            {
                _logger.LogError($"Create new store failed with {createNewStoreRequest.Name}");
                return Problem($"{MessageConstant.Store.CreateStoreFailMessage}: {createNewStoreRequest.Name}");
            }

            _logger.LogInformation($"Create new brand successful with {createNewStoreRequest.Name}");
            return CreatedAtAction(nameof(CreateNewStore), response);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager, RoleEnum.StoreManager)]
        [HttpGet(ApiEndPointConstant.Store.StoreAccountEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreEmployeesResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreEmployees(Guid storeId, [FromQuery] string? username,
            [FromQuery] int page, [FromQuery] int size)
        {
            var storeResponse = await _storeService.GetStoreEmployees(storeId, username, page, size);
            return Ok(storeResponse);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpPost(ApiEndPointConstant.Store.StoreAccountEndpoint)]
        [ProducesResponseType(typeof(CreateNewStoreAccountResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateNewStoreAccount(Guid storeId,
            CreateNewStoreAccountRequest newStoreAccountRequest)
        {
            CreateNewStoreAccountResponse response =
                await _accountService.CreateNewStoreAccount(storeId, newStoreAccountRequest);
            if (response == null)
            {
                _logger.LogError(
                    $"Create new store account failed: store {storeId} with account {newStoreAccountRequest.Username}");
                return Problem(MessageConstant.Account.CreateStaffAccountFailMessage);
            }

            _logger.LogInformation(
                $"Create staff account successfully with store: {storeId}, account: {newStoreAccountRequest.Username}, role {response.Role.GetDescriptionFromEnum()}");
            return CreatedAtAction(nameof(CreateNewStoreAccount), response);
        }

        [CustomAuthorize(RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Store.StoreEndpoint)]
        public async Task<IActionResult> UpdateStoreInformation(Guid id, UpdateStoreRequest updateStoreRequest)
        {
            await _storeService.UpdateStoreInformation(id, updateStoreRequest);
            return Ok(MessageConstant.Store.UpdateStoreInformationSuccessfulMessage);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.BrandManager, RoleEnum.BrandAdmin)]
        [HttpPatch(ApiEndPointConstant.Store.StoreUpdateEmployeeEndpoint)]
        public async Task<IActionResult> UpdateAccountInformation(Guid storeId, Guid id,
            UpdateStoreAccountInformationRequest staffAccountInformationRequest)
        {
            await _accountService.UpdateStoreAccountInformation(id, staffAccountInformationRequest);
            return Ok(MessageConstant.Store.UpdateStaffInformationSuccessfulMessage);
        }

        [CustomAuthorize(RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.MenuProductsForStaffEndPoint)]
        [ProducesResponseType(typeof(GetMenuDetailForStaffResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMenuDetailForStaff()
        {
            GetMenuDetailForStaffResponse response = await _storeService.GetMenuDetailForStaff();
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.StoreOrdersEndpoint)]
        [ProducesResponseType(typeof(IPaginate<ViewOrdersResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersOfStore(Guid id, [FromQuery] int page, [FromQuery] int size,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] OrderType? orderType,
            [FromQuery] OrderStatus? status)
        {
            var response = await _orderService.GetOrdersInStore(id, page, size, startDate, endDate, orderType, status);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager)]
        [HttpPost(ApiEndPointConstant.Store.StoreSessionsEndpoint)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewStoreSessions(Guid id,
            CreateStoreSessionsRequest createStoreSessionsRequest)
        {
            await _sessionService.CreateStoreSessions(id, createStoreSessionsRequest);
            return Ok(MessageConstant.Store.CreateStoreSessionsSuccessfully);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.StoreSessionsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreSessionListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreSessions(Guid id, [FromQuery] int page, [FromQuery] int size,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var response = await _sessionService.GetStoreSessions(id, page, size, startDate, endDate);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.StoreSessionEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreSessionListResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreEndShiftStatictis(Guid storeId, Guid id)
        {
            var response = await _storeService.GetStoreEndShiftStatistics(storeId, id);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager)]
        [HttpPatch(ApiEndPointConstant.Store.StoreSessionEndpoint)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStoreSession(Guid storeId, Guid id,
            UpdateStoreSessionRequest updateStoreSessionRequest)
        {
            var response = await _sessionService.UpdateStoreSession(storeId, id, updateStoreSessionRequest);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.StoreEndDayReportEndpoint)]
        [ProducesResponseType(typeof(GetStoreEndDayReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreEndDayReport(Guid id, [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var response = await _reportService.GetStoreEndDayReport(id, startDate, endDate);
            return Ok(response);
        }

        [CustomAuthorize(RoleEnum.StoreManager, RoleEnum.Staff)]
        [HttpGet(ApiEndPointConstant.Store.GetPromotion)]
        [ProducesResponseType(typeof(GetPromotionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPrmotion(Guid id)
        {
            var response = await _orderService.GetPromotion(id);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Store.GetListPromotion)]
        [ProducesResponseType(typeof(IEnumerable<PromotionPointifyResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPromotionInStore(Guid id)
        {
            var userResponse = await _storeService.GetPromotionInStore(id);
            return Ok(userResponse);
        }
        
        [HttpGet(ApiEndPointConstant.Store.ScanUserFromStore)]
        [ProducesResponseType(typeof(GetUserInfo), StatusCodes.Status200OK)]
        public async Task<IActionResult> ScanUserInStore(Guid id, [FromQuery] string phone)
        {
            var userResponse = await _userService.ScanUserPhoneNumber(phone, id);
            return Ok(userResponse);
        }
    }
}
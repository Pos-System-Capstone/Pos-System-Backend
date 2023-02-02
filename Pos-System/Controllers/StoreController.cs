﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Accounts;
using Pos_System.API.Payload.Request.Stores;
using Pos_System.API.Payload.Response.Accounts;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Validators;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class StoreController : BaseController<StoreController>
    {
        private readonly IStoreService _storeService;
        private readonly IAccountService _accountService;

        public StoreController(ILogger<StoreController> logger, IStoreService storeService, IAccountService accountService) : base(logger)
        {
            _storeService = storeService;
            _accountService = accountService;
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Store.StoreEndpoint)]
        [ProducesResponseType(typeof(GetStoreDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            var storeResponse = await _storeService.GetStoreById(id);
            return Ok(storeResponse);
        }

        [CustomAuthorize(RoleEnum.BrandManager)]
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
        [HttpGet(ApiEndPointConstant.Store.StoreGetEmployeeEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreEmployeesResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreEmployees(Guid storeId, [FromQuery] string? username, [FromQuery] int page, [FromQuery] int size)
        {
            var storeResponse = await _storeService.GetStoreEmployees(storeId, username, page, size);
            return Ok(storeResponse);
        }

        [CustomAuthorize(RoleEnum.StoreManager)]
        [HttpPost(ApiEndPointConstant.Store.StoreCreateAccountEndpoint)]
        [ProducesResponseType(typeof(CreateNewStaffAccountResponse), StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(ProblemDetails))]
        public async Task<IActionResult> CreateNewStaffAccount(Guid storeId,CreateNewStaffAccountRequest newStaffAccountRequest)
        {
            CreateNewStaffAccountResponse response = await _accountService.CreateNewStaffAccount(storeId, newStaffAccountRequest);
            if (response == null)
            {
                _logger.LogError($"Create new staff account failed: store {storeId} with account {newStaffAccountRequest.Username}");
                return Problem(MessageConstant.Account.CreateStaffAccountFailMessage);
            }
            _logger.LogInformation($"Create staff account successfully with store: {storeId}, account: {newStaffAccountRequest.Username}");
            return CreatedAtAction(nameof(CreateNewStaffAccount), response);
        }

        [CustomAuthorize(RoleEnum.BrandManager)]
        [HttpPut(ApiEndPointConstant.Store.StoreEndpoint)]
        public async Task<IActionResult> UpdateStoreInformation(Guid id, UpdateStoreRequest updateStoreRequest)
        {
            await _storeService.UpdateStoreInformation(id, updateStoreRequest);
            return Ok(MessageConstant.Store.UpdateStoreInformationSuccessfulMessage);
        }

        [CustomAuthorize(RoleEnum.StoreManager)]
        [HttpPut(ApiEndPointConstant.Store.StoreUpdateEmployeeEndpoint)]
        public async Task<IActionResult> UpdateStaffAccountInformation(Guid storeId, Guid id, UpdateStaffAccountInformationRequest staffAccountInformationRequest)
        {
            await _accountService.UpdateStaffAccountInformation(id, staffAccountInformationRequest);
            return Ok(MessageConstant.Store.UpdateStaffInformationSuccessfulMessage);
        }
    }
}

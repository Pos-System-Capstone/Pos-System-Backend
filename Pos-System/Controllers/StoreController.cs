﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
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

        public StoreController(ILogger<StoreController> logger, IStoreService storeService) : base(logger)
        {
            _storeService = storeService;
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager)]
        [HttpGet(ApiEndPointConstant.Store.StoreEndpoint)]
        [ProducesResponseType(typeof(GetStoreDetailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            var storeResponse = await _storeService.GetStoreById(id);
            return Ok(storeResponse);
        }

        [CustomAuthorize(RoleEnum.SysAdmin, RoleEnum.BrandAdmin, RoleEnum.BrandManager, RoleEnum.StoreManager)]
        [HttpGet(ApiEndPointConstant.Store.StoreGetEmployeeEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetStoreEmployeesResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStoreEmployees(Guid storeId, [FromQuery] string? searchUsername, [FromQuery] int page, [FromQuery] int size)
        {
            var storeResponse = await _storeService.GetStoreEmployees(storeId, searchUsername, page, size);
            return Ok(storeResponse);
        }
    }
}
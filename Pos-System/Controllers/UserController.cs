﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.BlogPost;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.API.Payload.Response.User;
using Pos_System.API.Services.Implements;
using Pos_System.API.Services.Interfaces;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Controllers
{
    [ApiController]
    public class UserController : BaseController<UserController>
    {
        private readonly IUserService _userService;
        private readonly IBlogPostService _blogPostService;
        private readonly IOrderService _orderService;

        public UserController(ILogger<UserController> logger, IUserService userService,
            IBlogPostService blogPostService, IOrderService orderService) : base(logger)
        {
            _userService = userService;
            _blogPostService = blogPostService;
            _orderService = orderService;
        }


        [HttpPost(ApiEndPointConstant.User.UsersEndpoint)]
        [ProducesResponseType(typeof(CreateNewUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNewUser([FromBody] CreateNewUserRequest newUserRequest,
            [FromQuery] string? brandCode)
        {
            CreateNewUserResponse response = await _userService.CreateNewUser(newUserRequest, brandCode);
            if (response == null)
            {
                _logger.LogError($"Create new user failed with {newUserRequest.FullName}");
                return Problem($"{MessageConstant.User.CreateNewUserFailedMessage}: {newUserRequest.FullName}");
            }

            _logger.LogInformation($"Create new user successful with {newUserRequest.FullName}");
            return CreatedAtAction(nameof(CreateNewUser), response);
        }

        [HttpPost(ApiEndPointConstant.User.UsersSignIn)]
        [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginUser([FromBody] LoginFirebase req)
        {
            SignInResponse response = await _userService.LoginUser(req);
            return Ok(response);
        }

        [HttpPost(ApiEndPointConstant.User.UsersSignUp)]
        [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SignUpUser([FromBody] CreateNewUserRequest req, [FromQuery] string? brandCode)
        {
            SignInResponse response = await _userService.SignUpUser(req, brandCode);
            return Ok(response);
        }

        [HttpPatch(ApiEndPointConstant.User.UserEndpoint)]
        public async Task<IActionResult> UpdateUserInformation(Guid id, [FromBody] UpdateUserRequest updateUserRequest)
        {
            bool isSuccessful = await _userService.UpdateUserInformation(id, updateUserRequest);
            if (isSuccessful)
            {
                _logger.LogInformation($"Update user {id} information successfully");
                return Ok(MessageConstant.User.UpdateUserSuccessfulMessage);
            }

            _logger.LogInformation($"Update Brand {id} information failed");
            return Ok(MessageConstant.User.UpdateUserFailedMessage);
        }

        [HttpGet(ApiEndPointConstant.User.UserEndpoint)]
        [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var userResponse = _userService.GetUserById(userId);
            return Ok(userResponse);
        }

        [HttpPost("users/order")]
        public async Task<IActionResult> CreateUserOrder([FromBody] CreateUserOrderRequest req)
        {
            var userResponse = await _userService.CreateNewUserOrder(req);
            return Ok(userResponse);
        }

        [HttpGet(ApiEndPointConstant.User.UserBlogPostEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetBlogPostResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlogPost([FromQuery] string? brandCode, [FromQuery] int page,
            [FromQuery] int size)
        {
            var blogPostInBrand = await _blogPostService.GetBlogPostByBrandCode(brandCode, page, size);
            return Ok(blogPostInBrand);
        }

        [HttpGet(ApiEndPointConstant.User.UserOrderEndpoint)]
        [ProducesResponseType(typeof(IPaginate<ViewOrdersResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListOrderByUserId(Guid id, [FromQuery] OrderStatus status,
            [FromQuery] int page,
            [FromQuery] int size)
        {
            var response = await _orderService.GetListOrderByUserId(id, status, page, size);
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.User.OrderDetailsEndpoint)]
        [ProducesResponseType(typeof(IPaginate<GetOrderDetailResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderDetail(Guid id)
        {
            var response = await _orderService.GetOrderDetailUser(id);
            return Ok(response);
        }
    }
}
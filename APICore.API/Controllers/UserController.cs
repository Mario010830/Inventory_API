using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/user")]
    public class UserController : Controller
    {

        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UserController(IUserService userService, IMapper mapper)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.UserCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest userRequest)
        {
            var result = await _userService.CreateUser(userRequest);
            var userResponse = _mapper.Map<UserResponse>(result);

            return Created("", new ApiCreatedResponse(userResponse));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.UserRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetUsers(int? page, int? perPage, string sortOrder = null)
        {
            var users = await _userService.GetAllUsers(page, perPage, sortOrder);
            var userList = _mapper.Map<IEnumerable<UserResponse>>(users);
            return Ok(new ApiOkPaginatedResponse(userList, users.GetPaginationData));
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.UserDelete)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.DeleteUser(id);
            return NoContent();
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.UserRead)]
        public async Task<IActionResult> GetUserById(int id)
        {

            var user = await _userService.GetUser(id);
            return Ok(new ApiOkResponse(user));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.UserUpdate)]
        public async Task<IActionResult> EditUser(int id, UpdateUserRequest user)
        {
            await _userService.UpdateUser(id, user);
            return NoContent();
            
        }

    }
}
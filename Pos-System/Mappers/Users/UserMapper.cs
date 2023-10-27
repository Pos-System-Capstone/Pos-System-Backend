using AutoMapper;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.User;
using Pos_System.Domain.Models;

namespace Pos_System.API.Mappers.Users
{
    public class UserMapper : Profile
    {
        public UserMapper()
        {
            CreateMap<CreateNewUserRequest, User>();
            CreateMap<User, CreateNewUserResponse>();
        }
    }
}

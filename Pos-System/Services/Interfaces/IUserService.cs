using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.User;

namespace Pos_System.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, string? brandCode);
        Task<bool> UpdateUserInformation(Guid userId, UpdateUserRequest updateUserRequest);
        Task<GetUserResponse> GetUserById(Guid userId);
        Task<SignInResponse> LoginUser(LoginFirebase req );
        Task<SignInResponse> SignUpUser(CreateNewUserRequest newUserRequest, string? brandCode);
        Task<Guid> CreateNewUserOrder(CreateUserOrderRequest createNewOrderRequest);
    }
}

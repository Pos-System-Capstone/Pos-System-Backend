using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.User;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, string? brandCode);
        Task<bool> UpdateUserInformation(Guid userId, UpdateUserRequest updateUserRequest);
        Task<UserResponse> GetUserById(Guid userId);
        Task<SignInResponse> LoginUser(LoginFirebase req);
        Task<SignInResponse> SignUpUser(CreateNewUserRequest newUserRequest, string? brandCode);
        Task<Guid> CreateNewUserOrder(PrepareOrderRequest createNewOrderRequest);
        Task<GetUserInfo> ScanUser(string phone);

        Task<List<PromotionPointifyResponse>?> GetPromotionsAsync(string brandCode, Guid id);

        Task<IEnumerable<VoucherResponse>?> GetVoucherOfUser(Guid userId);
        Task<IPaginate<Transaction>> GetListTransactionOfUser(Guid userId, int page, int size);
    }
}
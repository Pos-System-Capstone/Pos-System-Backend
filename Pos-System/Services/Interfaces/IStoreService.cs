using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.Stores;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Stores;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces;

public interface IStoreService
{
    public Task<IPaginate<GetStoreResponse>>
        GetStoresInBrand(Guid brandId, string? searchShortName, int page, int size);

    public Task<GetStoreDetailResponse> GetStoreById(Guid storeId);

    public Task<CreateNewStoreResponse> CreateNewStore(CreateNewStoreRequest newStoreRequest);

    public Task<IPaginate<GetStoreEmployeesResponse>> GetStoreEmployees(Guid storeId, string? searchUserName, int page,
        int size);

    public Task<bool> UpdateStoreInformation(Guid storeId, UpdateStoreRequest updateStoreRequest);
    public Task<GetMenuDetailForStaffResponse> GetMenuDetailForStaff();
    public Task<GetStoreEndShiftStatisticsResponse> GetStoreEndShiftStatistics(Guid storeId, Guid sessionId);

    public Task<IPaginate<GetStoreResponse>> GetStoresInBrandByBrandCode(string? brandCode, int page, int size);
    public Task<IEnumerable<PromotionPointifyResponse>?> GetPromotionInStore(Guid storeId);
    public Task<List<PaymentType>?> GetPaymentsInStore(Guid storeId);
}
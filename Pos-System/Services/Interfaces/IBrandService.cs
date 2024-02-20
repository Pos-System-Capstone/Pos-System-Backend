using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response.Brands;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Payload.Response.Orders;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces;

public interface IBrandService
{
    public Task<CreateNewBrandResponse> CreateNewBrand(CreateNewBrandRequest newBrandRequest);

    public Task<IPaginate<GetBrandResponse>> GetBrands(string? searchBrandName, int page, int size);

    public Task<GetBrandResponse> GetBrandById(Guid brandId);

    public Task<bool> UpdateBrandInformation(Guid brandId, UpdateBrandRequest updateBrandRequest);

    public Task<GetMenuDetailForStaffResponse> GetMenus(string? brandCode);

    public Task<IPaginate<ViewOrdersResponse>> GetOrderInBrand(Guid brandId, int page, int size,
        DateTime? startDate, DateTime? endDate, OrderType? orderType, OrderStatus? status,PaymentTypeEnum? paymentType);
}
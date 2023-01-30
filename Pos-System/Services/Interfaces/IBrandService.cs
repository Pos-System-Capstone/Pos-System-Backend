using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response.Brands;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces;

public interface IBrandService
{
    public Task<CreateNewBrandResponse> CreateNewBrand(CreateNewBrandRequest newBrandRequest);

    public Task<IPaginate<GetBrandResponse>> GetBrands(string? searchBrandName, int page, int size);

    public Task<GetBrandResponse> GetBrandById(Guid brandId);

    public Task<bool> UpdateBrandInformation(Guid brandId, UpdateBrandRequest updateBrandRequest);
}
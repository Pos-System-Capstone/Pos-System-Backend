using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.Request.Products;
using Pos_System.Domain.Paginate;
using Pos_System.API.Enums;

namespace Pos_System.API.Services.Interfaces
{
    public interface IProductService
    {
        Task<GetProductDetailsResponse> GetProductById(Guid productId);

        Task<CreateNewProductResponse> CreateNewProduct(CreateNewProductRequest createNewProductRequest);

        Task<Guid> UpdateProduct(Guid productId, UpdateProductRequest updateProductRequest);

        Task<IPaginate<GetProductResponse>> GetProducts(string? name, ProductType? type, int page, int size);

        Task<IEnumerable<GetProductDetailsResponse>> GetProductsInBrand(Guid brandId);
        Task<Guid> CreateNewGroupProduct(Guid brandId, CreateNewGroupProductRequest createUpdateNewGroupProductRequest);
        Task<Guid> UpdateGroupProduct(Guid brandId, Guid groupProductId, UpdateGroupProductRequest updateGroupProductRequest);
        Task<IEnumerable<GetGroupProductListResponse>> GetGroupProductListOfCombo(Guid brandId, Guid productId);
        Task<Guid> UpdateProductInGroup(Guid groupProductId, Guid id, UpdateProductInGroupRequest updateProductInGroupRequest);
    }
}


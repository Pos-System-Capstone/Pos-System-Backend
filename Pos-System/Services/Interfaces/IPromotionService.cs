using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Promotion;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
	public interface IPromotionService
	{

        Task<IPaginate<GetPromotionResponse>> GetListPromotion(PromotionEnum? type, int page, int size);
        public Task<Guid?> CreateNewPromotion(CreatePromotionRequest request);

	}
}


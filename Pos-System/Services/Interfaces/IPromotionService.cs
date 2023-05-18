﻿using System;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
	public interface IPromotionService
	{

        Task<IPaginate<GetPromotionResponse>> GetListPromotion(Guid brandId, int page, int size);

    }
}

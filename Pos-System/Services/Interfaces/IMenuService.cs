﻿using System.Drawing;
using Pos_System.API.Payload.Request.Menus;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.Domain.Paginate;

namespace Pos_System.API.Services.Interfaces
{
    public interface IMenuService
    {
        public Task<Guid> CreateNewMenu(CreateNewMenuRequest createNewMenuRequest);

        public Task<HasBaseMenuResponse> CheckHasBaseMenuInBrand(Guid brandId);
        public Task<IPaginate<GetMenuDetailResponse>> GetMenus(Guid brandId, string? code, int page = 1, int size = 10);
    }
}

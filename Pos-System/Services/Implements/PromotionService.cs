using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Promotion;
using Pos_System.API.Payload.Response.Promotion;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class PromotionService : BaseService<PromotionService>, IPromotionService
    {
        private readonly IMenuService _menuService;
        public readonly IOrderService _orderService;

        public PromotionService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<PromotionService> logger,
            IMapper mapper, IHttpContextAccessor httpContextAccessor, IMenuService menuService,
            IOrderService orderService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _menuService = menuService;
            _orderService = orderService;
        }


        public async Task<IPaginate<GetPromotionResponse>> GetListPromotion(PromotionEnum? type, int page, int size)
        {
            Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
            if (userBrandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            IPaginate<GetPromotionResponse> responese = await _unitOfWork.GetRepository<Promotion>().GetPagingListAsync(
                selector: x => new GetPromotionResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Description = x.Description,
                    Type = EnumUtil.ParseEnum<PromotionEnum>(x.Type),
                    MaxDiscount = x.MaxDiscount,
                    MinConditionAmount = x.MinConditionAmount,
                    DiscountAmount = x.DiscountAmount,
                    DiscountPercent = x.DiscountPercent,
                    ListProductApply = x.PromotionProductMappings.Select(x => new ProductApply(x.ProductId)).ToList(),
                    Status = x.Status
                },
                include: x => x.Include(product => product.PromotionProductMappings),
                predicate: (type == null)
                    ? x => x.BrandId.Equals(userBrandId)
                    : x => x.BrandId.Equals(userBrandId) && x.Type.Equals(type.GetDescriptionFromEnum()),
                orderBy: x => x.OrderBy(x => x.Name),
                page: page,
                size: size
            );
            return responese;
        }

        public async Task<Guid?> CreateNewPromotion(CreatePromotionRequest request)
        {
            _logger.LogInformation($"Start create new : {request}");
            Guid brandId = Guid.Parse(GetBrandIdFromJwt());
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(brandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            Promotion newPromotion = new Promotion()
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                BrandId = brandId,
                Description = request.Description,
                Status = EnumUtil.GetDescriptionFromEnum(request.Status),
                Type = EnumUtil.GetDescriptionFromEnum(request.Type),
                MaxDiscount = request.MaxDiscount,
                MinConditionAmount = request.MinConditionAmount,
                DiscountAmount = request.DiscountAmount,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DayFilter = request.DayFilter,
                DiscountPercent = request.DiscountPercent,

            };
            await _unitOfWork.GetRepository<Promotion>().InsertAsync(newPromotion);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            if (!isSuccessful)
            {
                throw new BadHttpRequestException(MessageConstant.Promotion.CreateNewPromotionFailedMessage);
            }
            return newPromotion.Id;
        }
    }
}
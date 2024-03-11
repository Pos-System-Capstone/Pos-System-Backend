using System.Security.Claims;
using AutoMapper;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services
{
    public abstract class BaseService<T> where T : class
    {
        protected IUnitOfWork<PosSystemContext> _unitOfWork;
        protected ILogger<T> _logger;
        protected IMapper _mapper;
        protected IHttpContextAccessor _httpContextAccessor;

        public BaseService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<T> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        protected string GetUsernameFromJwt()
        {
            string username = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return username;
        }

        protected string GetRoleFromJwt()
        {
            string role = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return role;
        }

        //Use for employee and store manager
        protected async Task<bool> CheckIsUserInStore(Account account, Store store)
        {
            ICollection<StoreAccount> storeAccount = await _unitOfWork.GetRepository<StoreAccount>()
                .GetListAsync(predicate: s => s.StoreId.Equals(store.Id));
            return storeAccount.Select(x => x.AccountId).Contains(account.Id);
        }

        protected string GetBrandIdFromJwt()
        {
            return _httpContextAccessor?.HttpContext?.User?.FindFirstValue("brandId");
        }

        protected string GetStoreIdFromJwt()
        {
            return _httpContextAccessor?.HttpContext?.User?.FindFirstValue("storeId");
        }
    }
}
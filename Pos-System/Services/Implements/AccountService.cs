using System.Linq.Expressions;
using System.Security.Cryptography.Xml;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request;
using Pos_System.API.Payload.Request.Accounts;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Response;
using Pos_System.API.Payload.Response.Accounts;
using Pos_System.API.Services;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Domain.Paginate;
using Pos_System.Repository.Interfaces;

namespace Pos_System.API.Services.Implements
{
    public class AccountService : BaseService<AccountService>, IAccountService
    {
        public AccountService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<AccountService> logger, IMapper mapper,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            Expression<Func<Account, bool>> searchFilter = p =>
                p.Username.Equals(loginRequest.Username) &&
                p.Password.Equals(PasswordUtil.HashPassword(loginRequest.Password));
            Account account = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: searchFilter, include: p => p.Include(x => x.Role));
            if (account == null) return null;
            RoleEnum role = EnumUtil.ParseEnum<RoleEnum>(account.Role.Name);
            Tuple<string, Guid> guidClaim = null;
            LoginResponse loginResponse = null;
            string? brandPicUrl = null;

            switch (role)
            {
                case RoleEnum.BrandAdmin:
                case RoleEnum.BrandManager:
                    Guid brandId = await _unitOfWork.GetRepository<BrandAccount>().SingleOrDefaultAsync(
                        selector: x => x.BrandId,
                        predicate: x => x.AccountId.Equals(account.Id));
                    guidClaim = new Tuple<string, Guid>("brandId", brandId);
                    var brand = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                        predicate: brand => brand.Id.Equals(brandId)
                    );
                    loginResponse = new BrandAccountLoginResponse(account.Id, account.Username, account.Name,
                        account.Role.Name, account.Status, brandId, brand.PicUrl, brand.BrandCode);
                    break;
                case RoleEnum.StoreManager:
                case RoleEnum.Staff:
                    var storeAccount = await _unitOfWork.GetRepository<StoreAccount>().SingleOrDefaultAsync(
                        predicate: x => x.AccountId.Equals(account.Id));
                    guidClaim = new Tuple<string, Guid>("storeId", storeAccount.StoreId);
                    var store = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                        predicate: store => store.Id.Equals(storeAccount.StoreId),
                        include: store => store.Include(store => store.Brand)
                    );
                    loginResponse = new StoreAccountLoginResponse(account.Id, account.Username, account.Name,
                        account.Role.Name, account.Status, store.Id, store.Code, store.Brand.BrandCode,
                        store.Brand.PicUrl);
                    break;
                default:
                    loginResponse = new LoginResponse(account.Id, account.Username, account.Name, account.Role.Name,
                        account.Status, brandPicUrl);
                    break;
            }

            var token = JwtUtil.GenerateJwtToken(account, guidClaim);
            loginResponse.AccessToken = token;
            return loginResponse;
        }

        public async Task<CreateNewBrandAccountResponse> CreateNewBrandAccount(
            CreateNewBrandAccountRequest createNewBrandAccountRequest)
        {
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(createNewBrandAccountRequest.BrandId));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            if (createNewBrandAccountRequest.Role != RoleEnum.BrandAdmin &&
                createNewBrandAccountRequest.Role != RoleEnum.BrandManager)
                throw new BadHttpRequestException(MessageConstant.Account.CreateAccountWithWrongRoleMessage);
            _logger.LogInformation(
                $"Create new account for brand {brand.Name} with account {createNewBrandAccountRequest.Username}, role: {createNewBrandAccountRequest.Role.GetDescriptionFromEnum()} ");
            createNewBrandAccountRequest.Password = PasswordUtil.HashPassword(createNewBrandAccountRequest.Password);
            Account newBrandAccount = _mapper.Map<Account>(createNewBrandAccountRequest);
            newBrandAccount.Id = Guid.NewGuid();
            newBrandAccount.RoleId = await _unitOfWork.GetRepository<Role>().SingleOrDefaultAsync(selector: x => x.Id,
                predicate: x => x.Name.Equals(createNewBrandAccountRequest.Role.GetDescriptionFromEnum()));
            newBrandAccount.BrandAccount = new BrandAccount()
            {
                Id = Guid.NewGuid(),
                AccountId = newBrandAccount.Id,
                BrandId = brand.Id
            };
            await _unitOfWork.GetRepository<Account>().InsertAsync(newBrandAccount);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            CreateNewBrandAccountResponse response =
                isSuccessful ? _mapper.Map<CreateNewBrandAccountResponse>(newBrandAccount) : null;
            return response;
        }

        public async Task<IPaginate<GetAccountResponse>> GetBrandAccounts(Guid brandId, string? searchUsername,
            RoleEnum? role, int page, int size)
        {
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandIdMessage);
            IPaginate<GetAccountResponse> accountsInBrand = new Paginate<GetAccountResponse>();
            searchUsername = searchUsername?.Trim().ToLower();
            switch (role)
            {
                case RoleEnum.BrandAdmin:
                case RoleEnum.BrandManager:
                    accountsInBrand = await _unitOfWork.GetRepository<Account>()
                        .GetPagingListAsync(
                            selector: x => new GetAccountResponse
                            {
                                Id = x.Id,
                                Username = x.Username,
                                Name = x.Name,
                                Role = EnumUtil.ParseEnum<RoleEnum>(x.Role.Name),
                                Status = EnumUtil.ParseEnum<AccountStatus>(x.Status)
                            },
                            predicate: string.IsNullOrEmpty(searchUsername)
                                ? x => x.BrandAccount != null && x.BrandAccount.BrandId.Equals(brandId) &&
                                       x.Role.Name.Equals(role.GetDescriptionFromEnum())
                                : x => x.BrandAccount != null && x.BrandAccount.BrandId.Equals(brandId) &&
                                       x.Role.Name.Equals(role.GetDescriptionFromEnum()) &&
                                       x.Username.ToLower().Contains(searchUsername),
                            orderBy: x => x.OrderBy(x => x.Username),
                            include: x => x.Include(x => x.BrandAccount).Include(x => x.Role),
                            page: page,
                            size: size);
                    break;
                case RoleEnum.StoreManager:
                case RoleEnum.Staff:
                    IEnumerable<Guid> storeIds = await _unitOfWork.GetRepository<Store>()
                        .GetListAsync(selector: x => x.Id, predicate: x => x.BrandId.Equals(brandId));
                    accountsInBrand = await _unitOfWork.GetRepository<Account>()
                        .GetPagingListAsync(
                            selector: x => new GetAccountResponse(x.Id, x.Username, x.Name,
                                EnumUtil.ParseEnum<RoleEnum>(x.Role.Name), EnumUtil.ParseEnum<AccountStatus>(x.Status)),
                            predicate: string.IsNullOrEmpty(searchUsername)
                                ? x => x.StoreAccount != null && storeIds.OrEmpty().Contains(x.StoreAccount.StoreId) &&
                                       x.Role.Name.Equals(role.GetDescriptionFromEnum())
                                : x => x.StoreAccount != null && storeIds.OrEmpty().Contains(x.StoreAccount.StoreId) &&
                                       x.Role.Name.Equals(role.GetDescriptionFromEnum()) &&
                                       x.Username.ToLower().Contains(searchUsername),
                            orderBy: x => x.OrderBy(x => x.Username),
                            include: x => x.Include(x => x.StoreAccount),
                            page: page,
                            size: size);
                    break;
                default:
                    accountsInBrand = await _unitOfWork.GetRepository<Account>()
                        .GetPagingListAsync(
                            selector: x => new GetAccountResponse
                            {
                                Id = x.Id,
                                Username = x.Username,
                                Name = x.Name,
                                Role = EnumUtil.ParseEnum<RoleEnum>(x.Role.Name),
                                Status = EnumUtil.ParseEnum<AccountStatus>(x.Status)
                            },
                            predicate: string.IsNullOrEmpty(searchUsername)
                                ? x => x.BrandAccount != null && x.BrandAccount.BrandId.Equals(brandId)
                                : x => x.BrandAccount != null && x.BrandAccount.BrandId.Equals(brandId) &&
                                       x.Username.ToLower().Contains(searchUsername),
                            orderBy: x => x.OrderBy(x => x.Username),
                            include: x => x.Include(x => x.BrandAccount).Include(x => x.Role),
                            page: page,
                            size: size);
                    break;
            }

            return accountsInBrand;
        }

        public async Task<bool> UpdateAccountStatus(Guid accountId,
            UpdateAccountStatusRequest updateAccountStatusRequest)
        {
            if (!updateAccountStatusRequest.Op.Equals("/update") || !updateAccountStatusRequest.Path.Equals("/status"))
                throw new BadHttpRequestException(MessageConstant.Account.UpdateAccountStatusRequestWrongFormatMessage);
            bool isValidValue = Enum.TryParse(updateAccountStatusRequest.Value, out AccountStatus newStatus);
            if (!isValidValue)
                throw new BadHttpRequestException(MessageConstant.Account.UpdateAccountStatusRequestWrongFormatMessage);

            RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
            switch (userRole)
            {
                case RoleEnum.StoreManager:
                {
                    Account updatedAccount = await _unitOfWork.GetRepository<Account>()
                        .SingleOrDefaultAsync(
                            predicate: x =>
                                x.Id.Equals(accountId) && x.Role.Name.Equals(RoleEnum.Staff.GetDescriptionFromEnum()),
                            include: x => x.Include(x => x.Role)
                        );
                    if (updatedAccount == null)
                        throw new BadHttpRequestException(MessageConstant.Account.AccountNotFoundMessage);
                    updatedAccount.Status = EnumUtil.GetDescriptionFromEnum(newStatus);
                    _unitOfWork.GetRepository<Account>().UpdateAsync(updatedAccount);
                    break;
                }
                case RoleEnum.SysAdmin:
                {
                    Account updatedAccount = await _unitOfWork.GetRepository<Account>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(accountId));
                    if (updatedAccount == null)
                        throw new BadHttpRequestException(MessageConstant.Account.AccountNotFoundMessage);
                    updatedAccount.Status = EnumUtil.GetDescriptionFromEnum(newStatus);
                    _unitOfWork.GetRepository<Account>().UpdateAsync(updatedAccount);
                    break;
                }
                case RoleEnum.BrandManager:
                {
                    Account updatedAccount = await _unitOfWork.GetRepository<Account>()
                        .SingleOrDefaultAsync(
                            predicate: x =>
                                x.Id.Equals(accountId) &&
                                x.Role.Name.Equals(RoleEnum.StoreManager.GetDescriptionFromEnum()),
                            include: x => x.Include(x => x.Role)
                        );
                    if (updatedAccount == null)
                        throw new BadHttpRequestException(MessageConstant.Account.AccountNotFoundMessage);
                    updatedAccount.Status = EnumUtil.GetDescriptionFromEnum(newStatus);
                    _unitOfWork.GetRepository<Account>().UpdateAsync(updatedAccount);
                    break;
                }
                default:
                {
                    throw new BadHttpRequestException(MessageConstant.Account.UserUnauthorizedMessage);
                }
            }

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }

        public async Task<GetAccountResponse> GetAccountDetail(Guid id)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Account.EmptyAccountId);
            GetAccountResponse account = await _unitOfWork.GetRepository<Account>().SingleOrDefaultAsync(
                selector: x => new GetAccountResponse(x.Id, x.Username, x.Name,
                    EnumUtil.ParseEnum<RoleEnum>(x.Role.Name), EnumUtil.ParseEnum<AccountStatus>(x.Status)),
                predicate: x => x.Id.Equals(id)
            );
            switch (account.Role)
            {
                case RoleEnum.Staff:
                case RoleEnum.StoreManager:
                {
                    account.StoreId = await _unitOfWork.GetRepository<StoreAccount>()
                        .SingleOrDefaultAsync(selector: x => x.StoreId, predicate: x => x.AccountId.Equals(account.Id));
                    var store = await _unitOfWork.GetRepository<Store>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(account.StoreId),
                            include: x => x.Include(brand => brand.Brand));
                    account.BrandCode = store.Brand.BrandCode;
                    account.StoreCode = store.Code;
                    break;
                }
                case RoleEnum.BrandManager:
                case RoleEnum.BrandAdmin:
                {
                    account.BrandId = await _unitOfWork.GetRepository<BrandAccount>()
                        .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.AccountId.Equals(account.Id));
                    var brand = await _unitOfWork.GetRepository<Brand>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(account.BrandId)
                        );
                    account.BrandCode = brand.BrandCode;
                    break;
                }
            }

            return account;
        }

        public async Task<bool> UpdateStoreAccountInformation(Guid accountId,
            UpdateStoreAccountInformationRequest staffAccountInformationRequest)
        {
            if (accountId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Account.EmptyAccountId);
            Account updatedAccount = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(accountId));
            if (updatedAccount == null)
                throw new BadHttpRequestException(MessageConstant.Account.AccountNotFoundMessage);

            updatedAccount.Name = string.IsNullOrEmpty(staffAccountInformationRequest.Name)
                ? updatedAccount.Name
                : staffAccountInformationRequest.Name;
            updatedAccount.Password = string.IsNullOrEmpty(staffAccountInformationRequest.Password)
                ? updatedAccount.Password
                : PasswordUtil.HashPassword(staffAccountInformationRequest.Password.Trim());

            _unitOfWork.GetRepository<Account>().UpdateAsync(updatedAccount);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }


        public async Task<CreateNewStoreAccountResponse> CreateNewStoreAccount(Guid storeId,
            CreateNewStoreAccountRequest createNewStoreAccountRequest)
        {
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            RoleEnum userRole = EnumUtil.ParseEnum<RoleEnum>(GetRoleFromJwt());
            RoleEnum newAccountRole;
            switch (userRole)
            {
                case RoleEnum.BrandAdmin:
                    newAccountRole = RoleEnum.StoreManager;
                    Guid userBrandId = Guid.Parse(GetBrandIdFromJwt());
                    if (!store.BrandId.Equals(userBrandId))
                        throw new BadHttpRequestException(MessageConstant.Store.StoreNotInBrandMessage);
                    break;
                case RoleEnum.StoreManager:
                    newAccountRole = RoleEnum.Staff;
                    break;
                default:
                    throw new BadHttpRequestException(MessageConstant.Store.CreateNewStoreAccountUnauthorizedMessage);
            }

            _logger.LogInformation(
                $"Create new account for store {store.Name} with account {createNewStoreAccountRequest.Username}, role: {newAccountRole} ");
            createNewStoreAccountRequest.Password = PasswordUtil.HashPassword(createNewStoreAccountRequest.Password);
            Account newStoreAccount = _mapper.Map<Account>(createNewStoreAccountRequest);
            newStoreAccount.Id = Guid.NewGuid();
            newStoreAccount.RoleId = await _unitOfWork.GetRepository<Role>().SingleOrDefaultAsync(selector: x => x.Id,
                predicate: x => x.Name.Equals(newAccountRole.GetDescriptionFromEnum()));
            newStoreAccount.Status = AccountStatus.Active.GetDescriptionFromEnum();
            newStoreAccount.StoreAccount = new StoreAccount()
            {
                Id = Guid.NewGuid(),
                AccountId = newStoreAccount.Id,
                StoreId = store.Id
            };
            await _unitOfWork.GetRepository<Account>().InsertAsync(newStoreAccount);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            CreateNewStoreAccountResponse response =
                isSuccessful ? _mapper.Map<CreateNewStoreAccountResponse>(newStoreAccount) : null;
            if (response != null)
            {
                response.StoreId = store.Id;
                response.Role = newAccountRole;
            }

            return response;
        }
    }
}
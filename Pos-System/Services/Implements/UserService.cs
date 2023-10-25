using AutoMapper;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.Brands;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response;
using Pos_System.API.Payload.Response.User;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ZaloPay.Helper;

namespace Pos_System.API.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        public UserService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<UserService> logger,
            IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, string? brandCode)
        {
            if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);
            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            _logger.LogInformation($"Create new brand with {newUserRequest.FullName}");
            User newUser = _mapper.Map<User>(newUserRequest);
            newUser.Status = UserStatus.Active.GetDescriptionFromEnum();
            newUser.Id = Guid.NewGuid();
            newUser.BrandId = brand.Id;
            
            newUser.FireBaseUid = "lELWhqB973M62ShrSKMFWJTXc703";
            newUser.Fcmtoken = "";

            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            //tạo membership bên pointify
            string createMemberPromoUrl = $"https://api-pointify.reso.vn/api/memberships?apiKey={brand.Id}";
            var data = new
            {
                membershipId = newUser.Id,
                fullname = newUser.FullName,
                email = newUser.Email,
                gender = newUserRequest.Gender.Equals("MALE")? 1 : (newUserRequest.Gender.Equals("FEMALE")? 2 : 3),
                phoneNumber = newUser.PhoneNumber,
                memberProgramId = "52b1f27d-885f-4b1c-9773-91ed894b4eac"
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(createMemberPromoUrl, content);
            CreateNewUserResponse createNewUserResponse = null;
            if (isSuccessful)
            {
                createNewUserResponse = _mapper.Map<CreateNewUserResponse>(newUser);
            }

            return createNewUserResponse;
        }

        public async Task<SignInResponse> LoginUser(SignInRequest req)
        {
            User userLogin = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(req.PhoneNumber) && x.FireBaseUid.Equals(req.UID)
                && x.Status.Equals("Active"));
            if (userLogin == null)
            {
                return new SignInResponse
                {
                    message = "Phone Number or UID is not correct"
                };
            }
            else
            {
                Guid storeId = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                    selector: x => x.Id,
                    predicate: x => x.BrandId.Equals(userLogin.BrandId));
                Tuple<string, Guid> guidClaim = new Tuple<string, Guid>("storeId", storeId);
                //string? brandPicUrl = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                //    selector: store => store.Brand.PicUrl,
                //    predicate: store => store.Id.Equals(storeId),
                //    include: store => store.Include(store => store.Brand)

                //);
                IConfiguration configuration = new ConfigurationBuilder()
                 .AddEnvironmentVariables(EnvironmentVariableConstant.Prefix).Build();
                JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
                SymmetricSecurityKey secrectKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
                var credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
                string issuer = configuration.GetValue<string>(JwtConstant.Issuer);
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub,userLogin.FullName),
                    new Claim(ClaimTypes.Role,"User"),
                };
                if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
                var expires = "User".Equals(RoleEnum.User.GetDescriptionFromEnum()) ? DateTime.Now.AddDays(1) :
                    DateTime.Now.AddMinutes(configuration.GetValue<long>(JwtConstant.TokenExpireInMinutes));
                var token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
                string accesstken = jwtHandler.WriteToken(token);
                return new SignInResponse
                {
                    message = "Login success",
                    AccessToken = accesstken,
                    UserInfo = new UserResponse()
                    {
                        Id = userLogin.Id,
                        FullName = userLogin.FullName,
                        PhoneNumber = userLogin.PhoneNumber,
                        BrandId = userLogin.BrandId,
                        Email = userLogin.Email,
                        FireBaseUid = userLogin.FireBaseUid,
                        CreatedAt = userLogin.CreatedAt,
                        Fcmtoken = userLogin.Fcmtoken,
                        Gender = userLogin.Gender,
                        Status = userLogin.Status,
                        UpdatedAt = userLogin.UpdatedAt,
                        UrlImg = userLogin.UrlImg
                    }
                };
            }
        }

        public async Task<SignInResponse> SignUpUser(CreateNewUserRequest newUserRequest, string? brandCode)
        {
            var newUser = await CreateNewUser(newUserRequest, brandCode);
            if(newUser == null)
            {
                return new SignInResponse
                {
                    message = "Create new user failed"
                };
            }
            User user= await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(newUser.Id)
                           && x.Status.Equals("Active"));
            Guid brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
            selector: brand => brand.Id,
            predicate: brand => brand.BrandCode.Equals(brandCode)
            );
            Guid storeId = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                    selector: x => x.Id,
                    predicate: x => x.BrandId.Equals(brandId));
            Tuple<string, Guid> guidClaim = new Tuple<string, Guid>("storeId", storeId);
            //string? brandPicUrl = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
            //    selector: store => store.Brand.PicUrl,
            //    predicate: store => store.Id.Equals(storeId),
            //    include: store => store.Include(store => store.Brand)

            //);
            IConfiguration configuration = new ConfigurationBuilder()
             .AddEnvironmentVariables(EnvironmentVariableConstant.Prefix).Build();
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
            SymmetricSecurityKey secrectKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
            var credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
            string issuer = configuration.GetValue<string>(JwtConstant.Issuer);
            List<Claim> claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub,newUser.FullName),
                    new Claim(ClaimTypes.Role,"User"),
                };
            if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            var expires = "User".Equals(RoleEnum.User.GetDescriptionFromEnum()) ? DateTime.Now.AddDays(1) :
                DateTime.Now.AddMinutes(configuration.GetValue<long>(JwtConstant.TokenExpireInMinutes));
            var token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
            string accesstken = jwtHandler.WriteToken(token);
            return new SignInResponse
            {
                message = "Sign Up success",
                AccessToken = accesstken,
                UserInfo = new UserResponse()
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    BrandId = brandId,
                    Email = user.Email,
                    FireBaseUid = user.FireBaseUid,
                    CreatedAt = user.CreatedAt,
                    Fcmtoken = user.Fcmtoken,
                    Gender = user.Gender,
                    Status = user.Status,
                    UpdatedAt = user.UpdatedAt,
                    UrlImg = user.UrlImg
                }
            };
        }

        public async Task<bool> UpdateUserInformation(Guid userId, UpdateUserRequest updatedUserRequest)
        {
            if (userId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.User.EmptyUserId);
            User updatedUser = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(userId));

            if (updatedUser == null) throw new BadHttpRequestException(MessageConstant.User.UserNotFound);

            _logger.LogInformation($"Start update user {userId}");
            updatedUserRequest.TrimString();
            updatedUser.FullName = string.IsNullOrEmpty(updatedUserRequest.FullName) ? updatedUser.FullName : updatedUserRequest.FullName;
            updatedUser.Email = string.IsNullOrEmpty(updatedUserRequest.Email) ? updatedUser.Email : updatedUserRequest.Email;
            updatedUser.Gender = string.IsNullOrEmpty(updatedUserRequest.Gender) ? updatedUser.Gender : updatedUserRequest.Gender;
            updatedUser.PhoneNumber = string.IsNullOrEmpty(updatedUserRequest.PhoneNunmer) ? updatedUser.PhoneNumber : updatedUserRequest.PhoneNunmer;
            _unitOfWork.GetRepository<User>().UpdateAsync(updatedUser);

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }

        public async Task<GetUserResponse> GetUserById(Guid userId)
        {
            if(userId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.User.EmptyUserId);

            GetUserResponse userResponse = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: x => new GetUserResponse(x.PhoneNumber, x.FullName, x.Gender, x.Email),
                predicate: x => x.Id.Equals(userId));

            return userResponse;
        }
    }
}

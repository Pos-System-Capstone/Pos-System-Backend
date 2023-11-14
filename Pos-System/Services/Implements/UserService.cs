﻿using AutoMapper;
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
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.Orders;
using ZaloPay.Helper;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Pos_System.API.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        public const double VAT_PERCENT = 0.08;
        public const double VAT_STANDARD = 1.08;
        private readonly IFirebaseService _firebaseService;

        public UserService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<UserService> logger,
            IMapper mapper, IHttpContextAccessor httpContextAccessor, IFirebaseService firebaseService) : base(
            unitOfWork, logger, mapper,
            httpContextAccessor)
        {
            _firebaseService = firebaseService;
        }


        public async Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, string? brandCode)
        {
            if (brandCode == null) throw new BadHttpRequestException(MessageConstant.Brand.EmptyBrandCodeMessage);

            Brand brand = await _unitOfWork.GetRepository<Brand>()
                .SingleOrDefaultAsync(predicate: x => x.BrandCode.Equals(brandCode));
            if (brand == null) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            _logger.LogInformation($"Create new brand with {newUserRequest.FullName}");
            User newUser = new User()
            {
                Id = Guid.NewGuid(),
                PhoneNumber = newUserRequest.PhoneNunmer,
                Status = UserStatus.Active.GetDescriptionFromEnum(),
                FullName = newUserRequest.FullName,
                BrandId = brand.Id,
                Gender = newUserRequest.Gender,
                FireBaseUid = newUserRequest.FireBaseUid,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                UpdatedAt = TimeUtils.GetCurrentSEATime(),
            };

            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            //tạo membership bên pointify
            string createMemberPromoUrl = $"https://api-pointify.reso.vn/api/memberships?apiKey={brand.Id}";
            var data = new
            {
                membershipId = newUser.Id,
                fullname = newUser.FullName,
                email = newUser.Email,
                gender = newUserRequest.Gender.Equals("MALE") ? 1 : (newUserRequest.Gender.Equals("FEMALE") ? 2 : 3),
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

        public async Task<SignInResponse> LoginUser(LoginFirebase req)
        {
            var cred = await _firebaseService.VerifyIdToken(req.Token);
            if (cred == null) throw new BadHttpRequestException("Token Firebase không hợp lệ!");
            var firebaseClaims = cred.Claims;

            var phone = firebaseClaims.First(c => c.Key == "phone_number").Value.ToString();
            var uid = firebaseClaims.First(c => c.Key == "user_id").Value.ToString();
            //if (email.Split("@").Last() != "fpt.edu.vn" && email != "johnnymc2001@gmail.com")
            //    throw new BadRequestException("The system currently only accepted @fpt.edu.vn email!", ErrorNameValues.InvalidCredential);


            User userLogin = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(phone)
                                                      && x.Status.Equals("Active"));
            DateTime expires;
            IConfiguration configuration;
            JwtSecurityTokenHandler jwtHandler;
            SymmetricSecurityKey secrectKey;
            SigningCredentials? credentials;
            string issuer;
            List<Claim> claims;
            Tuple<string, Guid> guidClaim;
            JwtSecurityToken? token;
            string accesstken;
            string? brandCode;
            Guid brandId;
            if (userLogin == null)
            {
                CreateNewUserRequest newUserRequest = new CreateNewUserRequest()
                {
                    PhoneNunmer = phone,
                    FullName = "Người dùng",
                    Gender = "ORTHER",
                    FireBaseUid = uid
                };
                var newUser = await CreateNewUser(newUserRequest, req.BrandCode);

                User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x =>
                    x.Id.Equals(newUser.Id)
                    && x.Status.Equals("Active"));
                brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                    selector: brand => brand.Id,
                    predicate: brand => brand.BrandCode.Equals(req.BrandCode)
                );
                guidClaim = new Tuple<string, Guid>("brandId", brandId);
                //string? brandPicUrl = await _unitOfWork.GetRepository<Store>().SingleOrDefaultAsync(
                //    selector: store => store.Brand.PicUrl,
                //    predicate: store => store.Id.Equals(storeId),
                //    include: store => store.Include(store => store.Brand)

                //);
                configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables(EnvironmentVariableConstant.Prefix).Build();
                jwtHandler = new JwtSecurityTokenHandler();
                secrectKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
                credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
                issuer = configuration.GetValue<string>(JwtConstant.Issuer);
                claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, newUser.FullName),
                    new Claim(ClaimTypes.Role, "User"),
                };
                if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
                expires = "User".Equals(RoleEnum.User.GetDescriptionFromEnum())
                    ? DateTime.Now.AddDays(1)
                    : DateTime.Now.AddMinutes(configuration.GetValue<long>(JwtConstant.TokenExpireInMinutes));
                token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
                accesstken = jwtHandler.WriteToken(token);
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

            brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: brand => brand.Id,
                predicate: brand => brand.BrandCode.Equals(req.BrandCode)
            );
            guidClaim = new Tuple<string, Guid>("brandId", brandId);
            configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(EnvironmentVariableConstant.Prefix).Build();
            jwtHandler = new JwtSecurityTokenHandler();
            secrectKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
            credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
            issuer = configuration.GetValue<string>(JwtConstant.Issuer);
            claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userLogin.FullName),
                new Claim(ClaimTypes.Role, "User"),
            };
            if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            expires = "User".Equals(RoleEnum.User.GetDescriptionFromEnum())
                ? DateTime.Now.AddDays(1)
                : DateTime.Now.AddMinutes(configuration.GetValue<long>(JwtConstant.TokenExpireInMinutes));
            token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
            accesstken = jwtHandler.WriteToken(token);
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

        public async Task<SignInResponse> SignUpUser(CreateNewUserRequest newUserRequest, string? brandCode)
        {
            var newUser = await CreateNewUser(newUserRequest, brandCode);
            if (newUser == null)
            {
                return new SignInResponse
                {
                    message = "Create new user failed"
                };
            }

            User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x =>
                x.Id.Equals(newUser.Id)
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
            SymmetricSecurityKey secrectKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>(JwtConstant.SecretKey)));
            var credentials = new SigningCredentials(secrectKey, SecurityAlgorithms.HmacSha256Signature);
            string issuer = configuration.GetValue<string>(JwtConstant.Issuer);
            List<Claim> claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, newUser.FullName),
                new Claim(ClaimTypes.Role, "User"),
            };
            if (guidClaim != null) claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            var expires = "User".Equals(RoleEnum.User.GetDescriptionFromEnum())
                ? DateTime.Now.AddDays(1)
                : DateTime.Now.AddMinutes(configuration.GetValue<long>(JwtConstant.TokenExpireInMinutes));
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
            updatedUser.FullName = string.IsNullOrEmpty(updatedUserRequest.FullName)
                ? updatedUser.FullName
                : updatedUserRequest.FullName;
            updatedUser.Email = string.IsNullOrEmpty(updatedUserRequest.Email)
                ? updatedUser.Email
                : updatedUserRequest.Email;
            updatedUser.Gender = string.IsNullOrEmpty(updatedUserRequest.Gender)
                ? updatedUser.Gender
                : updatedUserRequest.Gender;
            updatedUser.PhoneNumber = string.IsNullOrEmpty(updatedUserRequest.PhoneNunmer)
                ? updatedUser.PhoneNumber
                : updatedUserRequest.PhoneNunmer;
            _unitOfWork.GetRepository<User>().UpdateAsync(updatedUser);

            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;
            return isSuccessful;
        }

        public async Task<UserResponse> GetUserById(Guid userId)
        {
            if (userId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.User.EmptyUserId);

            User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: x => x.Id.Equals(userId));
            if (user == null)
            {
                throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            }

            using var client = new HttpClient();
            var url =
                $"https://api-pointify.reso.vn/api/memberships/{userId}";
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(msg);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
                throw new BadHttpRequestException(MessageConstant.User.MembershipNotFound);
            var responseContent =
                JsonConvert.DeserializeObject<MemberDetailsResponse>(response.Content
                    .ReadAsStringAsync().Result);
            UserResponse userResponse = new UserResponse()
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Gender = user.Gender,
                UrlImg = user.UrlImg,
                BrandId = user.BrandId,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Wallets = responseContent?.MemberWallet,
                Level = responseContent?.MemberLevel
            };
            return userResponse;
        }

        public async Task<Guid> CreateNewUserOrder(PrepareOrderRequest createNewOrderRequest)
        {
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(createNewOrderRequest.StoreId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
                x.StoreId.Equals(createNewOrderRequest.StoreId)
                && DateTime.Compare(x.StartDateTime, currentTime) < 0
                && DateTime.Compare(x.EndDateTime, currentTime) > 0);
            if (currentUserSession == null)
                throw new BadHttpRequestException(MessageConstant.Order.CanNotCreateOrderInThisTime);
            if (!createNewOrderRequest.ProductList.Any())
                throw new BadHttpRequestException(MessageConstant.Order.NoProductsInOrderMessage);

            string newInvoiceId = store.Code + currentTimeStamp;
            int defaultGuest = 1;

            double vatAmount = (createNewOrderRequest.FinalAmount / VAT_STANDARD) * VAT_PERCENT;

            Order newOrder = new Order()
            {
                Id = Guid.NewGuid(),
                CheckInPerson = Guid.Parse("6CFADCDC-25C2-4F0C-8335-5B45698B2375"),
                CheckInDate = currentTime,
                CheckOutDate = currentTime,
                InvoiceId = newInvoiceId,
                TotalAmount = createNewOrderRequest.TotalAmount,
                Discount = createNewOrderRequest.DiscountAmount,
                FinalAmount = createNewOrderRequest.FinalAmount,
                Vat = VAT_PERCENT,
                Vatamount = vatAmount,
                OrderType = createNewOrderRequest.OrderType.GetDescriptionFromEnum(),
                NumberOfGuest = defaultGuest,
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = createNewOrderRequest.PaymentType.GetDescriptionFromEnum()
            };


            List<OrderDetail> orderDetails = new List<OrderDetail>();
            List<PromotionOrderMapping> promotionMappingList = new List<PromotionOrderMapping>();
            createNewOrderRequest.ProductList.ForEach(product =>
            {
                Guid masterOrderDetailId = Guid.NewGuid();
                orderDetails.Add(new OrderDetail()
                {
                    Id = masterOrderDetailId,
                    MenuProductId = product.ProductInMenuId,
                    OrderId = newOrder.Id,
                    Quantity = product.Quantity,
                    SellingPrice = product.SellingPrice,
                    TotalAmount = product.TotalAmount,
                    Discount = product.Discount,
                    FinalAmount = product.FinalAmount,
                    Notes = product.Note
                });
                if (product.Extras.Count > 0)
                {
                    product.Extras.ForEach(extra =>
                    {
                        orderDetails.Add(new OrderDetail()
                        {
                            Id = Guid.NewGuid(),
                            MenuProductId = extra.ProductInMenuId,
                            OrderId = newOrder.Id,
                            Quantity = extra.Quantity,
                            SellingPrice = extra.SellingPrice,
                            TotalAmount = extra.TotalAmount,
                            Discount = 0,
                            FinalAmount = extra.TotalAmount,
                            MasterOrderDetailId = masterOrderDetailId,
                        });
                    });
                }

                if (product.PromotionCodeApplied != null)
                {
                    PromotionPrepare promotionPrepare = createNewOrderRequest.PromotionList.SingleOrDefault(
                        x => x.Code.Equals(product.PromotionCodeApplied));
                    promotionMappingList.Add(new PromotionOrderMapping()
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = promotionPrepare.PromotionId ?? Guid.NewGuid(),
                        OrderId = newOrder.Id,
                        Quantity = 1,
                        DiscountAmount = product.Discount,
                        OrderDetailId = masterOrderDetailId,
                        EffectType = promotionPrepare.EffectType
                    });
                    createNewOrderRequest.PromotionList.Remove(promotionPrepare);
                }
            });
            if (createNewOrderRequest.PromotionList != null && createNewOrderRequest.PromotionList.Any())
            {
                createNewOrderRequest.PromotionList.ForEach(orderPromotion =>
                {
                    promotionMappingList.Add(new PromotionOrderMapping()
                    {
                        Id = Guid.NewGuid(),
                        PromotionId = orderPromotion.PromotionId ?? Guid.NewGuid(),
                        OrderId = newOrder.Id,
                        Quantity = 1,
                        DiscountAmount = orderPromotion.DiscountAmount,
                        EffectType = orderPromotion.EffectType
                    });
                });
            }

            if (promotionMappingList.Any())
            {
                await _unitOfWork.GetRepository<PromotionOrderMapping>().InsertRangeAsync(promotionMappingList);
            }

            OrderUser orderSource = new OrderUser()
            {
                Id = Guid.NewGuid(),
                UserType = createNewOrderRequest.CustomerId == null ? "GUEST" : "USER",
                UserId = createNewOrderRequest.CustomerId ?? null,
                Address = createNewOrderRequest.DeliveryAddress,
                Name = createNewOrderRequest.CustomerName,
                Phone = createNewOrderRequest.CustomerPhone,
                CreatedAt = currentTime,
                Status = OrderSourceStatus.PENDING.GetDescriptionFromEnum(),
                CompletedAt = currentTime
            };
            newOrder.OrderSourceId = orderSource.Id;
            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            await _unitOfWork.GetRepository<OrderUser>().InsertAsync(orderSource);
            await _unitOfWork.CommitAsync();
            return newOrder.Id;
        }

        public async Task<GetUserInfo> ScanUser(string phone)
        {
            string modifiedPhoneNumber = Regex.Replace(phone, @"^0", "+84");
            GetUserInfo user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: x => new GetUserInfo(x.Id, x.PhoneNumber, x.FullName, x.Gender, x.Email),
                predicate: x =>
                    x.PhoneNumber.Equals(modifiedPhoneNumber)
                    && x.Status.Equals("Active"));
            if (user == null)
            {
                throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            }

            return user;
        }

        public async Task<IEnumerable<PromotionPointifyResponse>?> GetPromotionsAsync(string brandCode
        )
        {
            using var client = new HttpClient();
            var url =
                $"https://api-pointify.reso.vn/api/channels/list-promotions?brandCode={brandCode}&ChannelType=2";
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(msg);
            if (!response.StatusCode.Equals(HttpStatusCode.OK)) return null;
            var responseContent =
                JsonConvert.DeserializeObject<IEnumerable<PromotionPointifyResponse>>(response.Content
                    .ReadAsStringAsync().Result);
            return responseContent;
        }
    }
}
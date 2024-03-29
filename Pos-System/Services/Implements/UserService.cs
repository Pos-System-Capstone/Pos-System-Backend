﻿using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Payload.Request.User;
using Pos_System.API.Payload.Response.User;
using Pos_System.API.Services.Interfaces;
using Pos_System.API.Utils;
using Pos_System.Domain.Models;
using Pos_System.Repository.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pos_System.API.Payload.Pointify;
using Pos_System.API.Payload.Request.Orders;
using Pos_System.Domain.Paginate;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Pos_System.API.Payload.Response.Menus;
using Pos_System.API.Helpers;
using Pos_System.API.Payload.Response.Products;
using Pos_System.API.Payload.ZaloMiniApp;
using ProductInMenu = Pos_System.API.Payload.Response.Menus.ProductInMenu;

namespace Pos_System.API.Services.Implements
{
    public class UserService : BaseService<UserService>, IUserService
    {
        public const double VAT_PERCENT = 0.08;
        public const double VAT_STANDARD = 1.08;

        public UserService(IUnitOfWork<PosSystemContext> unitOfWork, ILogger<UserService> logger,
            IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(
            unitOfWork, logger, mapper,
            httpContextAccessor)
        {
        }


        public async Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, Guid brandId,
            string brandCode)
        {
            _logger.LogInformation($"Create new brand with {newUserRequest.FullName}");
            User newUser = new User()
            {
                Id = Guid.NewGuid(),
                PhoneNumber = newUserRequest.PhoneNunmer,
                Status = UserStatus.Active.GetDescriptionFromEnum(),
                FullName = newUserRequest.FullName,
                Email = newUserRequest.Email,
                BrandId = brandId,
                Gender = newUserRequest.Gender,
                Fcmtoken = newUserRequest.FcmToken,
                FireBaseUid = newUserRequest.FireBaseUid,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                UpdatedAt = TimeUtils.GetCurrentSEATime(),
                PinCode = newUserRequest.PinCode == null ? null : PasswordUtil.HashPassword(newUserRequest.PinCode),
            };

            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);


            //tạo membership bên pointify
            string createMemberPromoUrl = $"https://api-pointify.reso.vn/api/memberships?apiKey={brandId}";
            var data = new
            {
                membershipId = newUser.Id,
                fullname = newUser.FullName,
                email = newUser.Email,
                gender = newUserRequest.Gender.Equals("MALE") ? 1 : (newUserRequest.Gender.Equals("FEMALE") ? 2 : 3),
                phoneNumber = newUser.PhoneNumber,
                memberProgramId = brandCode == "DEERCOFFEE"
                    ? "52b1f27d-885f-4b1c-9773-91ed894b4eac"
                    : "CBA71E56-066E-4F58-B0FA-2871B538CF14"
            };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(createMemberPromoUrl, content);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                throw new BadHttpRequestException(MessageConstant.User.CreateNewUserFailedMessage);
            }

            var isSuccessful = await _unitOfWork.CommitAsync() > 0;
            CreateNewUserResponse createNewUserResponse = null;
            if (isSuccessful)
            {
                createNewUserResponse = _mapper.Map<CreateNewUserResponse>(newUser);
            }

            return createNewUserResponse;
        }

        public async Task<CheckMemberResponse> CheckMember(string phone, string brandCode)
        {
            Guid brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: brand => brand.Id,
                predicate: brand => brand.BrandCode.Equals(brandCode));
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            string modifiedPhoneNumber = Regex.Replace(phone, @"^0", "+84");
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: x =>
                    x.PhoneNumber.Contains(modifiedPhoneNumber)
                    && x.Status.Equals("Active") && x.BrandId.Equals(brandId));
            return user switch
            {
                {PinCode: null} => new CheckMemberResponse()
                {
                    Message = MessageConstant.User.UpdatePin, SignInMethod = "RESETPASS"
                },
                {PinCode: not null} => new CheckMemberResponse()
                {
                    Message = MessageConstant.User.InputPin, SignInMethod = "SIGNIN"
                },
                _ => new CheckMemberResponse()
                {
                    Message = MessageConstant.User.MembershipNotFound, SignInMethod = "SIGNUP"
                }
            };
        }

        public async Task<SignInResponse?> LoginUser(MemberLoginRequest req)
        {
            var modifiedPhoneNumber = Regex.Replace(req.Phone, @"^0", "+84");
            var brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: brand => brand.Id,
                predicate: brand => brand.BrandCode.Equals(req.BrandCode));
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            var userLogin = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(modifiedPhoneNumber)
                                                      && x.Status.Equals("Active") && x.BrandId.Equals(brandId));

            DateTime expires;
            IConfiguration configuration;
            JwtSecurityTokenHandler jwtHandler;
            SymmetricSecurityKey secrectKey;
            SigningCredentials? credentials;
            string issuer;
            List<Claim> claims;
            Tuple<string, Guid> guidClaim;
            JwtSecurityToken? token;
            string accesstoken;
            switch (req.Method)
            {
                case "SIGNIN":
                {
                    if (userLogin == null)
                    {
                        throw new BadHttpRequestException(MessageConstant.User.MembershipNotFound);
                    }

                    if (userLogin.PinCode == null || !userLogin.PinCode!.Equals(PasswordUtil.HashPassword(req.PinCode)))
                        throw new BadHttpRequestException(MessageConstant.User.MembershipPasswordFail);
                    userLogin.Fcmtoken = "";
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
                    claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
                    expires = DateTime.Now.AddDays(60);
                    token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires,
                        credentials);
                    accesstoken = jwtHandler.WriteToken(token);
                    _unitOfWork.GetRepository<User>().UpdateAsync(userLogin);
                    return new SignInResponse
                    {
                        message = "Đăng nhập thành công",
                        AccessToken = accesstoken,
                        UserId = userLogin.Id
                    };
                }
                case "RESETPASS":
                {
                    if (userLogin == null)
                    {
                        throw new BadHttpRequestException(MessageConstant.User.MembershipNotFound);
                    }

                    userLogin.Fcmtoken = "";
                    userLogin.PinCode = PasswordUtil.HashPassword(req.PinCode);
                    _unitOfWork.GetRepository<User>().UpdateAsync(userLogin);
                    await _unitOfWork.CommitAsync();
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
                    claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
                    expires = DateTime.Now.AddDays(60);
                    token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires,
                        credentials);
                    accesstoken = jwtHandler.WriteToken(token);

                    return new SignInResponse
                    {
                        message = "Cập nhập mã pin thành công",
                        AccessToken = accesstoken,
                        UserId = userLogin.Id
                    };
                }
                case "SIGNUP":
                {
                    if (userLogin != null)
                    {
                        throw new BadHttpRequestException(MessageConstant.User.MembershipFound);
                    }

                    CreateNewUserRequest newUserRequest = new CreateNewUserRequest()
                    {
                        PhoneNunmer = modifiedPhoneNumber,
                        FullName = req.FullName ?? "Người dùng",
                        Gender = req.Gender ?? "ORTHER",
                        Email = req.Email ?? null,
                        FireBaseUid = "",
                        FcmToken = "",
                        PinCode = req.PinCode
                    };
                    var newUser = await CreateNewUser(newUserRequest, brandId, req.BrandCode);

                    var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x =>
                        x.Id.Equals(newUser.Id)
                        && x.Status.Equals("Active"));

                    guidClaim = new Tuple<string, Guid>("brandId", brandId);
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
                    accesstoken = jwtHandler.WriteToken(token);
                    return new SignInResponse
                    {
                        message = "Đăng kí thành công",
                        AccessToken = accesstoken,
                        UserId = user.Id
                    };
                }
            }

            return null;
        }

        public async Task<SignInResponse> LoginUserMiniApp(LoginMiniApp req)
        {
            string modifiedPhoneNumber = Regex.Replace(req.Phone, @"^0", "+84");
            Guid brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: brand => brand.Id,
                predicate: brand => brand.BrandCode.Equals(req.BrandCode));
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);

            var userLogin = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(modifiedPhoneNumber)
                                                      && x.Status.Equals("Active") && x.BrandId.Equals(brandId));
            DateTime expires;
            IConfiguration configuration;
            JwtSecurityTokenHandler jwtHandler;
            SymmetricSecurityKey secrectKey;
            SigningCredentials? credentials;
            string issuer;
            List<Claim> claims;
            Tuple<string, Guid> guidClaim;
            JwtSecurityToken? token;

            string accesstoken;
            if (userLogin == null)
            {
                CreateNewUserRequest newUserRequest = new CreateNewUserRequest()
                {
                    PhoneNunmer = modifiedPhoneNumber,
                    FullName = req.FullName,
                    Gender = "ORTHER",
                    FireBaseUid = "ZaloMiniApp"
                };
                var newUser = await CreateNewUser(newUserRequest, brandId, req.BrandCode.ToUpper());

                guidClaim = new Tuple<string, Guid>("brandId", brandId);
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
                accesstoken = jwtHandler.WriteToken(token);
                return new SignInResponse
                {
                    message = "Đăng kí thành công",
                    AccessToken = accesstoken,
                    UserId = newUser.Id
                };
            }

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
            claims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            expires = DateTime.Now.AddDays(60);
            token = new JwtSecurityToken(issuer, null, claims, notBefore: DateTime.Now, expires, credentials);
            accesstoken = jwtHandler.WriteToken(token);
            _unitOfWork.GetRepository<User>().UpdateAsync(userLogin);

            return new SignInResponse
            {
                message = "Đăng nhập thành công",
                AccessToken = accesstoken,
                UserId = userLogin.Id
            };
        }

        public async Task<bool> UpdateUserInformation(Guid userId, UpdateUserRequest updatedUserRequest)
        {
            if (userId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.User.EmptyUserId);
            User updatedUser = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(userId));

            if (updatedUser == null) throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            var currentTime = TimeUtils.GetCurrentSEATime();
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
            updatedUser.UrlImg = string.IsNullOrEmpty(updatedUserRequest.UrlImg)
                ? updatedUser.UrlImg
                : updatedUserRequest.UrlImg;
            updatedUser.Status = string.IsNullOrEmpty(updatedUserRequest.Status.GetDescriptionFromEnum())
                ? updatedUser.Status
                : updatedUserRequest.Status.GetDescriptionFromEnum();
            updatedUser.UpdatedAt = currentTime;
            _unitOfWork.GetRepository<User>().UpdateAsync(updatedUser);

            var isSuccessful = await _unitOfWork.CommitAsync() > 0;
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

            var newInvoiceId = store.Code + currentTimeStamp;
            const int defaultGuest = 1;

            var vatAmount = (createNewOrderRequest.FinalAmount / VAT_STANDARD) * VAT_PERCENT;

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
                Status = OrderStatus.NEW.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = createNewOrderRequest.PaymentType.GetDescriptionFromEnum(),
                Note = createNewOrderRequest.Notes
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
                        EffectType = promotionPrepare.EffectType,
                        VoucherCode = createNewOrderRequest.VoucherCode
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
                        EffectType = orderPromotion.EffectType,
                        VoucherCode = createNewOrderRequest.VoucherCode
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
                Note = createNewOrderRequest.CustomerNote,
                CreatedAt = currentTime,
                Status = OrderSourceStatus.PENDING.GetDescriptionFromEnum(),
                PaymentStatus = PaymentStatusEnum.PENDING.GetDescriptionFromEnum(),
                CompletedAt = currentTime,
                IsSync = false
            };
            if (createNewOrderRequest.CustomerId != null)
            {
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(createNewOrderRequest.CustomerId));
                orderSource.Phone = user.PhoneNumber;
                orderSource.Name = user.FullName;
            }

            newOrder.OrderSourceId = orderSource.Id;
            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);

            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            await _unitOfWork.GetRepository<OrderUser>().InsertAsync(orderSource);
            var result = await _unitOfWork.CommitAsync();
            if (result > 0)
            {
                var orderHistory = new OrderHistory()
                {
                    Id = Guid.NewGuid(),
                    OrderId = newOrder.Id,
                    FromStatus = OrderStatus.NEW.GetDescriptionFromEnum(),
                    ToStatus = OrderStatus.NEW.GetDescriptionFromEnum(),
                    CreatedTime = currentTime,
                    ChangedBy = createNewOrderRequest.CustomerId
                };
                await _unitOfWork.GetRepository<OrderHistory>().InsertAsync(orderHistory);
                await _unitOfWork.CommitAsync();
            }

            if (result <= 0 || createNewOrderRequest.CustomerId == null ||
                !newOrder.PaymentType.Equals(PaymentTypeEnum.POINTIFY.GetDescriptionFromEnum())) return newOrder.Id;
            {
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: x => x.Id.Equals(createNewOrderRequest.CustomerId));
                MemberActionRequest request = new MemberActionRequest()
                {
                    ApiKey = user.BrandId,
                    Amount = newOrder.FinalAmount,
                    Description = newOrder.InvoiceId,
                    MembershipId = user.Id,
                    MemberActionType = MemberActionType.PAYMENT.GetDescriptionFromEnum()
                };
                var url = "https://api-pointify.reso.vn/api/member-action";
                var response = await CallApiUtils.CallApiEndpoint(url, request);
                if (!response.StatusCode.Equals(HttpStatusCode.OK))
                {
                    throw new BadHttpRequestException(MessageConstant.User.PaymentUserFail);
                }

                var actionResponse =
                    JsonConvert.DeserializeObject<MemberActionResponse>(response.Content
                        .ReadAsStringAsync().Result);
                if (actionResponse != null &&
                    actionResponse.Status.Equals(MemberActionStatus.FAIL.GetDescriptionFromEnum()))
                {
                    throw new BadHttpRequestException(actionResponse.Description);
                }

                if (actionResponse != null &&
                    actionResponse.Status.Equals(MemberActionStatus.SUCCESS.GetDescriptionFromEnum()))
                {
                    var transaction = new Transaction()
                    {
                        Id = Guid.NewGuid(),
                        BrandId = user.BrandId,
                        TransactionJson = response.Content
                            .ReadAsStringAsync().Result,
                        Amount = (decimal) newOrder.FinalAmount,
                        CreatedDate = TimeUtils.GetCurrentSEATime(),
                        UserId = user.Id,
                        OrderId = newOrder.Id,
                        IsIncrease = false,
                        Type = TransactionTypeEnum.PAYMENT.GetDescriptionFromEnum(),
                        Currency = "đ",
                        Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                    };
                    await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    throw new BadHttpRequestException(MessageConstant.User.PaymentUserFail);
                }
            }

            return newOrder.Id;
        }

        public async Task<GetUserInfo> ScanUser(string code)
        {
            try
            {
                //kiểm tra mã QRCode có hợp lệ không
                var response = EnCodeBase64.DecodeBase64Response(code);
                if (response == null) throw new BadHttpRequestException("Mã QRCode không hợp lệ!");
                //kiểm tra thời gian có quá 2 phút không
                var currentTime = TimeUtils.GetCurrentSEATime();
                var timeSpan = currentTime - response.CurrentTime;
                if (timeSpan.TotalMinutes > 2)
                {
                    throw new BadHttpRequestException("Mã QRCode đã hết hạn! Vui lòng tạo mã QR khác");
                }

                string modifiedPhoneNumber = Regex.Replace(response.Phone, @"^0", "+84");
                var brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                    selector: brand => brand.Id,
                    predicate: brand => brand.BrandCode != null && brand.BrandCode.Equals(response.BrandCode)
                );
                GetUserInfo user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    selector: x => new GetUserInfo(x.Id, x.BrandId, x.PhoneNumber, x.FullName, x.Gender, x.Email),
                    predicate: x =>
                        x.PhoneNumber.Equals(modifiedPhoneNumber)
                        && x.Status.Equals("Active") && x.BrandId.Equals(brandId));
                return user ?? throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            }
            catch (Exception e)
            {
                throw new BadHttpRequestException("Mã QRCode không hợp lệ hoặc đã hết hạn!");
            }
        }

        public async Task<List<PromotionPointifyResponse>?> GetPromotionsAsync(string brandCode, Guid userId
        )
        {
            using var client = new HttpClient();
            var url =
                $"https://api-pointify.reso.vn/api/channels/list-promotions?brandCode={brandCode}&ChannelType=2";
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(msg);
            if (!response.StatusCode.Equals(HttpStatusCode.OK)) return null;
            var promotionPointifyResponses =
                JsonConvert.DeserializeObject<List<PromotionPointifyResponse>>(response.Content
                    .ReadAsStringAsync().Result);
            List<PromotionPointifyResponse> listPromotionToRemove = new List<PromotionPointifyResponse>();
            var voucherList = await GetVoucherOfUser(userId);
            if (promotionPointifyResponses != null)
            {
                foreach (var promotion in promotionPointifyResponses)
                {
                    if (promotion.PromotionType.Equals((int) PromotionPointifyType.Automatic))
                    {
                        listPromotionToRemove.Add(promotion);
                        continue;
                    }

                    if (voucherList == null) continue;
                    foreach (var voucher in voucherList)
                    {
                        if (voucher.PromotionId.Equals(promotion.PromotionId) &&
                            voucher is {IsRedemped: true, IsUsed: false})
                        {
                            promotion.ListVoucher?.Add(voucher);
                        }
                    }

                    if (promotion.ListVoucher != null)
                        promotion.CurrentVoucherQuantity = promotion.ListVoucher.Count;
                }

                foreach (var promotionRemove in listPromotionToRemove)
                {
                    promotionPointifyResponses.Remove(promotionRemove);
                }

                return promotionPointifyResponses;
            }

            return null;
        }

        public async Task<IEnumerable<VoucherResponse>?> GetVoucherOfUser(Guid userId)
        {
            using var client = new HttpClient();
            var url =
                $"https://api-pointify.reso.vn/api/vouchers/membership?membershipId={userId}";
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(msg);
            if (!response.StatusCode.Equals(HttpStatusCode.OK)) return null;
            var responseContent =
                JsonConvert.DeserializeObject<IEnumerable<VoucherResponse>>(response.Content
                    .ReadAsStringAsync().Result);
            return responseContent;
        }

        public async Task<IPaginate<Transaction>> GetListTransactionOfUser(Guid userId, int page, int size)
        {
            IPaginate<Transaction> listTrans = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
                predicate: x => x.UserId.Equals(userId),
                page: page,
                size: size,
                orderBy: x =>
                    x.OrderByDescending(x => x.CreatedDate)
            );

            return listTrans;
        }

        public async Task<TopUpUserWalletResponse> TopUpUserWallet(TopUpUserWalletRequest req)
        {
            if (req.StoreId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(req.StoreId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(
                predicate: x =>
                    x.StoreId.Equals(req.StoreId)
                    && DateTime.Compare(x.StartDateTime, currentTime) < 0
                    && DateTime.Compare(x.EndDateTime, currentTime) > 0);

            if (currentUserSession == null)
                throw new BadHttpRequestException(MessageConstant.Order.UserNotInSessionMessage);
            string newInvoiceId = store.Code + currentTimeStamp;
            Order newOrder = new Order()
            {
                Id = Guid.NewGuid(),
                CheckInPerson = currentUser.Id,
                CheckInDate = currentTime,
                CheckOutDate = currentTime,
                InvoiceId = newInvoiceId,
                TotalAmount = req.Amount,
                Discount = 0,
                FinalAmount = req.Amount,
                Vat = 0,
                Vatamount = 0,
                OrderType = OrderType.TOP_UP.GetDescriptionFromEnum(),
                NumberOfGuest = 1,
                Status = OrderStatus.PAID.GetDescriptionFromEnum(),
                SessionId = currentUserSession.Id,
                PaymentType = req.PaymentType.GetDescriptionFromEnum(),
            };
            User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: x =>
                    x.Id.Equals(req.UserId)
                    && x.Status.Equals("Active")
            );
            if (user == null)
            {
                throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            }


            MemberActionRequest request = new MemberActionRequest()
            {
                ApiKey = store.BrandId,
                Amount = req.Amount,
                Description = "[TOP UP]",
                MembershipId = user.Id,
                MemberActionType = MemberActionType.TOP_UP.GetDescriptionFromEnum()
            };

            TopUpUserWalletResponse topUpUserWalletResponse = new TopUpUserWalletResponse()
            {
                UserId = req.UserId,
                PaymentType = req.PaymentType.GetDescriptionFromEnum(),
                Status = PaymentStatusEnum.FAIL.GetDescriptionFromEnum()
            };
            var url = "https://api-pointify.reso.vn/api/member-action";
            var response = await CallApiUtils.CallApiEndpoint(url, request);
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                topUpUserWalletResponse.Message = "Nạp tiền thất bại, vui lòng kiểm tra lại";
                return topUpUserWalletResponse;
            }

            var actionResponse =
                JsonConvert.DeserializeObject<MemberActionResponse>(response.Content
                    .ReadAsStringAsync().Result);
            if (actionResponse != null &&
                actionResponse.Status.Equals(MemberActionStatus.SUCCESS.GetDescriptionFromEnum()))
            {
                Transaction transaction = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    BrandId = user.BrandId,
                    TransactionJson = response.Content
                        .ReadAsStringAsync().Result,
                    Amount = (decimal) actionResponse.ActionValue,
                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                    UserId = user.Id,
                    IsIncrease = true,
                    Type = TransactionTypeEnum.TOP_UP.GetDescriptionFromEnum(),
                    Currency = "đ",
                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                    PaymentType = req.PaymentType.GetDescriptionFromEnum()
                };
                await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                topUpUserWalletResponse.Message =
                    "Nạp tiền thành công cho người dùng " + user.FullName + ":" + actionResponse.Description;
                topUpUserWalletResponse.Status = PaymentStatusEnum.SUCCESS.GetDescriptionFromEnum();

                await _unitOfWork.CommitAsync();
            }
            else
            {
                topUpUserWalletResponse.Message = actionResponse?.Description;
            }

            return topUpUserWalletResponse;
        }

        public async Task<string> CreateQrCode(Guid userId)
        {
            //kiểm tra user
            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                           predicate: x => x.Id.Equals(userId),
                           include: b => b.Include(brand => brand.Brand)) ??
                       throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            // mã hoá QRCode bằng userId và ngày hiện tại
            if (user.Brand.BrandCode == null)
            {
                throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
            }

            var base64 = EnCodeBase64.EncodeBase64User(user.PhoneNumber, user.Brand.BrandCode);
            return base64;
        }

        public async Task<GetUserInfo> ScanUserPhoneNumber(string phone, Guid storeId)
        {
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(storeId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);
            string modifiedPhoneNumber = Regex.Replace(phone, @"^0", "+84");
            GetUserInfo user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                selector: x => new GetUserInfo(x.Id, x.BrandId, x.PhoneNumber, x.FullName, x.Gender, x.Email),
                predicate: x =>
                    x.PhoneNumber.Contains(modifiedPhoneNumber)
                    && x.Status.Equals("Active") && x.BrandId.Equals(store.BrandId));
            return user ?? throw new BadHttpRequestException(MessageConstant.User.UserNotFound);
        }

        public async Task<Guid> UpdateOrder(Guid orderId, UpdateOrderRequest updateOrderRequest)
        {
            Order order = await _unitOfWork.GetRepository<Order>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(orderId)
                );
            if (order == null) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFoundMessage);
            var fromStatus = EnumUtil.ParseEnum<OrderStatus>(order.Status);
            order.PaymentType = updateOrderRequest.PaymentType != null
                ? updateOrderRequest.PaymentType.GetDescriptionFromEnum()
                : order.PaymentType;
            order.Status = updateOrderRequest.Status != null
                ? updateOrderRequest.Status.GetDescriptionFromEnum()
                : order.Status;
            if (updateOrderRequest.DeliStatus != null)
            {
                if (order.OrderSourceId != null)
                {
                    OrderUser orderUser = await _unitOfWork.GetRepository<OrderUser>()
                        .SingleOrDefaultAsync(predicate: x => x.Id.Equals(order.OrderSourceId)
                        );
                    orderUser.Status = updateOrderRequest.DeliStatus.GetDescriptionFromEnum();
                    _unitOfWork.GetRepository<OrderUser>().UpdateAsync(orderUser);
                    if (updateOrderRequest.Status != null && !updateOrderRequest.Status.Equals(fromStatus))
                    {
                        var currentTime = TimeUtils.GetCurrentSEATime();
                        var orderHistory = new OrderHistory()
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            FromStatus = fromStatus.GetDescriptionFromEnum(),
                            ToStatus = (updateOrderRequest.Status ?? fromStatus).GetDescriptionFromEnum(),
                            CreatedTime = currentTime,
                            ChangedBy = orderUser.UserId
                        };
                        await _unitOfWork.GetRepository<OrderHistory>().InsertAsync(orderHistory);
                        await _unitOfWork.CommitAsync();
                    }
                }
            }

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();
            return order.Id;
        }

        public async Task<GetMenuDetailForStaffResponse> GetMenuDetailFromStore(Guid storeId)
        {
            //Filter Menu, make sure it return correct menu in specific time
            var allMenuAvailable = (List<MenuStore>) await _unitOfWork.GetRepository<MenuStore>()
                .GetListAsync(predicate: x => x.StoreId.Equals(storeId)
                                              && x.Menu.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum())
                                              && x.Store.Brand.Status.Equals(
                                                  BrandStatus.Active.GetDescriptionFromEnum()),
                    include: x => x
                        .Include(x => x.Menu)
                        .Include(x => x.Store).ThenInclude(x => x.Brand)
                );
            if (!allMenuAvailable.Any())
                throw new BadHttpRequestException(MessageConstant.Menu.NoMenusFoundMessage);

            var currentSeaTime = TimeUtils.GetCurrentSEATime();
            var currentDay = DateTimeHelper.GetDateFromDateTime(currentSeaTime);
            var currentTime = TimeOnly.FromDateTime(currentSeaTime);

            List<MenuStore> menusAvailableInDay = (from menu in allMenuAvailable
                let menuAvailableDays = DateTimeHelper.GetDatesFromDateFilter(menu.Menu.DateFilter)
                let menuStartTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.StartTime)
                let menuEndTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.EndTime)
                where menuAvailableDays.Contains(currentDay) && currentTime <= menuEndTime &&
                      currentTime >= menuStartTime
                select menu).ToList();

            //If there are more than 2 menus available take the highest priority one
            var menuAvailableWithHighestPriority =
                menusAvailableInDay.MaxBy(x => x.Menu.Priority);
            if (menuAvailableWithHighestPriority == null)
                throw new BadHttpRequestException(MessageConstant.Menu.NoMenusAvailableMessage);
            var menuOfStoreId = menuAvailableWithHighestPriority.MenuId;

            var userBrandId = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(storeId));

            var menuOfStore = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
                selector: x => new GetMenuDetailForStaffResponse(
                    x.Id,
                    x.BrandId,
                    x.Code,
                    x.Priority,
                    true,
                    x.DateFilter,
                    x.StartTime,
                    x.EndTime),
                predicate: x =>
                    x.Id.Equals(menuOfStoreId) && x.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum()));

            menuOfStore.ProductsInMenu = (List<ProductDataForStaff>) await _unitOfWork.GetRepository<MenuProduct>()
                .GetListAsync(
                    selector: x => new ProductDataForStaff
                    (
                        x.ProductId,
                        x.Product.Code,
                        x.Product.Name,
                        x.SellingPrice,
                        x.Product.PicUrl,
                        x.Product.Status,
                        x.HistoricalPrice,
                        x.DiscountPrice,
                        x.Product.Description,
                        x.Product.DisplayOrder,
                        x.Product.Size,
                        x.Product.Type,
                        x.Product.ParentProductId,
                        x.Product.BrandId,
                        x.Product.CategoryId,
                        (List<Guid>) x.Product.CollectionProducts.Select(x => x.CollectionId),
                        (List<Guid>) x.Product.Category.ExtraCategoryProductCategories.Select(
                            x => x.ExtraCategoryId),
                        x.Id //This is the menuProductId in response body
                    ),
                    predicate: x =>
                        x.MenuId.Equals(menuOfStoreId) &&
                        x.Status.Equals(MenuProductStatus.Active.GetDescriptionFromEnum()) &&
                        x.Product.DisplayOrder > 0,
                    include: menuProduct => menuProduct
                        .Include(menuProduct => menuProduct.Product)
                        .ThenInclude(product => product.CollectionProducts)
                        .Include(menuProduct => menuProduct.Product)
                        .ThenInclude(product => product.Category)
                        .ThenInclude(category => category.ExtraCategoryProductCategories)
                );
            menuOfStore.CategoriesOfBrand = new List<CategoryOfBrand>();
            menuOfStore.CollectionsOfBrand = (List<CollectionOfBrand>) await _unitOfWork.GetRepository<Collection>()
                .GetListAsync(selector: x => new CollectionOfBrand(
                        x.Id,
                        x.Name,
                        x.Code,
                        x.PicUrl,
                        x.Description
                    ),
                    predicate: x =>
                        x.BrandId.Equals(userBrandId) &&
                        x.Status == CollectionStatus.Active.GetDescriptionFromEnum());

            var listCategory = (List<CategoryOfBrand>) await _unitOfWork.GetRepository<Category>()
                .GetListAsync(selector: x => new CategoryOfBrand(
                        x.Id,
                        x.Code,
                        x.Name,
                        EnumUtil.ParseEnum<CategoryType>(x.Type),
                        (List<Guid>) x.ExtraCategoryProductCategories.Select(e => e.ExtraCategoryId),
                        x.DisplayOrder,
                        x.Description,
                        x.PicUrl
                    ),
                    predicate: x =>
                        x.BrandId.Equals(userBrandId) &&
                        x.Status.Equals(CategoryStatus.Active.GetDescriptionFromEnum()) && x.DisplayOrder > 0);

            foreach (var category in listCategory)
            {
                if (category.Type.Equals(CategoryType.Child))
                {
                    var res = menuOfStore.ProductsInMenu.Exists(p => p.CategoryId.Equals(category.Id));
                    if (res)
                    {
                        menuOfStore.CategoriesOfBrand.Add(category);
                    }
                }
                else if (category.Type.Equals(CategoryType.Normal))
                {
                    var res = menuOfStore.ProductsInMenu.Exists(p => p.CategoryId.Equals(category.Id));
                    switch (res)
                    {
                        case true:
                            menuOfStore.CategoriesOfBrand.Add(category);
                            break;
                        case false when category.ChildCategoryIds is {Count: > 0}:
                        {
                            if (category.ChildCategoryIds.Select(childCategoryId =>
                                    menuOfStore.ProductsInMenu.Exists(p => p.CategoryId.Equals(childCategoryId)))
                                .Any(result => result))
                            {
                                menuOfStore.CategoriesOfBrand.Add(category);
                            }

                            break;
                        }
                    }
                }
                else
                {
                    menuOfStore.CategoriesOfBrand.Add(category);
                }
            }

            return menuOfStore;
        }


        public async Task<ZaloCallbackResponse> ZaloNotifyPayment(ZaloCallbackRequest data)
        {
            var appId = "1838228208681717250";
            var secretKey = "c876b6b2e0906a5413e9dd328b5de6b0";
            var dataReq = $"appId={data.Data.AppId}&orderId={data.Data.OrderId}&method={data.Data.Method}";
            var reqmac = EnCodeBase64.GenerateHmacSha256(dataReq, secretKey);
            // var url =
            //     "https://payment-mini.zalo.me/api/transaction/1838228208681717250/cod-callback-payment";
            //
            // var dataCallback =
            //     $"appId={appId}&orderId={data.Data.OrderId}&resultCode=1&privateKey={secretKey}";
            // var updateOrderCallBack = new UpdateOrderZaloPayment()
            // {
            //     AppId = appId,
            //     OrderId = data.Data.OrderId,
            //     ResultCode = 1,
            //     Mac = EnCodeBase64.GenerateHmacSha256(dataCallback, secretKey)
            // };

            if (reqmac == data.Mac)
            {
                _logger.LogInformation($"Notify to zalo success");
                return new ZaloCallbackResponse()
                {
                    ReturnCode = 1,
                    ReturnMessage = "Thanh toán thành công"
                };
            }

            _logger.LogInformation($"Notify to zalo fail");
            return new ZaloCallbackResponse()
            {
                ReturnCode = 0,
                ReturnMessage = "Xác nhận thanh toán thất bại"
            };

            // var response = await CallApiUtils.CallApiEndpoint(url, updateOrderCallBack);
            // if (!response.StatusCode.Equals(HttpStatusCode.OK))
            // {
            //     _logger.LogInformation(
            //         "Call back to zalo fail with status:  {ResponseStatusCode} and message {@ResponseContent}",
            //         response.StatusCode, response.Content);
            //     return new ZaloCallbackResponse()
            //     {
            //         ReturnCode = 2,
            //         ReturnMessage = "Xác nhận thanh toán thất bại, Khônng kết nối được với hệ thống ZALO  "
            //     };
            // }
            //
            // var zaloBaseCallbackResponse =
            //     JsonConvert.DeserializeObject<ZaloBaseCallbackResponse>(response.Content
            //         .ReadAsStringAsync().Result);
            //
            // if (zaloBaseCallbackResponse != null && zaloBaseCallbackResponse.Error != 0)
            // {
            //     _logger.LogInformation(
            //         "Notify to zalo fail with status:  {ResponseStatusCode} and message: {@ResponseContent}",
            //         zaloBaseCallbackResponse.Error, zaloBaseCallbackResponse.Msg);
            //     return new ZaloCallbackResponse()
            //     {
            //         ReturnCode = 3,
            //         ReturnMessage = $"Thanh toán thất bại , {zaloBaseCallbackResponse.Msg}"
            //     };
            // }
            //
            // _logger.LogInformation(
            //     "Notify to zalo success with status:  {ResponseStatusCode} and message: {@ResponseContent}",
            //     zaloBaseCallbackResponse.Error, zaloBaseCallbackResponse.Msg);
        }
    }
}
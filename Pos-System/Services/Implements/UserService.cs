using AutoMapper;
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


        public async Task<CreateNewUserResponse> CreateNewUser(CreateNewUserRequest newUserRequest, Guid brandId)
        {
            _logger.LogInformation($"Create new brand with {newUserRequest.FullName}");
            User newUser = new User()
            {
                Id = Guid.NewGuid(),
                PhoneNumber = newUserRequest.PhoneNunmer,
                Status = UserStatus.Active.GetDescriptionFromEnum(),
                FullName = newUserRequest.FullName,
                BrandId = brandId,
                Gender = newUserRequest.Gender,
                Fcmtoken = newUserRequest.FcmToken,
                FireBaseUid = newUserRequest.FireBaseUid,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                UpdatedAt = TimeUtils.GetCurrentSEATime(),
            };

            await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            //tạo membership bên pointify
            string createMemberPromoUrl = $"https://api-pointify.reso.vn/api/memberships?apiKey={brandId}";
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

            Guid brandId = await _unitOfWork.GetRepository<Brand>().SingleOrDefaultAsync(
                selector: brand => brand.Id,
                predicate: brand => brand.BrandCode.Equals(req.BrandCode));
            if (brandId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Brand.BrandNotFoundMessage);
            User userLogin = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: x => x.PhoneNumber.Equals(phone)
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
                    PhoneNunmer = phone,
                    FullName = "Người dùng",
                    Gender = "ORTHER",
                    FireBaseUid = uid,
                    FcmToken = req.FcmToken
                };
                var newUser = await CreateNewUser(newUserRequest, brandId);

                User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x =>
                    x.Id.Equals(newUser.Id)
                    && x.Status.Equals("Active"));

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
                accesstoken = jwtHandler.WriteToken(token);
                return new SignInResponse
                {
                    message = "Sign Up success",
                    AccessToken = accesstoken,
                    UserInfo = new UserResponse()
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        BrandId = user.BrandId,
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

            userLogin.Fcmtoken = req.FcmToken;
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
                message = "Login success",
                AccessToken = accesstoken,
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
                var newUser = await CreateNewUser(newUserRequest, brandId);
                User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: x =>
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
                    message = "Sign Up success",
                    AccessToken = accesstoken,
                    UserInfo = new UserResponse()
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        BrandId = user.BrandId,
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
                message = "Login success",
                AccessToken = accesstoken,
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
            updatedUser.UpdatedAt = currentTime;
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
                CreatedAt = currentTime,
                Status = OrderSourceStatus.PENDING.GetDescriptionFromEnum(),
                PaymentStatus = PaymentStatusEnum.PENDING.GetDescriptionFromEnum(),
                CompletedAt = currentTime
            };
            newOrder.OrderSourceId = orderSource.Id;
            await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
            await _unitOfWork.GetRepository<OrderDetail>().InsertRangeAsync(orderDetails);
            await _unitOfWork.GetRepository<OrderUser>().InsertAsync(orderSource);
            await _unitOfWork.CommitAsync();
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

                    if (promotion.ListVoucher != null) promotion.CurrentVoucherQuantity = promotion.ListVoucher.Count;
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
            if (req.StoreId == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Store.EmptyStoreIdMessage);
            Store store = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(predicate: x => x.Id.Equals(req.StoreId));
            if (store == null) throw new BadHttpRequestException(MessageConstant.Store.StoreNotFoundMessage);

            string currentUserName = GetUsernameFromJwt();
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            Account currentUser = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: x => x.Username.Equals(currentUserName));
            Session currentUserSession = await _unitOfWork.GetRepository<Session>().SingleOrDefaultAsync(predicate: x =>
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
                Status = OrderStatus.PENDING.GetDescriptionFromEnum(),
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
                StoreCode = store.Code,
                Amount = req.Amount,
                Description = newInvoiceId,
                MembershipId = user.Id,
                MemberActionType = MemberActionType.TOP_UP.GetDescriptionFromEnum()
            };

            TopUpUserWalletResponse topUpUserWalletResponse = new TopUpUserWalletResponse()
            {
                OrderId = newOrder.Id,
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
                    Amount = (decimal) req.Amount,
                    CreatedDate = TimeUtils.GetCurrentSEATime(),
                    UserId = user.Id,
                    OrderId = newOrder.Id,
                    IsIncrease = true,
                    Type = TransactionTypeEnum.TOP_UP.GetDescriptionFromEnum(),
                    Currency = "đ",
                    Status = TransactionStatusEnum.SUCCESS.GetDescriptionFromEnum(),
                };
                newOrder.Status = OrderStatus.PAID.GetDescriptionFromEnum();
                await _unitOfWork.GetRepository<Order>().InsertAsync(newOrder);
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                topUpUserWalletResponse.Message =
                    "Nạp tiền thành công cho người dùng " + user.FullName + ":" + actionResponse.Description;
                topUpUserWalletResponse.Status = PaymentStatusEnum.PAID.GetDescriptionFromEnum();

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
                }
            }

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();
            return order.Id;
        }

        public async Task<GetMenuDetailForStaffResponse> GetMenuDetailFromStore(Guid storeId)
        {
            //Filter Menu, make sure it return correct menu in specific time
            List<MenuStore> allMenuAvailable = (List<MenuStore>) await _unitOfWork.GetRepository<MenuStore>()
                .GetListAsync(predicate: x => x.StoreId.Equals(storeId)
                                              && x.Menu.Status.Equals(MenuStatus.Active.GetDescriptionFromEnum())
                                              && x.Store.Brand.Status.Equals(
                                                  BrandStatus.Active.GetDescriptionFromEnum()),
                    include: x => x
                        .Include(x => x.Menu)
                        .Include(x => x.Store).ThenInclude(x => x.Brand)
                );
            if (!allMenuAvailable.Any()) throw new BadHttpRequestException(MessageConstant.Menu.NoMenusFoundMessage);

            DateTime currentSEATime = TimeUtils.GetCurrentSEATime();
            DateFilter currentDay = DateTimeHelper.GetDateFromDateTime(currentSEATime);
            TimeOnly currentTime = TimeOnly.FromDateTime(currentSEATime);

            List<MenuStore> menusAvailableInDay = new List<MenuStore>();
            foreach (var menu in allMenuAvailable)
            {
                //Find the menu available days and time
                List<DateFilter> menuAvailableDays = DateTimeHelper.GetDatesFromDateFilter(menu.Menu.DateFilter);
                TimeOnly menuStartTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.StartTime);
                TimeOnly menuEndTime = DateTimeHelper.ConvertIntToTimeOnly(menu.Menu.EndTime);
                if (menuAvailableDays.Contains(currentDay) && currentTime <= menuEndTime &&
                    currentTime >= menuStartTime)
                    menusAvailableInDay.Add(menu);
            }

            //If there are more than 2 menus available take the highest priority one
            MenuStore menuAvailableWithHighestPriority =
                menusAvailableInDay.OrderByDescending(x => x.Menu.Priority).FirstOrDefault();
            if (menuAvailableWithHighestPriority == null)
                throw new BadHttpRequestException(MessageConstant.Menu.NoMenusAvailableMessage);
            Guid menuOfStoreId = menuAvailableWithHighestPriority.MenuId;

            Guid userBrandId = await _unitOfWork.GetRepository<Store>()
                .SingleOrDefaultAsync(selector: x => x.BrandId, predicate: x => x.Id.Equals(storeId));

            GetMenuDetailForStaffResponse menuOfStore = await _unitOfWork.GetRepository<Menu>().SingleOrDefaultAsync(
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
                        (List<Guid>) x.Product.Category.ExtraCategoryProductCategories.Select(x => x.ExtraCategoryId),
                        x.Id //This is the menuProductId in response body
                    ),
                    predicate: x =>
                        x.MenuId.Equals(menuOfStoreId) &&
                        x.Status.Equals(MenuProductStatus.Active.GetDescriptionFromEnum()),
                    include: menuProduct => menuProduct
                        .Include(menuProduct => menuProduct.Product)
                        .ThenInclude(product => product.CollectionProducts)
                        .Include(menuProduct => menuProduct.Product)
                        .ThenInclude(product => product.Category)
                        .ThenInclude(category => category.ExtraCategoryProductCategories)
                );

            menuOfStore.CollectionsOfBrand = (List<CollectionOfBrand>) await _unitOfWork.GetRepository<Collection>()
                .GetListAsync(selector: x => new CollectionOfBrand(
                        x.Id,
                        x.Name,
                        x.Code,
                        x.PicUrl,
                        x.Description
                    ),
                    predicate: x =>
                        x.BrandId.Equals(userBrandId) && x.Status == CollectionStatus.Active.GetDescriptionFromEnum());

            menuOfStore.CategoriesOfBrand = (List<CategoryOfBrand>) await _unitOfWork.GetRepository<Category>()
                .GetListAsync(selector: x => new CategoryOfBrand(
                        x.Id,
                        x.Code,
                        x.Name,
                        EnumUtil.ParseEnum<CategoryType>(x.Type),
                        x.DisplayOrder,
                        x.Description,
                        x.PicUrl
                    ),
                    predicate: x =>
                        x.BrandId.Equals(userBrandId) &&
                        x.Status.Equals(CategoryStatus.Active.GetDescriptionFromEnum()));

            //Use to filter which productInGroups is added to menu
            List<Guid> productIdsInMenu = menuOfStore.ProductsInMenu.Select(x => x.Id).ToList();

            menuOfStore.groupProductInMenus = (List<GroupProductInMenu>) await _unitOfWork.GetRepository<GroupProduct>()
                .GetListAsync(
                    x => new GroupProductInMenu
                    {
                        Id = x.Id,
                        ComboProductId = (Guid) x.ComboProductId,
                        Name = x.Name,
                        CombinationMode = EnumUtil.ParseEnum<GroupCombinationMode>(x.CombinationMode),
                        Priority = x.Priority,
                        Quantity = x.Quantity,
                        Status = EnumUtil.ParseEnum<GroupProductStatus>(x.Status),
                    },
                    predicate: x =>
                        x.ComboProduct.BrandId.Equals(userBrandId) &&
                        x.Status.Equals(GroupProductStatus.Active.GetDescriptionFromEnum()),
                    include: x => x.Include(x => x.ComboProduct)
                );

            menuOfStore.productInGroupList = (List<ProductsInGroupResponse>) await _unitOfWork
                .GetRepository<ProductInGroup>().GetListAsync(
                    selector: x => new ProductsInGroupResponse
                    {
                        Id = x.Id,
                        GroupProductId = x.GroupProductId,
                        ProductId = x.ProductId,
                        Priority = x.Priority,
                        AdditionalPrice = x.AdditionalPrice,
                        Min = x.Min,
                        Max = x.Max,
                        Quantity = x.Quantity,
                        Status = EnumUtil.ParseEnum<ProductInGroupStatus>(x.Status)
                    },
                    predicate: x => x.Product.BrandId.Equals(userBrandId)
                                    && productIdsInMenu.Contains(x.ProductId)
                                    && x.Status.Equals(ProductInGroupStatus.Active.GetDescriptionFromEnum())
                                    && x.Product.Status.Equals(ProductStatus.Active.GetDescriptionFromEnum()),
                    include: x => x.Include(x => x.Product)
                );

            foreach (GroupProductInMenu groupProduct in menuOfStore.groupProductInMenus)
            {
                groupProduct.ProductsInGroupIds = (List<Guid>) menuOfStore.productInGroupList
                    .Where(x => x.GroupProductId.Equals(groupProduct.Id))
                    .Select(x => x.Id).ToList();
            }

            return menuOfStore;
        }
    }
}
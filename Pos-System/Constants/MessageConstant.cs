namespace Pos_System.API.Constants;

public static class MessageConstant
{
    public static class LoginMessage
    {
        public const string InvalidUsernameOrPassword = "Tên đăng nhập hoặc mật khẩu không chính xác";
        public const string DeactivatedAccount = "Tài khoản đang bị vô hiệu hoá";
    }

    public static class Brand
    {
        public const string EmptyBrandCodeMessage = "Brand Code bị trống";
        public const string BrandCodeNotFoundMessage = "Không tìm thấy Brand Code trong hệ thống";

        public const string CreateBrandFailMessage = "Tạo nhãn hiệu mới thất bại";
        public const string CreateBrandSucceedMessage = "Tạo nhãn hiệu mới thành công";
        public const string BrandNotFoundMessage = "Brand không tồn tại trong hệ thống";
        public const string EmptyBrandIdMessage = "Brand Id bị trống hoặc bạn không trong một brand nào";
        public const string UpdateBrandSuccessfulMessage = "Cập nhật thông tin brand thành công";
        public const string UpdateBrandFailedMessage = "Cập nhật thông tin brand thất bại";
    }

    public static class Account
    {
        public const string CreateAccountWithWrongRoleMessage = "Please create with acceptent role";
        public const string CreateBrandAccountFailMessage = "Tạo tài khoản mới cho nhãn hiệu thất bại";
        public const string CreateStaffAccountFailMessage = "Tạo tài khoản nhân viên thất bại";
        public const string UserUnauthorizedMessage = "Bạn không được phép cập nhật status cho tài khoản này";

        public const string UpdateAccountStatusRequestWrongFormatMessage =
            "Cập nhật status tài khoản request sai format";

        public const string AccountNotFoundMessage = "Không tìm thấy tài khoản";
        public const string UpdateAccountStatusSuccessfulMessage = "Cập nhật status tài khoản thành công";
        public const string UpdateAccountStatusFailedMessage = "Cập nhật status tài khoản thất bại";
        public const string EmptyAccountId = "Account id bị trống";
    }

    public static class Store
    {
        public const string EmptyStoreCodeMessage = "Store Code bị trống";

        public const string EmptyStoreIdMessage = "Store Id bị trống";
        public const string CreateStoreFailMessage = "Tạo của hàng mới thất bại";
        public const string StoreNotFoundMessage = "Store không tồn tại trong hệ thống";
        public const string UpdateStoreInformationSuccessfulMessage = "Cập nhật thông tin store thành công";
        public const string UpdateStaffInformationSuccessfulMessage = "Cập nhật thông tin staff thành công";
        public const string CreateNewStoreAccountUnauthorizedMessage = "Bạn không có quyền tạo tài khoản cho một store";
        public const string StoreNotInBrandMessage = "Bạn không được phép tạo account nằm ngoài brand đang quản lí";
        public const string GetStoreOrdersUnAuthorized = "Bạn không được phép lấy orders của store khác!";
        public const string CreateStoreSessionsSuccessfully = "Tạo Session cho store thành công";
        public const string GetStoreSessionUnAuthorized = "Bạn không được phép lấy session của store khác!";
        public const string CreateStoreSessionUnAuthorized = "Bạn không được phép tạo session cho store khác!";
    }

    public static class Category
    {
        public const string CreateNewCategoryFailedMessage = "Tạo mới Category bị failed";
        public const string EmptyCategoryIdMessage = "Category Id bị trống";
        public const string CategoryNotFoundMessage = "Category không có trong hệ thống";
        public const string UpdateCategorySuccessfulMessage = "Category được cập nhật thành công";
        public const string UpdateCategoryFailedMessage = "Category cập nhật thất bại";
        public const string UpdateExtraCategorySuccessfulMessage = "Extra Category được cập nhật thành công";
        public const string UpdateExtraCategoryFailedMessage = "Extra Category cập nhật thất bại";
    }

    public static class Collection
    {
        public const string EmptyCollectionIdMessage = "Collection Id bị trống";
        public const string CollectionNotFoundMessage = "Collection không tồn tại trong hệ thống";
        public const string CreateNewCollectionFailedMessage = "Tạo mới collection thất bại";
        public const string UpdateProductInCollectionSuccessfulMessage = "Product collection trong được cập nhật thành công";
        public const string UpdateProductInCollectionFailedMessage = "Product collection cập nhật thất bại";
    }

    public static class Product
    {
        public const string CreateNewProductFailedMessage = "Tạo mới product thất bại";
        public const string UpdateProductFailedMessage = "Cập nhật thông tin product thất bại";
        public const string EmptyProductIdMessage = "Product Id bị trống";
        public const string ProductNotFoundMessage = "Product không tồn tại trong hệ thống";
    }

    public static class Menu
    {
        public const string CreateNewMenuFailedMessage = "Tạo mới Menu thất bại";
        public const string EmptyMenuIdMessage = "Id của menu không hợp lệ.";
        public const string MenuNotFoundMessage = "Menu không tồn tại trong hệ thống";
        public const string NoMenusFoundMessage = "Không có menu nào tồn tại trong hệ thống";
        public const string NoMenusAvailableMessage = "Không có menu nào để phục vụ ở thời gian hiện tại";
        public const string ProductNotInBrandMessage = "Bạn không thể thêm product của brand khác, product id: ";
        public const string MissingStoreIdMessage = "Bạn đang không thuộc về một store, không thể lấy menu";
        public const string BrandIdWithMenuIdIsNotExistedMessage = "BrandId và MenuId không tồn tại trong hệ thống";
        public const string BaseMenuIsExistedInBrandMessage = "Brand đã tồn tại menu cơ bản, không thể cập nhật priority là 0";
        public const string UpdateMenuInformationSuccessfulMessage = "Cập nhật thông tin menu thành công";
        public const string UpdateMenuInformationFailedMessage = "Cập nhật thông tin menu thất bại";
        public const string EndTimeLowerThanStartTimeMessage = "Thời gian kết thúc không được nhỏ hơn thời gian bắt đầu";
        public const string StartTimeRequestBiggerThanCurrentMenuEndTimeMessage = "Thời gian bắt đầu khi cập nhật không được lớn hơn thời gian kết thúc hiện tại của menu";
        public const string EndTimeRequestLowerThanCurrentMenuStartTimeMessage = "Thời gian kết thúc khi cập nhật không được nhỏ hơn thời gian bắt đầu hiện tại của menu";
        public const string CanNotUsePriorityAsBaseMenu = "Bạn không thể dùng priority của base menu để tạo menu khác";
        public const string BaseMenuExistedMessage = "Nhãn hàng đã có menu cơ bản";
        public const string UpdateMenuStatusRequestWrongFormatMessage =
            "Cập nhật status menu request sai format";
    }

    public static class Order
    {
        public const string UserNotInSessionMessage = "Tài khoản không trong ca làm để tạo Order";
        public const string NoProductsInOrderMessage = "Không thể tạo order khi order không đính kèm sản phẩm bên trong";
        public const string CreateOrderFailedMessage = "Tạo mới order thất bại";
        public const string EmptyOrderIdMessage = "Id của order không hợp lệ";
        public const string OrderNotFoundMessage = "Order không tồn tại trong hệ thống";
        public const string CanNotCreateOrderInThisTime = "Không thể tạo đơn trong khoản thời gian này";
        public const string CanNotCancelOrder = "Đơn hàng đã làm ko thể huỷ";
    }

    public static class Session
    {
        public const string EmptySessionIdMessage = "Id của session không hợp lệ";
        public const string SessionNotFoundMessage = "Session không tồn tại trong store";
        public const string CreateNewSessionInvalidStartDate = "Session startDate bị trùng với session: ";
        public const string CreateNewSessionInvalidEndDate = "Session endDate bị trùng với session: ";
        public const string AlreadySessionAvailableInTimeGap = "Thời gian của session bạn đang tạo hoặc cập nhật hiện đã có một vài session vui lòng thay đổi thời gian phù hợp";
    }

    public static class GroupProduct
    {
        public const string WrongComboInformationMessage = "Combo bạn đang chọn không tồn tại hoặc không thuộc brand của bạn";
        public const string GroupProductNotFoundMessage = "Group Product không tồn tại trong hệ thống";
    }

    public static class ProductInGroup
    {
        public const string EmptyProductInGroupId = "Id của product in group bị trống";
        public const string ProductInGroupNotFound = "Product in Group không tồn tại trong hệ thống";
        public const string ProductNotInGroupMessage = "Data cũ của product không tồn tại, productId: ";
    }
    
    public static class Promotion
    {
        public const string CreateNewPromotionFailedMessage = "Tạo mới Promotion thất bại";
        public const string UpdatePromotionFailedMessage = "Cập nhật thông tin Promotion thất bại";
        public const string EmptyPromotionIdMessage = "Promotion Id bị trống";
        public const string PromotionNotFoundMessage = "Promotion không tồn tại trong hệ thống";
    }

    public static class User
    {
        public const string CreateNewUserFailedMessage = "Tạo mới User thất bại";
        public const string UpdateUserFailedMessage = "Cập nhật thông tin User thất bại";
        public const string UpdateUserSuccessfulMessage = "Cập nhật thông tin User thành công";
        public const string EmptyUserId = "Id của User bị trống";
        public const string UserNotFound = "User không tồn tại trong hệ thống";

    }

    public static class BlogPost
    {
        public const string BlogNotFoundMessage = "Bài Blog không tồn tại trong hệ thống";
        public const string EmptyBlogIdMessage = "Blog Id bị trống";
        public const string CreateNewBlogPostFailedMessage = "Tạo BlogPost thất bại";
        public const string UpdateBlogPostFailedMessage = "Cập nhật thông tin Blog thất bại";
        public const string UpdateBlogPostSuccessfulMessage = "Cập nhật thông tin Blog thành công";
    }
}
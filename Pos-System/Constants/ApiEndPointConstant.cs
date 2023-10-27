namespace Pos_System.API.Constants;

public static class ApiEndPointConstant
{
    static ApiEndPointConstant()
    {
    }

    public const string RootEndPoint = "/api";
    public const string ApiVersion = "/v1";
    public const string ApiEndpoint = RootEndPoint + ApiVersion;

    public static class Authentication
    {
        public const string AuthenticationEndpoint = ApiEndpoint + "/auth";
        public const string Login = AuthenticationEndpoint + "/login";
    }

    public static class Brand
    {
        public const string BrandsEndpoint = ApiEndpoint + "/brands";
        public const string BrandEndpoint = BrandsEndpoint + "/{id}";
        public const string BrandAccountEndpoint = BrandEndpoint + "/users";
        public const string StoresInBrandEndpoint = BrandEndpoint + "/stores";
        public const string GetCategoriesInBrand = BrandEndpoint + "/categories";
        public const string GetCategoryDetailsInBrand = GetCategoriesInBrand + "/{id}";
        public const string GetCollectionsInBrand = BrandEndpoint + "/collections";
        public const string GetCollectionDetailsInBrand = GetCollectionsInBrand + "/{id}";
        public const string GetProductsInBrand = BrandEndpoint + "/products";
        public const string GetProductDetailsInBrand = GetProductsInBrand + "/{id}";

        public const string ExportStoreEndDateReport = BrandsEndpoint + "/day-report";

        public const string BrandMenuEndpoint = BrandsEndpoint + "/menus";
        public const string StoresInBrandCodeEndpoint = BrandsEndpoint + "/brandcode/stores";
    }

    public static class Store
    {
        public const string StoresEndpoint = ApiEndpoint + "/stores";
        public const string StoreEndpoint = StoresEndpoint + "/{id}";
        public const string StoreUpdateEmployeeEndpoint = StoresEndpoint + "/{storeId}/users/{id}";
        public const string StoreAccountEndpoint = StoresEndpoint + "/{storeId}/users";
        public const string MenuProductsForStaffEndPoint = StoresEndpoint + "/menus";
        public const string StoreOrdersEndpoint = StoreEndpoint + "/orders";
        public const string StoreSessionsEndpoint = StoreEndpoint + "/sessions";
        public const string StoreSessionEndpoint = StoresEndpoint + "/{storeId}/sessions/{id}";
        public const string StoreEndDayReportEndpoint = StoreEndpoint + "/day-report";
        public const string GetPromotion = StoreEndpoint + "/promotion";
    }

    public static class Account
    {
        public const string AccountsEndpoint = ApiEndpoint + "/accounts";
        public const string AccountEndpoint = AccountsEndpoint + "/{id}";
    }

    public static class Category
    {
        public const string CategoriesEndpoint = ApiEndpoint + "/categories";
        public const string CategoryEndpoint = CategoriesEndpoint + "/{id}";
        public const string ExtraCategoryEndpoint = CategoriesEndpoint + "/{categoryId}/extra-categories";
        public const string ProductsInCategoryEndpoint = CategoriesEndpoint + "/{categoryId}/products";
    }

    public static class Collection
    {
        public const string CollectionsEndPoint = ApiEndpoint + "/collections";
        public const string CollectionEndPoint = CollectionsEndPoint + "/{id}";
        public const string ProductsInCollectionEndpoint = CollectionsEndPoint + "/{collectionId}/products";
    }

    public static class Product
    {
        public const string ProductsEndPoint = ApiEndpoint + "/products";
        public const string ProductEndPoint = ProductsEndPoint + "/{id}";
        public const string ProductsInBrandEndPoint = Brand.BrandEndpoint + "/products";
        public const string GroupProductsInBrandEndPoint = Brand.BrandEndpoint + "/groupProducts";
        public const string GroupProductInBrandEndPoint = Brand.BrandsEndpoint + "/{brandId}/groupProducts/{id}";

        public const string GroupProductOfComboEndPoint =
            Brand.BrandsEndpoint + "/{brandId}/products/{id}/groupProducts";

        public const string ProductInGroupEndPoint =
            ApiEndpoint + "/groupProducts/{groupProductId}/productInGroup/{id}";
    }

    public static class Menu
    {
        public const string MenusEndPoint = ApiEndpoint + "/menus";
        public const string MenuEndPoint = MenusEndPoint + "/{menuId}";
        public const string MenusInBrandEndPoint = Brand.BrandEndpoint + "/menus";
        public const string HasBaseMenuEndPoint = MenusInBrandEndPoint + "/hasBaseMenu";
        public const string MenuProductsEndpoint = MenusEndPoint + "/{menuId}/products";
        public const string MenuStoresEndPoint = MenuEndPoint + "/stores";
    }

    public static class PaymentType
    {
        public const string PaymentTypesEndPoint = ApiEndpoint + "/payment-types";
    }

    public static class Order
    {
        public const string OrdersEndPoint = Store.StoresEndpoint + "/{storeId}/orders";
        public const string OrderEndPoint = OrdersEndPoint + "/{id}";
    }


    public static class Report
    {
        public const string ReportEndpoint = ApiEndpoint + "/report";
        public const string SessionReportEndPoint = ReportEndpoint + "/session-report/{id}";
        public const string StoreReportExcelEndPoint = ReportEndpoint + "/store/{id}/download-excel";
    }

    public static class Promotion
    {
        public const string PromotionEndpoint = ApiEndpoint + "/promotion";
        public const string SessionReportEndPoint = PromotionEndpoint + "/session-report/{id}";
    }

    public static class User
    {
        public const string UsersEndpoint = ApiEndpoint + "/users";
        public const string UserEndpoint = UsersEndpoint + "/{id}";
        public const string UserBlogPostEndpoint = UsersEndpoint + "/blog";
    }

    public static class BlogPost
    {
        public const string BlogPostsEndpoint = ApiEndpoint + "/blogposts";
        public const string BlogPostEndpoint = BlogPostsEndpoint + "/{id}";
        public const string GetBlogPostByBrandCodeEndpoint = BlogPostsEndpoint + "/blogpost";
        public const string StatusBlogPostEndpoint = BlogPostEndpoint + "/status";
    }
}
namespace Pos_System.API.Enums;

public enum PromotionPointifyType
{
    UsingVoucher = 3,
    UsingPromoCode = 2,
    Automatic = 1,
}

public enum MemberActionType
{
    PAYMENT,
    TOP_UP,
    GET_POINT
}

public enum MemberActionStatus
{
    SUCCESS,
    FAIL,
    PROSSECING
}
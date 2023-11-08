namespace Pos_System.API.Enums
{
    public enum OrderStatus
    {
        PENDING,
        PAID,
        CANCELED,
        CANCELED_BY_USER
    }
    
    public enum OrderSourceStatus
    {
        PENDING,
        DELIVERING,
        DELIVERED,
        CANCELED
    }
}

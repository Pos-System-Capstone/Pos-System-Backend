using Pos_System.API.Enums;

namespace Pos_System.API.Payload.Request.User;

public class TopUpUserWalletRequest
{
    public Guid StoreId { get; set; }
    
    public Guid UserId { get; set; }
    public double Amount { get; set; }
    public PaymentTypeEnum PaymentType { get; set; }
}
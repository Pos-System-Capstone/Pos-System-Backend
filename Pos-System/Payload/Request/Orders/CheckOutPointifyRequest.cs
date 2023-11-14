namespace Pos_System.API.Payload.Request.Orders;

using System;
using System.Collections.Generic;

public class CheckOutPointifyRequest
{
    public string StoreCode { get; set; }

    public Guid UserId { get; set; }

    public List<ListEffect> ListEffect { get; set; } = new List<ListEffect>();

    public string VoucherCode { get; set; }

    public double FinalAmount { get; set; }

    public double BonusPoint { get; set; }
}

public  class ListEffect
{
    public Guid PromotionId { get; set; }

    public string EffectType { get; set; }
}
namespace Pos_System.API.Payload.Response.Stores
{
    public class GetStoreEndDayReport
    {
      
        public List<CategoryReport> CategoryReports { get; set; } = new List<CategoryReport>();
        public List<PromotionReport> PromotionReports { get; set; } = new List<PromotionReport>();
        public double TotalAmount { get; set; }
        public double TotalProductDiscount { get; set; }
        public double TotalPromotionDiscount { get; set; }
        public double TotalDiscount { get; set; }
        public double VatAmount { get; set; }
        public double FinalAmount { get; set; }
        public double ProductCosAmount { get; set; }
        public double TotalRevenue { get; set; }
        public double AverageBill { get; set; }
        public int TotalProduct { get; set; }
        public int TotalPromotionUsed { get; set; }
        public int TotalOrder { get; set; }
        public int TotalOrderInStore { get; set; }
        public int TotalOrderTakeAway { get; set; }
        public int TotalOrderDeli { get; set; }
        
        public int TotalOrderTopUp { get; set; }
        public double InStoreAmount { get; set; }
        public double DeliAmount { get; set; }
        public double TakeAwayAmount { get; set; }
        
        public double TopUpAmount { get; set; }
        public int TotalCash { get; set; }
        public int TotalBanking { get; set; }
        public int TotalMomo { get; set; }
        public int TotalVisa { get; set; }
        
        public int TotalPointify { get; set; }
        public double CashAmount { get; set; }
        public double MomoAmount { get; set; }
        public double BankingAmount { get; set; }
        public double VisaAmount { get; set; }
        
        public double PointifyAmount { get; set; }
        public int TotalSizeS { get; set; }
        public int TotalSizeM { get; set; }
        public int TotalSizeL { get; set; }
        public double TotalAmountSizeS { get; set; }
        public double TotalAmountSizeM { get; set; }
        public double TotalAmountSizeL { get; set; }

        public List<int> TimeLine { get; set; } = new List<int>();
        public List<int> TotalOrderTimeLine { get; set; } = new List<int>();
        public List<double> TotalAmountTimeLine { get; set; } = new List<double>();
    }

    public class CategoryReport
    {

        public CategoryReport(Guid id, string name, int totalProduct, double totalAmount, double totalDiscount, double finalAmount, List<ProductReport> productReports)
        {
            Id = id;
            Name = name;
            TotalProduct = totalProduct;
            TotalAmount = totalAmount;
            TotalDiscount = totalDiscount;
            ProductReports = productReports;
            FinalAmount = finalAmount;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int TotalProduct { get; set; }
        public double TotalAmount { get; set; }
        public double TotalDiscount { get; set; }
        public double FinalAmount { get; set; }
        public List<ProductReport> ProductReports { get; set; } = new List<ProductReport>();
    }

    public class ProductReport
    {
        public ProductReport(Guid id, string name, int quantity, double sellingPrice, double totalAmount, double totalDiscount, double finalAmount)
        {
            Id = id;
            Name = name;
            Quantity = quantity;
            SellingPrice = sellingPrice;
            TotalAmount = totalAmount;
            TotalDiscount = totalDiscount;
            FinalAmount = finalAmount;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SellingPrice { get; set; }
        public double TotalAmount { get; set; }
        public double TotalDiscount { get; set; }
        public double FinalAmount { get; set; }
    }
    
    public class PromotionReport
    {
        public PromotionReport(Guid id, string name, int quantity, double totalDiscount)
        {
            Id = id;
            Name = name;
            Quantity = quantity;
            TotalDiscount = totalDiscount;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double TotalDiscount { get; set; }
    }
}


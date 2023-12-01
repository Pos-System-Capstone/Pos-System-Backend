using System;
namespace Pos_System.API.Payload.Response.Report
{
    public class SessionReport
    {
        public double TotalAmount { get; set; }
        public double TotalDiscount { get; set; }
        public double FinalAmount { get; set; }
        public int TotalOrder { get; set; }
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

        public SessionReport(double totalAmount, double totalDiscount, double finalAmount, int totalOrder, int totalCash, int totalBanking, int totalMomo, int totalVisa, int totalPointify, double cashAmount, double momoAmount, double bankingAmount, double visaAmount, double pointifyAmount)
        {
            TotalAmount = totalAmount;
            TotalDiscount = totalDiscount;
            FinalAmount = finalAmount;
            TotalOrder = totalOrder;
            TotalCash = totalCash;
            TotalBanking = totalBanking;
            TotalMomo = totalMomo;
            TotalVisa = totalVisa;
            TotalPointify = totalPointify;
            CashAmount = cashAmount;
            MomoAmount = momoAmount;
            BankingAmount = bankingAmount;
            VisaAmount = visaAmount;
            PointifyAmount = pointifyAmount;
        }
    }

}


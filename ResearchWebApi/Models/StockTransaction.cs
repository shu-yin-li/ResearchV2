
using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class StockTransaction
    {
        public double TransTime { get; set; }
        public string TransTimeString { get; set; }
        public double TransPrice { get; set; }
        public TransactionType TransType { get; set; }
        public int TransVolume { get; set; }
        public int Fees { get; set; } = 0;
        public int Tax { get; set; } = 0;
        public double Balance { get; set; } = 0;
        // for validation
        public double? BuyShortMaPrice1DayBefore { get; set; }
        public double? BuyLongMaPrice1DayBefore { get; set; }
        public double? BuyShortMaPrice { get; set; }
        public double? BuyLongMaPrice { get; set; }
        public double? SellShortMaPrice1DayBefore { get; set; }
        public double? SellLongMaPrice1DayBefore { get; set; }
        public double? SellShortMaPrice { get; set; }
        public double? SellLongMaPrice { get; set; }
    }

    public class StockTransList
    {
        public string Name { get; set; }
        public TestCase TestCase { get; set; }
        public List<StockTransaction> Transactions { get; set; }
    }

    public enum TransactionType
    {
        AddFunds,
        Buy,
        Sell
    }
}
using System;
namespace ResearchWebApi.Interface
{
    public interface ITestCase
    {
        public string Symbol { get; set; }
        public double Funds { get; set; }
        public int BuyShortTermMa { get; set; }
        public int BuyLongTermMa { get; set; }
        public int SellShortTermMa { get; set; }
        public int SellLongTermMa { get; set; }
    }
}

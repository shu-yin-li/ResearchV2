using System;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class TestCaseRSI: ITestCase
    {
        public string Symbol { get; set; } = "AAPL";
        public double Funds { get; set; }
        public int MeasureRangeDay { get; set; }
        public int OverSold { get; set; }
        public int OverBought { get; set; }
        public int BuyShortTermMa { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int BuyLongTermMa { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SellShortTermMa { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SellLongTermMa { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TestCaseRSI DeepClone()
        {
            return new TestCaseRSI {
                Symbol = Symbol,
                Funds = Funds,
                MeasureRangeDay = MeasureRangeDay,
                OverSold = OverSold,
                OverBought = OverBought
            };
        }
    }
}

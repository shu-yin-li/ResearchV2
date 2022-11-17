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

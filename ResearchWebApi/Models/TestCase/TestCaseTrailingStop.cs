using System;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class TestCaseTrailingStop : ITestCase
    {
        public TestCaseTrailingStop()
        {
        }

        public string Symbol { get; set; } = "AAPL";
        public double Funds { get; set; }
        public ResultTypeEnum Type { get; set; } = ResultTypeEnum.Train;
        public int BuyShortTermMa { get; set; }
        public int BuyLongTermMa { get; set; }
        public int StopPercentage { get; set; }

        public TestCaseTrailingStop DeepClone()
        {
            return new TestCaseTrailingStop
            {
                Symbol = Symbol,
                Funds = Funds,
                BuyShortTermMa = BuyShortTermMa,
                BuyLongTermMa = BuyLongTermMa,
                StopPercentage = StopPercentage,
            };
        }
    }
}


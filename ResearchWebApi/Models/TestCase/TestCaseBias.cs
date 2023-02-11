using System;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class TestCaseBias : ITestCase
    {
        public TestCaseBias()
        {
        }

        public string Symbol { get; set; } = "AAPL";
        public double Funds { get; set; }
        public ResultTypeEnum Type { get; set; } = ResultTypeEnum.Train;
        public int BuyShortTermMa { get; set; }
        public int BuyLongTermMa { get; set; }
        public int SellShortTermMa { get; set; }
        public int SellLongTermMa { get; set; }
        public int StopPercentage { get; set; }
        public int BuyBiasPercentage { get; set; }
        public int SellBiasPercentage { get; set; }

        public TestCaseBias DeepClone()
        {
            return new TestCaseBias
            {
                Symbol = Symbol,
                Funds = Funds,
                BuyShortTermMa = BuyShortTermMa,
                BuyLongTermMa = BuyLongTermMa,
                SellShortTermMa = SellShortTermMa,
                SellLongTermMa = SellLongTermMa,
                StopPercentage = StopPercentage,
                BuyBiasPercentage = BuyBiasPercentage,
                SellBiasPercentage = SellBiasPercentage,
            };
        }
    }
}


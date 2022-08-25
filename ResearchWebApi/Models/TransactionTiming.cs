using ResearchWebApi.Enum;

namespace ResearchWebApi.Models
{
    public class TransactionTiming
    {
        public StrategyType Buy { get; set; }
        public StrategyType Sell { get; set; }

        public TransactionTiming() { }

        public TransactionTiming(StrategyType strategyType)
        {
            Buy = strategyType;
            Sell = strategyType;
        }

        public TransactionTiming(StrategyType buyType, StrategyType sellType)
        {
            Buy = buyType;
            Sell = sellType;
        }
    }
}

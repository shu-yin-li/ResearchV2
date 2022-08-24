using ResearchWebApi.Enum;

namespace ResearchWebApi.Models
{
    public class TransactionTiming
    {
        private StrategyType Buy1 { get; set; }
        private StrategyType Buy2 { get; set; }
        private StrategyType Sell1 { get; set; }
        private StrategyType Sell2 { get; set; }

        public TransactionTiming(StrategyType strategyType)
        {
            Buy1 = strategyType;
            Buy2 = strategyType;
            Sell1 = strategyType;
            Sell2 = strategyType;
        }

        public TransactionTiming(StrategyType buyType, StrategyType sellType)
        {
            Buy1 = buyType;
            Buy2 = buyType;
            Sell1 = sellType;
            Sell2 = sellType;
        }

        public TransactionTiming(StrategyType buy1Type, StrategyType buy2Type, StrategyType sell1Type, StrategyType sell2Type)
        {
            Buy1 = buy1Type;
            Buy2 = buy2Type;
            Sell1 = sell1Type;
            Sell2 = sell2Type;
        }
    }
}

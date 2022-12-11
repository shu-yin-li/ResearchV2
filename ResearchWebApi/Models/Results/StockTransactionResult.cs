using System;
using ResearchWebApi.Enums;

namespace ResearchWebApi.Models.Results
{
    public class StockTransactionResult: StockTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TrainId { get; set; }
        public string SlidingWinPairName { get; set; }
        public string TransactionNodes { get; set; }
        public string FromDateToDate { get; set; }
        public StrategyType Strategy { get; set; }

        public StockTransactionResult()
        {

        }
    }
}


using System;
using ResearchWebApi.Enum;

namespace ResearchWebApi.Models
{
    public class TrainParameter
    {
        public SlidingWinPair SlidingWinPair { get; set; }
        public MaSelection MaSelection { get; set; }
        public TransactionTiming TransactionTiming { get; set; }
        public Period Period { get; set; }
        public string Symbol { get; set; }

        public TrainParameter()
        {

        }
    }
}

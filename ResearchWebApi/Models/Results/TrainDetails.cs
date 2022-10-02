using System;

namespace ResearchWebApi.Models
{
    public class TrainDetails
    {
        public Guid Id { get; set; }
        public Guid CommonResultId { get; set; }
        public string SlidingWinPairName { get; set; }
        public string RandomSource { get; set; }
        public string AlgorithmName { get; set; }
        public double Delta { get; set; }
        public int ExperimentNumber { get; set; }
        public int Generations { get; set; }
        public int SearchNodeNumber { get; set; }
        public string TrainId { get; set; } // = exp result table
        public string TransactionNodes { get; set; } // = (Buy1, Buy2, Sell1, Sell2) / (RSI)
        public double ExperimentNumberOfBest { get; set; } = 0;
        public double GenerationOfBest { get; set; } = 0;
        public double BestCount { get; set; } = 0;
        public long ExecuteDate { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();
        public string BestSmaList { get; set; }

        public TrainDetails()
        {
        }
    }
}

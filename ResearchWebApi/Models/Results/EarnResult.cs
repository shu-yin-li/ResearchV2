using System;
using ResearchWebApi.Enums;

namespace ResearchWebApi.Models
{
    public class EarnResult
    {
        public Guid Id { get; set; }
        public Guid CommonResultId { get; set; }
        public ResultTypeEnum Mode { get; set; }
        public StrategyType Strategy { get; set; }
        public string TrainId { get; set; } // = mode == Train / Test is not null
        public string FromDateToDate { get; set; }
        public int DayNumber { get; set; }
        public double FinalCapital { get; set; }
        public double FinalEarn { get; set; }
        public double ReturnRates { get; set; }
        public double ARR { get; set; }

        public EarnResult()
        {
        }
    }
}

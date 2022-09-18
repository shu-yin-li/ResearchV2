using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class EachWindowResultParameter
    {
        public double Result { get; set; } = 0;
        public List<StockModelDTO> StockList { get; set; }
        public double PeriodStartTimeStamp { get; set; }
        public SlidingWindow SlidingWindow { get; set; }
        public TrainDetails TrainDetails { get; set; }
        public string SlidingWinPairName { get; set; }
        public Period Period { get; set; }
        public int DayNumber { get; set; }

        public EachWindowResultParameter()
        {
        }
    }
}

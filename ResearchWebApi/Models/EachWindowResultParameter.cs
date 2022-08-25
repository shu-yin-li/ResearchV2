using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class EachWindowResultParameter
    {
        public double Result { get; set; } = 0;
        public List<StockModelDTO> StockList { get; set; }
        public double PeriodStartTimeStamp { get; set; }
        public SlidingWindow SlidingWindow { get; set; }

        public EachWindowResultParameter()
        {
        }
    }
}

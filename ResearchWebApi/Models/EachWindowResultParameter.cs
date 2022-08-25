using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class EachWindowResultParameter
    {
        public TestCase BestTestCase { get; set; }
        public double Result { get; set; } = 0;
        public List<StockModelDTO> StockList { get; set; }
        public double PeriodStartTimeStamp { get; set; }

        public EachWindowResultParameter()
        {
        }
    }
}

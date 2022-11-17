using System.Collections.Generic;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class TrailingStopStatusValue: IStatusValue
    {
        public List<int> BuyMa1 { get; set; } = new List<int>();
        public List<int> BuyMa2 { get; set; } = new List<int>();
        public List<int> StopPercentage { get; set; } = new List<int>();
        public double Fitness { get; set; } = 0;
        public int Experiment { get; set; } = -1;
        public int Generation { get; set; } = -1;

        public TrailingStopStatusValue() { }
        public TrailingStopStatusValue(double funds)
        {
            Fitness = funds;
        }

        public IStatusValue DeepClone()
        {
            return new TrailingStopStatusValue
            {
                BuyMa1 = BuyMa1,
                BuyMa2 = BuyMa2,
                StopPercentage = StopPercentage,
                Fitness = Fitness,
                Experiment = Experiment,
                Generation = Generation
            };
        }
    }

    
}

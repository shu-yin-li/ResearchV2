using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class StatusValue
    {
        public List<int> BuyMa1 { get; set; } = new List<int>();
        public List<int> BuyMa2 { get; set; } = new List<int>();
        public List<int> SellMa1 { get; set; } = new List<int>();
        public List<int> SellMa2 { get; set; } = new List<int>();
        public double Fitness { get; set; } = 0;
        public int Experiment { get; set; } = -1;
        public int Generation { get; set; } = -1;

        public StatusValue() { }
        public StatusValue(double funds)
        {
            Fitness = funds;
        }

        public StatusValue DeepClone()
        {
            return new StatusValue
            {
                BuyMa1 = BuyMa1,
                BuyMa2 = BuyMa2,
                SellMa1 = SellMa1,
                SellMa2 = SellMa2,
                Fitness = Fitness,
                Experiment = Experiment,
                Generation = Generation
            };
        }
    }

    
}

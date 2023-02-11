using System.Collections.Generic;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class BiasStatusValue: IStatusValue
    {
        public List<int> BuyMa1 { get; set; } = new List<int>();
        public List<int> BuyMa2 { get; set; } = new List<int>();
        public List<int> SellMa1 { get; set; } = new List<int>();
        public List<int> SellMa2 { get; set; } = new List<int>();
        public List<int> StopPercentage { get; set; } = new List<int>();
        public List<int> BuyBiasPercentage { get; set; } = new List<int>();
        public List<int> SellBiasPercentage { get; set; } = new List<int>();
        public double Fitness { get; set; } = 0;
        public int Experiment { get; set; } = -1;
        public int Generation { get; set; } = -1;

        public BiasStatusValue() { }
        public BiasStatusValue(double funds)
        {
            Fitness = funds;
        }

        public IStatusValue DeepClone()
        {
            return new BiasStatusValue
            {
                BuyMa1 = BuyMa1,
                BuyMa2 = BuyMa2,
                SellMa1 = SellMa1,
                SellMa2 = SellMa2,
                StopPercentage = StopPercentage,
                BuyBiasPercentage = BuyBiasPercentage,
                SellBiasPercentage = SellBiasPercentage,
                Fitness = Fitness,
                Experiment = Experiment,
                Generation = Generation
            };
        }
    }

    
}

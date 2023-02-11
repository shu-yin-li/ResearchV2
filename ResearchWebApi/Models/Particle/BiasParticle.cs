using System.Collections.Generic;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class BiasParticle: IParticle
    {
        public IStatusValue CurrentFitness { get; set; } = new BiasStatusValue();
        public List<double> BuyMa1Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> BuyMa2Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> SellMa1Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> SellMa2Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> StopPercentageBeta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> BuyBiasPercentageBeta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> SellBiasPercentageBeta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };

        public ITestCase TestCase { get; set; } = new TestCaseBias();
    }
}

using System.Collections.Generic;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class SMAParticle : IParticle
    {
        public IStatusValue CurrentFitness { get; set; } = new SMAStatusValue();
        public List<double> BuyMa1Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> BuyMa2Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> SellMa1Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> SellMa2Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };

        public ITestCase TestCase { get; set; } = new TestCaseSMA();

        //public XValue BestFitness { get; set; } = new XValue();
        //public XValue WorstFitness { get; set; } = new XValue();
    }
}

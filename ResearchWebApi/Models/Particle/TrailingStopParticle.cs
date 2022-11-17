using System.Collections.Generic;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class TrailingStopParticle: IParticle
    {
        public IStatusValue CurrentFitness { get; set; } = new TrailingStopStatusValue();
        public List<double> BuyMa1Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> BuyMa2Beta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        public List<double> StopPercentageBeta { get; set; } = new List<double> { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };

        public ITestCase TestCase { get; set; } = new TestCaseTrailingStop();
    }
}

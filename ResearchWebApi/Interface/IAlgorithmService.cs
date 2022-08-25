using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IAlgorithmService
    {
        
        void UpdateGBestAndGWorst(Particle p, ref StatusValue gBest, ref StatusValue gWorst, int experiment, int iteration);
        void GetLocalBestAndWorst(List<Particle> particles, ref StatusValue localBest, ref StatusValue localWorst);
        void UpdateProbability(Particle p, StatusValue localBest, StatusValue localWorst);
        AlgorithmConst GetConst();
    }
}

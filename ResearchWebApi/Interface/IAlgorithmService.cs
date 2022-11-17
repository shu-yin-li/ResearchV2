using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IAlgorithmService
    {
        
        void UpdateGBestAndGWorst(IParticle p, ref IStatusValue gBest, ref IStatusValue gWorst, int experiment, int iteration);
        void GetLocalBestAndWorst(List<IParticle> particles, ref IStatusValue localBest, ref IStatusValue localWorst);
        void UpdateProbability(IParticle p, IStatusValue localBest, IStatusValue localWorst);
        AlgorithmConst GetConst();
    }
}

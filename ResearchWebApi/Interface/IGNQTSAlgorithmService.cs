using System;
using System.Collections.Generic;
using CsvHelper;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IGNQTSAlgorithmService : IAlgorithmService
    {
        IStatusValue Fit(Queue<int> cRandom, Random random, double funds, List<StockModelDTO> stockList, int experiment, double periodStartTimeStamp, StrategyType strategyType, CsvWriter csv);
        double GetFitness(ITestCase currentTestCase, List<StockModelDTO> stockList, double periodStartTimeStamp, StrategyType strategyType);
        public void UpdateProByGN(IParticle p, IStatusValue gbest, IStatusValue localWorst);
        public void SetDelta(double delta);
        void MetureX(Queue<int> cRandom, Random random, List<IParticle> particles, double funds);
    }
}

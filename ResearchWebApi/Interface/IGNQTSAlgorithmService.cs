using System;
using System.Collections.Generic;
using CsvHelper;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IGNQTSAlgorithmService : IAlgorithmService
    {
        StatusValue Fit(Queue<int> cRandom, Random random, double funds, List<StockModelDTO> stockList, int experiment, double periodStartTimeStamp, StrategyType strategyType, CsvWriter csv);
        double GetFitness(TestCaseSMA currentTestCase, List<StockModelDTO> stockList, double periodStartTimeStamp, StrategyType strategyType);
        public void UpdateProByGN(Particle p, StatusValue gbest, StatusValue localWorst);
        public void SetDelta(double delta);
        void MetureX(Queue<int> cRandom, Random random, List<Particle> particles, double funds);
    }
}

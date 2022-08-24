using System;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IJobsService
    {
        void TrainGNQTSWithSMA(SlidingWinPair SlidingWinPair, string symbol, Period period);
        void TrainGNQTSWithRSI(SlidingWinPair SlidingWinPair, string symbol, Period period);
        void TrainTraditionalWithSMA(SlidingWinPair SlidingWinPair, string symbol, Period period);
        void TrainTraditionalWithRSI(SlidingWinPair SlidingWinPair, string symbol, Period period);
        //void TrainTraditionalWithHybrid(SlidingWinPair SlidingWinPair, string symbol, Period period);
        void Test(Guid trainResultId);
        void BuyAndHold(string symbol, Period period);
    }
}

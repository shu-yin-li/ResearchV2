using System;
using System.Collections.Generic;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IJobsService
    {
        void TrainGNQTSWithSMA(SlidingWinPair pair, string symbol, Period period, bool isCRandom);
        void TrainGNQTSWithTrailingStop(SlidingWinPair pair, string symbol, Period period, bool isCRandom);
        void TrainGNQTSWithRSI(SlidingWinPair pair, string symbol, Period period);
        void TrainTraditionalWithSMA(SlidingWinPair pair, string symbol, Period period);
        void TrainTraditionalWithTrailingStop(SlidingWinPair pair, string symbol, Period period);
        void TrainTraditionalWithRSI(SlidingWinPair pair, string symbol, Period period);
        //void TrainTraditionalWithHybrid(SlidingWinPair SlidingWinPair, string symbol, Period period);
        void Test(SlidingWinPair pair, string algorithmName, string symbol, Period period, StrategyType strategy);
        void BuyAndHold(string symbol, Period period, ResultTypeEnum resultType);
        List<StockTransaction> Temp(Period period);
    }
}

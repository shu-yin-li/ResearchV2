
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IOutputResultService
    {
        void UpdateBuyAndHoldResultInDb(double funds, string stockName, List<StockModelDTO> stockList, double periodStartTimeStamp, double fitness);
        void UpdateTraditionalResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameter, List<TrainDetailsParameter> trainDetailsParameterList);
        void UpdateGNQTSTrainResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameter, List<TrainDetailsParameter> trainDetailsParameterList);
        void UpdateGNQTSTestResultsInDb(double funds, List<EachWindowResultParameter> eachWindowResultParameterList);
    }
}


using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IOutputResultService
    {
        void UpdateBuyAndHoldResultInDb(double funds, string stockName, List<EachWindowResultParameter> eachWindowResultParameterList);
        void UpdateTraditionalResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList);
        void UpdateGNQTSTrainResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList);
        void UpdateGNQTSTestResultsInDb(double funds, List<EachWindowResultParameter> eachWindowResultParameterList);
    }
}

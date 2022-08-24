
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IOutputResultService
    {
        void UpdateBuyAndHoldResultInDb(double funds, string stockName, List<StockModelDTO> stockList, double periodStartTimeStamp, double fitness);
    }
}

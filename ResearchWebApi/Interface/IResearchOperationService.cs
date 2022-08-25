using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IResearchOperationService
    {
        void CalculateAllMa(ref List<StockModel> stockList);
        double GetEarningsResults(List<StockTransaction> myTrans);
        List<StockTransaction> GetBuyAndHoldTransactions(List<StockModelDTO> stockList, double funds);
        List<StockTransaction> GetMyTransactions(List<StockModelDTO> stockList, TestCase testCase, double periodStartTimeStamp);

    }
}

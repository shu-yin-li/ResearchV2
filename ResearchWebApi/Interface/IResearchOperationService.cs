using System.Collections.Generic;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IResearchOperationService
    {
        double GetEarningsResults(List<StockTransaction> myTrans);
        List<StockTransaction> GetBuyAndHoldTransactions(List<StockModelDTO> stockList, double funds);
        List<StockTransaction> GetMyTransactions(
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StrategyType strategyType);
        List<StockTransaction> GetMyTransactionsSMA(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans);
        List<StockTransaction> GetMyTransactionsTrailingStop(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans);
        List<StockTransaction> GetMyTransactionsRSI(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans);
        List<StockTransaction> ProfitSettlement(double currentStock, List<StockModelDTO> stockList, ITestCase testCase, List<StockTransaction> myTrans, double periodEnd);
    }
}

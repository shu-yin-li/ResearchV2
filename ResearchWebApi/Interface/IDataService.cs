using System;
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IDataService
    {
        List<StockModel> GetPeriodDataFromYahooApi(string stockSymbol, DateTime period1, DateTime period2);
        List<StockModelDTO> GetStockDataFromDb(string stockSymbol, DateTime period1, DateTime period2);
        string SendGetRequest(string url);
    }
}

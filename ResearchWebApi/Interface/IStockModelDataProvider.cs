using System;
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IStockModelDataProvider: IDataProvider<StockModel>
    {
        public IEnumerable<StockModel> Find(string stockSymbol, DateTime period1, DateTime period2);
    }
}

using System;
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IStockModelOldDataProvider: IDataProvider<StockModelOld>
    {
        public IEnumerable<StockModelOld> Find(string stockSymbol, DateTime period1, DateTime period2);
    }
}

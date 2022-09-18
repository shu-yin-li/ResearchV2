using System;
using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IRelativeStrengthIndexService
    {
        List<StockModel> CalculateRelativeStrengthIndex(List<StockModel> stockList, int measureRange);
    }
}

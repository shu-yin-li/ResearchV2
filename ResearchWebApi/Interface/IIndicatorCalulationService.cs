using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IIndicatorCalulationService
    {
        List<StockModel> CalculateMovingAvarage(List<StockModel> stockList, int avgDay);
        void CalculateMovingAvarage(ref List<StockModel> stockList);
        void CalculateRelativeStrengthIndex(ref List<StockModel> stockList);
    }
}

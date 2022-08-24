using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface IMovingAvarageService
    {
        List<StockModel> CalculateMovingAvarage(List<StockModel> stockList, int avgDay);
    }
}

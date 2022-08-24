using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class MovingAvarageService : IMovingAvarageService
    {
        public MovingAvarageService()
        {

        }

        public List<StockModel> CalculateMovingAvarage(List<StockModel> stockList, int avgDay)
        {
            var sortedStock = stockList.OrderByDescending(s => s.Date).ToList();
            double? firstSum = 0;
            if (stockList.Count < avgDay) { return stockList; }
            for (var i = 0; i < avgDay; i++)
            {
                firstSum += sortedStock[i].Price;
            }

            var index = 0;
            double? prePrice = 0;
            var maProperty = typeof(StockModel).GetProperty($"Ma{avgDay}");

            sortedStock.ForEach(stock =>
            {
                double? sumPrice = 0;
                var stopCaculate = (index + avgDay - 1) >= sortedStock.Count;
                if (index == 0)
                {
                    sumPrice = firstSum;
                }
                else if (!stopCaculate)
                {
                    sumPrice = sortedStock[index - 1].Price != null && sortedStock[index + avgDay - 1].Price != null ?
                        prePrice - sortedStock[index - 1].Price + sortedStock[index + avgDay - 1].Price
                        : prePrice;
                }

                maProperty.SetValue(stock, sumPrice == 0 ? null : (double)Math.Round(((decimal)sumPrice) / avgDay, 10, MidpointRounding.AwayFromZero));
                
                prePrice = sumPrice;
                index++;
            });
            return sortedStock;
        }
    }
}

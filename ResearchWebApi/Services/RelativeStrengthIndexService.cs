using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class RelativeStrengthIndexService : IRelativeStrengthIndexService
    {
        public RelativeStrengthIndexService()
        {
        }

        public List<StockModel> CalculateRelativeStrengthIndex(List<StockModel> stockList, int measureRange)
        {
            var sortedStock = stockList.OrderByDescending(s => s.Date).ToList();
            //double? firstSum = 0;
            //if (stockList.Count < measureRange) { return stockList; }

            //double? prevStockPrice = 0.0;
            //var gainList = stockList.Select(stock => {
            //    var result = stock.Price - prevStockPrice;
            //    prevStockPrice = stock.Price;
            //    return result > 0 ? result : 0;
            //    }).ToList();

            //prevStockPrice = 0.0;
            //var changeList = stockList.Select(stock => {
            //    var result = stock.Price - prevStockPrice;
            //    prevStockPrice = stock.Price;
            //    return Math.Abs(result ?? 0);
            //}).ToList();

            //for (var i = 0; i < measureRange; i++)
            //{
            //    firstSum += sortedStock[i].Price;
            //}

            //var index = 0;
            //double? prePrice = 0;
            //var maProperty = typeof(StockModel).GetProperty($"Ma{avgDay}");

            //sortedStock.ForEach(stock =>
            //{
            //    double? sumPrice = 0;
            //    var stopCaculate = (index + avgDay - 1) >= sortedStock.Count;
            //    if (index == 0)
            //    {
            //        sumPrice = firstSum;
            //    }
            //    else if (!stopCaculate)
            //    {
            //        sumPrice = sortedStock[index - 1].Price != null && sortedStock[index + avgDay - 1].Price != null ?
            //            prePrice - sortedStock[index - 1].Price + sortedStock[index + avgDay - 1].Price
            //            : prePrice;
            //    }

            //    maProperty.SetValue(stock, sumPrice == 0 ? (double?)null : (double?)Math.Round(((decimal)sumPrice) / avgDay, 10, MidpointRounding.AwayFromZero));

            //    prePrice = sumPrice;
            //    index++;
            //});
            return sortedStock;
        }
    }
}

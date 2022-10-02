using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ResearchWebApi.Helper;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class IndicatorCalculationService : IIndicatorCalulationService
    {
        public IndicatorCalculationService()
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

                maProperty.SetValue(stock, sumPrice == 0 ? (double?)null : (double?)Math.Round(((decimal)sumPrice) / avgDay, 10, MidpointRounding.AwayFromZero));
                
                prePrice = sumPrice;
                index++;
            });
            return sortedStock;
        }

        public void CalculateMovingAvarage(ref List<StockModel> stockList)
        {
            if (stockList is null)
            {
                throw new ArgumentNullException(nameof(stockList));
            }

            var sortedStock = stockList.OrderByDescending(s => s.Date).ToList();

            var maProperties = typeof(MaModel).GetProperties().ToList();
            sortedStock = sortedStock.Select((stock, index) => {
                var maModel = new MaModel();
                maProperties.ForEach(prop =>
                {
                    var avgDay = int.Parse(prop.Name.Replace("Ma", ""));
                    var currentPriceList = sortedStock.Select(s => s.Price).Skip(index).Take(avgDay);
                    var sumPrice = currentPriceList.Count() < avgDay ? 0 : currentPriceList.Sum();
                    var ma = sumPrice == 0 ? null : (double?)Math.Round(((decimal)sumPrice) / avgDay, 10, MidpointRounding.AwayFromZero);
                    prop.SetValue(maModel, ma);
                });
                stock.MaString = JsonConvert.SerializeObject(maModel);
                return stock;
            }).ToList();
        }

        public void CalculateRelativeStrengthIndex(ref List<StockModel> stockList)
        {
            if (stockList is null)
            {
                throw new ArgumentNullException(nameof(stockList));
            }

            var rsiModelList = new List<RsiModel>();
            var sortedStock = stockList.OrderByDescending(s => s.Date).ToList();
            var rsiProperties = typeof(RsiModel).GetProperties().ToList();

            var (gainList, lossList) = CalculateGainLossList(sortedStock);
            sortedStock = sortedStock.Select((stock, index) => {
                if (index == 0) return stock;
                
                var rsiModel = new RsiModel();
                rsiProperties.ForEach(prop =>
                {
                    var measureRange = int.Parse(prop.Name.Replace("Rsi", ""));
                    var prevAvg = 0.0;
                    var avgGain = gainList
                                    .Select((gain, i) => {
                                        if (i == 0 || i == 1)
                                        {
                                            prevAvg = (double)gain;
                                            return gain;
                                        }

                                        var result = (decimal)(prevAvg * (measureRange - 1) + (double)gain) * 1m / measureRange;
                                        prevAvg = (double)result;
                                        return (double)result;
                                    })
                                    .Skip(index)
                                    .Take(1)
                                    .FirstOrDefault();
                    prevAvg = 0.0;
                    var avgLoss = lossList
                                    .Select((loss, i) => {
                                        if (i == 0 || i == 1)
                                        {
                                            prevAvg = (double)loss;
                                            return loss;
                                        }

                                        var result = (decimal)(prevAvg * (measureRange - 1) + (double)loss) * 1m / measureRange;
                                        prevAvg = (double)result;
                                        return (double)result;
                                    }).Skip(index)
                                    .Take(1)
                                    .FirstOrDefault();

                    prop.SetValue(rsiModel, 100 - (100 / (1 + avgGain/avgLoss)));
                });
                stock.RsiString = JsonConvert.SerializeObject(rsiModel);
                return stock;
            }).ToList();
        }

        private static (List<double?>, List<double?>) CalculateGainLossList(List<StockModel> sortedStock)
        {
            var currentPriceList = sortedStock.Select(s => s.Price);
            var prevPriceList = new List<double?> { null };
            prevPriceList.AddRange(sortedStock.Select(s => s.Price));

            var gainList = currentPriceList.Select((price, i) =>
            {
                var prevPrice = prevPriceList.Skip(i).Take(1).FirstOrDefault();
                if (price is not null && prevPrice is not null && price >= prevPrice)
                    return price - prevPrice;
                else
                    return 0;
            })
            .ToList();
            var lossList = currentPriceList.Select((price, i) =>
            {
                var prevPrice = prevPriceList.Skip(i).Take(1).FirstOrDefault();
                if (price is not null && prevPrice is not null && price < prevPrice)
                    return prevPrice - price;
                else
                    return 0;
            })
            .ToList();

            return (gainList, lossList);
        }

        private static double? GetAvg(double diff, int index, int measureRange, ref double prevAvg)
        {
            if (index == 0 || index == 1)
            {
                prevAvg = diff;
                return diff;
            }

            var result = (prevAvg * (measureRange - 1) + diff)/measureRange;
            prevAvg = result;
            return result;
        }
    }
}

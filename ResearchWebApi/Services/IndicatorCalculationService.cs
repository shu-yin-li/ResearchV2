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
            var sortedStock = stockList.OrderBy(s => s.Date).ToList();
            var rsiProperties = typeof(RsiModel).GetProperties().ToList();

            var (gainList, lossList) = CalculateGainLossList(sortedStock);
            var avgGain1 = GetAvg1(gainList);
            var avgLoss1 = GetAvg1(lossList);

            var currentAvgGainList = new List<decimal>();
            var currentAvgLossList = new List<decimal>();
            var prevAvgGainList = new List<decimal>();
            var prevAvgLossList = new List<decimal>();

            sortedStock = sortedStock.Select((stock, index) => {
                if (index == 0) return stock;
                
                var rsiModel = new RsiModel();
                rsiProperties.ForEach(prop =>
                {
                    var measureRange = int.Parse(prop.Name.Replace("Rsi", ""));
                    if (index < measureRange) return;
                    if (prevAvgGainList.Count() < measureRange)
                    {
                        // Summarize all previous change and cal RSI
                        var gain = avgGain1.ElementAt(index);
                        var loss = avgLoss1.ElementAt(index);

                        currentAvgGainList.Add(gain / measureRange);
                        currentAvgLossList.Add(loss / measureRange);

                        prop.SetValue(rsiModel, (gain + loss) is not 0 ? 100 * gain / (gain + loss) : 100);
                    }
                    else
                    {
                        decimal prevAvgGain = prevAvgGainList.ElementAt(measureRange - 1);
                        decimal prevAvgLoss = prevAvgLossList.ElementAt(measureRange - 1);
                        var gain = gainList.ElementAt(index);
                        var avgGain = (prevAvgGain * (measureRange - 1) + gain) * 1m / measureRange;
                        currentAvgGainList.Add(avgGain);

                        var loss = lossList.ElementAt(index);
                        var avgLoss = (prevAvgLoss * (measureRange - 1) + loss) * 1m / measureRange;
                        currentAvgLossList.Add(avgLoss);

                        prop.SetValue(rsiModel, (avgGain + avgLoss) is not 0 ? 100 * avgGain / (avgGain + avgLoss) : 100);
                    }
                });
                stock.RsiString = JsonConvert.SerializeObject(rsiModel);
                prevAvgGainList.Clear();
                prevAvgGainList.AddRange(currentAvgGainList);
                prevAvgLossList.Clear();
                prevAvgLossList.AddRange(currentAvgLossList);
                currentAvgGainList.Clear();
                currentAvgLossList.Clear();
                return stock;
            }).ToList();
        }

        private static (List<decimal>, List<decimal>) CalculateGainLossList(List<StockModel> sortedStock)
        {
            var currentPriceList = sortedStock.Select(s => s.Price);
            var prevPriceList = new List<double?> { null };
            prevPriceList.AddRange(sortedStock.Select(s => s.Price));

            var gainList = currentPriceList.Select((price, i) =>
            {
                var prevPrice = prevPriceList.Skip(i).Take(1).FirstOrDefault();
                if (price is not null && prevPrice is not null && price >= prevPrice)
                    return (decimal)(price - prevPrice);
                else
                    return 0;
            })
            .ToList();
            var lossList = currentPriceList.Select((price, i) =>
            {
                var prevPrice = prevPriceList.Skip(i).Take(1).FirstOrDefault();
                if (price is not null && prevPrice is not null && price < prevPrice)
                    return (decimal)(prevPrice - price);
                else
                    return 0;
            })
            .ToList();

            return (gainList, lossList);
        }

        private static List<decimal> GetAvg1(List<decimal> list)
        {
            decimal sum = 0;
            var result = list.Select(value => sum += value);
            return result.ToList();
        }
    }
}

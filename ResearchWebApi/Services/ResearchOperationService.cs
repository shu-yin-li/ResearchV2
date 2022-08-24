using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class ResearchOperationService: IResearchOperationService
    {
        private ICalculateVolumeService _calculateVolumeService;
        private IMovingAvarageService _movingAvgService;

        public ResearchOperationService(ICalculateVolumeService calculateVolumeService, IMovingAvarageService movingAvgService)
        {
            _movingAvgService = movingAvgService ?? throw new ArgumentNullException(nameof(movingAvgService));
            _calculateVolumeService = calculateVolumeService ?? throw new ArgumentNullException(nameof(calculateVolumeService));
        }

        public void CalculateAllMa(ref List<StockModel> stockList)
        {
            for (var i = 1; i <= 256; i++)
            {
                stockList = _movingAvgService.CalculateMovingAvarage(stockList, i);
            }
        }

        public List<StockTransaction> ProfitSettlement(double currentStock, List<StockModelDTO> stockList, TestCase testCase, List<StockTransaction> myTrans, double periodEnd)
        {
            var hasQty = myTrans.Last().TransType == TransactionType.Buy;
            if (hasQty && myTrans.Last().TransTime == stockList.Last().Date) {
                myTrans.RemoveAt(myTrans.Count - 1);
            }
            else if (hasQty)
            {
                //var timeString = Utils.UnixTimeStampToDateTime(periodEnd);
                var price = currentStock;
                var sellMaValList = stockList.TakeLast(2);
                myTrans.Add(new StockTransaction
                {
                    TransTime = periodEnd,
                    //TransTimeString = $"{timeString.Year}-{timeString.Month}-{timeString.Day}",
                    TransPrice = price,
                    TransType = TransactionType.Sell,
                    TransVolume = myTrans.Last().TransVolume,
                    Balance = myTrans.Last().Balance + Math.Round(currentStock * myTrans.Last().TransVolume, 10, MidpointRounding.ToZero),
                    SellShortMaPrice = sellMaValList.LastOrDefault().MaList[testCase.SellShortTermMa] ?? 0,
                    SellLongMaPrice = sellMaValList.LastOrDefault().MaList[testCase.SellLongTermMa] ?? 0,
                    SellShortMaPrice1DayBefore = sellMaValList.FirstOrDefault().MaList[testCase.SellShortTermMa] ?? 0,
                    SellLongMaPrice1DayBefore = sellMaValList.FirstOrDefault().MaList[testCase.SellLongTermMa] ?? 0,
                });
            }

            return myTrans;
        }

        public double GetEarningsResults(List<StockTransaction> myTrans)
        {
            var buy = myTrans.Where(trans => trans.TransType == TransactionType.Buy)
                .Sum(trans => Math.Round(trans.TransPrice * trans.TransVolume, 10, MidpointRounding.ToZero));
            var sell = myTrans.Where(trans => trans.TransType == TransactionType.Sell)
                .Sum(trans => Math.Round(trans.TransPrice * trans.TransVolume, 10, MidpointRounding.ToZero));
            var earn = sell - buy + myTrans.FirstOrDefault(t=>t.TransType == TransactionType.AddFunds).Balance;
            return earn;
        }

        public List<StockTransaction> GetBuyAndHoldTransactions(List<StockModelDTO> stockList, double funds)
        {
            var myTransactions = new List<StockTransaction>();
            var lastTrans = new StockTransaction
            {
                TransTime = 0,
                TransTimeString = string.Empty,
                TransPrice = 0,
                TransVolume = 0,
                TransType = TransactionType.AddFunds,
                Balance = funds
            };
            myTransactions.Add(lastTrans);

            var firstStock = stockList.First();
            var buyPrice = firstStock.Price ?? 0;
            var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, buyPrice);
            lastTrans = new StockTransaction
            {
                TransTime = firstStock.Date,
                TransPrice = buyPrice,
                TransType = TransactionType.Buy,
                TransVolume = volume,
                Balance = lastTrans.Balance - Math.Round(buyPrice * volume, 10, MidpointRounding.ToZero),
                BuyShortMaPrice = 0,
                BuyLongMaPrice = 0,
                BuyShortMaPrice1DayBefore = 0,
                BuyLongMaPrice1DayBefore = 0,
            };
            myTransactions.Add(lastTrans);

            var lastStock = stockList.Last();
            var sellPrice = lastStock.Price ?? 0;
            lastTrans = new StockTransaction
            {
                TransTime = lastStock.Date,
                TransPrice = sellPrice,
                TransType = TransactionType.Sell,
                TransVolume = volume,
                Balance = lastTrans.Balance + Math.Round(sellPrice * volume, 10, MidpointRounding.ToZero),
                SellShortMaPrice = 0,
                SellLongMaPrice = 0,
                SellShortMaPrice1DayBefore = 0,
                SellLongMaPrice1DayBefore = 0,
            };
            myTransactions.Add(lastTrans);
            return myTransactions;
        }
    }
}

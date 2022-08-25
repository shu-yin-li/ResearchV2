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
        private ITransTimingService _transTimingService;


        public ResearchOperationService(
            ICalculateVolumeService calculateVolumeService,
            IMovingAvarageService movingAvgService,
            ITransTimingService transTimingService)
        {
            _movingAvgService = movingAvgService ?? throw new ArgumentNullException(nameof(movingAvgService));
            _calculateVolumeService = calculateVolumeService ?? throw new ArgumentNullException(nameof(calculateVolumeService));
            _transTimingService = transTimingService ?? throw new ArgumentNullException(nameof(transTimingService));
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

        public List<StockTransaction> GetMyTransactions(
            List<StockModelDTO> stockList,
            TestCase testCase,
            double periodStartTimeStamp)
        {
            var myTransactions = new List<StockTransaction>();
            var lastTrans = new StockTransaction
            {
                TransTime = 0,
                TransTimeString = string.Empty,
                TransPrice = 0,
                TransVolume = 0,
                TransType = TransactionType.AddFunds,
                Balance = testCase.Funds
            };
            myTransactions.Add(lastTrans);

            bool hasQty = false;
            StockModelDTO prevStock = stockList.FirstOrDefault();
            stockList.ForEach(stock =>
            {
                var buyShortMaVal = stock.MaList[testCase.BuyShortTermMa] ?? null;
                var buyLongMaVal = stock.MaList[testCase.BuyLongTermMa] ?? null;
                var sellShortMa = stock.MaList[testCase.SellShortTermMa] ?? null;
                var sellLongMaVal = stock.MaList[testCase.SellLongTermMa] ?? null;

                var prevBuyShortMa = prevStock.MaList[testCase.BuyShortTermMa] ?? null;
                var prevBuyLongMaVal = prevStock.MaList[testCase.BuyLongTermMa] ?? null;
                var prevSellShortMaVal = prevStock.MaList[testCase.SellShortTermMa] ?? null;
                var prevSellLongMaVal = prevStock.MaList[testCase.SellLongTermMa] ?? null;
                if (stock.Date > periodStartTimeStamp)
                {
                    var price = stock.Price ?? 0;

                    bool testToBuy = _transTimingService.TimeToBuy(buyShortMaVal, buyLongMaVal, prevBuyShortMa, prevBuyLongMaVal, hasQty);
                    bool testToSell = _transTimingService.TimeToSell(sellShortMa, sellLongMaVal, prevSellShortMaVal, prevSellLongMaVal, hasQty);

                    if (buyShortMaVal != null && buyLongMaVal != null && testToBuy)
                    {

                        var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, price);
                        lastTrans = new StockTransaction
                        {
                            TransTime = stock.Date,
                            TransPrice = price,
                            TransType = TransactionType.Buy,
                            TransVolume = volume,
                            Balance = lastTrans.Balance - Math.Round(price * volume, 10, MidpointRounding.ToZero),
                            BuyShortMaPrice = buyShortMaVal,
                            BuyLongMaPrice = buyLongMaVal,
                            BuyShortMaPrice1DayBefore = prevBuyShortMa,
                            BuyLongMaPrice1DayBefore = prevBuyLongMaVal,
                        };
                        myTransactions.Add(lastTrans);
                        hasQty = !hasQty;

                    }
                    // todo: 停損比例改為參數，從testcase丟進來
                    // todo: 注意現在是用哪一種時機點
                    else if (sellShortMa != null && sellLongMaVal != null && testToSell)
                    {
                        var volume = _calculateVolumeService.CalculateSellingVolume(myTransactions.LastOrDefault().TransVolume);
                        lastTrans = new StockTransaction
                        {
                            TransTime = stock.Date,
                            TransPrice = price,
                            TransType = TransactionType.Sell,
                            TransVolume = volume,
                            Balance = lastTrans.Balance + Math.Round(price * volume, 10, MidpointRounding.ToZero),
                            SellShortMaPrice = sellShortMa,
                            SellLongMaPrice = sellLongMaVal,
                            SellShortMaPrice1DayBefore = prevSellShortMaVal,
                            SellLongMaPrice1DayBefore = prevSellLongMaVal,
                        };
                        myTransactions.Add(lastTrans);
                        hasQty = !hasQty;
                    }
                }
                prevStock = stock;
            });

            var currentStock = stockList.Last().Price ?? 0;
            var periodEnd = stockList.Last().Date;
            ProfitSettlement(currentStock, stockList, testCase, myTransactions, periodEnd);

            return myTransactions;
        }

    }
}

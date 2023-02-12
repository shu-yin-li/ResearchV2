using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hangfire.Server;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class ResearchOperationService: IResearchOperationService
    {
        private ICalculateVolumeService _calculateVolumeService;
        private IIndicatorCalulationService _movingAvgService;
        private ITransTimingService _transTimingService;


        public ResearchOperationService(
            ICalculateVolumeService calculateVolumeService,
            IIndicatorCalulationService movingAvgService,
            ITransTimingService transTimingService)
        {
            _movingAvgService = movingAvgService ?? throw new ArgumentNullException(nameof(movingAvgService));
            _calculateVolumeService = calculateVolumeService ?? throw new ArgumentNullException(nameof(calculateVolumeService));
            _transTimingService = transTimingService ?? throw new ArgumentNullException(nameof(transTimingService));
        }

        public List<StockTransaction> ProfitSettlement(double currentStock, List<StockModelDTO> stockList, ITestCase testCase, List<StockTransaction> myTrans, double periodEnd)
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
                    // for debugging
                    //SellShortMaPrice = sellMaValList.LastOrDefault().MaList[testCaseSma.SellShortTermMa] ?? 0,
                    //SellLongMaPrice = sellMaValList.LastOrDefault().MaList[testCaseSma.SellLongTermMa] ?? 0,
                    //SellShortMaPrice1DayBefore = sellMaValList.FirstOrDefault().MaList[testCaseSma.SellShortTermMa] ?? 0,
                    //SellLongMaPrice1DayBefore = sellMaValList.FirstOrDefault().MaList[testCaseSma.SellLongTermMa] ?? 0,
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
            };
            myTransactions.Add(lastTrans);
            return myTransactions;
        }

        public List<StockTransaction> GetMyTransactions(
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StrategyType strategyType)
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

            if (strategyType == StrategyType.SMA) GetMyTransactionsSMA(myTransactions, stockList, testCase, periodStartTimeStamp, lastTrans);
            if (strategyType == StrategyType.RSI) GetMyTransactionsRSI(myTransactions, stockList, testCase, periodStartTimeStamp, lastTrans);
            if (strategyType == StrategyType.TrailingStop) GetMyTransactionsTrailingStop(myTransactions, stockList, testCase, periodStartTimeStamp, lastTrans);
            if (strategyType == StrategyType.Bias) GetMyTransactionsBias(myTransactions, stockList, testCase, periodStartTimeStamp, lastTrans);

            return myTransactions;
        }

        public List<StockTransaction> GetMyTransactionsSMA(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans)
        {
            bool hasQty = false;
            var testCaseSma = (TestCaseSMA)testCase;
            List<StockModelTransInfo> stockInfoList = MapStockInfo(stockList, periodStartTimeStamp, testCaseSma);

            stockInfoList.FindAll(stock => stock.Date > periodStartTimeStamp).ForEach(stock =>
            {
                var price = stock.Price ?? 0;

                bool testToBuy = _transTimingService.TimeToBuy(stock.BuyShortTermMa, stock.BuyLongTermMa, stock.PrevBuyShortTermMa, stock.PrevBuyLongTermMa, hasQty);
                bool testToSell = _transTimingService.TimeToSell(stock.SellShortTermMa, stock.SellLongTermMa, stock.PrevSellShortTermMa, stock.PrevSellLongTermMa, hasQty);

                if (stock.BuyShortTermMa != null && stock.BuyLongTermMa != null && testToBuy)
                {
                    var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, price);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Buy,
                        TransVolume = volume,
                        Balance = lastTrans.Balance - Math.Round(price * volume, 10, MidpointRounding.ToZero),
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;

                }
                // Note: 注意現在是用哪一種時機點
                else if (stock.SellShortTermMa != null && stock.SellLongTermMa != null && testToSell)
                {
                    var volume = _calculateVolumeService.CalculateSellingVolume(myTransactions.LastOrDefault().TransVolume);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Sell,
                        TransVolume = volume,
                        Balance = lastTrans.Balance + Math.Round(price * volume, 10, MidpointRounding.ToZero),
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;
                }
            });

            return myTransactions;
        }

        public List<StockTransaction> GetMyTransactionsTrailingStop(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans)
        {
            bool hasQty = false;
            bool firstDay = true;
            var testCaseTrailingStop = (TestCaseTrailingStop)testCase;
            double maxPrice = 0;
            var trailingStopPercentage = testCaseTrailingStop.StopPercentage;
            List<StockModelTransInfo> stockInfoList = MapStockInfo(stockList, periodStartTimeStamp, testCaseTrailingStop);

            stockInfoList.FindAll(stock => stock.Date > periodStartTimeStamp).ForEach(stock =>
            {
                var price = stock.Price ?? 0;

                bool testToBuy = firstDay
                        ? stock.BuyShortTermMa > stock.BuyLongTermMa
                        : _transTimingService.TimeToBuy(stock.BuyShortTermMa, stock.BuyLongTermMa, stock.PrevBuyShortTermMa, stock.PrevBuyLongTermMa, hasQty);
                bool testToSell = _transTimingService.TimeToSell(stock.SellShortTermMa, stock.SellLongTermMa, stock.PrevSellShortTermMa, stock.PrevSellLongTermMa, hasQty)
                                  || _transTimingService.TimeToSell(lastTrans, ref maxPrice, price, stock.Date, trailingStopPercentage, hasQty);

                firstDay = false;

                if (stock.BuyShortTermMa != null && stock.BuyLongTermMa != null && testToBuy)
                {
                    var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, price);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Buy,
                        TransVolume = volume,
                        Balance = lastTrans.Balance - Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        BuyShortMaPrice = stock.BuyShortTermMa,
                        BuyLongMaPrice = stock.BuyLongTermMa,
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;

                }
                // Note: 注意現在是用哪一種時機點
                else if (stock.SellShortTermMa != null && stock.SellLongTermMa != null && testToSell)
                {
                    var volume = _calculateVolumeService.CalculateSellingVolume(myTransactions.LastOrDefault().TransVolume);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Sell,
                        TransVolume = volume,
                        Balance = lastTrans.Balance + Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        TrailingStopPercentage = trailingStopPercentage,
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;
                    maxPrice = 0;
                }
            });

            return myTransactions;
        }

        public List<StockTransaction> GetMyTransactionsBias(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans)
        {
            bool hasQty = false;
            bool firstDay = true;
            var testCaseBias = (TestCaseBias)testCase;
            double maxPrice = 0;
            var trailingStopPercentage = testCaseBias.StopPercentage;
            var buyBiasPercentage = testCaseBias.BuyBiasPercentage;
            var sellBiasPercentage = testCaseBias.SellBiasPercentage;
            List<StockModelTransInfo> stockInfoList = MapStockInfo(stockList, periodStartTimeStamp, testCaseBias);

            stockInfoList.FindAll(stock => stock.Date > periodStartTimeStamp).ForEach(stock =>
            {
                var price = stock.Price ?? 0;

                bool testToBuy = firstDay
                        ? _transTimingService.TimeToBuyCheckingByBias(price, stock.BuyShortTermMa, buyBiasPercentage) || stock.BuyShortTermMa > stock.BuyLongTermMa
                        : _transTimingService.TimeToBuyCheckingByBias(price, stock.BuyShortTermMa, buyBiasPercentage) || _transTimingService.TimeToBuy(stock.BuyShortTermMa, stock.BuyLongTermMa, stock.PrevBuyShortTermMa, stock.PrevBuyLongTermMa, hasQty);
                bool testToSell = _transTimingService.TimeToSellCheckingByBias(price, stock.SellShortTermMa, sellBiasPercentage) 
                                    || _transTimingService.TimeToSell(stock.SellShortTermMa, stock.SellLongTermMa, stock.PrevSellShortTermMa, stock.PrevSellLongTermMa, hasQty)
                                    || _transTimingService.TimeToSell(lastTrans, ref maxPrice, price, stock.Date, trailingStopPercentage, hasQty);

                firstDay = false;

                if (stock.BuyShortTermMa != null && stock.BuyLongTermMa != null && testToBuy)
                {
                    var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, price);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Buy,
                        TransVolume = volume,
                        Balance = lastTrans.Balance - Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        BuyShortMaPrice = stock.BuyShortTermMa,
                        BuyLongMaPrice = stock.BuyLongTermMa,
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;

                }
                // Note: 注意現在是用哪一種時機點
                else if (stock.SellShortTermMa != null && stock.SellLongTermMa != null && testToSell)
                {
                    var volume = _calculateVolumeService.CalculateSellingVolume(myTransactions.LastOrDefault().TransVolume);
                    lastTrans = new StockTransactionSMA
                    {
                        TransTime = stock.Date,
                        TransPrice = price,
                        TransType = TransactionType.Sell,
                        TransVolume = volume,
                        Balance = lastTrans.Balance + Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        TrailingStopPercentage = trailingStopPercentage,
                    };
                    myTransactions.Add(lastTrans);
                    hasQty = !hasQty;
                    maxPrice = 0;
                }
            });

            return myTransactions;
        }


        private static List<StockModelTransInfo> MapStockInfo(List<StockModelDTO> stockList, double periodStartTimeStamp, ITestCase testCaseTrailingStop)
        {
            StockModelDTO prevStock = stockList.FirstOrDefault();
            var stockInfoList = stockList.Select(stock =>
            {
                var result =  new StockModelTransInfo
                {
                    StockName = stock.StockName,
                    Date = stock.Date,
                    Price = stock.Price,
                    BuyShortTermMa = stock.MaList[testCaseTrailingStop.BuyShortTermMa] ?? null,
                    BuyLongTermMa = stock.MaList[testCaseTrailingStop.BuyLongTermMa] ?? null,
                    SellShortTermMa = stock.MaList[testCaseTrailingStop.SellShortTermMa] ?? null,
                    SellLongTermMa = stock.MaList[testCaseTrailingStop.SellLongTermMa] ?? null,
                    PrevBuyShortTermMa = prevStock.MaList[testCaseTrailingStop.BuyShortTermMa] ?? null,
                    PrevBuyLongTermMa = prevStock.MaList[testCaseTrailingStop.BuyLongTermMa] ?? null,
                    PrevSellShortTermMa = prevStock.MaList[testCaseTrailingStop.SellShortTermMa] ?? null,
                    PrevSellLongTermMa = prevStock.MaList[testCaseTrailingStop.SellLongTermMa] ?? null,
                };
                prevStock = stock;
                return result;
            }).ToList();

            return stockInfoList;
        }

        public List<StockTransaction> GetMyTransactionsRSI(
            List<StockTransaction> myTransactions,
            List<StockModelDTO> stockList,
            ITestCase testCase,
            double periodStartTimeStamp,
            StockTransaction lastTrans)
        {
            bool hasQty = false;
            StockModelDTO prevStock = stockList.FirstOrDefault();
            var testCaseRsi = (TestCaseRSI)testCase;
            stockList.ForEach(stock =>
            {
                var rsi = stock.RsiList[testCaseRsi.MeasureRangeDay];
                var prevRsi = prevStock.RsiList[testCaseRsi.MeasureRangeDay];
                if (stock.Date > periodStartTimeStamp)
                {
                    var price = stock.Price ?? 0;

                    bool testToBuy = _transTimingService.TimeToBuy(rsi, prevRsi, testCaseRsi.OverSold, hasQty);
                    bool testToSell = _transTimingService.TimeToSell(rsi, prevRsi, testCaseRsi.OverBought, hasQty);

                    if (
                    //rsi != null &&
                    testToBuy)
                    {
                        var volume = _calculateVolumeService.CalculateBuyingVolumeOddShares(lastTrans.Balance, price);
                        lastTrans = new StockTransaction
                        {
                            TransTime = stock.Date,
                            TransPrice = price,
                            TransType = TransactionType.Buy,
                            TransVolume = volume,
                            Balance = lastTrans.Balance - Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        };
                        myTransactions.Add(lastTrans);
                        hasQty = !hasQty;
                    }
                    // Note: 注意現在是用哪一種時機點
                    else if (
                    //rsi != null &&
                    testToSell)
                    {
                        var volume = _calculateVolumeService.CalculateSellingVolume(myTransactions.LastOrDefault().TransVolume);
                        lastTrans = new StockTransaction
                        {
                            TransTime = stock.Date,
                            TransPrice = price,
                            TransType = TransactionType.Sell,
                            TransVolume = volume,
                            Balance = lastTrans.Balance + Math.Round(price * volume, 10, MidpointRounding.ToZero),
                        };
                        myTransactions.Add(lastTrans);
                        hasQty = !hasQty;
                    }
                }
                prevStock = stock;
            });
            return myTransactions;
        }
    }
}

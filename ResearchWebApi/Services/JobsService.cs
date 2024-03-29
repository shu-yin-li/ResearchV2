using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using AutoMapper;
using CsvHelper;
using Newtonsoft.Json;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;
using ResearchWebApi.Models.Results;

namespace ResearchWebApi.Services
{
    public class JobsService: IJobsService
    {
        private readonly IResearchOperationService _researchOperationService;
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IOutputResultService _outputResultService;
        private readonly ISlidingWindowService _slidingWindowService;
        private readonly ISMAGNQTSAlgorithmService _smaGnqtsAlgorithmService;
        private readonly ITrailingStopGNQTSAlgorithmService _trailingStopGnqtsAlgorithmService;
        private readonly IBiasGNQTSAlgorithmService _biasGNQTSAlgorithmService;
        private readonly ITrainDetailsDataProvider _trainDetailsDataProvider;
        private readonly IFileHandler _fileHandler;


        private const double FUNDS = 10000000;

        public JobsService(
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IMapper mapper,
            IOutputResultService outputResultService,
            ISlidingWindowService slidingWindowService,
            ISMAGNQTSAlgorithmService smaGnqtsAlgorithmService,
            ITrailingStopGNQTSAlgorithmService trailingStopGnqtsAlgorithmService,
            IBiasGNQTSAlgorithmService biasGNQTSAlgorithmService,
            ITrainDetailsDataProvider trainDetailsDataProvider,
            IFileHandler fileHandler)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outputResultService = outputResultService ?? throw new ArgumentNullException(nameof(outputResultService));
            _slidingWindowService = slidingWindowService ?? throw new ArgumentNullException(nameof(slidingWindowService));
            _smaGnqtsAlgorithmService = smaGnqtsAlgorithmService ?? throw new ArgumentNullException(nameof(smaGnqtsAlgorithmService));
            _trailingStopGnqtsAlgorithmService = trailingStopGnqtsAlgorithmService ?? throw new ArgumentNullException(nameof(trailingStopGnqtsAlgorithmService));
            _biasGNQTSAlgorithmService = biasGNQTSAlgorithmService ?? throw new ArgumentNullException(nameof(biasGNQTSAlgorithmService));
            _trainDetailsDataProvider = trainDetailsDataProvider ?? throw new ArgumentNullException(nameof(trainDetailsDataProvider));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        public void BuyAndHold(string symbol, Period period, ResultTypeEnum resultType)
        {
            List<SlidingWinPair> pairList = Utils.Get13TraditionalSlidingWindows();

            var eachWindowResultParameterList = pairList.Select(pair =>
            {
                var windowPeriod = GetBuyAndHoldPeriod(period, pair, resultType);
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(windowPeriod.Start);
                var stockListAll = _dataService.GetStockDataFromDb(symbol, windowPeriod.Start, windowPeriod.End.AddDays(1));
                #region get first and last 7 stock model from all

                var stockListDto = stockListAll.Take(7).ToList();
                stockListAll.Reverse();
                var stockListEnd = stockListAll.Take(7).Reverse();
                stockListDto.AddRange(stockListEnd);

                #endregion
                var transactions = _researchOperationService.GetBuyAndHoldTransactions(stockListDto, FUNDS);
                var earns = _researchOperationService.GetEarningsResults(transactions);
                var result = Math.Round(earns, 10);

                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    Result = result,
                    SlidingWinPairName = slidingWinPairName,
                    Period = windowPeriod,
                    DayNumber = stockListAll.Count
                };

                return eachWindowResultParameter;
            }).ToList();

            #region get 10y B&H

            var stockListAll = _dataService.GetStockDataFromDb(symbol, period.Start, period.End.AddDays(1));
            #region get first and last 7 stock model from all

            var stockListDto = stockListAll.Take(7).ToList();
            stockListAll.Reverse();
            var stockListEnd = stockListAll.Take(7).Reverse();
            stockListDto.AddRange(stockListEnd);

            #endregion
            var transactions = _researchOperationService.GetBuyAndHoldTransactions(stockListDto, FUNDS);
            var earns = _researchOperationService.GetEarningsResults(transactions);
            var result = Math.Round(earns, 10);

            var slidingWinPairName = "10Y";
            var eachWindowResultParameter = new EachWindowResultParameter
            {
                StockList = stockListDto,
                PeriodStartTimeStamp = Utils.ConvertToUnixTimestamp(period.Start),
                Result = result,
                SlidingWinPairName = slidingWinPairName,
                Period = period,
                DayNumber = 2517
            };

            eachWindowResultParameterList.Add(eachWindowResultParameter);

            #endregion

            _outputResultService.UpdateBuyAndHoldResultInDb(FUNDS, symbol, eachWindowResultParameterList);
        }

        private Period GetBuyAndHoldPeriod(Period period, SlidingWinPair pair, ResultTypeEnum resultType)
        {
            List<SlidingWindow> slidingWindows = pair.IsStar
                            ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                            : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            if(resultType == ResultTypeEnum.Train)
            {
                return slidingWindows.Any() ? new Period
                {
                    Start = slidingWindows.First().TrainPeriod.Start,
                    End = slidingWindows.Last().TrainPeriod.End
                } : period;
            } else
            {
                return slidingWindows.Any() ? new Period
                {
                    Start = slidingWindows.First().TestPeriod.Start,
                    End = slidingWindows.Last().TestPeriod.End
                } : period;
            }
        }

        public void Test(SlidingWinPair pair, string algorithmName, string symbol, Period period, StrategyType strategy)
        {
            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var stockTransactionResultList = new List<StockTransactionResult>();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var trainId = $"{algorithmName}_{strategy}_{slidingWinPairName}_{Utils.ConvertToUnixTimestamp(window.TrainPeriod.Start)}";
                var trainDetails = _trainDetailsDataProvider.FindLatest(trainId, symbol);
                if(trainDetails ==  null) throw new InvalidOperationException($"{trainId} is not found.");
                var transNodes = trainDetails.TransactionNodes.Split(",");

                ITestCase testCase;
                if (strategy == StrategyType.SMA)
                {
                    testCase = new TestCaseSMA
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        Type = ResultTypeEnum.Test,
                        BuyShortTermMa = int.Parse(transNodes[0]),
                        BuyLongTermMa = int.Parse(transNodes[1]),
                        SellShortTermMa = int.Parse(transNodes[2]),
                        SellLongTermMa = int.Parse(transNodes[3]),
                    };
                }
                else if(strategy == StrategyType.TrailingStop)
                {
                    testCase = new TestCaseTrailingStop
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        Type = ResultTypeEnum.Test,
                        BuyShortTermMa = int.Parse(transNodes[0]),
                        BuyLongTermMa = int.Parse(transNodes[1]),
                        SellShortTermMa = int.Parse(transNodes[2]),
                        SellLongTermMa = int.Parse(transNodes[3]),
                        StopPercentage = int.Parse(transNodes[4])
                    };
                }
                else
                {
                    testCase = new TestCaseBias
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        Type = ResultTypeEnum.Test,
                        BuyShortTermMa = int.Parse(transNodes[0]),
                        BuyLongTermMa = int.Parse(transNodes[1]),
                        SellShortTermMa = int.Parse(transNodes[2]),
                        SellLongTermMa = int.Parse(transNodes[3]),
                        StopPercentage = int.Parse(transNodes[4]),
                        BuyBiasPercentage = int.Parse(transNodes[5]),
                        SellBiasPercentage = int.Parse(transNodes[6]),
                    };
                }


                List<StockTransaction> transactions = new List<StockTransaction>();
                var periodStart = window.TestPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);
                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TestPeriod.Start.AddDays(-7), window.TestPeriod.End.AddDays(1));
                transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp, strategy);

                //var stockListDto = new List<StockModelDTO>();
                //var increasedEndDay = 1;

                //do {
                //    var currentStockList = stockList.FindAll(s => s.Date < Utils.ConvertToUnixTimestamp(window.TestPeriod.End.AddDays(increasedEndDay)));
                //    stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(currentStockList);
                //    transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp, strategy);
                //    increasedEndDay++;
                //} while ((transactions.Count == 1 || transactions.Last().TransType == TransactionType.Buy) && stockListDto.Count != stockList.Count);

                var results = transactions.Select(trans =>
                {
                    var result = new StockTransactionResult
                    {
                        TrainId = trainId,
                        SlidingWinPairName = slidingWinPairName,
                        TransactionNodes = trainDetails.TransactionNodes,
                        FromDateToDate = $"{window.TestPeriod.Start} - {window.TestPeriod.End}",
                        Strategy = strategy,
                        TransTime = trans.TransTime,
                        TransTimeString = "",
                        TransPrice = trans.TransPrice,
                        TransType = trans.TransType,
                        TransVolume = trans.TransVolume,
                        Balance = trans.Balance,
                        Mode = ResultTypeEnum.Test
                    };

                    return result;
                });
                stockTransactionResultList.AddRange(results);

                var currentStock = stockListDto.Last().Price ?? 0;
                var periodEnd = stockListDto.Last().Date;
                _researchOperationService.ProfitSettlement(currentStock, stockListDto, testCase, transactions, periodEnd);
                var earns = _researchOperationService.GetEarningsResults(transactions);
                var result = Math.Round(earns, 10);
                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window,
                    Result = result,
                    TrainDetails = trainDetails,
                    Strategy = strategy
                };

                eachWindowResultParameterList.Add(eachWindowResultParameter);
            });

            _outputResultService.UpdateGNQTSTestResultsInDb(FUNDS, eachWindowResultParameterList);
            _outputResultService.UpdateStockTransactionResult(stockTransactionResultList);
        }

        public void TrainGNQTSWithRSI(SlidingWinPair SlidingWinPair, string symbol, Period period)
        {
            throw new NotImplementedException();
        }

        public void TrainGNQTSWithSMA(SlidingWinPair pair, string symbol, Period period, bool isCRandom)
        {
            var random = new Random(343);
            Queue<int> cRandom = new Queue<int>();
            if (isCRandom)
            {
                Console.WriteLine("Reading C Random.");
                cRandom = _fileHandler.Readcsv("Data/srand343");
            }

            var strategy = StrategyType.SMA;

            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodEnd = window.TrainPeriod.End;
                var copyCRandom = new Queue<int>(cRandom);
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                #region Train method

                // 用這邊在控制取fitness/transaction的日期區間
                // -7 是為了取得假日之前的前一日股票，後面再把period start丟進去確認起始時間正確
                // +1 是為了時差 取正確的最後一天
                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                var bestGbestList = new List<ITestCase>();
                var bestGbest = new SMAStatusValue();
                int gBestCount = 0;
                //var periodStart = Utils.UnixTimeStampToDateTime(periodStartTimeStamp);

                var randomSource = copyCRandom.Any() ? "CRandom" : "C#";
                var algorithmConst = _smaGnqtsAlgorithmService.GetConst();

                for (var e = 0; e < algorithmConst.EXPERIMENT_NUMBER; e++)
                {
                    SMAStatusValue gBest;
                    #region debug
                    //var path = Path.Combine(Environment.CurrentDirectory, $"Output/debug G best transaction exp: {e} - {randomSource}.csv");
                    //using (var writer = new StreamWriter(path))
                    //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, false))
                    //{
                    //    gBest = (SMAStatusValue)_smaGnqtsAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, strategy, csv);
                    //}
                    #endregion
                    gBest = (SMAStatusValue)_smaGnqtsAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, strategy, null);
                    CompareSMAGBestByBits(ref bestGbest, ref gBestCount, gBest, ref bestGbestList, symbol);
                }

                #endregion

                #region generate result

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window,
                    Strategy = strategy
                };

                var trainDetailsParameter = new TrainDetailsParameter
                {
                    RandomSource = randomSource,
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    Strategy = strategy
                };

                if (bestGbest.BuyMa1.Count > 0)
                {
                    eachWindowResultParameter.Result = bestGbest.Fitness;
                    trainDetailsParameter.BestTestCase =
                        new TestCaseSMA
                        {
                            Funds = FUNDS,
                            Symbol = symbol,
                            BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                            BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                            SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                            SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2)
                        };
                    trainDetailsParameter.ExperimentNumberOfBest = bestGbest.Experiment;
                    trainDetailsParameter.GenerationOfBest = bestGbest.Generation;
                    trainDetailsParameter.BestCount = gBestCount;
                    trainDetailsParameter.BestGbestList = bestGbestList;
                }

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);

                #endregion
            });

            _outputResultService.UpdateGNQTSTrainResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
        }

        public void TrainGNQTSWithTrailingStop(SlidingWinPair pair, string symbol, Period period, bool isCRandom)
        {
            var random = new Random(343);
            Queue<int> cRandom = new Queue<int>();
            if (isCRandom)
            {
                Console.WriteLine("Reading C Random.");
                cRandom = _fileHandler.Readcsv("Data/srand343");
            }

            var strategy = StrategyType.TrailingStop;
            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodEnd = window.TrainPeriod.End;
                var copyCRandom = new Queue<int>(cRandom);
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                #region Train method

                // 用這邊在控制取fitness/transaction的日期區間
                // -7 是為了取得假日之前的前一日股票，後面再把period start丟進去確認起始時間正確
                // +1 是為了時差 取正確的最後一天
                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                var bestGbestList = new List<ITestCase>();
                var bestGbest = new TrailingStopStatusValue();
                int gBestCount = 0;
                //var periodStart = Utils.UnixTimeStampToDateTime(periodStartTimeStamp);

                var randomSource = copyCRandom.Any() ? "CRandom" : "C#";
                var algorithmConst = _trailingStopGnqtsAlgorithmService.GetConst();

                for (var e = 0; e < algorithmConst.EXPERIMENT_NUMBER; e++)
                {
                    TrailingStopStatusValue gBest;
                    gBest = (TrailingStopStatusValue)_trailingStopGnqtsAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, strategy, null);
                    CompareTrailingStopGBestByBits(ref bestGbest, ref gBestCount, gBest, ref bestGbestList, symbol);
                }

                #endregion

                #region generate result

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window,
                    Strategy = strategy
                };

                var trainDetailsParameter = new TrainDetailsParameter
                {
                    RandomSource = randomSource,
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    Strategy = strategy
                };

                if (bestGbest.BuyMa1.Count > 0)
                {
                    eachWindowResultParameter.Result = bestGbest.Fitness;
                    trainDetailsParameter.BestTestCase =
                        new TestCaseTrailingStop
                        {
                            Funds = FUNDS,
                            Symbol = symbol,
                            BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                            BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                            SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                            SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2),
                            StopPercentage = Utils.GetMaNumber(bestGbest.StopPercentage),
                        };
                    trainDetailsParameter.ExperimentNumberOfBest = bestGbest.Experiment;
                    trainDetailsParameter.GenerationOfBest = bestGbest.Generation;
                    trainDetailsParameter.BestCount = gBestCount;
                    trainDetailsParameter.BestGbestList = bestGbestList;
                }

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);

                #endregion

            });
            _outputResultService.UpdateGNQTSTrainResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
        }

        public void TrainGNQTSWithBias(SlidingWinPair pair, string symbol, Period period, bool isCRandom)
        {
            var random = new Random(343);
            Queue<int> cRandom = new Queue<int>();
            if (isCRandom)
            {
                Console.WriteLine("Reading C Random.");
                cRandom = _fileHandler.Readcsv("Data/srand343");
            }
            var strategy = StrategyType.Bias;

            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodEnd = window.TrainPeriod.End;
                var copyCRandom = new Queue<int>(cRandom);
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                #region Train method

                // 用這邊在控制取fitness/transaction的日期區間
                // -7 是為了取得假日之前的前一日股票，後面再把period start丟進去確認起始時間正確
                // +1 是為了時差 取正確的最後一天
                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                var bestGbestList = new List<ITestCase>();
                var bestGbest = new BiasStatusValue();
                int gBestCount = 0;
                //var periodStart = Utils.UnixTimeStampToDateTime(periodStartTimeStamp);

                var randomSource = copyCRandom.Any() ? "CRandom" : "C#";
                var algorithmConst = _biasGNQTSAlgorithmService.GetConst();

                for (var e = 0; e < algorithmConst.EXPERIMENT_NUMBER; e++)
                {
                    BiasStatusValue gBest;
                    gBest = (BiasStatusValue)_biasGNQTSAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, strategy, null);
                    CompareBiasGBestByBits(ref bestGbest, ref gBestCount, gBest, ref bestGbestList, symbol);
                }

                #endregion

                #region generate result

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window,
                    Strategy = strategy
                };

                var trainDetailsParameter = new TrainDetailsParameter
                {
                    RandomSource = randomSource,
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    Strategy = strategy
                };

                if (bestGbest.BuyMa1.Count > 0)
                {
                    eachWindowResultParameter.Result = bestGbest.Fitness;
                    trainDetailsParameter.BestTestCase =
                        new TestCaseBias
                        {
                            Funds = FUNDS,
                            Symbol = symbol,
                            BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                            BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                            SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                            SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2),
                            StopPercentage = Utils.GetMaNumber(bestGbest.StopPercentage),
                            BuyBiasPercentage = Utils.GetMaNumber(bestGbest.BuyBiasPercentage),
                            SellBiasPercentage = Utils.GetMaNumber(bestGbest.SellBiasPercentage),
                        };
                    trainDetailsParameter.ExperimentNumberOfBest = bestGbest.Experiment;
                    trainDetailsParameter.GenerationOfBest = bestGbest.Generation;
                    trainDetailsParameter.BestCount = gBestCount;
                    trainDetailsParameter.BestGbestList = bestGbestList;
                }

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);

                #endregion

            });
            _outputResultService.UpdateGNQTSTrainResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
        }

        public void TrainTraditionalWithRSI(SlidingWinPair pair, string symbol, Period period)
        {
            List<int> range = new List<int> { 5, 6, 14 };
            var testCases = new List<ITestCase>();
            testCases.AddRange(range.Select((r) => {
                return new TestCaseRSI
                {
                    Funds = FUNDS,
                    Symbol = symbol,
                    MeasureRangeDay = r,
                    OverSold = 30,
                    OverBought = 70
                };
            }));
            testCases.AddRange(range.Select((r) => {
                return new TestCaseRSI
                {
                    Funds = FUNDS,
                    Symbol = symbol,
                    MeasureRangeDay = r,
                    OverSold = 20,
                    OverBought = 80
                };
            }));
            TraditionalTrain(pair, symbol, period, StrategyType.RSI,testCases);
        }

        public void TrainTraditionalWithSMA(SlidingWinPair pair, string symbol, Period period)
        {
            var testCases = new List<ITestCase>();
            List<int> shortMaList = new List<int> { 5, 10 };
            List<int> midMaList = new List<int> { 20, 60 };
            List<int> longMaList = new List<int> { 120, 240 };
            shortMaList.ForEach((ma1) =>
            {
                midMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseSMA
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                    });
                });
            });
            midMaList.ForEach((ma1) =>
            {
                longMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseSMA
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                    });
                });
            });
            TraditionalTrain(pair, symbol, period, StrategyType.SMA, testCases);
        }

        public void TrainTraditionalWithTrailingStop(SlidingWinPair pair, string symbol, Period period)
        {
            var testCases = new List<ITestCase>();
            List<int> shortMaList = new List<int> { 5, 10 };
            List<int> midMaList = new List<int> { 20, 60 };
            List<int> longMaList = new List<int> { 120, 240 };
            var trailingStop = 10;
            shortMaList.ForEach((ma1) =>
            {
                midMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseTrailingStop
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                        StopPercentage = trailingStop,
                    });
                });
            });
            midMaList.ForEach((ma1) =>
            {
                longMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseTrailingStop
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                        StopPercentage = trailingStop,
                    });
                });
            });

            TraditionalTrain(pair, symbol, period, StrategyType.TrailingStop, testCases);
        }

        public void TrainTraditionalWithBias(SlidingWinPair pair, string symbol, Period period)
        {
            var testCases = new List<ITestCase>();
            List<int> shortMaList = new List<int> { 5, 10 };
            List<int> midMaList = new List<int> { 20, 60 };
            List<int> longMaList = new List<int> { 120, 240 };
            var defaultValue = 10;
            shortMaList.ForEach((ma1) =>
            {
                midMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseBias
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                        StopPercentage = defaultValue,
                        BuyBiasPercentage = 15,
                        SellBiasPercentage = 15
                    });
                });
            });
            midMaList.ForEach((ma1) =>
            {
                longMaList.ForEach((ma2) =>
                {
                    testCases.Add(new TestCaseBias
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        BuyShortTermMa = ma1,
                        BuyLongTermMa = ma2,
                        SellShortTermMa = ma1,
                        SellLongTermMa = ma2,
                        StopPercentage = defaultValue,
                        BuyBiasPercentage = 15,
                        SellBiasPercentage = 15
                    });
                });
            });

            TraditionalTrain(pair, symbol, period, StrategyType.Bias, testCases);
        }

        public void GetStockTransaction(SlidingWinPair pair, string algorithmName, string symbol, Period period, StrategyType strategy)
        {
            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
            var stockTransactionResultList = new List<StockTransactionResult>();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var trainId = $"{algorithmName}_{strategy}_{slidingWinPairName}_{Utils.ConvertToUnixTimestamp(window.TrainPeriod.Start)}";
                var trainDetails = _trainDetailsDataProvider.FindLatest(trainId, symbol);
                if (trainDetails == null) throw new InvalidOperationException($"{trainId} is not found.");
                var transNodes = trainDetails.TransactionNodes.Split(",");

                ITestCase testCase;
                if (strategy == StrategyType.SMA)
                {
                    testCase = new TestCaseSMA
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        Type = ResultTypeEnum.Train,
                        BuyShortTermMa = int.Parse(transNodes[0]),
                        BuyLongTermMa = int.Parse(transNodes[1]),
                        SellShortTermMa = int.Parse(transNodes[2]),
                        SellLongTermMa = int.Parse(transNodes[3]),
                    };
                }
                else
                {
                    testCase = new TestCaseTrailingStop
                    {
                        Funds = FUNDS,
                        Symbol = symbol,
                        Type = ResultTypeEnum.Train,
                        BuyShortTermMa = int.Parse(transNodes[0]),
                        BuyLongTermMa = int.Parse(transNodes[1]),
                        SellShortTermMa = int.Parse(transNodes[2]),
                        SellLongTermMa = int.Parse(transNodes[3]),
                        StopPercentage = int.Parse(transNodes[4])
                    };
                }

                List<StockTransaction> transactions = new List<StockTransaction>();
                var periodStart = window.TrainPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);
                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp, strategy);

                var currentStock = stockListDto.Last().Price ?? 0;
                var periodEnd = stockListDto.Last().Date;
                //_researchOperationService.ProfitSettlement(currentStock, stockListDto, testCase, transactions, periodEnd);
                var earns = _researchOperationService.GetEarningsResults(transactions);
                var result = Math.Round(earns, 10);
                BuildStockTransactionResults(strategy, window, slidingWinPairName, stockTransactionResultList, trainId, trainDetails.TransactionNodes, transactions, ResultTypeEnum.Train);
            });

            _outputResultService.UpdateStockTransactionResult(stockTransactionResultList);
        }

        public List<StockTransaction> Temp(Period period)
        {
            var testCase = new TestCaseTrailingStop
            {
                Funds = FUNDS,
                Symbol = "AAPL",
                BuyShortTermMa = 60,
                BuyLongTermMa = 120,
                SellShortTermMa = 60,
                SellLongTermMa = 120,
                StopPercentage = 10,
            };

            var testCaseSMA = new TestCaseSMA
            {
                Funds = FUNDS,
                Symbol = "AAPL",
                BuyShortTermMa = 60,
                BuyLongTermMa = 120,
                SellShortTermMa = 60,
                SellLongTermMa = 120,
            };

            var strategy = StrategyType.SMA;
            SlidingWindow window = _slidingWindowService.GetSlidingWindows(period, PeriodEnum.H, PeriodEnum.M).First();
            var periodStart = window.TrainPeriod.Start;
            var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);
            var stockListDto = _dataService.GetStockDataFromDb("AAPL", window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
            var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCaseSMA, periodStartTimeStamp, strategy);
            var currentStock = stockListDto.Last().Price ?? 0;
            var periodEnd = stockListDto.Last().Date;
            _researchOperationService.ProfitSettlement(currentStock, stockListDto, testCaseSMA, transactions, periodEnd);
            var earns = _researchOperationService.GetEarningsResults(transactions);
            var result = Math.Round(earns, 10);
            var stockTransactionResultList = new List<StockTransactionResult>();
            var results = transactions.Select(trans =>
            {
                var result = new StockTransactionResult
                {
                    TrainId = "0111_test",
                    SlidingWinPairName = "",
                    TransactionNodes = $"{testCase.BuyShortTermMa}, {testCase.BuyLongTermMa}, {testCase.SellShortTermMa}, {testCase.SellLongTermMa}, {testCase.StopPercentage}",
                    FromDateToDate = $"{window.TrainPeriod.Start} - {window.TrainPeriod.End}",
                    Strategy = strategy,
                    TransTime = trans.TransTime,
                    TransTimeString = "",
                    TransPrice = trans.TransPrice,
                    TransType = trans.TransType,
                    TransVolume = trans.TransVolume,
                    Balance = trans.Balance,
                    Mode = ResultTypeEnum.Test
                };

                return result;
            });
            stockTransactionResultList.AddRange(results);
            _outputResultService.UpdateStockTransactionResult(stockTransactionResultList);
            return transactions;
        }
        #region Private method

        private void TraditionalTrain(SlidingWinPair pair, string symbol, Period period, StrategyType strategyType, List<ITestCase> testCases)
        {
            List<SlidingWindow> slidingWindows = pair.IsStar
                            ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                            : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
            var stockTransactionResultList = new List<StockTransactionResult>();

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();
            var algorithmConst = strategyType == StrategyType.SMA ? _smaGnqtsAlgorithmService.GetConst() : _trailingStopGnqtsAlgorithmService.GetConst();

            var allStock = _dataService.GetStockDataFromDb(symbol, period.Start.AddYears(-1).AddDays(-10), period.End.AddDays(10));

            slidingWindows.ForEach((window) =>
            {
                var trainId = $"Traditional_{strategyType}_{slidingWinPairName}_{Utils.ConvertToUnixTimestamp(window.TrainPeriod.Start)}";
                var periodStart = window.TrainPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                var stockListDto = _dataService.GetStockDataFromExistList(allStock, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    SlidingWindow = window,
                    Strategy = strategyType,
                    PeriodStartTimeStamp = periodStartTimeStamp
                };
                var trainDetailsParameter = new TrainDetailsParameter
                {
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp
                };

                testCases.ForEach(testCase =>
                {
                    (double result, var t) = GetResult(strategyType, testCase, periodStartTimeStamp, stockListDto);
                    if (result != 0 && result > eachWindowResultParameter.Result)
                    {
                        trainDetailsParameter.BestTestCase = testCase;
                        trainDetailsParameter.Strategy = strategyType;
                        eachWindowResultParameter.Result = result;
                    }
                });

                periodStartTimeStamp = Utils.ConvertToUnixTimestamp(window.TestPeriod.Start);
                var testStockListDto = _dataService.GetStockDataFromExistList(allStock, window.TestPeriod.Start.AddDays(-7), window.TestPeriod.End.AddDays(1));
                // get test result by best test case
                (double result, var transactions) = GetResult(strategyType, trainDetailsParameter.BestTestCase, periodStartTimeStamp, testStockListDto);
                eachWindowResultParameter.StockList = testStockListDto;
                eachWindowResultParameter.Result = result;

                BuildStockTransactionResults(strategyType, window, slidingWinPairName, stockTransactionResultList, trainId, JsonConvert.SerializeObject(trainDetailsParameter.BestTestCase), transactions, ResultTypeEnum.Traditional);

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);
            });

            _outputResultService.UpdateTraditionalResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
            _outputResultService.UpdateStockTransactionResult(stockTransactionResultList);
        }

        private (double, List<StockTransaction>) GetResult(StrategyType strategyType, ITestCase testCase, double periodStartTimeStamp, List<StockModelDTO> stockListDto)
        {
            var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp, strategyType);
            var currentStock = stockListDto.Last().Price ?? 0;
            var periodEnd = stockListDto.Last().Date;
            _researchOperationService.ProfitSettlement(currentStock, stockListDto, testCase, transactions, periodEnd);
            var earns = _researchOperationService.GetEarningsResults(transactions);
            var result = Math.Round(earns, 10);
            return (result, transactions);
        }

        private static void BuildStockTransactionResults(StrategyType strategy, SlidingWindow window, string slidingWinPairName, List<StockTransactionResult> stockTransactionResultList, string trainId, string transactionNodes, List<StockTransaction> transactions, ResultTypeEnum resultTypeEnum)
        {
            var results = transactions.Select(trans =>
            {
                var result = new StockTransactionResult
                {
                    TrainId = trainId,
                    SlidingWinPairName = slidingWinPairName,
                    TransactionNodes = transactionNodes,
                    FromDateToDate = $"{window.TrainPeriod.Start} - {window.TrainPeriod.End}",
                    Strategy = strategy,
                    TransTime = trans.TransTime,
                    TransTimeString = "",
                    TransPrice = trans.TransPrice,
                    TransType = trans.TransType,
                    TransVolume = trans.TransVolume,
                    Balance = trans.Balance,
                    Mode = resultTypeEnum
                };

                return result;
            });
            stockTransactionResultList.AddRange(results);
        }

        private static void CompareSMAGBestByBits(ref SMAStatusValue bestGbest, ref int gBestCount, SMAStatusValue gBest, ref List<ITestCase> bestGbestList, string symbol)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                bestGbest = (SMAStatusValue)gBest.DeepClone();
                gBestCount = 0;
            }

            if (
                Utils.GetMaNumber(bestGbest.BuyMa1) == Utils.GetMaNumber(gBest.BuyMa1) &&
                Utils.GetMaNumber(bestGbest.BuyMa2) == Utils.GetMaNumber(gBest.BuyMa2) &&
                Utils.GetMaNumber(bestGbest.SellMa1) == Utils.GetMaNumber(gBest.SellMa1) &&
                Utils.GetMaNumber(bestGbest.SellMa2) == Utils.GetMaNumber(gBest.SellMa2) &&
                bestGbest.Fitness == gBest.Fitness
                ) gBestCount++;

            if (bestGbest.Fitness == gBest.Fitness)
            {
                bestGbestList.Add(new TestCaseSMA
                {
                    Symbol = symbol,
                    BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                    BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                    SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                    SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2)
                });
            }
        }

        private static void CompareTrailingStopGBestByBits(ref TrailingStopStatusValue bestGbest, ref int gBestCount, TrailingStopStatusValue gBest, ref List<ITestCase> bestGbestList, string symbol)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                bestGbest = (TrailingStopStatusValue)gBest.DeepClone();
                gBestCount = 0;
            }

            if (
                Utils.GetMaNumber(bestGbest.BuyMa1) == Utils.GetMaNumber(gBest.BuyMa1) &&
                Utils.GetMaNumber(bestGbest.BuyMa2) == Utils.GetMaNumber(gBest.BuyMa2) &&
                Utils.GetMaNumber(bestGbest.SellMa1) == Utils.GetMaNumber(gBest.SellMa1) &&
                Utils.GetMaNumber(bestGbest.SellMa2) == Utils.GetMaNumber(gBest.SellMa2) &&
                Utils.GetMaNumber(bestGbest.StopPercentage) == Utils.GetMaNumber(gBest.StopPercentage) &&
                bestGbest.Fitness == gBest.Fitness
                ) gBestCount++;

            if (bestGbest.Fitness == gBest.Fitness)
            {
                bestGbestList.Add(new TestCaseTrailingStop
                {
                    Symbol = symbol,
                    BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                    BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                    SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                    SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2),
                    StopPercentage = Utils.GetMaNumber(bestGbest.StopPercentage),
                });
            }
        }

        private static void CompareBiasGBestByBits(ref BiasStatusValue bestGbest, ref int gBestCount, BiasStatusValue gBest, ref List<ITestCase> bestGbestList, string symbol)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                bestGbest = (BiasStatusValue)gBest.DeepClone();
                gBestCount = 0;
            }

            if (
                Utils.GetMaNumber(bestGbest.BuyMa1) == Utils.GetMaNumber(gBest.BuyMa1) &&
                Utils.GetMaNumber(bestGbest.BuyMa2) == Utils.GetMaNumber(gBest.BuyMa2) &&
                Utils.GetMaNumber(bestGbest.SellMa1) == Utils.GetMaNumber(gBest.SellMa1) &&
                Utils.GetMaNumber(bestGbest.SellMa2) == Utils.GetMaNumber(gBest.SellMa2) &&
                Utils.GetMaNumber(bestGbest.StopPercentage) == Utils.GetMaNumber(gBest.StopPercentage) &&
                Utils.GetMaNumber(bestGbest.BuyBiasPercentage) == Utils.GetMaNumber(gBest.BuyBiasPercentage) &&
                Utils.GetMaNumber(bestGbest.SellBiasPercentage) == Utils.GetMaNumber(gBest.SellBiasPercentage) &&
                bestGbest.Fitness == gBest.Fitness
                ) gBestCount++;

            if (bestGbest.Fitness == gBest.Fitness)
            {
                bestGbestList.Add(new TestCaseBias
                {
                    Symbol = symbol,
                    BuyShortTermMa = Utils.GetMaNumber(bestGbest.BuyMa1),
                    BuyLongTermMa = Utils.GetMaNumber(bestGbest.BuyMa2),
                    SellShortTermMa = Utils.GetMaNumber(bestGbest.SellMa1),
                    SellLongTermMa = Utils.GetMaNumber(bestGbest.SellMa2),
                    StopPercentage = Utils.GetMaNumber(bestGbest.StopPercentage),
                    BuyBiasPercentage = Utils.GetMaNumber(bestGbest.BuyBiasPercentage),
                    SellBiasPercentage = Utils.GetMaNumber(bestGbest.SellBiasPercentage),
                });
            }
        }

        private static void CompareSMAGBestByFitness(ref IStatusValue bestGbest, ref int gBestCount, IStatusValue gBest, ref List<IStatusValue> bestGbestList)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                var statusValue = (SMAStatusValue)gBest;
                bestGbest = statusValue.DeepClone();
                gBestCount = 0;
            }

            if (bestGbest.Fitness == gBest.Fitness)
            {
                gBestCount++;
                bestGbestList.Add(bestGbest);
            }
        }

        private static void CompareTrailingStopGBestByFitness(ref IStatusValue bestGbest, ref int gBestCount, IStatusValue gBest, ref List<IStatusValue> bestGbestList)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                var statusValue = (TrailingStopStatusValue)gBest;
                bestGbest = statusValue.DeepClone();
                gBestCount = 0;
            }

            if (bestGbest.Fitness == gBest.Fitness)
            {
                gBestCount++;
                bestGbestList.Add(bestGbest);
            }
        }

        #endregion
    }
}

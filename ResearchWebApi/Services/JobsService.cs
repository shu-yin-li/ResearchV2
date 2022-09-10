using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class JobsService: IJobsService
    {
        private readonly IResearchOperationService _researchOperationService;
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IOutputResultService _outputResultService;
        private readonly ISlidingWindowService _slidingWindowService;
        private readonly IGNQTSAlgorithmService _qtsAlgorithmService;
        private readonly ITrainDetailsDataProvider _trainDetailsDataProvider;
        private readonly IFileHandler _fileHandler;


        private const double FUNDS = 10000000;

        public JobsService(
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IMapper mapper,
            IOutputResultService outputResultService,
            ISlidingWindowService slidingWindowService,
            IGNQTSAlgorithmService qtsAlgorithmService,
            ITrainDetailsDataProvider trainDetailsDataProvider,
            IFileHandler fileHandler)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outputResultService = outputResultService ?? throw new ArgumentNullException(nameof(outputResultService));
            _slidingWindowService = slidingWindowService ?? throw new ArgumentNullException(nameof(slidingWindowService));
            _qtsAlgorithmService = qtsAlgorithmService ?? throw new ArgumentNullException(nameof(qtsAlgorithmService));
            _trainDetailsDataProvider = trainDetailsDataProvider ?? throw new ArgumentNullException(nameof(trainDetailsDataProvider));
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        public void BuyAndHold(string symbol, Period period)
        {
            var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(period.Start);
            var stockList = _dataService.GetStockDataFromDb(symbol, period.Start, period.Start.AddDays(7));
            var stockListEnd = _dataService.GetStockDataFromDb(symbol, period.End.AddDays(-7), period.End.AddDays(1));
            stockList.AddRange(stockListEnd);
            var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);
            var transactions = _researchOperationService.GetBuyAndHoldTransactions(stockListDto, FUNDS);
            var earns = _researchOperationService.GetEarningsResults(transactions);
            var result = Math.Round(earns, 10);

            _outputResultService.UpdateBuyAndHoldResultInDb(FUNDS, symbol, stockListDto, periodStartTimeStamp, result);
        }

        public void Test(SlidingWinPair pair, string algorithmName, string symbol, Period period)
        {
            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
            var eachWindowResultParameterList = new List<EachWindowResultParameter>();

            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TestPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);
                var stockList = _dataService.GetStockDataFromDb(symbol, window.TestPeriod.Start, window.TestPeriod.End.AddDays(1));
                var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);

                var trainId = $"{algorithmName}_{slidingWinPairName}_{Utils.ConvertToUnixTimestamp(window.TrainPeriod.Start)}";
                var trainDetails = _trainDetailsDataProvider.FindLatest(trainId);
                if(trainDetails ==  null) throw new InvalidOperationException($"{trainId} is not found.");
                var transNodes = trainDetails.TransactionNodes.Split(",");
                var testCase = new TestCase {
                    Funds = FUNDS,
                    Symbol = symbol,
                    BuyShortTermMa = int.Parse(transNodes[0]),
                    BuyLongTermMa = int.Parse(transNodes[1]),
                    SellShortTermMa = int.Parse(transNodes[2]),
                    SellLongTermMa = int.Parse(transNodes[3]),
                };

                var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp);
                var earns = _researchOperationService.GetEarningsResults(transactions);
                var result = Math.Round(earns, 10);
                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window,
                    Result = result,
                    TrainDetails = trainDetails
                };

                eachWindowResultParameterList.Add(eachWindowResultParameter);
            });

            _outputResultService.UpdateGNQTSTestResultsInDb(FUNDS, eachWindowResultParameterList);
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

            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();
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
                var stockList = _dataService.GetStockDataFromDb(symbol, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);
                var bestGbest = new StatusValue();
                int gBestCount = 0;
                //var periodStart = Utils.UnixTimeStampToDateTime(periodStartTimeStamp);

                var randomSource = copyCRandom.Any() ? "CRandom" : "C#";
                var algorithmConst = _qtsAlgorithmService.GetConst();

                for (var e = 0; e < algorithmConst.EXPERIMENT_NUMBER; e++)
                {
                    StatusValue gBest;
                    gBest = _qtsAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, null);
                    CompareGBestByBits(ref bestGbest, ref gBestCount, gBest);
                }

                #endregion

                #region generate result

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window
                };

                var trainDetailsParameter = new TrainDetailsParameter
                {
                    RandomSource = randomSource,
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp
                };

                if (bestGbest.BuyMa1.Count > 0)
                {
                    eachWindowResultParameter.Result = bestGbest.Fitness;
                    trainDetailsParameter.BestTestCase =
                        new TestCase
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
                }

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);

                #endregion

            });
            _outputResultService.UpdateGNQTSTrainResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
        }

        public void TrainTraditionalWithRSI(SlidingWinPair SlidingWinPair, string symbol, Period period)
        {
            throw new NotImplementedException();
        }

        public void TrainTraditionalWithSMA(SlidingWinPair pair, string symbol, Period period)
        {
            var testCases = new List<TestCase>();
            List<int> shortMaList = new List<int> { 5, 10 };
            List<int> midMaList = new List<int> { 20, 60 };
            List<int> longMaList = new List<int> { 120, 240 };
            shortMaList.ForEach((ma1) => {
                midMaList.ForEach((ma2) => {
                    shortMaList.ForEach((ma3) => {
                        midMaList.ForEach((ma4) => {
                            testCases.Add(new TestCase
                            {
                                Funds = FUNDS,
                                BuyShortTermMa = ma1,
                                BuyLongTermMa = ma2,
                                SellShortTermMa = ma3,
                                SellLongTermMa = ma4,
                            });
                        });
                    });
                });
            });
            midMaList.ForEach((ma1) => {
                longMaList.ForEach((ma2) => {
                    midMaList.ForEach((ma3) => {
                        longMaList.ForEach((ma4) => {
                            testCases.Add(new TestCase
                            {
                                Funds = FUNDS,
                                BuyShortTermMa = ma1,
                                BuyLongTermMa = ma2,
                                SellShortTermMa = ma3,
                                SellLongTermMa = ma4,
                            });
                        });
                    });
                });
            });

            List<SlidingWindow> slidingWindows = pair.IsStar
                ? _slidingWindowService.GetSlidingWindows(period, pair.Train)
                : _slidingWindowService.GetSlidingWindows(period, pair.Train, pair.Test);

            var eachWindowResultParameterList = new List<EachWindowResultParameter>();
            var trainDetailsParameterList = new List<TrainDetailsParameter>();
            var algorithmConst = _qtsAlgorithmService.GetConst();
            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                var stockList = _dataService.GetStockDataFromDb(symbol, window.TrainPeriod.Start.AddDays(-7), window.TrainPeriod.End.AddDays(1));
                var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);

                var eachWindowResultParameter = new EachWindowResultParameter
                {
                    StockList = stockListDto,
                    PeriodStartTimeStamp = periodStartTimeStamp,
                    SlidingWindow = window
                };

                var trainDetailsParameter = new TrainDetailsParameter
                {
                    Delta = algorithmConst.DELTA,
                    ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER,
                    Generations = algorithmConst.GENERATIONS,
                    SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER,
                    PeriodStartTimeStamp = periodStartTimeStamp
                };

                testCases.ForEach(testCase => {
                    var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp);
                    var earns = _researchOperationService.GetEarningsResults(transactions);
                    var result = Math.Round(earns, 10);
                    if (result != 0 && result > eachWindowResultParameter.Result) {
                        trainDetailsParameter.BestTestCase = testCase;
                        eachWindowResultParameter.Result = result;
                    }
                });

                eachWindowResultParameterList.Add(eachWindowResultParameter);
                trainDetailsParameterList.Add(trainDetailsParameter);
            });


            _outputResultService.UpdateTraditionalResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
        }

        #region Private method

        private static void CompareGBestByBits(ref StatusValue bestGbest, ref int gBestCount, StatusValue gBest)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                bestGbest = gBest.DeepClone();
                gBestCount = 0;
            }

            if (
                Utils.GetMaNumber(bestGbest.BuyMa1) == Utils.GetMaNumber(gBest.BuyMa1) &&
                Utils.GetMaNumber(bestGbest.BuyMa2) == Utils.GetMaNumber(gBest.BuyMa2) &&
                Utils.GetMaNumber(bestGbest.SellMa1) == Utils.GetMaNumber(gBest.SellMa1) &&
                Utils.GetMaNumber(bestGbest.SellMa2) == Utils.GetMaNumber(gBest.SellMa2) &&
                bestGbest.Fitness == gBest.Fitness
                ) gBestCount++;
        }

        private static void CompareGBestByFitness(ref StatusValue bestGbest, ref int gBestCount, StatusValue gBest)
        {
            if (bestGbest.Fitness < gBest.Fitness)
            {
                bestGbest = gBest.DeepClone();
                gBestCount = 0;
            }

            if (bestGbest.Fitness == gBest.Fitness) gBestCount++;
        }

        #endregion
    }
}

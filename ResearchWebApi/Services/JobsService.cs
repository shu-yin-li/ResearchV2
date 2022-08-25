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
        private IResearchOperationService _researchOperationService;
        private IDataService _dataService;
        private IMapper _mapper;
        private IOutputResultService _outputResultService;
        private ISlidingWindowService _slidingWindowService;
        private IGNQTSAlgorithmService _qtsAlgorithmService;


        private const double FUNDS = 10000000;
        const int EXPERIMENT_NUMBER = 50;

        public JobsService(
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IMapper mapper,
            IOutputResultService outputResultService,
            ISlidingWindowService slidingWindowService,
            IGNQTSAlgorithmService qtsAlgorithmService)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outputResultService = outputResultService ?? throw new ArgumentNullException(nameof(outputResultService));
            _slidingWindowService = slidingWindowService ?? throw new ArgumentNullException(nameof(slidingWindowService));
            _qtsAlgorithmService = qtsAlgorithmService ?? throw new ArgumentNullException(nameof(qtsAlgorithmService));
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

        public void Test(Guid trainResultId)
        {
            throw new NotImplementedException();
        }

        public void TrainGNQTSWithRSI(SlidingWinPair SlidingWinPair, string symbol, Period period)
        {
            throw new NotImplementedException();
        }

        public void TrainGNQTSWithSMA(SlidingWinPair pair, string symbol, Period period)
        {
            var random = new Random(343);
            Queue<int> cRandom = new Queue<int>();
            // TODO: Add for C Random
            //if (isCrandom)
            //{
            //    Console.WriteLine("Reading C Random.");
            //    cRandom = _fileHandler.Readcsv("Data/srand343");
            //}

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

                var randomSource = copyCRandom.Any() ? "C#" : "CRandom";

                for (var e = 0; e < EXPERIMENT_NUMBER; e++)
                {
                    StatusValue gBest;
                    gBest = _qtsAlgorithmService.Fit(copyCRandom, random, FUNDS, stockListDto, e, periodStartTimeStamp, null);
                    CompareGBestByBits(ref bestGbest, ref gBestCount, gBest);
                }

                var eachWindowResultParameter = new EachWindowResultParameter();
                var trainDetailsParameter = new TrainDetailsParameter();
                if (bestGbest.BuyMa1.Count > 0)
                {
                    eachWindowResultParameter.Result = bestGbest.Fitness;
                    eachWindowResultParameter.StockList = stockListDto;
                    eachWindowResultParameter.PeriodStartTimeStamp = periodStartTimeStamp;
                    eachWindowResultParameter.SlidingWindow = window;

                    var algorithmConst = _qtsAlgorithmService.GetConst();
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
                    trainDetailsParameter.RandomSource = randomSource;
                    trainDetailsParameter.Delta = algorithmConst.DELTA;
                    trainDetailsParameter.ExperimentNumber = algorithmConst.EXPERIMENT_NUMBER;
                    trainDetailsParameter.Generations = algorithmConst.GENERATIONS;
                    trainDetailsParameter.SearchNodeNumber = algorithmConst.SEARCH_NODE_NUMBER;
                    trainDetailsParameter.ExperimentNumberOfBest = bestGbest.Experiment;
                    trainDetailsParameter.GenerationOfBest = bestGbest.Generation;
                    trainDetailsParameter.BestCount = gBestCount;
                }

                #endregion

            });
            _outputResultService.UpdateGNQTSResultsInDb(FUNDS, symbol, pair, eachWindowResultParameterList, trainDetailsParameterList);
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
            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                var stockList = _dataService.GetStockDataFromDb(symbol, period.Start, period.End.AddDays(1));
                var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);

                var eachWindowResultParameter = new EachWindowResultParameter();
                var trainDetailsParameter = new TrainDetailsParameter();
                testCases.ForEach(testCase => {
                    var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp);
                    var earns = _researchOperationService.GetEarningsResults(transactions);
                    var result = Math.Round(earns, 10);
                    if (result != 0 && result > eachWindowResultParameter.Result) {
                        trainDetailsParameter.BestTestCase = testCase;
                        eachWindowResultParameter.Result = result;
                        eachWindowResultParameter.StockList = stockListDto;
                        eachWindowResultParameter.PeriodStartTimeStamp = periodStartTimeStamp;
                        eachWindowResultParameter.SlidingWindow = window;
                    }
                });

                if (trainDetailsParameter.BestTestCase == null) return;

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

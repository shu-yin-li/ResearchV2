using System;
using System.Collections.Generic;
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
        private static ISlidingWindowService _slidingWindowService;

        private const double FUNDS = 10000000;

        public JobsService(
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IMapper mapper,
            IOutputResultService outputResultService,
            ISlidingWindowService slidingWindowService)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outputResultService = outputResultService ?? throw new ArgumentNullException(nameof(outputResultService));
            _slidingWindowService = slidingWindowService ?? throw new ArgumentNullException(nameof(slidingWindowService));
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

        public void TrainGNQTSWithSMA(SlidingWinPair SlidingWinPair, string symbol, Period period)
        {
            throw new NotImplementedException();
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

            var eachWindowResultParameter = new EachWindowResultParameter();
            slidingWindows.ForEach((window) =>
            {
                var periodStart = window.TrainPeriod.Start;
                var periodStartTimeStamp = Utils.ConvertToUnixTimestamp(periodStart);

                var stockList = _dataService.GetStockDataFromDb(symbol, period.Start, period.End.AddDays(1));
                var stockListDto = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);
                testCases.ForEach(testCase => {
                    var transactions = _researchOperationService.GetMyTransactions(stockListDto, testCase, periodStartTimeStamp);
                    var earns = _researchOperationService.GetEarningsResults(transactions);
                    var result = Math.Round(earns, 10);
                    if (result != 0 && result > eachWindowResultParameter.Result) {
                        eachWindowResultParameter.BestTestCase = testCase;
                        eachWindowResultParameter.Result = result;
                        eachWindowResultParameter.StockList = stockListDto;
                        eachWindowResultParameter.PeriodStartTimeStamp = periodStartTimeStamp;
                    }
                });
            });

            if (eachWindowResultParameter.BestTestCase == null) return;

            _outputResultService.UpdateTraditionalResultsInDb(FUNDS, symbol, pair, eachWindowResultParameter);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;
using ResearchWebApi.Models.Results;

namespace ResearchWebApi.Services
{
    public class OutputResultService : IOutputResultService
    {
        private readonly IResearchOperationService _researchOperationService;
        private readonly IDataProvider<CommonResult> _commonResultDataProvider;
        private readonly IDataProvider<EarnResult> _earnResultDataProvider;
        private readonly IDataProvider<StockTransactionResult> _stockTransactionResultDataProvider;
        private readonly ITrainDetailsDataProvider _trainDetailsDataProvider;
        public OutputResultService(
            IResearchOperationService researchOperationService,
            IDataProvider<CommonResult> commonResultDataProvider,
            IDataProvider<EarnResult> earnResultDataProvider,
            IDataProvider<StockTransactionResult> stockTransactionResultDataProvider,
            ITrainDetailsDataProvider trainDetailsDataProvider
            )
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _commonResultDataProvider = commonResultDataProvider ?? throw new ArgumentNullException(nameof(commonResultDataProvider));
            _earnResultDataProvider = earnResultDataProvider ?? throw new ArgumentNullException(nameof(earnResultDataProvider));
            _stockTransactionResultDataProvider = stockTransactionResultDataProvider ?? throw new ArgumentNullException(nameof(stockTransactionResultDataProvider));
            _trainDetailsDataProvider = trainDetailsDataProvider ?? throw new ArgumentNullException(nameof(trainDetailsDataProvider));
        }

        public void UpdateBuyAndHoldResultInDb(double funds, string stockName, List<EachWindowResultParameter> eachWindowResultParameterList)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };
            var earnResultList = eachWindowResultParameterList.Select(eachWindowResultParameter => {
                var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
                var dayNumber = eachWindowResultParameter.DayNumber;
                var algorithmName = "BuyAndHold";
                var dayARR = Math.Round(CalculateARR(returnRate, (double)dayNumber) - 1, 10);

                return new EarnResult
                {
                    CommonResultId = commonResultId,
                    Mode = ResultTypeEnum.BuyAndHold,
                    TrainId = $"{algorithmName}_{nameof(ResultTypeEnum.BuyAndHold)}_{eachWindowResultParameter.SlidingWinPairName}",
                    FromDateToDate = $"{eachWindowResultParameter.Period.Start} - {eachWindowResultParameter.Period.End}",
                    FinalCapital = eachWindowResultParameter.Result,
                    FinalEarn = eachWindowResultParameter.Result - funds,
                    ReturnRates = returnRate,
                    DayNumber = dayNumber,
                    Strategy = eachWindowResultParameter.Strategy,
                    ExecuteDate = commonResult.ExecuteDate,
                    ARR = Math.Round(Math.Pow(dayARR + 1, 251.7) - 1, 10)
                };
            }).ToList();
            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.AddBatch(earnResultList);
        }

        public void UpdateTraditionalResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };

            var earnResultList = eachWindowResultParameterList.Select(eachWindowResultParameter => {
                var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
                var dayNumber = eachWindowResultParameter.StockList.FindAll(stock => stock.Date > eachWindowResultParameter.PeriodStartTimeStamp).Count;
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "Traditional";
                return
                    new EarnResult
                    {
                        CommonResultId = commonResultId,
                        Mode = ResultTypeEnum.Traditional,
                        TrainId = $"{algorithmName}_{eachWindowResultParameter.Strategy}_{slidingWinPairName}_{eachWindowResultParameter.PeriodStartTimeStamp}",
                        Strategy = eachWindowResultParameter.Strategy,
                        FromDateToDate = $"{eachWindowResultParameter.SlidingWindow.TestPeriod.Start} - {eachWindowResultParameter.SlidingWindow.TestPeriod.End}",
                        DayNumber = dayNumber,
                        FinalCapital = eachWindowResultParameter.Result,
                        FinalEarn = eachWindowResultParameter.Result - funds,
                        ReturnRates = returnRate,
                        ARR = Math.Round(CalculateARR(returnRate, dayNumber) - 1, 10),
                        ExecuteDate = commonResult.ExecuteDate
                    };
            }).ToList();
            
            var trainDetailsList = trainDetailsParameterList.Select(trainDetailsParameter => {
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "Traditional";
                var transactionNodes = string.Empty;
                if (trainDetailsParameter.Strategy == StrategyType.SMA)
                {
                    var testCaseSma = (TestCaseSMA)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseSma.BuyShortTermMa},{testCaseSma.BuyLongTermMa},{testCaseSma.SellShortTermMa},{testCaseSma.SellLongTermMa}";
                }
                else if(trainDetailsParameter.Strategy == StrategyType.RSI)
                {
                    var testCaseRsi = (TestCaseRSI)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseRsi.MeasureRangeDay},{testCaseRsi.OverSold},{testCaseRsi.OverBought}";
                }
                else if (trainDetailsParameter.Strategy == StrategyType.TrailingStop)
                {
                    var testCaseTrailingStop = (TestCaseTrailingStop)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseTrailingStop.BuyShortTermMa},{testCaseTrailingStop.BuyLongTermMa}," +
                                       $"{testCaseTrailingStop.SellShortTermMa},{testCaseTrailingStop.SellLongTermMa}," +
                                       $"{testCaseTrailingStop.StopPercentage}";
                }
                else if (trainDetailsParameter.Strategy == StrategyType.Bias)
                {
                    var testCaseBias= (TestCaseBias)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseBias.BuyShortTermMa},{testCaseBias.BuyLongTermMa}," +
                                       $"{testCaseBias.SellShortTermMa},{testCaseBias.SellLongTermMa}," +
                                       $"{testCaseBias.StopPercentage},{testCaseBias.BuyBiasPercentage}," +
                                       $"{testCaseBias.SellBiasPercentage}";
                }

                return new TrainDetails
                {
                    CommonResultId = commonResultId,
                    SlidingWinPairName = slidingWinPairName,
                    AlgorithmName = algorithmName,
                    TrainId = $"{algorithmName}_{trainDetailsParameter.Strategy}_{slidingWinPairName}_{trainDetailsParameter.PeriodStartTimeStamp}",
                    TransactionNodes = transactionNodes,
                };
            }).ToList();

            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.AddBatch(earnResultList);
            _trainDetailsDataProvider.AddBatch(trainDetailsList);
        }

        public void UpdateGNQTSTrainResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };

            var earnResultList = eachWindowResultParameterList.Select(eachWindowResultParameter => {
                var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
                var dayNumber = eachWindowResultParameter.StockList.FindAll(stock => stock.Date > eachWindowResultParameter.PeriodStartTimeStamp).Count;
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "GNQTS";
                return
                    new EarnResult
                    {
                        CommonResultId = commonResultId,
                        Mode = ResultTypeEnum.Train,
                        TrainId = $"{algorithmName}_{eachWindowResultParameter.Strategy}_{slidingWinPairName}_{eachWindowResultParameter.PeriodStartTimeStamp}",
                        FromDateToDate = $"{eachWindowResultParameter.SlidingWindow.TrainPeriod.Start} - {eachWindowResultParameter.SlidingWindow.TrainPeriod.End}",
                        DayNumber = dayNumber,
                        FinalCapital = eachWindowResultParameter.Result,
                        FinalEarn = eachWindowResultParameter.Result - funds,
                        ReturnRates = returnRate,
                        Strategy = eachWindowResultParameter.Strategy,
                        ExecuteDate = commonResult.ExecuteDate,
                        ARR = Math.Round(CalculateARR(returnRate, dayNumber) - 1, 10)
                    };
            }).ToList();

            commonResult.AvgARR = GetAvgARR(earnResultList);

            var trainDetailsList = trainDetailsParameterList.Select(trainDetailsParameter => {
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "GNQTS";
                var transactionNodes = string.Empty;
                if (trainDetailsParameter.Strategy == StrategyType.SMA)
                {
                    var testCaseSma = (TestCaseSMA)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseSma.BuyShortTermMa},{testCaseSma.BuyLongTermMa},{testCaseSma.SellShortTermMa},{testCaseSma.SellLongTermMa}";
                }
                else if (trainDetailsParameter.Strategy == StrategyType.RSI)
                {
                    var testCaseRsi = (TestCaseRSI)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseRsi.MeasureRangeDay},{testCaseRsi.OverSold},{testCaseRsi.OverBought}";
                }
                else if (trainDetailsParameter.Strategy == StrategyType.TrailingStop)
                {
                    var testCaseTrailingStop = (TestCaseTrailingStop)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseTrailingStop.BuyShortTermMa},{testCaseTrailingStop.BuyLongTermMa}," +
                                       $"{testCaseTrailingStop.SellShortTermMa},{testCaseTrailingStop.SellLongTermMa}," +
                                       $"{testCaseTrailingStop.StopPercentage}";
                }
                else if (trainDetailsParameter.Strategy == StrategyType.Bias)
                {
                    var testCaseBias = (TestCaseBias)trainDetailsParameter.BestTestCase;
                    transactionNodes = $"{testCaseBias.BuyShortTermMa},{testCaseBias.BuyLongTermMa}," +
                                       $"{testCaseBias.SellShortTermMa},{testCaseBias.SellLongTermMa}," +
                                       $"{testCaseBias.StopPercentage},{testCaseBias.BuyBiasPercentage}," +
                                       $"{testCaseBias.SellBiasPercentage}";
                }

                return new TrainDetails
                {
                    CommonResultId = commonResultId,
                    SlidingWinPairName = slidingWinPairName,
                    RandomSource = trainDetailsParameter.RandomSource,
                    AlgorithmName = algorithmName,
                    Delta = trainDetailsParameter.Delta,
                    ExperimentNumber = trainDetailsParameter.ExperimentNumber,
                    Generations = trainDetailsParameter.Generations,
                    SearchNodeNumber = trainDetailsParameter.SearchNodeNumber,
                    TrainId = $"{algorithmName}_{trainDetailsParameter.Strategy}_{slidingWinPairName}_{trainDetailsParameter.PeriodStartTimeStamp}",
                    TransactionNodes = transactionNodes,
                    ExperimentNumberOfBest = trainDetailsParameter.ExperimentNumberOfBest,
                    GenerationOfBest = trainDetailsParameter.GenerationOfBest,
                    BestCount = trainDetailsParameter.BestCount,
                    ExecuteDate = commonResult.ExecuteDate,
                    BestSmaList = JsonConvert.SerializeObject(trainDetailsParameter.BestGbestList)
                };
            }).ToList();

            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.AddBatch(earnResultList);
            _trainDetailsDataProvider.AddBatch(trainDetailsList);
        }

        public void UpdateGNQTSTestResultsInDb(double funds, List<EachWindowResultParameter> eachWindowResultParameterList)
        {
            var earnResultList = eachWindowResultParameterList.Select(eachWindowResultParameter => {
                var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
                var dayNumber = eachWindowResultParameter.StockList.FindAll(stock => stock.Date > eachWindowResultParameter.PeriodStartTimeStamp).Count;
                return
                    new EarnResult
                    {
                        CommonResultId = eachWindowResultParameter.TrainDetails.CommonResultId,
                        Mode = ResultTypeEnum.Test,
                        TrainId = eachWindowResultParameter.TrainDetails.TrainId,
                        FromDateToDate = $"{eachWindowResultParameter.SlidingWindow.TestPeriod.Start} - {eachWindowResultParameter.SlidingWindow.TestPeriod.End}",
                        DayNumber = dayNumber,
                        FinalCapital = eachWindowResultParameter.Result,
                        FinalEarn = eachWindowResultParameter.Result - funds,
                        ReturnRates = returnRate,
                        Strategy = eachWindowResultParameter.Strategy,
                        ARR = Math.Round(CalculateARR(returnRate, dayNumber) - 1, 10),
                    };
            }).ToList();

            _earnResultDataProvider.AddBatch(earnResultList);
        }

        public void UpdateStockTransactionResult(List<StockTransactionResult> stockTransactionResults)
        {
            _stockTransactionResultDataProvider.AddBatch(stockTransactionResults);
        }

        #region Private method

        private double GetAvgARR(List<EarnResult> earnResultList)
        {
            var irrList = earnResultList.Select(result => CalculateARR(result.ReturnRates, result.DayNumber));
            var avgIRR = irrList.Sum() / earnResultList.Count();
            var avgDay = Math.Round(earnResultList.Select(result => result.DayNumber).Sum() / 10m, 10);
            return Math.Pow(avgIRR, (double)avgDay) - 1;
        }

        private double CalculateARR(double returnRate, int dayNumber)
        {
            double exp = 1 / (double)dayNumber;
            return Math.Pow(returnRate * 0.01 + 1, exp);
        }

        private double CalculateARR(double returnRate, double dayNumber)
        {
            double exp = 1 / dayNumber;
            return Math.Pow(returnRate * 0.01 + 1, exp);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Enum;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class OutputResultService : IOutputResultService
    {
        private readonly IResearchOperationService _researchOperationService;
        private readonly IDataProvider<CommonResult> _commonResultDataProvider;
        private readonly IDataProvider<EarnResult> _earnResultDataProvider;
        private readonly ITrainDetailsDataProvider _trainDetailsDataProvider;
        public OutputResultService(
            IResearchOperationService researchOperationService,
            IDataProvider<CommonResult> commonResultDataProvider,
            IDataProvider<EarnResult> earnResultDataProvider,
            ITrainDetailsDataProvider trainDetailsDataProvider
            )
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _commonResultDataProvider = commonResultDataProvider ?? throw new ArgumentNullException(nameof(commonResultDataProvider));
            _earnResultDataProvider = earnResultDataProvider ?? throw new ArgumentNullException(nameof(earnResultDataProvider));
            _trainDetailsDataProvider = trainDetailsDataProvider ?? throw new ArgumentNullException(nameof(trainDetailsDataProvider));
        }

        public void UpdateBuyAndHoldResultInDb(double funds, string stockName, List<StockModelDTO> stockList, double periodStartTimeStamp, double fitness)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };
            var returnRate = (fitness - funds) / funds * 100;
            var earnResult =
                new EarnResult
                {
                    CommonResultId = commonResultId,
                    Mode = ResultTypeEnum.BuyAndHold,
                    FinalCapital = fitness,
                    FinalEarn = fitness - funds,
                    ReturnRates = returnRate,
                    ARR = Math.Round(CalculateARR(returnRate, 10) - 1, 10)
                };
            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.Add(earnResult);
        }

        public void UpdateTraditionalResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };

            var earnResultList = eachWindowResultParameterList.Select(eachWindowResultParameter => {
                var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
                var dayNumber = eachWindowResultParameter.StockList.FindAll(stock => stock.Date > eachWindowResultParameter.PeriodStartTimeStamp).Count;
                return
                    new EarnResult
                    {
                        CommonResultId = commonResultId,
                        Mode = ResultTypeEnum.Traditional,
                        FromDateToDate = $"{eachWindowResultParameter.SlidingWindow.TrainPeriod.Start} - {eachWindowResultParameter.SlidingWindow.TrainPeriod.End}",
                        DayNumber = dayNumber,
                        FinalCapital = eachWindowResultParameter.Result,
                        FinalEarn = eachWindowResultParameter.Result - funds,
                        ReturnRates = returnRate,
                        ARR = Math.Round(Math.Pow(CalculateARR(returnRate, dayNumber), 251.6) - 1, 10)
                    };
            }).ToList();
            
            var trainDetailsList = trainDetailsParameterList.Select(trainDetailsParameter => {
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "Traditional";
                return new TrainDetails
                {
                    CommonResultId = commonResultId,
                    SlidingWinPairName = slidingWinPairName,
                    AlgorithmName = algorithmName,
                    TrainId = $"{slidingWinPairName}_{algorithmName}",
                    TransactionNodes = $"({trainDetailsParameter.BestTestCase.BuyShortTermMa},{trainDetailsParameter.BestTestCase.BuyLongTermMa},{trainDetailsParameter.BestTestCase.SellShortTermMa},{trainDetailsParameter.BestTestCase.SellLongTermMa})",
                };
            }).ToList();

            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.AddBatch(earnResultList);
            _trainDetailsDataProvider.AddBatch(trainDetailsList);
        }

        public void UpdateGNQTSResultsInDb(double funds, string stockName, SlidingWinPair pair, List<EachWindowResultParameter> eachWindowResultParameterList, List<TrainDetailsParameter> trainDetailsParameterList)
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
                        TrainId = $"{algorithmName}_{slidingWinPairName}_{eachWindowResultParameter.PeriodStartTimeStamp}",
                        FromDateToDate = $"{eachWindowResultParameter.SlidingWindow.TrainPeriod.Start} - {eachWindowResultParameter.SlidingWindow.TrainPeriod.End}",
                        DayNumber = dayNumber,
                        FinalCapital = eachWindowResultParameter.Result,
                        FinalEarn = eachWindowResultParameter.Result - funds,
                        ReturnRates = returnRate,
                        ARR = Math.Round(CalculateARR(returnRate, dayNumber) - 1, 10)
                    };
            }).ToList();

            commonResult.AvgARR = GetAvgARR(earnResultList);

            var trainDetailsList = trainDetailsParameterList.Select(trainDetailsParameter => {
                var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
                var algorithmName = "GNQTS";
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
                    TrainId = $"{algorithmName}_{slidingWinPairName}_{trainDetailsParameter.PeriodStartTimeStamp}",
                    TransactionNodes = $"({trainDetailsParameter.BestTestCase.BuyShortTermMa},{trainDetailsParameter.BestTestCase.BuyLongTermMa},{trainDetailsParameter.BestTestCase.SellShortTermMa},{trainDetailsParameter.BestTestCase.SellLongTermMa})",
                    ExperimentNumberOfBest = trainDetailsParameter.ExperimentNumberOfBest,
                    GenerationOfBest = trainDetailsParameter.GenerationOfBest,
                    BestCount = trainDetailsParameter.BestCount,
                };
            }).ToList();

            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.AddBatch(earnResultList);
            _trainDetailsDataProvider.AddBatch(trainDetailsList);
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

        #endregion
    }
}

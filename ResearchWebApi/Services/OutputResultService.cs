using System;
using System.Collections.Generic;
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
        private readonly IDataProvider<TrainDetails> _trainDetailsDataProvider;
        public OutputResultService(
            IResearchOperationService researchOperationService,
            IDataProvider<CommonResult> commonResultDataProvider,
            IDataProvider<EarnResult> earnResultDataProvider,
            IDataProvider<TrainDetails> trainDetailsDataProvider
            )
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _commonResultDataProvider = commonResultDataProvider ?? throw new ArgumentNullException(nameof(commonResultDataProvider));
            _earnResultDataProvider = earnResultDataProvider ?? throw new ArgumentNullException(nameof(commonResultDataProvider));
            _trainDetailsDataProvider = trainDetailsDataProvider ?? throw new ArgumentNullException(nameof(commonResultDataProvider));
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
                    DayNumber = stockList.FindAll(stock => stock.Date > periodStartTimeStamp).Count,
                    FinalCapital = fitness,
                    FinalEarn = fitness - funds,
                    ReturnRates = returnRate,
                    ARR = Math.Round(CalculateARR(returnRate, 10) - 1, 10)
                };
            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.Add(earnResult);
        }

        public void UpdateTraditionalResultsInDb(double funds, string stockName, SlidingWinPair pair,EachWindowResultParameter eachWindowResultParameter)
        {
            var commonResultId = Guid.NewGuid();
            var commonResult = new CommonResult { Id = commonResultId, InitialCapital = funds, StockName = stockName };
            var returnRate = (eachWindowResultParameter.Result - funds) / funds * 100;
            var dayNumber = eachWindowResultParameter.StockList.FindAll(stock => stock.Date > eachWindowResultParameter.PeriodStartTimeStamp).Count;
            var earnResult =
                new EarnResult
                {
                    CommonResultId = commonResultId,
                    Mode = ResultTypeEnum.BuyAndHold,
                    DayNumber = dayNumber,
                    FinalCapital = eachWindowResultParameter.Result,
                    FinalEarn = eachWindowResultParameter.Result - funds,
                    ReturnRates = returnRate,
                    ARR = Math.Round(Math.Pow(CalculateARR(returnRate, dayNumber), 251.6) - 1, 10)
                };
            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.Add(earnResult);

            var slidingWinPairName = pair.IsStar ? $"{pair.Train}*" : $"{pair.Train}2{pair.Test}";
            var algorithmName = "Traditional";
            var trainDetails = new TrainDetails
            {
                CommonResultId = commonResultId,
                SlidingWinPairName = slidingWinPairName,
                AlgorithmName = algorithmName,
                TrainId = $"{slidingWinPairName}_{algorithmName}",
                TransactionNodes = $"({eachWindowResultParameter.BestTestCase.BuyShortTermMa},{eachWindowResultParameter.BestTestCase.BuyLongTermMa},{eachWindowResultParameter.BestTestCase.SellShortTermMa},{eachWindowResultParameter.BestTestCase.SellLongTermMa})",
            };
            _trainDetailsDataProvider.Add(trainDetails);
        }

        #region Private method

        //private double GetAvgARR(EarnResult earnResult)
        //{
        //    var irrList = irrSource.Select(result => CalculateIRR(result.ReturnRates, result.DayNumber));
        //    var avgIRR = irrList.Sum() / irrSource.Count();
        //    var avgDay = Math.Round(irrSource.Select(result => result.DayNumber).Sum() / 10m, 10);
        //    return Math.Pow(avgIRR, (double)avgDay) - 1;
        //}

        private double CalculateARR(double returnRate, int dayNumber)
        {
            double exp = 1 / (double)dayNumber;
            return Math.Pow(returnRate * 0.01 + 1, exp);
        }

        #endregion
    }
}

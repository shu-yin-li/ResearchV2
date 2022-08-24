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
                    ARR = Math.Round(CalculateIRR(returnRate, 10) - 1, 10)
                };
            _commonResultDataProvider.Add(commonResult);
            _earnResultDataProvider.Add(earnResult);
        }

        #region Private method

        private double CalculateIRR(double returnRate, int dayNumber)
        {
            double exp = 1 / (double)dayNumber;
            return Math.Pow(returnRate * 0.01 + 1, exp);
        }

        #endregion
    }
}

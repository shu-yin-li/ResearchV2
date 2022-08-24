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
        private const double FUNDS = 10000000;

        public JobsService(
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IMapper mapper,
            IOutputResultService outputResultService)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outputResultService = outputResultService ?? throw new ArgumentNullException(nameof(outputResultService));
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

        public void TrainTraditionalWithSMA(SlidingWinPair SlidingWinPair, string symbol, Period period)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Controllers
{
    [ApiController]
    [Route("request")]
    public class RequestController : Controller
    {

        private IResearchOperationService _researchOperationService;
        private IDataService _dataService;
        private IMapper _mapper;
        private IStockModelDataProvider _stockModelDataProvider;
        private IJobsService _jobsService;
        private Period _defaultPeriod = new Period {
            Start = new DateTime(2012, 1, 1, 0, 0, 0),
            End = new DateTime(2021, 12, 31, 0, 0, 0)
        };
        public RequestController(
            IJobsService jobsService,
            IResearchOperationService researchOperationService,
            IDataService dataService,
            IStockModelDataProvider stockModelDataProvider,
            IMapper mapper)
        {
            _jobsService = jobsService ?? throw new ArgumentNullException(nameof(jobsService));
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _stockModelDataProvider = stockModelDataProvider ?? throw new ArgumentNullException(nameof(stockModelDataProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost("Train")]
        public IActionResult SubmitTrainRequests([FromBody] TrainParameter trainParameter)
        {
            PrepareSource(trainParameter.Symbol);
            BackgroundJob.Enqueue(() => Console.WriteLine("Train"));
            return Ok();
        }

        [HttpPost("Test")]
        public IActionResult SubmitTestRequests([FromBody] TrainParameter trainParameter)
        {
            PrepareSource(trainParameter.Symbol);
            BackgroundJob.Enqueue(() => Console.WriteLine("Test"));
            return Ok();
        }

        [HttpPost("BuyAndHold")]
        public IActionResult SubmitBuyAndHoldRequests(string symbol, [FromBody] Period period)
        {
            PrepareSource(symbol);
            BackgroundJob.Enqueue(() => _jobsService.BuyAndHold(symbol, period));
            return Ok();
        }

        private void PrepareSource(string symbol)
        {
            var stockList = _dataService.GetStockDataFromDb(symbol, new DateTime(2021, 12, 20, 0, 0, 0), new DateTime(2021, 12, 31, 0, 0, 0));
            if (stockList.Any()) return;

            var periodEnd = new DateTime(2022, 5, 31, 0, 0, 0);
            List<StockModel> maStockList;

            maStockList = _dataService.GetPeriodDataFromYahooApi(symbol, new DateTime(2010, 1, 1, 0, 0, 0), periodEnd);
            _researchOperationService.CalculateAllMa(ref maStockList);
            for (var year = 2010; year <= 2022; year++)
            {
                var end = new DateTime(year, 12, 31, 0, 0, 0);
                if (year == 2022) end = periodEnd.AddDays(1);
                maStockList.FindAll(s => s.Date > Utils.ConvertToUnixTimestamp(new DateTime(year, 1, 1, 0, 0, 0)) && s.Date < Utils.ConvertToUnixTimestamp(end));
                _stockModelDataProvider.AddBatch(maStockList);
            }
        }
    }
}

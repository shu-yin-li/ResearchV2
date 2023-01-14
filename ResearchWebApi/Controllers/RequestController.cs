using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Controllers
{
    [ApiController]
    [Route("request")]
    public class RequestController : Controller
    {

        private IIndicatorCalulationService _indictorCalculationService;
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
            IIndicatorCalulationService movingAvarageService,
            IDataService dataService,
            IStockModelDataProvider stockModelDataProvider,
            IMapper mapper)
        {
            _jobsService = jobsService ?? throw new ArgumentNullException(nameof(jobsService));
            _indictorCalculationService = movingAvarageService ?? throw new ArgumentNullException(nameof(movingAvarageService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _stockModelDataProvider = stockModelDataProvider ?? throw new ArgumentNullException(nameof(stockModelDataProvider));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost("Train")]
        public IActionResult SubmitTrainRequests([FromBody] TrainParameter trainParameter, bool AllTrain)
        {
            PrepareSource(trainParameter.Symbol);
            var returnValue = true;
            if (AllTrain)
            {
                foreach(var pair in Utils.Get13TraditionalSlidingWindows())
                {
                    trainParameter.SlidingWinPair = pair;
                    var r = SubmitTrainJobs(trainParameter);
                    if (!r)
                    {
                        returnValue = r;
                        break;
                    }
                };
            }
            else
            {
                returnValue = SubmitTrainJobs(trainParameter);
            }
            return SubmitTrainJobs(trainParameter) ? Ok() : BadRequest();
        }

        [HttpPost("Test")]
        public IActionResult SubmitTestRequests([FromBody] TrainParameter trainParameter)
        {
            PrepareSource(trainParameter.Symbol);

            if (trainParameter.MaSelection == MaSelection.Traditional)
                return Ok("Calculated the best indicator sets when training in traditional mode. So no need to Test for Traditional mod");

            if (trainParameter.TransactionTiming.Buy == StrategyType.SMA
                && trainParameter.TransactionTiming.Sell == StrategyType.SMA)
            {
                var tempYear = trainParameter.Period.Start.Year;
                do
                {
                    var period = new Period
                    {
                        Start = new DateTime(tempYear, 1, 1),
                        End = new DateTime(tempYear, 12, 31),
                    };
                    BackgroundJob.Enqueue(()
                    => _jobsService.Test(trainParameter.SlidingWinPair, Enum.GetName(typeof(MaSelection), trainParameter.MaSelection), trainParameter.Symbol, period, trainParameter.TransactionTiming.Buy));
                    tempYear++;
                } while (tempYear <= trainParameter.Period.End.Year) ;
            return Ok();
            }

            if (trainParameter.TransactionTiming.Buy == StrategyType.RSI
                && trainParameter.TransactionTiming.Sell == StrategyType.RSI)
            {
                BackgroundJob.Enqueue(() => Console.WriteLine("Test RSI"));
                return Ok();
            }

            if (trainParameter.TransactionTiming.Buy == StrategyType.TrailingStop
                && trainParameter.TransactionTiming.Sell == StrategyType.TrailingStop)
            {
                var tempYear = trainParameter.Period.Start.Year;
                do
                {
                    var period = new Period
                    {
                        Start = new DateTime(tempYear, 1, 1),
                        End = new DateTime(tempYear, 12, 31),
                    };
                    BackgroundJob.Enqueue(()
                    => _jobsService.Test(trainParameter.SlidingWinPair, Enum.GetName(typeof(MaSelection), trainParameter.MaSelection), trainParameter.Symbol, period, trainParameter.TransactionTiming.Buy));
                    tempYear++;
                } while (tempYear <= trainParameter.Period.End.Year);
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost("GetStockTransaction")]
        public IActionResult GetStockTransaction([FromBody] TrainParameter trainParameter)
        {
            PrepareSource(trainParameter.Symbol);
            BackgroundJob.Enqueue(()
                    => _jobsService.GetStockTransaction(trainParameter.SlidingWinPair, Enum.GetName(typeof(MaSelection), trainParameter.MaSelection), trainParameter.Symbol, trainParameter.Period, trainParameter.TransactionTiming.Buy));
            return Ok();
        }

        [HttpPost("BuyAndHold")]
        public IActionResult SubmitBuyAndHoldRequests(string symbol, [FromBody] Period period)
        {
            PrepareSource(symbol);
            BackgroundJob.Enqueue(() => _jobsService.BuyAndHold(symbol, period, ResultTypeEnum.Train));
            return Ok();
        }

        [HttpPost("TestRSI")]
        public IActionResult TestRSI([FromBody] Period period)
        {
            var transaction = _jobsService.Temp(period);
            return Ok(transaction.ToString());
        }

        private void PrepareSource(string symbol)
        {
            var stockList = _dataService.GetStockDataFromDb(symbol, new DateTime(2021, 12, 20, 0, 0, 0), new DateTime(2021, 12, 31, 0, 0, 0));
            if (stockList.Any()) return;

            var periodEnd = new DateTime(2022, 5, 31, 0, 0, 0);
            List<StockModel> indicatorStockList = _dataService.GetPeriodDataFromYahooApi(symbol, new DateTime(1980, 12, 12, 0, 0, 0), periodEnd);
            _indictorCalculationService.CalculateMovingAvarage(ref indicatorStockList);
            _indictorCalculationService.CalculateRelativeStrengthIndex(ref indicatorStockList);

            _stockModelDataProvider.AddBatch(indicatorStockList);
        }

        private bool SubmitTrainJobs(TrainParameter trainParameter)
        {
            if (trainParameter.MaSelection == MaSelection.Traditional
                    && trainParameter.TransactionTiming.Buy == StrategyType.SMA
                    && trainParameter.TransactionTiming.Sell == StrategyType.SMA)
            {
                BackgroundJob.Enqueue(()
                    => _jobsService.TrainTraditionalWithSMA(trainParameter.SlidingWinPair, trainParameter.Symbol, trainParameter.Period));
                return true;
            }

            if (trainParameter.MaSelection == MaSelection.Traditional
                && trainParameter.TransactionTiming.Buy == StrategyType.TrailingStop
                && trainParameter.TransactionTiming.Sell == StrategyType.TrailingStop)
            {
                BackgroundJob.Enqueue(()
                    => _jobsService.TrainTraditionalWithTrailingStop(trainParameter.SlidingWinPair, trainParameter.Symbol, trainParameter.Period));
                return true;
            }

            if (trainParameter.MaSelection == MaSelection.Traditional
                && trainParameter.TransactionTiming.Buy == StrategyType.RSI
                && trainParameter.TransactionTiming.Sell == StrategyType.RSI)
            {
                BackgroundJob.Enqueue(() => _jobsService.TrainTraditionalWithRSI(trainParameter.SlidingWinPair, trainParameter.Symbol, trainParameter.Period));
                return true;
            }

            if (trainParameter.MaSelection == MaSelection.GNQTS
                && trainParameter.TransactionTiming.Buy == StrategyType.SMA
                && trainParameter.TransactionTiming.Sell == StrategyType.SMA)
            {
                var tempYear = trainParameter.Period.Start.Year;
                do
                {
                    var period = new Period
                    {
                        Start = new DateTime(tempYear, 1, 1),
                        End = new DateTime(tempYear, 12, 31),
                    };
                    BackgroundJob.Enqueue(() => _jobsService.TrainGNQTSWithSMA(trainParameter.SlidingWinPair, trainParameter.Symbol, period, trainParameter.IsCRandom));
                    tempYear++;
                } while (tempYear <= trainParameter.Period.End.Year);
                return true;
            }

            if (trainParameter.MaSelection == MaSelection.GNQTS
                && trainParameter.TransactionTiming.Buy == StrategyType.TrailingStop
                && trainParameter.TransactionTiming.Sell == StrategyType.TrailingStop)
            {
                var tempYear = trainParameter.Period.Start.Year;
                do
                {
                    var period = new Period
                    {
                        Start = new DateTime(tempYear, 1, 1),
                        End = new DateTime(tempYear, 12, 31),
                    };
                    BackgroundJob.Enqueue(() => _jobsService.TrainGNQTSWithTrailingStop(trainParameter.SlidingWinPair, trainParameter.Symbol, period, trainParameter.IsCRandom));
                    tempYear++;
                } while (tempYear <= trainParameter.Period.End.Year);
                return true;
            }

            if (trainParameter.MaSelection == MaSelection.GNQTS
                && trainParameter.TransactionTiming.Buy == StrategyType.RSI
                && trainParameter.TransactionTiming.Sell == StrategyType.RSI)
            {
                BackgroundJob.Enqueue(() => Console.WriteLine("GNQTS RSI"));
                return true;
            }
            return false;
        }
    }
}

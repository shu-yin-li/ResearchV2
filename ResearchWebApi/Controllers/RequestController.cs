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
        private IStockModelOldDataProvider _stockModelOldDataProvider;
        private IJobsService _jobsService;
        private Period _defaultPeriod = new Period {
            Start = new DateTime(2012, 1, 1, 0, 0, 0, System.DateTimeKind.Utc),
            End = new DateTime(2021, 12, 31, 0, 0, 0, System.DateTimeKind.Utc)
        };
        private List<string> _defaultSymbols = new List<string> { "^DJI", "^INX", "^IXIC", "^NYA", "AAPL", "AMGN", "AXP", "BA", "CAT", "CRM", "CSCO", "CVX",
                                                                    "DD", "DIS", "GS", "HD", "HON", "IBM", "INTC", "JNJ", "JPM", "KO", "MCD", "MMM", "MRK",
                                                                    "MSFT", "NKE", "PG", "TRV", "UNH", "V", "VZ","WBA", "WMT"};

        public RequestController(
            IJobsService jobsService,
            IIndicatorCalulationService movingAvarageService,
            IDataService dataService,
            IStockModelDataProvider stockModelDataProvider,
            IStockModelOldDataProvider stockModelOldDataProvider,
            IMapper mapper)
        {
            _jobsService = jobsService ?? throw new ArgumentNullException(nameof(jobsService));
            _indictorCalculationService = movingAvarageService ?? throw new ArgumentNullException(nameof(movingAvarageService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _stockModelDataProvider = stockModelDataProvider ?? throw new ArgumentNullException(nameof(stockModelDataProvider));
            _stockModelOldDataProvider = stockModelOldDataProvider ?? throw new ArgumentNullException(nameof(stockModelDataProvider));
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
            return returnValue ? (IActionResult)Ok() : (IActionResult)BadRequest();
        }

        [HttpPost("Test")]
        public IActionResult SubmitTestRequests([FromBody] TrainParameter trainParameter, bool AllTrain)
        {
            PrepareSource(trainParameter.Symbol);

            if (trainParameter.MaSelection == MaSelection.Traditional)
                return Ok("Calculated the best indicator sets when training in traditional mode. So no need to Test for Traditional mod");

            var returnValue = true;
            if (AllTrain)
            {
                foreach (var pair in Utils.Get13TraditionalSlidingWindows())
                {
                    trainParameter.SlidingWinPair = pair;
                    var r = SubmitTestJobs(trainParameter);
                    if (!r)
                    {
                        returnValue = r;
                        break;
                    }
                };
            }
            else
            {
                returnValue = SubmitTestJobs(trainParameter);
            }
            return returnValue ? (IActionResult)Ok() : (IActionResult)BadRequest();
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

        [HttpPost("compareMa")]
        public IActionResult TestRSI([FromBody] Period period)
        {
            PrepareSource("AAPL");

            var stockList = _stockModelDataProvider.Find("AAPL", period.Start, period.End).OrderBy(s => s.Date).ToList();
            var stockDtoList = _mapper.Map<List<StockModel>, List<StockModelDTO>>(stockList);
            var stockOldList = _stockModelOldDataProvider.Find("AAPL", period.Start, period.End).OrderBy(s => s.Date).ToList();

            if (stockOldList.First().Date != stockDtoList.First().Date) return BadRequest();

            stockOldList.ForEach(stockOld =>
            {
                var stockDto = stockDtoList.First();
                if (stockOld.Price != stockDto.Price) throw new InvalidOperationException(stockOld.Date.ToString());

                var properties = typeof(MaModel).GetProperties();
                foreach (var prop in properties)
                {
                    var stockOldMaValue = (double?)typeof(StockModelOld).GetProperty(prop.Name).GetValue(stockOld);
                    var key = int.Parse(prop.Name.Replace("Ma", ""));
                    if (stockOldMaValue != stockDto.MaList[key])
                    {
                        continue;
                    }
                }

                stockDtoList.RemoveAt(0);
            });


            return Ok();
        }

        private void PrepareSource(string symbol)
        {
            var stockList = _dataService.GetStockDataFromDb(symbol, new DateTime(2021, 12, 20, 0, 0, 0, System.DateTimeKind.Utc), new DateTime(2021, 12, 31, 0, 0, 0, System.DateTimeKind.Utc));
            if (stockList.Any()) return;

            var periodEnd = new DateTime(2022, 12, 31, 0, 0, 0, System.DateTimeKind.Utc);
            List<StockModel> indicatorStockList = _dataService.GetPeriodDataFromYahooApi(symbol, new DateTime(2008, 1, 1, 0, 0, 0, System.DateTimeKind.Utc), periodEnd);
            _indictorCalculationService.CalculateMovingAvarage(ref indicatorStockList);
            //_indictorCalculationService.CalculateRelativeStrengthIndex(ref indicatorStockList);

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
                        Start = new DateTime(tempYear, 1, 1, 0, 0, 0, System.DateTimeKind.Utc),
                        End = new DateTime(tempYear, 12, 31, 0, 0, 0, System.DateTimeKind.Utc),
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
                        Start = new DateTime(tempYear, 1, 1, 0, 0, 0, System.DateTimeKind.Utc),
                        End = new DateTime(tempYear, 12, 31, 0, 0, 0, System.DateTimeKind.Utc),
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

        private bool SubmitTestJobs(TrainParameter trainParameter)
        {
            if (trainParameter.TransactionTiming.Buy == StrategyType.SMA
                && trainParameter.TransactionTiming.Sell == StrategyType.SMA)
            {

                BackgroundJob.Enqueue(()
                => _jobsService.Test(trainParameter.SlidingWinPair, Enum.GetName(typeof(MaSelection), trainParameter.MaSelection), trainParameter.Symbol, trainParameter.Period, trainParameter.TransactionTiming.Buy));
                return true;
            }

            if (trainParameter.TransactionTiming.Buy == StrategyType.RSI
                && trainParameter.TransactionTiming.Sell == StrategyType.RSI)
            {
                BackgroundJob.Enqueue(() => Console.WriteLine("Test RSI"));
                return true;
            }

            if (trainParameter.TransactionTiming.Buy == StrategyType.TrailingStop
                && trainParameter.TransactionTiming.Sell == StrategyType.TrailingStop)
            {
                BackgroundJob.Enqueue(()
                => _jobsService.Test(trainParameter.SlidingWinPair, Enum.GetName(typeof(MaSelection), trainParameter.MaSelection), trainParameter.Symbol, trainParameter.Period, trainParameter.TransactionTiming.Buy));
                return true;
            }

            return false;
        }
    }
}

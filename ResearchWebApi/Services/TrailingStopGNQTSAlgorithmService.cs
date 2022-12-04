using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using ResearchWebApi.Enums;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class TrailingStopGNQTSAlgorithmService : ITrailingStopGNQTSAlgorithmService
    {
        private IResearchOperationService _researchOperationService;
        // GNQTS paremeters
        private double DELTA = 0.00016;
        const int GENERATIONS = 10000;
        const int SEARCH_NODE_NUMBER = 200;
        const int DIGIT_NUMBER = 8;
        const int DIGIT_NUMBER_2 = 6;
        const double RANDOM_MAX = 32767.0;
        const int EXPERIMENT_NUMBER = 50;

        public TrailingStopGNQTSAlgorithmService(IResearchOperationService researchOperationService)
        {
            _researchOperationService = researchOperationService ?? throw new ArgumentNullException(nameof(researchOperationService));
        }

        public AlgorithmConst GetConst()
        {
            return new AlgorithmConst
            {
                Name = "GNQTS",
                DELTA = DELTA,
                GENERATIONS = GENERATIONS,
                SEARCH_NODE_NUMBER = SEARCH_NODE_NUMBER,
                EXPERIMENT_NUMBER = EXPERIMENT_NUMBER
            };
        }

        public void SetDelta(double delda)
        {
            DELTA = delda;
        }

        public IStatusValue Fit(Queue<int> cRandom, Random random, double funds, List<StockModelDTO> stockList, int experiment, double periodStartTimeStamp, StrategyType strategyType, CsvWriter csv)
        {
            var iteration = 0;

            IStatusValue gBest = new TrailingStopStatusValue(funds);
            IStatusValue gWorst = new TrailingStopStatusValue(funds);
            IStatusValue localBest = new TrailingStopStatusValue(funds);
            IStatusValue localWorst = new TrailingStopStatusValue(funds);
            //#region debug

            //csv.WriteField($"exp:{experiment}");
            //csv.NextRecord();
            //csv.WriteField($"gen:{iteration}");
            //csv.NextRecord();

            //#endregion
            // initialize nodes
            List<IParticle> particles = new List<IParticle>();
            for (var i = 0; i < SEARCH_NODE_NUMBER; i++)
            {
                particles.Add(new TrailingStopParticle());
            }

            MetureX(cRandom, random, particles, funds);

            //var index = 0;
            particles.ForEach((p) =>
            {
                p.CurrentFitness.Fitness = GetFitness(p.TestCase, stockList, periodStartTimeStamp, strategyType);
                //#region debug

                //csv.WriteField($"P{index}");
                //DebugPrintParticle(csv, p.CurrentFitness);
                //csv.WriteField($"{p.CurrentFitness.Fitness / funds * 100}% ({p.CurrentFitness.Fitness}/{funds})");
                //csv.NextRecord();
                //index++;

                //#endregion

            });
            bool hasAnyTransaction = particles.FindAll(p => p.CurrentFitness.Fitness - funds != 0).Any();

            particles.ForEach((p) =>
            {
                if (hasAnyTransaction)
                {
                    UpdateGBestAndGWorst(p, ref gBest, ref gWorst, experiment, iteration);
                }
            });

            // update probability
            GetLocalBestAndWorst(particles, ref localBest, ref localWorst);
            //#region debug

            //csv.WriteField("global best");
            //DebugPrintParticle(csv, gBest);
            //csv.NextRecord();
            //csv.WriteField("local worst");
            //DebugPrintParticle(csv, localWorst);
            //csv.NextRecord();

            //#endregion
            particles.ForEach((p) =>
            {
                UpdateProByGN(p, gBest, localWorst);
            });
            particles.ForEach((p) =>
            {
                UpdateProbability(p, gBest, localWorst);
            });

            //#region debug

            //csv.WriteField("beta matrix");
            //DebugPrintBetaMatrix(csv, particles.FirstOrDefault());
            //csv.NextRecord();

            //#endregion
            while (iteration < GENERATIONS - 1)
            {
                iteration++;
                //#region debug

                //csv.WriteField($"gen:{iteration}");
                //csv.NextRecord();

                //#endregion
                MetureX(cRandom, random, particles, funds);
                //index = 0;

                particles.ForEach((p) =>
                {
                    p.CurrentFitness.Fitness = GetFitness(p.TestCase, stockList, periodStartTimeStamp, strategyType);
                    //#region debug

                    //csv.WriteField($"P{index}");
                    //DebugPrintParticle(csv, p.CurrentFitness);
                    //csv.WriteField($"{p.CurrentFitness.Fitness / funds * 100}% ({p.CurrentFitness.Fitness}/{funds})");
                    //csv.NextRecord();
                    //index++;

                    //#endregion
                });


                hasAnyTransaction = particles.FindAll(p => p.CurrentFitness.Fitness - funds != 0).Any();

                particles.ForEach((p) =>
                {
                    if (hasAnyTransaction)
                    {
                        UpdateGBestAndGWorst(p, ref gBest, ref gWorst, experiment, iteration);
                    }
                });

                GetLocalBestAndWorst(particles, ref localBest, ref localWorst);
                //#region debug

                //csv.WriteField("global best");
                //DebugPrintParticle(csv, gBest);
                //csv.NextRecord();
                //csv.WriteField("local worst");
                //DebugPrintParticle(csv, localWorst);
                //csv.NextRecord();

                //#endregion
                // update probability
                particles.ForEach((p) =>
                {
                    UpdateProByGN(p, gBest, localWorst);
                });
                particles.ForEach((p) =>
                {
                    UpdateProbability(p, gBest, localWorst);

                });

                //#region debug

                //csv.WriteField("beta matrix");
                //DebugPrintBetaMatrix(csv, particles.FirstOrDefault());
                //csv.NextRecord();

                //#endregion
            }

            return gBest;
        }

        private static void DebugPrintBetaMatrix(CsvWriter csv, IParticle particle)
        {
            var p = (TrailingStopParticle)particle;
            var str = string.Empty;
            p.BuyMa1Beta.ForEach(digit =>
            {
                str += $"{digit},";
            });
            csv.WriteField(str);
            csv.WriteField("");

            str = string.Empty;
            p.BuyMa2Beta.ForEach(digit =>
            {
                str += $"{digit},";
            });
            csv.WriteField(str);
            csv.WriteField("");

            str = string.Empty;
            p.StopPercentageBeta.ForEach(digit =>
            {
                str += $"{digit},";
            });
            csv.WriteField(str);
            csv.WriteField("");
        }

        private void DebugPrintParticle(CsvWriter csv, TrailingStopStatusValue current)
        {
            var str = string.Empty;
            current.BuyMa1.ForEach(digit =>
            {
                str += $"{digit}";
            });
            csv.WriteField($"{Utils.GetMaNumber(current.BuyMa1)} ({str})");
            csv.WriteField("");

            str = string.Empty;
            current.BuyMa2.ForEach(digit =>
            {
                str += $"{digit}";
            });
            csv.WriteField($"{Utils.GetMaNumber(current.BuyMa2)} ({str})");
            csv.WriteField("");

            str = string.Empty;
            current.StopPercentage.ForEach(digit =>
            {
                str += $"{digit}";
            });
            csv.WriteField($"{Utils.GetMaNumber(current.StopPercentage)} ({str})");
            csv.WriteField("");
        }

        public void UpdateGBestAndGWorst(IParticle p, ref IStatusValue gBest, ref IStatusValue gWorst, int experiment, int iteration)
        {
            TrailingStopStatusValue currentFitness = (TrailingStopStatusValue)p.CurrentFitness;
            if (gBest.Fitness < p.CurrentFitness.Fitness)
            {
                gBest = currentFitness.DeepClone();
                gBest.Experiment = experiment;
                gBest.Generation = iteration;
            }

            if (gWorst.Fitness > p.CurrentFitness.Fitness)
            {
                gWorst = currentFitness.DeepClone();
                gWorst.Experiment = experiment;
                gWorst.Generation = iteration;
            }
        }

        public void GetLocalBestAndWorst(List<IParticle> particles, ref IStatusValue localBest, ref IStatusValue localWorst)
        {
            IStatusValue max = particles.First().CurrentFitness;
            IStatusValue min = particles.First().CurrentFitness;

            particles.ForEach((p) =>
            {
                TrailingStopStatusValue currentFitness = (TrailingStopStatusValue)p.CurrentFitness;
                if (p.CurrentFitness.Fitness > max.Fitness)
                {
                    max = currentFitness.DeepClone();
                }
                if (p.CurrentFitness.Fitness < min.Fitness)
                {
                    min = currentFitness.DeepClone();
                }
            });
            localBest = max;
            localWorst = min;
        }

        public void UpdateProbability(IParticle particle, IStatusValue gbestStatusValue, IStatusValue localWorstStatusValue)
        {
            var p = (TrailingStopParticle)particle;
            var gBest = (TrailingStopStatusValue)gbestStatusValue;
            var localWorst = (TrailingStopStatusValue)localWorstStatusValue;
            if (!gBest.BuyMa1.Any()) return;
            var deltaDigitNum = DELTA.ToString().Split('.')[1].Count();
            for (var index = 0; index < DIGIT_NUMBER; index++)
            {
                // BuyMa1
                if (gBest.BuyMa1[index] > localWorst.BuyMa1[index])
                {
                    p.BuyMa1Beta[index] = Math.Round(p.BuyMa1Beta[index] + DELTA, deltaDigitNum);
                }
                else if (gBest.BuyMa1[index] < localWorst.BuyMa1[index])
                {
                    p.BuyMa1Beta[index] = Math.Round(p.BuyMa1Beta[index] - DELTA, deltaDigitNum);
                }
                // BuyMa2
                if (gBest.BuyMa2[index] > localWorst.BuyMa2[index])
                {
                    p.BuyMa2Beta[index] = Math.Round(p.BuyMa2Beta[index] + DELTA, deltaDigitNum);
                }
                else if (gBest.BuyMa2[index] < localWorst.BuyMa2[index])
                {
                    p.BuyMa2Beta[index] = Math.Round(p.BuyMa2Beta[index] - DELTA, deltaDigitNum);
                }
                // Stop Percentage
                if (index >= DIGIT_NUMBER_2) continue;
                if (gBest.StopPercentage[index] > localWorst.StopPercentage[index])
                {
                    p.StopPercentageBeta[index] = Math.Round(p.StopPercentageBeta[index] + DELTA, deltaDigitNum);
                }
                else if (gBest.StopPercentage[index] < localWorst.StopPercentage[index])
                {
                    p.StopPercentageBeta[index] = Math.Round(p.StopPercentageBeta[index] - DELTA, deltaDigitNum);
                }
            }
        }

        public void UpdateProByGN(IParticle particle, IStatusValue gbestStatusValue, IStatusValue localWorstStatusValue)
        {
            var p = (TrailingStopParticle)particle;
            var gBest = (TrailingStopStatusValue)gbestStatusValue;
            var localWorst = (TrailingStopStatusValue)localWorstStatusValue;
            if (!gBest.BuyMa1.Any()) return;
            var deltaDigitNum = DELTA.ToString().Split('.')[1].Count();
            for (var index = 0; index < DIGIT_NUMBER; index++)
            {
                // BuyMa1
                if ((gBest.BuyMa1[index] > localWorst.BuyMa1[index] && p.BuyMa1Beta[index] < 0.5)
                    || (gBest.BuyMa1[index] < localWorst.BuyMa1[index] && p.BuyMa1Beta[index] > 0.5))
                {
                    p.BuyMa1Beta[index] = Math.Round(1 - p.BuyMa1Beta[index], deltaDigitNum);
                }

                // BuyMa2
                if ((gBest.BuyMa2[index] > localWorst.BuyMa2[index] && p.BuyMa2Beta[index] < 0.5)
                    || (gBest.BuyMa2[index] < localWorst.BuyMa2[index] && p.BuyMa2Beta[index] > 0.5))
                {
                    p.BuyMa2Beta[index] = Math.Round(1 - p.BuyMa2Beta[index], deltaDigitNum);
                }
                if (index >= DIGIT_NUMBER_2) continue;
                // StopPercentage
                if ((gBest.StopPercentage[index] > localWorst.StopPercentage[index] && p.StopPercentageBeta[index] < 0.5)
                    || (gBest.StopPercentage[index] < localWorst.StopPercentage[index] && p.StopPercentageBeta[index] > 0.5))
                {
                    p.StopPercentageBeta[index] = Math.Round(1 - p.StopPercentageBeta[index], deltaDigitNum);
                }
            }

        }

        public void MetureX(Queue<int> cRandom, Random random, List<IParticle> particles, double funds)
        {
            var isCrandom = cRandom.Any();
            particles.ForEach((particle) =>
            {
                var p = (TrailingStopParticle)particle;
                var currentFitness = (TrailingStopStatusValue)p.CurrentFitness;
                currentFitness.BuyMa1 = new List<int>();
                p.BuyMa1Beta.ForEach((x) =>
                {
                    if (isCrandom)
                        currentFitness.BuyMa1.Add(x >= cRandom.Dequeue() / RANDOM_MAX ? 1 : 0);
                    else
                        currentFitness.BuyMa1.Add(x >= random.NextDouble() ? 1 : 0);
                });

                currentFitness.BuyMa2 = new List<int>();
                p.BuyMa2Beta.ForEach((x) =>
                {
                    if (isCrandom)
                        currentFitness.BuyMa2.Add(x >= cRandom.Dequeue() / RANDOM_MAX ? 1 : 0);
                    else
                        currentFitness.BuyMa2.Add(x >= random.NextDouble() ? 1 : 0);
                });

                currentFitness.StopPercentage = new List<int>();
                p.StopPercentageBeta.ForEach((x) =>
                {
                    if (isCrandom)
                        currentFitness.StopPercentage.Add(x >= cRandom.Dequeue() / RANDOM_MAX ? 1 : 0);
                    else
                        currentFitness.StopPercentage.Add(x >= random.NextDouble() ? 1 : 0);
                });
                p.CurrentFitness = currentFitness;

                var buyMa1 = Utils.GetMaNumber(currentFitness.BuyMa1);
                var buyMa2 = Utils.GetMaNumber(currentFitness.BuyMa2);
                var stopPct = Utils.GetMaNumber(currentFitness.StopPercentage);
                p.TestCase = new TestCaseTrailingStop
                {
                    Funds = funds,
                    BuyShortTermMa = buyMa1,
                    BuyLongTermMa = buyMa2,
                    StopPercentage = stopPct,
                };
            });

        }

        public double GetFitness(
            ITestCase currentTestCase,
            List<StockModelDTO> stockList,
            double periodStartTimeStamp,
            StrategyType strategyType)
        {

            var transactions = _researchOperationService.GetMyTransactions(stockList, currentTestCase, periodStartTimeStamp, strategyType);
            var currentStock = stockList.Last().Price ?? 0;
            var periodEnd = stockList.Last().Date;
            _researchOperationService.ProfitSettlement(currentStock, stockList, currentTestCase, transactions, periodEnd);
            var earns = _researchOperationService.GetEarningsResults(transactions);
            var result = Math.Round(earns, 10);
            return result;
        }
    }
}

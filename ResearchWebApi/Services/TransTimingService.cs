using System.Collections.Generic;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class TransTimingService : ITransTimingService
    {
        public TransTimingService()
        {
        }

        public bool TrueCheckGoldCross(bool check, double? shortMaVal, double? longMaVal)
        {
            if (!check && shortMaVal < longMaVal)
            {
                check = true;
            }
            else if (check && shortMaVal > longMaVal)
            {
                check = false;
            }
            return check;
        }

        // 黃金交叉
        public bool TimeToBuy(double? shortMaVal, double? longMaVal, double? prevShortMaVal, double? prevLongMaVal, bool hasQty)
        {
            var check = prevShortMaVal <= prevLongMaVal;
            return shortMaVal > longMaVal && hasQty == false && check;
        }

        // 黃金交叉且均線向上
        public bool TimeToBuy(int index, List<double?> shortMaValList, List<double?> longMaValList, bool hasQty, bool check)
        {
            var shortMaVal = shortMaValList[index];
            var longMaVal = longMaValList[index];
            var condition1 = shortMaVal > longMaVal && !hasQty && check;
            bool condition2;
            if (index == 0)
            {
                condition2 = true;
            }
            else
            {
                var shortMaGoUp = shortMaValList[index - 1] < shortMaVal;
                var LongMaGoUp = longMaValList[index - 1] < longMaVal;
                condition2 = shortMaGoUp && LongMaGoUp;
            }
            return condition1 && condition2;
        }

        // RSI
        public bool TimeToBuy(decimal rsi, decimal prevRsi, int overSell, bool hasQty)
        {
            var check = prevRsi >= overSell;
            return rsi < overSell && hasQty == false && check;
        }

        // 死亡交叉
        public bool TimeToSell(double? shortMaVal, double? longMaVal, double? prevShortMaVal, double? prevLongMaVal, bool hasQty)
        {
            var check = prevShortMaVal >= prevLongMaVal;
            return shortMaVal < longMaVal && hasQty == true && check;
        }

        // 一般停損
        public bool TimeToSell(double currentPrice, double buyPrice, double sellPct, bool hasQty)
        {
            if (!hasQty)
            {
                return false;
            }
            var lossPrice = buyPrice * (100 - sellPct) / 100;
            var earnPrice = buyPrice * (100 + sellPct) / 100;
            if (currentPrice <= lossPrice || currentPrice >= earnPrice)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // 移動停損
        public bool TimeToSell(StockTransaction lastTrans, ref double maxPrice, double currentPrice,
            double currentTime, double sellPct, bool hasQty)
        {
            if (!hasQty)
            {
                return false;
            }
            if (currentTime > lastTrans.TransTime)
            {
                if (currentPrice > maxPrice)
                {
                    maxPrice = currentPrice;
                }
                double stopPrice = calculateStopPrice(lastTrans.TransPrice, maxPrice, sellPct);
                if (currentPrice <= stopPrice)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // RSI
        public bool TimeToSell(decimal rsi, decimal prevRsi, int overBuy, bool hasQty)
        {
            var check = prevRsi <= overBuy;
            return rsi > overBuy && hasQty == false && check;
        }
        #region Private Method

        private static double calculateStopPrice(double lastTransPrice, double maxPrice, double sellPct)
        {
            var stopPrice = maxPrice * (100 - sellPct) / 100;
            return stopPrice;
        }

        #endregion

    }
}
﻿using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface ITransTimingService
    {
        bool TrueCheckGoldCross(bool check, double? shortMaVal, double? longMaVal);
        bool TimeToBuyCheckingByBias(double currentPrice, double? shortMaVal);
        bool TimeToBuy(double? shortMaVal, double? longMaVal, double? prevShortMaVal, double? prevLongMaVal, bool hasQty);
        bool TimeToBuy(int index, List<double?> shortMaVal, List<double?> longMaVal, bool hasQty, bool check);
        bool TimeToBuy(decimal rsi, decimal prevRsi, int overBuy, bool hasQty);
        bool TimeToSellCheckingByBias(double currentPrice, double? shortMaVal);
        bool TimeToSell(double? shortMaVal, double? longMaVal, double? prevShortMaVal, double? prevLongMaVal, bool hasQty);
        bool TimeToSell(double currentPrice, double buyPrice, double sellPct, bool hasQty);
        bool TimeToSell(double? shortMaVal, double? longMaVal, double? prevShortMaVal, double? prevLongMaVal, double currentPrice, double buyPrice, bool hasQty);
        bool TimeToSell(StockTransaction lastTrans, ref double maxPrice, double currentPrice, double currentTime, double sellPct, bool hasQty);
        bool TimeToSell(decimal rsi, decimal prevRsi, int overBuy, bool hasQty);
    }
}

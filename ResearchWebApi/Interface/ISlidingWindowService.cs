using System.Collections.Generic;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface ISlidingWindowService
    {
        List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum train, PeriodEnum test);
        List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum XStar);
    }
}
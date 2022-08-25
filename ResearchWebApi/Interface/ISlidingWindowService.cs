using System.Collections.Generic;
using ResearchWebApi.Enum;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface ISlidingWindowService
    {
        List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum train, PeriodEnum test);
        List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum XStar);
    }
}
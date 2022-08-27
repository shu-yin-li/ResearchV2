using ResearchWebApi.Enums;

namespace ResearchWebApi.Models
{
    public class SlidingWinPair
    {
        public bool IsStar { get; set; } = false;
        public PeriodEnum Train { get; set; }
        public PeriodEnum Test { get; set; }
    }
}

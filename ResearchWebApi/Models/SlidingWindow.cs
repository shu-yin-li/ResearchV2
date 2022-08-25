namespace ResearchWebApi.Models
{
    public class SlidingWindow
    {
        public Period TrainPeriod { get; set; } = new Period();
        public Period TestPeriod { get; set; } = new Period();
    }
}
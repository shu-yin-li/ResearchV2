using System.Collections.Generic;

namespace ResearchWebApi.Models
{
    public class YahooFinanceChartDataModel
    {
        public Chart chart { get; set; }
    }

    public class Chart
    {
        public List<Result> result { get; set; }
    }

    public class Result
    {
        public Meta meta { get; set; }
        public List<int> timestamp { get; set; }
        public Indicators indicators { get; set; }
    }

    public class Indicators
    {
        public List<Dictionary<string, List<double?>>> quote { get; set; }
    }

    public class Meta
    {
        public string symbol { get; set; }
        public string currency { get; set; }
    }
}

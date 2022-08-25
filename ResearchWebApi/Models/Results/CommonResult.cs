using System;
namespace ResearchWebApi.Models
{
    public class CommonResult
    {
        public Guid Id { get; set; }
        public string StockName { get; set; }
        public double InitialCapital { get; set; }
        public double AvgARR { get; set; }
        public long ExecuteDate { get; } = DateTimeOffset.Now.ToUnixTimeSeconds();

        public CommonResult()
        {
        }
    }
}

using System;
namespace ResearchWebApi.Interface
{
    public interface ITestCase
    {
        public string Symbol { get; set; }
        public double Funds { get; set; }
    }
}

using System;

namespace ResearchWebApi.Models
{
    public class StockModel
    {
        public Guid Id { get; }
        public string StockName { get; set; }
        public double Date { get; set; }
        public double? Price { get; set; }
        public string MaString { get; set; }
        public string RsiString { get; set; }

        public StockModel()
        {
            
        }
    }
}


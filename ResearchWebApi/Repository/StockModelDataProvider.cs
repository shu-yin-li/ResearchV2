using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class StockModelDataProvider : IStockModelDataProvider
    {
        private readonly StockModelDbContext _context;
        public StockModelDataProvider(StockModelDbContext context)
        {
            _context = context;
        }

        public void Add(StockModel stockModel)
        {
            _context.StockModel.Add(stockModel);
            _context.SaveChanges();
        }

        public void AddBatch(List<StockModel> entities)
        {
            _context.StockModel.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(StockModel stockModel)
        {
            var entity = _context.StockModel.Find(stockModel);
            _context.StockModel.RemoveRange(entity);
            _context.SaveChanges();
        }

        public List<StockModel> GetAll(string stockName)
        {
            return _context.StockModel.ToList().FindAll(t => t.StockName == stockName);
        }

        public void Update(StockModel stockModel)
        {
            _context.StockModel.Update(stockModel);
            _context.SaveChanges();
        }

        public IEnumerable<StockModel> Find(string stockSymbol, DateTime period1, DateTime period2)
        {
            return _context.StockModel
                .Where(e => e.StockName.Equals(stockSymbol)
                            && e.Date > period1.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds
                            && e.Date < period2.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }

    }
}

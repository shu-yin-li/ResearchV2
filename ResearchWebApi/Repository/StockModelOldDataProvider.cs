using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class StockModelOldDataProvider : IStockModelOldDataProvider
    {
        private readonly StockModelOldDbContext _context;
        public StockModelOldDataProvider(StockModelOldDbContext context)
        {
            _context = context;
        }

        public void Add(StockModelOld StockModelOld)
        {
            _context.StockModelOld.Add(StockModelOld);
            _context.SaveChanges();
        }

        public void AddBatch(List<StockModelOld> entities)
        {
            _context.StockModelOld.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(StockModelOld StockModelOld)
        {
            var entity = _context.StockModelOld.Find(StockModelOld);
            _context.StockModelOld.RemoveRange(entity);
            _context.SaveChanges();
        }

        public List<StockModelOld> GetAll(string stockName)
        {
            return _context.StockModelOld.ToList().FindAll(t => t.StockName == stockName);
        }

        public void Update(StockModelOld StockModelOld)
        {
            _context.StockModelOld.Update(StockModelOld);
            _context.SaveChanges();
        }

        public IEnumerable<StockModelOld> Find(string stockSymbol, DateTime period1, DateTime period2)
        {
            return _context.StockModelOld
                .Where(e => e.StockName.Equals(stockSymbol)
                            && e.Date > period1.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds
                            && e.Date < period2.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds);
        }

    }
}

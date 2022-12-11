using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models.Results;

namespace ResearchWebApi.Repository
{
    public class StockTransactionResultDataProvider : IDataProvider<StockTransactionResult>
    {
        private readonly StockTransactionResultDbContext _context;
        public StockTransactionResultDataProvider(StockTransactionResultDbContext context)
        {
            _context = context;
        }

        public void Add(StockTransactionResult entity)
        {
            _context.StockTransactionResult.Add(entity);
            _context.SaveChanges();
        }

        public void AddBatch(List<StockTransactionResult> entities)
        {
            _context.StockTransactionResult.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(StockTransactionResult myEntity)
        {
            var entity = _context.StockTransactionResult.Find(myEntity);
            _context.StockTransactionResult.RemoveRange(entity);
            _context.SaveChanges();
        }

        public List<StockTransactionResult> GetAll(string stockName)
        {
            throw new NotImplementedException();
        }

        public void Update(StockTransactionResult entity)
        {
            _context.StockTransactionResult.Update(entity);
            _context.SaveChanges();
        }
    }
}

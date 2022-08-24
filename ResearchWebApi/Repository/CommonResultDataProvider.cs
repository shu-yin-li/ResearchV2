using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class CommonResultDataProvider : IDataProvider<CommonResult>
    {
        private readonly CommonResultDbContext _context;
        public CommonResultDataProvider(CommonResultDbContext context)
        {
            _context = context;
        }

        public void Add(CommonResult entity)
        {
            _context.CommonResult.Add(entity);
            _context.SaveChanges();
        }

        public void AddBatch(List<CommonResult> entities)
        {
            _context.CommonResult.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(CommonResult myEntity)
        {
            var entity = _context.CommonResult.Find(myEntity);
            _context.CommonResult.RemoveRange(entity);
            _context.SaveChanges();
        }

        public List<CommonResult> GetAll(string stockName)
        {
            return _context.CommonResult.ToList().FindAll(t => t.StockName == stockName);
        }

        public void Update(CommonResult entity)
        {
            _context.CommonResult.Update(entity);
            _context.SaveChanges();
        }
    }
}

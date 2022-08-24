using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class EarnResultDataProvider : IDataProvider<EarnResult>
    {
        private readonly EarnResultDbContext _context;
        public EarnResultDataProvider(EarnResultDbContext context)
        {
            _context = context;
        }

        public void Add(EarnResult entity)
        {
            _context.EarnResult.Add(entity);
            _context.SaveChanges();
        }

        public void AddBatch(List<EarnResult> entities)
        {
            _context.EarnResult.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(EarnResult myEntity)
        {
            var entity = _context.EarnResult.Find(myEntity);
            _context.EarnResult.RemoveRange(entity);
            _context.SaveChanges();
        }

        public List<EarnResult> GetAll(Guid commonResultId)
        {
            return _context.EarnResult.ToList().FindAll(t => t.CommonResultId == commonResultId);
        }

        public List<EarnResult> GetAll(string stockName)
        {
            throw new NotImplementedException();
        }

        public void Update(EarnResult entity)
        {
            _context.EarnResult.Update(entity);
            _context.SaveChanges();
        }
    }
}

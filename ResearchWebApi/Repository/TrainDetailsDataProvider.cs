using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Repository
{
    public class TrainDetailsDataProvider : ITrainDetailsDataProvider
    {
        private readonly TrainDetailsDbContext _context;
        public TrainDetailsDataProvider(TrainDetailsDbContext context)
        {
            _context = context;
        }

        public void Add(TrainDetails entity)
        {
            _context.TrainDetails.Add(entity);
            _context.SaveChanges();
        }

        public void AddBatch(List<TrainDetails> entities)
        {
            _context.TrainDetails.AddRange(entities);
            _context.SaveChanges();
        }

        public void Delete(TrainDetails myEntity)
        {
            var entity = _context.TrainDetails.Find(myEntity);
            _context.TrainDetails.RemoveRange(entity);
            _context.SaveChanges();
        }

        public IEnumerable<TrainDetails> Find(string trainId)
        {
            return _context.TrainDetails.ToList().FindAll(t => t.TrainId == trainId);
        }

        public TrainDetails FindLatest(string trainId)
        {
            var entities = _context.TrainDetails.ToList().FindAll(t => t.TrainId == trainId);
            return entities.OrderByDescending(t => t.ExecuteDate).FirstOrDefault();
        }

        public List<TrainDetails> GetAll(Guid commonResultId)
        {
            return _context.TrainDetails.ToList().FindAll(t => t.CommonResultId == commonResultId);
        }

        public List<TrainDetails> GetAll(string stockName)
        {
            throw new NotImplementedException();
        }

        public void Update(TrainDetails entity)
        {
            _context.TrainDetails.Update(entity);
            _context.SaveChanges();
        }
    }
}

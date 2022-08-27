using System.Collections.Generic;
using ResearchWebApi.Models;

namespace ResearchWebApi.Interface
{
    public interface ITrainDetailsDataProvider : IDataProvider<TrainDetails>
    {
        public IEnumerable<TrainDetails> Find(string trainId);
    }
}

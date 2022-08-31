using System.Collections.Generic;

namespace ResearchWebApi.Interface
{
    public interface IFileHandler
    {
        Queue<int> Readcsv(string fileName);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Services
{
    public class FileHandler: IFileHandler
    {
        public FileHandler()
        {
        }
        
        public Queue<int> Readcsv(string fileName)
        {
            var result = new Queue<int>();
            var path = Path.Combine(Environment.CurrentDirectory, $"{fileName}.csv");
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                result.Enqueue(1158);
                while (csv.Read())
                {
                    var random = csv.GetRecord<int>();
                    result.Enqueue(random);
                }
            }
            return result;
        }
    }
}

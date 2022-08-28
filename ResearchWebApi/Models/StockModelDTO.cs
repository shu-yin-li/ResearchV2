using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace ResearchWebApi.Models
{
    public class StockModelDTO : Profile
    {
        public string StockName { get; set; }
        public double Date { get; set; }
        public double? Price { get; set; }
        public Dictionary<int,double?> MaList { get; set; }


        public StockModelDTO()
        {
            CreateMap<StockModel, StockModelDTO>()
                .ForMember(d => d.StockName, opt => opt.MapFrom(s => s.StockName))
                .ForMember(d => d.Date, opt => opt.MapFrom(s => s.Date))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Price))
                .ForMember(x => x.MaList, opt => opt.MapFrom(new MaListResolver()));
        }
    }

    public class MaListResolver : IValueResolver<StockModel, StockModelDTO, Dictionary<int, double?>>
    {
        public Dictionary<int, double?> Resolve(StockModel source, StockModelDTO destination, Dictionary<int, double?> member, ResolutionContext context)
        {
            var maList = new Dictionary<int, double?>();
            var properties = typeof(StockModel).GetProperties().ToList().FindAll(prop => prop.Name.Contains("Ma"));

            foreach (var prop in properties) {
                var key = Convert.ToInt32(prop.Name.Replace("Ma", ""));
                var value = Convert.ToDouble(prop.GetValue(source));
                maList.Add(key, value == 0 ? (double?)null : (double?)value);
            }

            return maList;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;

namespace ResearchWebApi.Models
{
    public class StockModelDTO : Profile
    {
        public string StockName { get; set; }
        public double Date { get; set; }
        public double? Price { get; set; }
        public Dictionary<int,double?> MaList { get; set; }
        public Dictionary<int,double?> RsiList { get; set; }

        public StockModelDTO()
        {
            CreateMap<StockModel, StockModelDTO>()
                .ForMember(d => d.StockName, opt => opt.MapFrom(s => s.StockName))
                .ForMember(d => d.Date, opt => opt.MapFrom(s => s.Date))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Price))
                .ForMember(d => d.MaList, opt => opt.MapFrom(new MaListResolver()))
                .ForMember(d => d.RsiList, opt => opt.Ignore());
        }
    }

    public class MaListResolver : IValueResolver<StockModel, StockModelDTO, Dictionary<int, double?>>
    {
        public Dictionary<int, double?> Resolve(StockModel source, StockModelDTO destination, Dictionary<int, double?> member, ResolutionContext context)
        {
            var maList = new Dictionary<int, double?>();
            MaModel maObject = JsonConvert.DeserializeObject<MaModel>(source.MaString);

            var properties = typeof(MaModel).GetProperties();
            foreach (var prop in properties)
            {
                var key = int.Parse(prop.Name.Replace("Ma", ""));
                var value = (double?)prop.GetValue(maObject);
                maList.Add(key, value);
            }

            return maList;
        }
    }
}


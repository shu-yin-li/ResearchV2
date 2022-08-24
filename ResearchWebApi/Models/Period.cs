using System;

namespace ResearchWebApi.Models
{
    public class Period
    {
        public DateTime Start { get; set; } = DateTime.Today;
        public DateTime End { get; set; } = DateTime.Today;
    }
}
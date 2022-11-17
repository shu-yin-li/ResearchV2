using System;
using ResearchWebApi.Models;
using System.Collections.Generic;

namespace ResearchWebApi.Interface
{
    public interface IStatusValue
    {
        public double Fitness { get; set; }
        public int Experiment { get; set; }
        public int Generation { get; set; }
    }
}


using System;
using ResearchWebApi.Models;
using System.Collections.Generic;

namespace ResearchWebApi.Interface
{
    public interface IParticle
    {
        public IStatusValue CurrentFitness { get; set; }
        public ITestCase TestCase { get; set; }
    }
}


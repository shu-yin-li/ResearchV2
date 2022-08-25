﻿using System;
namespace ResearchWebApi.Models
{
    public class TrainDetailsParameter
    {
        public TestCase BestTestCase { get; set; }
        public string RandomSource { get; set; }
        public double Delta { get; set; }
        public int ExperimentNumber { get; set; }
        public int Generations { get; set; }
        public int SearchNodeNumber { get; set; }
        public double ExperimentNumberOfBest { get; set; }
        public double GenerationOfBest { get; set; }
        public double BestCount { get; set; }

        public TrainDetailsParameter()
        {
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using AutoMapper;
using Newtonsoft.Json;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Models
{
    public class StockModelTransInfo
    {
        public string StockName { get; set; }
        public double Date { get; set; }
        public double? Price { get; set; }
        public readonly ITestCase TestCase;
        public double? BuyShortTermMa { get; set; }
        public double? BuyLongTermMa { get; set; }
        public double? SellShortTermMa { get; set; }
        public double? SellLongTermMa { get; set; }
        public double? PrevBuyShortTermMa { get; set; }
        public double? PrevBuyLongTermMa { get; set; }
        public double? PrevSellShortTermMa { get; set; }
        public double? PrevSellLongTermMa { get; set; }

        public StockModelTransInfo() {
            
        }
        public StockModelTransInfo(ITestCase testCase)
        {
            TestCase = testCase;
        }
    }

    
}


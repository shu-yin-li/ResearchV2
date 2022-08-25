using System;
using System.Collections.Generic;
using ResearchWebApi.Enum;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;

namespace ResearchWebApi.Services
{
    public class SlidingWindowService: ISlidingWindowService
    {
        public SlidingWindowService()
        {
        }

        public List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum train, PeriodEnum test)
        {
            var slidingWindows = new List<SlidingWindow>();
            var periodMonthNumber = period.End.Month - period.Start.Month + 1;
            if (periodMonthNumber >= (int)test || period.End.Year - period.Start.Year > 0)
            {
                var startDate = period.Start;
                do
                {
                    var sw = new SlidingWindow();
                    var endMonth = monthConverter(startDate.Month + (int)test - 1);
                    var testEnd = new DateTime(startDate.Year, endMonth, DateTime.DaysInMonth(startDate.Year, endMonth), 0, 0, 0);
                    sw.TestPeriod.Start = startDate;
                    sw.TestPeriod.End = testEnd;
                    GenerateTrainPeriod(train, startDate, sw);
                    slidingWindows.Add(sw);
                    startDate = startDate.AddMonths((int)test);
                } while (startDate.AddMonths((int)test - 1) <= period.End);
            }

            return slidingWindows;
        }

        private void GenerateTrainPeriod(PeriodEnum train, DateTime startDate, SlidingWindow sw)
        {
                var startMonth = startDate.Month - (int)train;
                var endMonth = startDate.Month - 1;
                sw.TrainPeriod.Start = new DateTime(convertYear(startDate.Year, startMonth), monthConverter(startMonth), 1, 0, 0, 0);
                sw.TrainPeriod.End = new DateTime(
                    convertYear(startDate.Year, endMonth),
                    monthConverter(endMonth),
                    DateTime.DaysInMonth(startDate.Year, monthConverter(endMonth)),
                    0, 0, 0);
        }

        private int monthConverter(int month)
        {
            if (month == 0 || month % 12 == 0) return 12;

            if (month < 0) return (month + 12) % 12;

            return month % 12;
        }

        private int convertYear(int y, int m)
        {
            if (m <= 0) return y - 1;

            if (m > 12)
                return y + 1;

            return y;
        }

        public List<SlidingWindow> GetSlidingWindows(Period period, PeriodEnum XStar)
        {
            var slidingWindows = new List<SlidingWindow>();
            var periodMonthNumber = period.End.Month - period.Start.Month + 1;
            if (periodMonthNumber >= (int)XStar || period.End.Year - period.Start.Year > 0)
            {
                var startDate = period.Start;
                do
                {
                    var sw = new SlidingWindow();
                    var testEndMonth = monthConverter(startDate.Month + (int)XStar - 1);
                    var testEnd = new DateTime(startDate.Year,testEndMonth, DateTime.DaysInMonth(startDate.Year, testEndMonth), 0, 0, 0);
                    sw.TestPeriod.Start = startDate;
                    sw.TestPeriod.End = testEnd;
                    sw.TrainPeriod.Start = startDate.AddYears(-1);
                    sw.TrainPeriod.End = testEnd.AddYears(-1);
                    slidingWindows.Add(sw);
                    startDate = startDate.AddMonths((int)XStar);
                } while (startDate.AddMonths((int)XStar - 1) <= period.End);
            }

            return slidingWindows;
        }
    }
}

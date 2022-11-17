using System;
using System.Collections.Generic;
using System.Linq;
using ResearchWebApi.Enums;
using ResearchWebApi.Models;

namespace ResearchWebApi
{
    public static class Utils
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static int GetMaNumber(List<int> metrix)
        {
            if (metrix.Any() && metrix.Count == 8)
                return 1 + metrix[7] * 1 + metrix[6] * 2 + metrix[5] * 4 + metrix[4] * 8 + metrix[3] * 16 + metrix[2] * 32 + metrix[1] * 64 + metrix[0] * 128;
            else if (metrix.Any() && metrix.Count == 6)
                return 1 + metrix[5] * 1 + metrix[4] * 2 + metrix[3] * 4 + metrix[2] * 8 + metrix[1] * 16 + metrix[0] * 32;
            else
                return 0;
        }

        public static List<SlidingWinPair> Get13TraditionalSlidingWindows()
        {
            return new List<SlidingWinPair> {
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.M, Test = PeriodEnum.M},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Q, Test = PeriodEnum.M},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.H, Test = PeriodEnum.M},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Y, Test = PeriodEnum.M},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Q, Test = PeriodEnum.Q},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.H, Test = PeriodEnum.Q},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Y, Test = PeriodEnum.Q},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.H, Test = PeriodEnum.H},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Y, Test = PeriodEnum.H},
                new SlidingWinPair { IsStar = false, Train = PeriodEnum.Y, Test = PeriodEnum.Y},
                new SlidingWinPair { IsStar = true, Train = PeriodEnum.M, Test = PeriodEnum.M},
                new SlidingWinPair { IsStar = true, Train = PeriodEnum.Q, Test = PeriodEnum.Q},
                new SlidingWinPair { IsStar = true, Train = PeriodEnum.H, Test = PeriodEnum.H},
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

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
            if (metrix.Any())
                return 1 + metrix[7] * 1 + metrix[6] * 2 + metrix[5] * 4 + metrix[4] * 8 + metrix[3] * 16 + metrix[2] * 32 + metrix[1] * 64 + metrix[0] * 128;
            else
                return 0;
        }
    }
}

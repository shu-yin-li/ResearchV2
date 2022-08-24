using System;
using ResearchWebApi.Interface;

namespace ResearchWebApi.Services
{
    public class CalculateVolumeService: ICalculateVolumeService
    {
        public CalculateVolumeService()
        {
        }

        public int CalculateBuyingVolume(double funds, double price)
        {
            if (price == 0)
            {
                return 0;
            }
            return (int)Math.Round(funds / (price * 1000), 0, MidpointRounding.ToNegativeInfinity) * 1000;
        }
        public int CalculateBuyingVolumeOddShares(double funds, double price)
        {
            if (price == 0)
            {
                return 0;
            }
            return (int)Math.Round(funds / price, 0, MidpointRounding.ToNegativeInfinity);
        }

        public int CalculateSellingVolume(decimal holdingVolumn)
        {
            return (int)holdingVolumn;
        }
    }
}

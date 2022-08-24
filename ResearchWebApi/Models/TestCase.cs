namespace ResearchWebApi.Models
{
    public class TestCase
    {
        public string Symbol { get; set; } = "AAPL";
        public double Funds { get; set; }
        public int BuyShortTermMa { get; set; }
        public int BuyLongTermMa { get; set; }
        public int SellShortTermMa { get; set; }
        public int SellLongTermMa { get; set; }

        public TestCase DeepClone()
        {
            return new TestCase {
                Symbol = Symbol,
                Funds = Funds,
                BuyShortTermMa = BuyShortTermMa,
                BuyLongTermMa = BuyLongTermMa,
                SellShortTermMa = SellShortTermMa,
                SellLongTermMa = SellLongTermMa };
        }
    }
}
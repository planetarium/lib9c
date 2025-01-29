namespace Lib9c.Tests.Fixtures.TableCSV.Swap
{
    public static class SwapRateSheetFixtures
    {
        public const string Default = @"currency_from,currency_to,rate
TEST_A;2;null;false;null,TEST_B;18;null;true;null,2/3
TEST_B;18;null;true;null,TEST_A;18;null;false;null,3/2
TEST_C;2;0000000000000000000000000000000000000000;false;null,TEST_D;18;0000000000000000000000000000000000000001:0000000000000000000000000000000000000002;true;null,3/2
TEST_E;2;null;true;100:99,TEST_F;18;null;true;1000:1000,1/1";

        public const string CappedLegacyFrom = @"currency_from,currency_to,rate
LEGACY_CAPPED;2;null;false;100:99,TEST_A;2;null;false;null,1/1";

        public const string CapExceedsDecimalFrom = @"currency_from,currency_to,rate
EXCEED_DECIMAL_CAP;2;null;true;100:100,TEST_A;2;null;false;null,1/1";
    }
}

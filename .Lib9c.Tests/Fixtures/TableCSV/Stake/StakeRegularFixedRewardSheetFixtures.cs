namespace Lib9c.Tests.Fixtures.TableCSV.Stake
{
    public static class StakeRegularFixedRewardSheetFixtures
    {
        public const string V1 =
            @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2";

        public const string V2 = @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2
6,5000000,500000,2
7,10000000,500000,2";

        public const string V3 = @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2
6,1000000,500000,2
7,5000000,500000,2
8,10000000,500000,2";

        /// <summary>
        /// Fixture with the optional <c>tradable</c> column present.
        /// Level 1 uses <c>tradable=true</c> (explicit) and level 2 uses <c>tradable=false</c>,
        /// so that both explicit values can be tested.
        /// </summary>
        public const string V1WithTradable = @"level,required_gold,item_id,count,tradable
1,50,500000,1,true
2,500,500000,2,false
3,5000,500000,2,true
4,50000,500000,2,true
5,500000,500000,2,true";
    }
}

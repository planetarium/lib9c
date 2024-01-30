namespace Lib9c.Tests.Fixtures.TableCSV.Stake
{
    public static class StakeRegularRewardSheetFixtures
    {
        public const string V1 =
            @"level,required_gold,item_id,rate,type,currency_ticker,currency_decimal_places,decimal_rate
1,50,400000,,Item,,,10
1,50,500000,,Item,,,800
1,50,20001,,Rune,,,6000
2,500,400000,,Item,,,8
2,500,500000,,Item,,,800
2,500,20001,,Rune,,,6000
3,5000,400000,,Item,,,5
3,5000,500000,,Item,,,800
3,5000,20001,,Rune,,,6000
4,50000,400000,,Item,,,5
4,50000,500000,,Item,,,800
4,50000,20001,,Rune,,,6000
5,500000,400000,,Item,,,5
5,500000,500000,,Item,,,800
5,500000,20001,,Rune,,,6000";

        public const string V2 =
            @"level,required_gold,item_id,rate,type,currency_ticker
1,50,400000,10,Item,
1,50,500000,800,Item,
1,50,20001,6000,Rune,
2,500,400000,4,Item,
2,500,500000,600,Item,
2,500,20001,6000,Rune,
3,5000,400000,2,Item,
3,5000,500000,400,Item,
3,5000,20001,6000,Rune,
4,50000,400000,2,Item,
4,50000,500000,400,Item,
4,50000,20001,6000,Rune,
5,500000,400000,2,Item,
5,500000,500000,400,Item,
5,500000,20001,6000,Rune,
6,5000000,400000,2,Item,
6,5000000,500000,400,Item,
6,5000000,20001,6000,Rune,
6,5000000,800201,50,Item,
7,10000000,400000,2,Item,
7,10000000,500000,400,Item,
7,10000000,20001,6000,Rune,
7,10000000,600201,50,Item,
7,10000000,800201,50,Item,
7,10000000,,100,Currency,GARAGE";

        // NOTE: belows are same.
        // since "claim_stake_reward8".
        // 7,10000000,20001,6000,Rune,
        // 7,10000000,,6000,Rune,
        // since "claim_stake_reward9".
        // 7,10000000,,6000,Currency,RUNE_GOLDENLEAF
        public const string V6 =
            @"level,required_gold,item_id,rate,type,currency_ticker,currency_decimal_places,decimal_rate,tradable
1,50,400000,,Item,,,10,true
1,50,500000,,Item,,,800,true
1,50,20001,,Rune,,,6000,true
2,500,400000,,Item,,,4,true
2,500,500000,,Item,,,600,true
2,500,20001,,Rune,,,6000,true
2,500,,,Currency,CRYSTAL,18,0.1,true
3,5000,400000,,Item,,,2,true
3,5000,500000,,Item,,,400,true
3,5000,20001,,Rune,,,5000,true
3,5000,,,Currency,CRYSTAL,18,0.02,true
3,5000,600201,,Item,,,500,false
3,5000,800201,,Item,,,500,false
4,50000,400000,,Item,,,2,true
4,50000,500000,,Item,,,400,true
4,50000,20001,,Rune,,,5000,true
4,50000,,,Currency,CRYSTAL,18,0.02,true
4,50000,600201,,Item,,,500,false
4,50000,800201,,Item,,,500,false
4,50000,800202,,Item,,,10000,false
5,500000,400000,,Item,,,1,true
5,500000,500000,,Item,,,200,true
5,500000,20001,,Rune,,,3000,true
5,500000,,,Currency,CRYSTAL,18,0.02,true
5,500000,600201,,Item,,,357,false
5,500000,800201,,Item,,,357,false
5,500000,800202,,Item,,,10000,false
6,1000000,400000,,Item,,,1,true
6,1000000,500000,,Item,,,200,true
6,1000000,20001,,Rune,,,3000,true
6,1000000,,,Currency,CRYSTAL,18,0.02,true
6,1000000,600201,,Item,,,200,false
6,1000000,800201,,Item,,,200,false
6,1000000,800202,,Item,,,10000,false
6,1000000,,,Currency,GARAGE,18,10000,true
7,5000000,400000,,Item,,,1,true
7,5000000,500000,,Item,,,200,true
7,5000000,20001,,Rune,,,3000,true
7,5000000,800201,,Item,,,100,false
7,5000000,,,Currency,CRYSTAL,18,0.02,true
7,5000000,600201,,Item,,,100,false
7,5000000,800202,,Item,,,100,false
7,5000000,,,Currency,GARAGE,18,500,true
8,10000000,400000,,Item,,,0.4,true
8,10000000,500000,,Item,,,80,true
8,10000000,20001,,Rune,,,1200,true
8,10000000,600201,,Item,,,50,false
8,10000000,800201,,Item,,,50,false
8,10000000,,,Currency,GARAGE,18,100,true
8,10000000,,,Currency,CRYSTAL,18,0.01,true
8,10000000,800202,,Item,,,50,false";
    }
}

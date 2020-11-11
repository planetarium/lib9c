using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class Addresses
    {
        public static readonly Address Shop              = new Address("0000000000000000000000000000000000000000");
        public static readonly Address Ranking           = new Address("0000000000000000000000000000000000000001");
        public static readonly Address WeeklyArena       = new Address("0000000000000000000000000000000000000002");
        public static readonly Address TableSheet        = new Address("0000000000000000000000000000000000000003");
        public static readonly Address GameConfig        = new Address("0000000000000000000000000000000000000004");
        public static readonly Address RedeemCode        = new Address("0000000000000000000000000000000000000005");
        public static readonly Address Admin             = new Address("0000000000000000000000000000000000000006");
        public static readonly Address PendingActivation = new Address("0000000000000000000000000000000000000007");
        public static readonly Address ActivatedAccount  = new Address("0000000000000000000000000000000000000008");
        public static readonly Address Blacksmith        = new Address("0000000000000000000000000000000000000009");
        public static readonly Address GoldCurrency      = new Address("000000000000000000000000000000000000000a");
        public static readonly Address GoldDistribution  = new Address("000000000000000000000000000000000000000b");
        public static readonly Address AuthorizedMiners  = new Address("000000000000000000000000000000000000000c");
        public static readonly Address Credits           = new Address("000000000000000000000000000000000000000d");

        public static List<Address> GetAll(bool ignoreDerive = false)
        {
            var addressType = typeof(Address);
            var addresses = typeof(Addresses).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fieldInfo => fieldInfo.FieldType == addressType)
                .Select(fieldInfo => fieldInfo.GetValue(null))
                .Where(value => value != null)
                .Select(value => (Address) value)
                .ToList();

            if (ignoreDerive)
            {
                return addresses;
            }
            
            // RankingState
            for (var i = 0; i < RankingState.RankingMapCapacity; i++)
            {
                addresses.Add(RankingState.Derive(i));
            }
            
            // WeeklyArenaState
            // The address of `WeeklyArenaState` to subscribe to is not specified because since the address of `WeeklyArenaState` is different according to `BlockIndex`

            // TableSheet
            var iSheetType = typeof(ISheet);
            addresses.AddRange(iSheetType.Assembly.GetTypes()
                .Where(type => !type.IsInterface && !type.IsAbstract && type.IsSubclassOf(iSheetType))
                .Select(type => TableSheet.Derive(type.Name)));
            
            return addresses;
        }
        
        public static Address GetSheetAddress<T>() where T : ISheet
        {
            return TableSheet.Derive(typeof(T).Name);
        }
    }
}

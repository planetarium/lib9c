using System;
using System.Collections.Generic;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.TableData;
using Libplanet.Action;

namespace Lib9c.Battle
{
    public class TestSimulator : Simulator
    {
        public override IEnumerable<ItemBase> Reward => new List<ItemBase>();

        public TestSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            SimulatorSheets simulatorSheets)
            : base(random, avatarState, foods, simulatorSheets)
        {
        }
    }
}

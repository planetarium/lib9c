using System;
using Lib9c.DevExtensions.Action.Stage;
using Lib9c.Tests;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Libplanet.Action.State;
using Nekoyume.Module;
using Xunit;

namespace Lib9c.DevExtensions.Tests.Action.Stage
{
    public class ClearStageTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IWorld _initialStateV2;

        public ClearStageTest()
        {
            (_tableSheets, _agentAddress, _avatarAddress, _initialStateV2) =
                InitializeUtil.InitializeStates(isDevEx: true);
        }

        [Fact]
        public void ClearStage()
        {
            var random = new Random();
            var targetStage = random.Next(1, 300 + 1);

            var action = new ClearStage
            {
                AvatarAddress = _avatarAddress,
                TargetStage = targetStage,
            };

            var state = action.Execute(new ActionContext
            {
                PreviousState = _initialStateV2,
                Signer = _agentAddress,
                BlockIndex = 0L,
            });

            var avatarState = state.GetAvatarState(_avatarAddress);
            for (var i = 1; i <= targetStage; i++)
            {
                Assert.True(avatarState.worldInformation.IsStageCleared(i));
            }
        }
    }
}

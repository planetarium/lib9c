namespace Lib9c.Tests.Module
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class LegacyModuleTest
    {
        [Fact]
        public void TryGetPatchedSheet_ReportsAnUnpatchedChain()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);

            Assert.False(world.TryGetPatchedSheet<RestrictionSheet>(out var sheet));
            Assert.Null(sheet);
        }

        [Fact]
        public void TryGetPatchedSheet_TreatsNullAsUnpatched()
        {
            IWorld world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GetSheetAddress<RestrictionSheet>(), Null.Value);

            Assert.False(world.TryGetPatchedSheet<RestrictionSheet>(out var sheet));
            Assert.Null(sheet);
        }

        [Fact]
        public void TryGetPatchedSheet_ReadsAPatchedSheet()
        {
            IWorld world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.GetSheetAddress<RestrictionSheet>(),
                    (Text)"key,market_registrable,synthesize_material\n1,false,\n");

            Assert.True(world.TryGetPatchedSheet<RestrictionSheet>(out var sheet));
            Assert.False(sheet.IsItemMarketRegistrable(1));
        }

        [Fact]
        public void TryGetPatchedSheet_ThrowsOnAMalformedSheet()
        {
            // A sheet that carries restrictions must not be able to switch itself off by
            // becoming unreadable, which is what TryGetSheet would do here.
            IWorld world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GetSheetAddress<RestrictionSheet>(), (Text)string.Empty);

            Assert.Throws<ArgumentNullException>(
                () => world.TryGetPatchedSheet<RestrictionSheet>(out _));

            // TryGetSheet reports the very same state as a plain miss, which would read as
            // "no restrictions" at every call site.
            Assert.False(world.TryGetSheet<RestrictionSheet>(out _));
        }
    }
}

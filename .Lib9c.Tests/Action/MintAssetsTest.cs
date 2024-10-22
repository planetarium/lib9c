namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Tx;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class MintAssetsTest
    {
        private readonly Address _adminAddress;
        private readonly ISet<Address> _minters;
        private readonly Currency _ncgCurrency;
        private readonly IWorld _prevState;

        private readonly TableSheets _tableSheets;

        public MintAssetsTest()
        {
            _adminAddress = new PrivateKey().Address;
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
            _minters = new HashSet<Address>
            {
                new PrivateKey().Address,
                new PrivateKey().Address,
                new PrivateKey().Address,
            };
            _prevState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, new AdminState(_adminAddress, 100).Serialize())
                .SetLegacyState(Addresses.AssetMinters, new List(_minters.Select(m => m.Serialize())));

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _prevState = _prevState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void PlainValue()
        {
            var r = new List<MintAssets.MintSpec>()
            {
                new (default, _ncgCurrency * 100, null),
                new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
            };
            var act = new MintAssets(r, null);
            var expected = Dictionary.Empty
                .Add("type_id", MintAssets.TypeIdentifier)
                .Add(
                    "values",
                    List.Empty
                        .Add(Null.Value)
                        .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                        .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            Assert.Equal(
                expected,
                act.PlainValue
            );

            var act2 = new MintAssets(r, "memo");
            var expected2 = Dictionary.Empty
                .Add("type_id", MintAssets.TypeIdentifier)
                .Add(
                    "values",
                    List.Empty
                        .Add((Text)"memo")
                        .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                        .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            Assert.Equal(
                expected2,
                act2.PlainValue
            );
        }

        [Fact]
        public void LoadPlainValue()
        {
            var pv = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add(
                    "values",
                    List.Empty
                        .Add(default(Null))
                        .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                        .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            var act = new MintAssets();
            act.LoadPlainValue(pv);

            var expected = new List<MintAssets.MintSpec>()
            {
                new (default, _ncgCurrency * 100, null),
                new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
            };
            Assert.Equal(expected, act.MintSpecs);
            Assert.Null(act.Memo);

            var pv2 = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add(
                    "values",
                    List.Empty
                        .Add((Text)"memo")
                        .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                        .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            var act2 = new MintAssets();
            act2.LoadPlainValue(pv2);
            Assert.Equal("memo", act2.Memo);
        }

        [Fact]
        public void Execute_With_FungibleAssetValue()
        {
            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (default, _ncgCurrency * 100, null),
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                },
                null
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _minters.First(),
                    BlockIndex = 1,
                }
            );

            Assert.Equal(
                _ncgCurrency * 100,
                nextState.GetBalance(default, _ncgCurrency)
            );
            Assert.Equal(
                _ncgCurrency * 1000,
                nextState.GetBalance(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency)
            );
        }

        [Fact]
        public void Execute_With_FungibleItemValue()
        {
            var prevState = GenerateAvatar(_prevState, out var avatarAddress);
            var fungibleId = HashDigest<SHA256>.FromString(
                "7f5d25371e58c0f3d5a33511450f73c2e0fa4fac32a92e1cbe64d3bf2fef6328"
            );

            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (
                        avatarAddress,
                        null,
                        new FungibleItemValue(fungibleId, 42)
                    ),
                },
                "Execute_With_FungibleItemValue"
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _minters.First(),
                    BlockIndex = 1,
                }
            );

            var inventory = nextState.GetInventoryV2(avatarAddress);
            Assert.Contains(inventory.Items, i => i.count == 42 && i.item is Material m && m.FungibleId.Equals(fungibleId));

            var avatarState = nextState.GetAvatarState(avatarAddress);
            Assert.Single(avatarState.mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(avatarState.mailBox.First());
            Assert.Equal(new[] { (fungibleId, 42), }, mail.FungibleIdAndCounts);
            Assert.Equal(action.Memo, mail.Memo);
        }

        [Fact]
        public void Execute_With_Mixed()
        {
            var prevState = GenerateAvatar(_prevState, out var avatarAddress);
            var fungibleId = HashDigest<SHA256>.FromString(
                "7f5d25371e58c0f3d5a33511450f73c2e0fa4fac32a92e1cbe64d3bf2fef6328"
            );

            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                    new (
                        avatarAddress,
                        Currencies.StakeRune * 123,
                        null
                    ),
                    new (
                        avatarAddress,
                        null,
                        new FungibleItemValue(fungibleId, 42)
                    ),
                },
                "Execute_With_FungibleItemValue"
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _minters.First(),
                    BlockIndex = 1,
                }
            );

            var inventory = nextState.GetInventoryV2(avatarAddress);
            Assert.Contains(inventory.Items, i => i.count == 42 && i.item is Material m && m.FungibleId.Equals(fungibleId));

            var avatarState = nextState.GetAvatarState(avatarAddress);
            Assert.Single(avatarState.mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(avatarState.mailBox.First());
            Assert.Equal(new[] { (fungibleId, 42), }, mail.FungibleIdAndCounts);
            Assert.Equal(new[] { (avatarAddress, Currencies.StakeRune * 123), }, mail.FungibleAssetValues);
            Assert.Equal(action.Memo, mail.Memo);
        }

        [Fact]
        public void Execute_Throws_InvalidMinterException()
        {
            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (default, _ncgCurrency * 100, null),
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                },
                null
            );

            // Allows admin
            _ = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            // Allows minters
            foreach (var m in _minters)
            {
                _ = action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _prevState,
                        Signer = m,
                        BlockIndex = 1,
                    }
                );
            }

            // Denies others
            Assert.Throws<InvalidMinterException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _prevState,
                        Signer = default,
                        BlockIndex = 1,
                    }
                ));
        }

        [Fact]
        public void Execute_Crystal()
        {
            var tx = Transaction.Deserialize(
                Convert.FromBase64String(
                    "ZDE6UzcxOjBFAiEAhzt5mDMzPwi6y+W+DJ53T4TKwt6YMaFTi38rKYqf7ZMCICV36ngA3Gi+rXkdG5hCUtlLXjAz8H2IKMNaCdCy/N90MTphbGR1Nzp0eXBlX2lkdTExOm1pbnRfYXNzZXRzdTY6dmFsdWVzbHU3Mzp7ImlhcCI6IHsiZ19za3UiOiAiZ19wa2dfYmxhY2tmcmlkYXkwMSIsICJhX3NrdSI6ICJhX3BrZ19ibGFja2ZyaWRheTAxIn19bDIwOgNS/yy36WH9ZHgDqZJiTdhQeCGFbGR1MTM6ZGVjaW1hbFBsYWNlczE6EnU3Om1pbnRlcnNudTY6dGlja2VydTc6Q1JZU1RBTGVpMjUwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMGVlbmVsMjA6H3yZ4KY1m33Wn0P0t+LAvTf5oidubDMyOjmR4E3YCNwLwksh9a23vxmXMS+HANrxM0vzSTbooIE6aTMwMDAwZWVlbDIwOh98meCmNZt91p9D9LfiwL03+aInbmwzMjr4+vksnA0OjgZpQ2Hqh7/IspqK6N6TBEuYRwpXY27Q4Gk0MDBlZWVlZWUxOmczMjpyn6JpWGSKNbU+jjkF0R7FOxtJKb9fSZiErtffYW9ZEzE6bGk0ZTE6bWxkdTEzOmRlY2ltYWxQbGFjZXMxOhJ1NzptaW50ZXJzbnU2OnRpY2tlcnU0Ok1lYWRlaTEwMDAwMDAwMDAwMDAwMDAwMDBlZTE6bmkxMTU3N2UxOnA2NToEq54xog2Nv1BCv8Js6dntmg4yrXh6HlqjroGI+lFDhhU1rMcTLNjnTUwfC5T4Q1deOt1piNPMsfVNfFn7lTXXiTE6czIwOhwq6XOAz7T3MgSeRU9tmiXUlnxvMTp0dTI3OjIyMDEtMDEtMzFUMjM6NTk6NTkuOTk5MDAwWjE6dWxlZQ=="));
            var a = tx.Actions.First();
            var action = new MintAssets();
            action.LoadPlainValue(a);
            var address = action.MintSpecs!.First().Recipient;
            var avatarAddress = action.MintSpecs.Last().Recipient;
            var prevState = GenerateAvatar(_prevState, address, avatarAddress);
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _minters.First(),
                    BlockIndex = 1,
                }
            );

            var inventory = nextState.GetInventoryV2(avatarAddress);
            var avatarState = nextState.GetAvatarState(avatarAddress);
            Assert.Single(avatarState.mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(avatarState.mailBox.First());
            Assert.Equal(action.Memo, mail.Memo);

            foreach (var mintSpec in action.MintSpecs)
            {
                if (mintSpec.Assets.HasValue)
                {
                    var fav = mintSpec.Assets.Value;
                    Assert.Equal(fav, nextState.GetBalance(address, fav.Currency));
                    Assert.Contains(mail.FungibleAssetValues, tuple => tuple.value == fav && tuple.balanceAddr.Equals(address));
                }

                if (mintSpec.Items.HasValue)
                {
                    var item = mintSpec.Items.Value;
                    var fungibleId = item.Id;
                    var itemCount = item.Count;
                    Assert.Contains(inventory.Items, i => i.count == itemCount && i.item is Material m && m.FungibleId.Equals(fungibleId));
                    Assert.Contains(
                        mail.FungibleIdAndCounts,
                        tuple => tuple.count == itemCount && tuple.fungibleId.Equals(fungibleId)
                    );
                }
            }
        }

        private IWorld GenerateAvatar(IWorld state, out Address avatarAddress)
        {
            var address = new PrivateKey().Address;
            var agentState = new AgentState(address);
            avatarAddress = address.Derive("avatar");
            var rankingMapAddress = new PrivateKey().Address;
            var avatarState = AvatarState.Create(
                avatarAddress,
                address,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            agentState.avatarAddresses[0] = avatarAddress;

            state = state
                .SetAgentState(address, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            return state;
        }

        private IWorld GenerateAvatar(IWorld state, Address address, Address avatarAddress)
        {
            var agentState = new AgentState(address);
            var rankingMapAddress = new PrivateKey().Address;
            var avatarState = AvatarState.Create(
                avatarAddress,
                address,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            agentState.avatarAddresses[0] = avatarAddress;

            state = state
                .SetAgentState(address, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            return state;
        }
    }
}

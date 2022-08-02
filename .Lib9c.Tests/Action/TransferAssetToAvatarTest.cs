namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class TransferAssetToAvatarTest
    {
        private readonly Address _senderAddress;
        private readonly Address _senderAgentAddress;
        private readonly Address _recipientAddress;
        private readonly Address _recipientAgentAddress;
        private readonly Currency _crystalCurrency;

        public TransferAssetToAvatarTest()
        {
            _senderAddress = new PrivateKey().ToAddress();
            _senderAgentAddress = new PrivateKey().ToAddress();
            _recipientAddress = new PrivateKey().ToAddress();
            _recipientAgentAddress = new PrivateKey().ToAddress();
            _crystalCurrency = CrystalCalculator.CRYSTAL;
        }

        [Theory]
        [InlineData(null, true, true, true, false, false, true, true, false, false)]
        [InlineData(null, true, true, true, false, false, true, false, true, false)]
        [InlineData(null, false, false, false, false, false, true, true, false, false)]
        [InlineData(typeof(InvalidTransferSignerException), true, true, false, false, false, false, false, false, false)]
        [InlineData(typeof(InvalidTransferSignerException), true, false, false, false, false, false, false, false, false)]
        [InlineData(typeof(InvalidTransferRecipientException), true, true, true, false, true, false, false, false, false)]
        [InlineData(typeof(InvalidTransferRecipientException), false, false, false, true, false, false, false, false, false)]
        [InlineData(typeof(AgentStateNotContainsAvatarAddressException), false, false, false, false, false, false, false, false, false)]
        [InlineData(typeof(InvalidTransferUnactivatedRecipientException), false, false, false, false, false, true, false, false, false)]
        [InlineData(typeof(InvalidTransferMinterException), false, false, false, false, false, true, true, false, true)]
        public void Execute(
            Type exc,
            bool isAvatar,
            bool senderAgentExist,
            bool includeSender,
            bool sameAgent,
            bool sameAvatar,
            bool includeRecipient,
            bool deriveActivated,
            bool legacyActivated,
            bool includeNcg
        )
        {
            var sender = isAvatar ? _senderAddress : _senderAgentAddress;
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var recipientAgentState = new AgentState(_recipientAgentAddress);
            if (includeRecipient)
            {
                recipientAgentState.avatarAddresses[0] = _recipientAddress;
            }

            var senderAgentState = new AgentState(_senderAgentAddress);
            if (includeSender)
            {
                senderAgentState.avatarAddresses[0] = _senderAddress;
            }

            IAccountStateDelta state = new State().SetState(_recipientAgentAddress, recipientAgentState.Serialize());
            if (senderAgentExist)
            {
                state = state.SetState(_senderAgentAddress, senderAgentState.Serialize());
            }

            if (deriveActivated)
            {
                state = state.SetState(_recipientAgentAddress.Derive(ActivationKey.DeriveKey), true.Serialize());
            }

            var activatedAccountState = new ActivatedAccountsState();
            if (legacyActivated)
            {
                activatedAccountState = activatedAccountState.AddAccount(_recipientAgentAddress);
            }
            else
            {
                activatedAccountState = activatedAccountState.AddAccount(sender);
            }

            state = state.SetState(Addresses.ActivatedAccount, activatedAccountState.Serialize());

            var rune = RuneHelper.ToFungibleAssetValue(tableSheets.RuneSheet[1], 10);
            var amounts = new List<FungibleAssetValue>
            {
                _crystalCurrency * 100,
                rune,
            };
            if (includeNcg)
            {
                amounts.Add(new FungibleAssetValue(new Currency("NCG", 2, minters: new[] { default(Address) }.ToImmutableHashSet())) * 100);
            }

            state = amounts.Aggregate(state, (current, amount) => current.MintAsset(sender, amount));

            var action = new TransferAssetToAvatar(
                sender,
                sameAvatar ? _senderAddress : _recipientAddress,
                sameAgent ? _senderAgentAddress : _recipientAgentAddress,
                amounts,
                "memo"
            );
            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    Signer = _senderAgentAddress,
                    PreviousStates = state,
                    BlockIndex = 1,
                });

                foreach (var amount in amounts)
                {
                    Currency currency = amount.Currency;
                    Assert.Equal(0 * currency, nextState.GetBalance(sender, currency));
                    Assert.Equal(0 * currency, nextState.GetBalance(_recipientAgentAddress, currency));
                    Assert.Equal(amount, nextState.GetBalance(_recipientAddress, currency));
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    Signer = _senderAgentAddress,
                    PreviousStates = state,
                    BlockIndex = 1,
                }));
            }
        }
    }
}

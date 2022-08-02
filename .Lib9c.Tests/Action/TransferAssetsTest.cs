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

    public class TransferAssetsTest
    {
        private readonly Address _senderAddress;
        private readonly Address _recipientAddress;
        private readonly Address _recipientAgentAddress;
        private readonly Currency _crystalCurrency;

        public TransferAssetsTest()
        {
            _senderAddress = new PrivateKey().ToAddress();
            _recipientAddress = new PrivateKey().ToAddress();
            _recipientAgentAddress = new PrivateKey().ToAddress();
            _crystalCurrency = CrystalCalculator.CRYSTAL;
        }

        [Theory]
        [InlineData(null, false, false, true, true, false, false)]
        [InlineData(null, false, false, true, false, true, false)]
        [InlineData(typeof(InvalidTransferSignerException), true, true, true, false, false, false)]
        [InlineData(typeof(InvalidTransferRecipientException), false, true, true, true, false, false)]
        [InlineData(typeof(AgentStateNotContainsAvatarAddressException), false, false, false, true, false, false)]
        [InlineData(typeof(AgentStateNotContainsAvatarAddressException), false, false, false, false, true, false)]
        [InlineData(typeof(InvalidTransferUnactivatedRecipientException), false, false, false, false, false, false)]
        [InlineData(typeof(InvalidTransferMinterException), false, false, true, true, false, true)]
        [InlineData(typeof(InvalidTransferMinterException), false, false, true, false, true, true)]
        public void Execute(
            Type exc,
            bool invalidSigner,
            bool sameAgent,
            bool includeRecipient,
            bool deriveActivated,
            bool legacyActivated,
            bool containsMinter
        )
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var recipientAgentState = new AgentState(_recipientAgentAddress);
            if (includeRecipient)
            {
                recipientAgentState.avatarAddresses[0] = _recipientAddress;
            }

            IAccountStateDelta state = new State().SetState(_recipientAgentAddress, recipientAgentState.Serialize());

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
                activatedAccountState = activatedAccountState.AddAccount(_senderAddress);
            }

            state = state.SetState(Addresses.ActivatedAccount, activatedAccountState.Serialize());

            var rune = RuneHelper.ToFungibleAssetValue(tableSheets.RuneSheet[1], 10);
            var minters = new[] { default(Address) }.ToImmutableHashSet();
            if (containsMinter)
            {
                minters = minters.Add(_senderAddress);
            }

            var amountInfo = new List<(Address recipient, FungibleAssetValue amount)>
            {
                (_recipientAgentAddress, _crystalCurrency * 100),
                (_recipientAddress, rune),
                (_recipientAgentAddress, new Currency("NCG", 2, minters: minters) * 100),
            };

            state = amountInfo.Aggregate(state, (current, info) => current.MintAsset(_senderAddress, info.amount));

            var action = new TransferAssets(
                _senderAddress,
                sameAgent ? _senderAddress : _recipientAgentAddress,
                amountInfo,
                "memo"
            );
            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    Signer = _senderAddress,
                    PreviousStates = state,
                    BlockIndex = 1,
                });

                foreach (var (recipient, amount) in amountInfo)
                {
                    Currency currency = amount.Currency;
                    Assert.Equal(0 * currency, nextState.GetBalance(_senderAddress, currency));
                    Assert.Equal(amount, nextState.GetBalance(recipient, currency));
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    Signer = invalidSigner ? new PrivateKey().ToAddress() : _senderAddress,
                    PreviousStates = state,
                    BlockIndex = 1,
                }));
            }
        }
    }
}

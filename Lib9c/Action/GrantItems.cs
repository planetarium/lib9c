using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Grants items/assets to target avatars from token-like inputs.
    /// This action is restricted to a fixed allow-list of signers and the current admin signer.
    /// Even if the signer does not have sufficient token balance, the grant will proceed
    /// and will burn only the available amount of the token.
    /// </summary>
    [ActionType(ActionTypeText)]
    public class GrantItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "grant_items";
        private const int MaxClaimDataCount = 100;

        private static readonly Address[] AllowedSigners =
        {
            new("Cb75C84D76A6f97A2d55882Aea4436674c288673"),
            new("0E19A992ad976B4986098813DfCd24B0775AC0AA"),
        };

        /// <inheritdoc />
        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; private set; }

        /// <summary>
        /// Optional memo written to the resulting mail.
        /// </summary>
        public string Memo;

        /// <summary>
        /// Creates an empty action instance.
        /// </summary>
        public GrantItems()
        {
            ClaimData = Array.Empty<(Address, IReadOnlyList<FungibleAssetValue>)>();
        }

        /// <summary>
        /// Creates an action instance with the given claim data.
        /// </summary>
        /// <param name="claimData">Grant targets and token-like inputs.</param>
        /// <param name="memo">Optional memo written to the resulting mail.</param>
        public GrantItems(
            IReadOnlyList<(Address, IReadOnlyList<FungibleAssetValue>)> claimData,
            string memo = null)
        {
            ClaimData = claimData ?? throw new ArgumentNullException(nameof(claimData));
            Memo = memo;
        }

        /// <inheritdoc />
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            GetPlainValueInternal();

        private IImmutableDictionary<string, IValue> GetPlainValueInternal()
        {
            var dict = ImmutableDictionary<string, IValue>.Empty
                .Add(ClaimDataKey, ClaimData.Aggregate(List.Empty, (list, tuple) =>
                {
                    var serializedFungibleAssetValues =
                        tuple.fungibleAssetValues.Select(x => x.Serialize()).Serialize();

                    return list.Add(new List(tuple.address.Bencoded, serializedFungibleAssetValues));
                }));

            if (!string.IsNullOrEmpty(Memo))
            {
                dict = dict.Add(MemoKey, Memo.Serialize());
            }

            return dict;
        }

        /// <inheritdoc />
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            ClaimData = ((List)plainValue[ClaimDataKey])
                .Select(pairValue =>
                {
                    List pair = (List)pairValue;
                    return (
                        new Address(pair[0]),
                        pair[1].ToList(x => x.ToFungibleAssetValue()) as IReadOnlyList<FungibleAssetValue>);
                }).ToList();

            if (plainValue.ContainsKey(MemoKey))
            {
                if (plainValue[MemoKey] is Text t && !string.IsNullOrEmpty(t))
                {
                    Memo = t;
                }
                else
                {
                    throw new ArgumentException(nameof(PlainValue));
                }
            }
        }

        /// <inheritdoc />
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            CheckSignerAllowed(context);

            if (ClaimData.Count > MaxClaimDataCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ClaimData),
                    ClaimData.Count,
                    $"ClaimData should be less than {MaxClaimDataCount}");
            }

            var states = context.PreviousState;
            var random = context.GetRandom();

            // Phase 1: Aggregate total requested burn amount by currency.
            var totalRequestedByCurrency = new Dictionary<Currency, FungibleAssetValue>();
            foreach (var (_, fungibleAssetValues) in ClaimData)
            {
                foreach (var requestedToken in fungibleAssetValues)
                {
                    if (requestedToken.Sign < 0)
                    {
                        throw new ArgumentException(
                            $"{nameof(requestedToken)} must be non-negative.",
                            nameof(ClaimData));
                    }

                    if (requestedToken.RawValue <= 0)
                    {
                        continue;
                    }

                    var tokenCurrency = requestedToken.Currency;
                    if (totalRequestedByCurrency.TryGetValue(tokenCurrency, out var total))
                    {
                        totalRequestedByCurrency[tokenCurrency] = total + requestedToken;
                    }
                    else
                    {
                        totalRequestedByCurrency[tokenCurrency] = requestedToken;
                    }
                }
            }

            // Phase 2: Burn by currency, up to the available signer balance.
            foreach (var (currency, totalRequested) in totalRequestedByCurrency)
            {
                if (totalRequested.Sign <= 0)
                {
                    continue;
                }

                var balance = states.GetBalance(context.Signer, currency);
                var burnAmount = totalRequested <= balance ? totalRequested : balance;
                if (burnAmount.Sign > 0)
                {
                    states = states.BurnAsset(
                        context,
                        context.Signer,
                        burnAmount
                    );
                }
            }

            // Phase 3: Grant requested amounts (force-grant), after burn has completed.
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();

            foreach (var (avatarAddress, fungibleAssetValues) in ClaimData)
            {
                var avatarState = states.GetAvatarState(
                        avatarAddress,
                        getQuestList: false,
                        getWorldInformation: false)
                    ?? throw new FailedLoadStateException(
                        ActionTypeText,
                        GetSignerAndOtherAddressesHex(context, avatarAddress),
                        typeof(AvatarState),
                        avatarAddress);

                var favs = new List<FungibleAssetValue>();
                var items = new List<(int id, int count)>();

                foreach (var requestedToken in fungibleAssetValues)
                {
                    var tokenCurrency = requestedToken.Currency;

                    if (Currencies.IsWrappedCurrency(tokenCurrency))
                    {
                        var currency = Currencies.GetUnwrappedCurrency(tokenCurrency);
                        var recipientAddress = Currencies.PickAddress(
                            currency,
                            avatarState.agentAddress,
                            avatarAddress);
                        var granted = FungibleAssetValue.FromRawValue(currency, requestedToken.RawValue);

                        states = states.MintAsset(context, recipientAddress, granted);
                        favs.Add(granted);
                    }
                    else
                    {
                        (bool tradable, int itemId) = Currencies.ParseItemCurrency(tokenCurrency);

                        // See ClaimItems.cs for context about the InventoryExtensions.MintItem workaround.
                        var itemRow = itemSheet[itemId];
                        var itemCount = (int)requestedToken.RawValue;
                        avatarState.inventory.MintItem(itemRow, itemCount, tradable, random);
                        items.Add((itemRow.Id, itemCount));
                    }
                }

                var mailBox = avatarState.mailBox;
                var mail = new ClaimItemsMail(
                    context.BlockIndex,
                    random.GenerateRandomGuid(),
                    context.BlockIndex,
                    favs,
                    items,
                    Memo);
                mailBox.Add(mail);
                mailBox.CleanUp();
                avatarState.mailBox = mailBox;
                states = states.SetAvatarState(
                    avatarAddress,
                    avatarState,
                    setWorldInformation: false,
                    setQuestList: false);
            }

            return states;
        }

        private void CheckSignerAllowed(IActionContext context)
        {
            if (AllowedSigners.Contains(context.Signer))
            {
                return;
            }

            if (TryGetAdminState(context, out AdminState policy))
            {
                if (context.BlockIndex > policy.ValidUntil)
                {
                    throw new PolicyExpiredException(policy, context.BlockIndex);
                }

                if (policy.AdminAddress != context.Signer)
                {
                    throw new PermissionDeniedException(policy, context.Signer);
                }

                return;
            }

            throw new InvalidMinterException(context.Signer);
        }
    }
}

using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Lib9c;
using Lib9c.Abstractions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Stake;
using Serilog;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2143
    /// Updated at https://github.com/planetarium/lib9c/pull/2143
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class TransferAsset : ActionBase, ISerializable, ITransferAsset, ITransferAssetV1
    {
        private const int MemoMaxLength = 80;
        public const string TypeIdentifier = "transfer_asset5";
        public const long CrystalTransferringRestrictionStartIndex = 6_220_000L;
        public static readonly IReadOnlyList<Address> AllowedCrystalTransfers = new Address[]
        {
            // world boss service
            new Address("CFCd6565287314FF70e4C4CF309dB701C43eA5bD"),
            // world boss ops
            new Address("3ac40802D359a6B51acB0AC0710cc90de19C9B81"),
        };

        public TransferAsset()
        {
        }

        public TransferAsset(Address sender, Address recipient, FungibleAssetValue amount, string memo = null)
        {
            Sender = sender;
            Recipient = recipient;
            Amount = amount;

            CheckMemoLength(memo);
            Memo = memo;
        }

        protected TransferAsset(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary)new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public Address Recipient { get; private set; }
        public FungibleAssetValue Amount { get; private set; }
        public string Memo { get; private set; }

        Address ITransferAssetV1.Sender => Sender;
        Address ITransferAssetV1.Recipient => Recipient;
        FungibleAssetValue ITransferAssetV1.Amount => Amount;
        string ITransferAssetV1.Memo => Memo;

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "recipient", Recipient.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "amount", Amount.Serialize()),
                };

                if (!(Memo is null))
                {
                    pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text)"memo", Memo.Serialize()));
                }

                return Dictionary.Empty
                    .Add("type_id", TypeIdentifier)
                    .Add("values", new Dictionary(pairs));
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(4);
            Address signer = context.Signer;
            var world = context.PreviousState;
            if (context.Rehearsal)
            {
                return LegacyModule.MarkBalanceChanged(world, context, Amount.Currency, new[] { Sender, Recipient });
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset5 exec started", addressesHex);
            if (Sender != signer)
            {
                throw new InvalidTransferSignerException(signer, Sender, Recipient);
            }

            if (Sender == Recipient)
            {
                throw new InvalidTransferRecipientException(Sender, Recipient);
            }

            Currency currency = Amount.Currency;
            if (!(currency.Minters is null) &&
                (currency.Minters.Contains(Sender) || currency.Minters.Contains(Recipient)))
            {
                throw new InvalidTransferMinterException(
                    currency.Minters,
                    Sender,
                    Recipient
               );
            }

            CheckCrystalSender(currency, context.BlockIndex, Sender);
            if (LegacyModule.TryGetState(world, Recipient, out IValue serializedStakeState))
            {
                bool isStakeStateOrMonsterCollectionState;
                if (serializedStakeState is Dictionary dictionary)
                {
                    try
                    {
                        _ = new StakeState(dictionary);
                        isStakeStateOrMonsterCollectionState = true;
                    }
                    catch (Exception)
                    {
                        isStakeStateOrMonsterCollectionState = false;
                    }

                    if (isStakeStateOrMonsterCollectionState)
                    {
                        throw new ArgumentException(
                            "You can't send assets to staking state.",
                            nameof(Recipient));
                    }

                    try
                    {
                        _ = new MonsterCollectionState0(dictionary);
                        isStakeStateOrMonsterCollectionState = true;
                    }
                    catch (Exception)
                    {
                        isStakeStateOrMonsterCollectionState = false;
                    }

                    if (isStakeStateOrMonsterCollectionState)
                    {
                        throw new ArgumentException(
                            "You can't send assets to staking state.",
                            nameof(Recipient));
                    }

                    try
                    {
                        _ = new MonsterCollectionState(dictionary);
                        isStakeStateOrMonsterCollectionState = true;
                    }
                    catch (Exception)
                    {
                        isStakeStateOrMonsterCollectionState = false;
                    }

                    if (isStakeStateOrMonsterCollectionState)
                    {
                        throw new ArgumentException(
                            "You can't send assets to staking state.",
                            nameof(Recipient));
                    }
                }

                if (serializedStakeState is List serializedStakeStateV2)
                {
                    try
                    {
                        _ = new StakeStateV2(serializedStakeStateV2);
                        isStakeStateOrMonsterCollectionState = true;
                    }
                    catch (Exception)
                    {
                        isStakeStateOrMonsterCollectionState = false;
                    }

                    if (isStakeStateOrMonsterCollectionState)
                    {
                        throw new ArgumentException(
                            "You can't send assets to staking state.",
                            nameof(Recipient));
                    }
                }
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset5 Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return LegacyModule.TransferAsset(world, context, Sender, Recipient, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];

            Sender = asDict["sender"].ToAddress();
            Recipient = asDict["recipient"].ToAddress();
            Amount = asDict["amount"].ToFungibleAssetValue();
            Memo = asDict.TryGetValue((Text)"memo", out IValue memo) ? memo.ToDotnetString() : null;

            CheckMemoLength(Memo);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(PlainValue));
        }

        private void CheckMemoLength(string memo)
        {
            if (memo?.Length > MemoMaxLength)
            {
                string msg = $"The length of the memo, {memo.Length}, " +
                             $"is overflowed than the max length, {MemoMaxLength}.";
                throw new MemoLengthOverflowException(msg);
            }
        }

        public static void CheckCrystalSender(Currency currency, long blockIndex, Address sender)
        {
            if (currency.Equals(CrystalCalculator.CRYSTAL) &&
                blockIndex >= CrystalTransferringRestrictionStartIndex && !AllowedCrystalTransfers.Contains(sender))
            {
                throw new InvalidTransferCurrencyException($"transfer crystal not allowed {sender}");
            }
        }
    }
}

using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Blockchain;

namespace Lib9c.Proposer.Tests;

public class CustomActionsDeserializableValidatorTest
{
    [Fact]
    public void Validate()
    {
        var validator = new CustomActionsDeserializableValidator(new MockActionLoader(), 10);
        Assert.False(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")),
        }));
        Assert.False(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")
                    .Add("values", Dictionary.Empty.Add("a", ImmutableArray<byte>.Empty))),
        }));
        Assert.True(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")
                    .Add("values", Dictionary.Empty.Add("a", new Address().ByteArray))),
        }));
    }

    private class DailyReward : IAction
    {
        private Address AvatarAddress { get; set; }

        public IValue PlainValue => Dictionary.Empty.Add("a", AvatarAddress.ByteArray);

        public void LoadPlainValue(IValue plainValue)
        {
            AvatarAddress = new Address(((Dictionary)plainValue)["a"]);
        }

        public IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            return context.PreviousState;
        }
    }

    private class MockTransaction : ITransaction
    {
        private IImmutableSet<Address>? _updatedAddresses;
        private PublicKey? _publicKey;
        private byte[]? _signature;

        public long Nonce { get; init; }
        public Address Signer { get; init; }
        public IImmutableSet<Address> UpdatedAddresses
        {
            get => _updatedAddresses
                ?? throw new InvalidOperationException("UpdatedAddresses is not set.");
            init => _updatedAddresses = value;
        }
        public DateTimeOffset Timestamp { get; init; }
        public PublicKey PublicKey
        {
            get => _publicKey ?? throw new InvalidOperationException("PublicKey is not set.");
            init => _publicKey = value;
        }
        public BlockHash? GenesisHash { get; init; }
        public TxActionList Actions =>
            new(SystemAction is { } sa ? new IValue[]{ sa } : CustomActions!);

        public FungibleAssetValue? MaxGasPrice => null;

        public long? GasLimit => null;

        public TxId Id { get; init; }
        public byte[] Signature
        {
            get => _signature ?? throw new InvalidOperationException("Signature is not set.");
            init => _signature = value;
        }
        public IValue? SystemAction { get; init; }
        public IImmutableList<IValue>? CustomActions { get; init; }
        public bool Equals(ITxInvoice? other)
        {
            return UpdatedAddresses.Equals(other?.UpdatedAddresses) &&
                   Timestamp.Equals(other.Timestamp) &&
                   Nullable.Equals(GenesisHash, other.GenesisHash) &&
                   Actions.Equals(other.Actions);
        }

        public bool Equals(ITxSigningMetadata? other)
        {
            return Nonce == other?.Nonce &&
                   Signer.Equals(other.Signer) &&
                   PublicKey.Equals(other.PublicKey);
        }

        public bool Equals(IUnsignedTx? other)
        {
            return ((ITxSigningMetadata)this).Equals(other) &&
                   ((ITxInvoice)this).Equals(other);
        }
    }

    private class MockActionLoader : IActionLoader
    {
        public IAction LoadAction(long index, IValue value)
        {
            try
            {
                if ((Text)((Dictionary)value)["type_id"] != "daily_reward")
                {
                    throw new ArgumentException(
                        $"Given {nameof(value)} should have daily_reward as its type_id");
                }

                var act = new DailyReward();
                act.LoadPlainValue(((Dictionary)value)["values"]);
                return act;
            }
            catch (Exception e)
            {
                throw new InvalidActionException(
                    $"Failed to load an action from given {nameof(value)}: {value}", value, e);
            }
        }
    }
}

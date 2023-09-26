using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Action.Results;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.State
{
    public class CombinationSlotState : State
    {
        public const string DeriveFormat = "combination-slot-{0}";
        public long UnlockBlockIndex { get; private set; }
        public int UnlockStage { get; private set; }
        public long StartBlockIndex { get; private set; }
        public AttachmentActionResult Result { get; private set; }
        public int? PetId { get; private set; }
        public long RequiredBlockIndex => UnlockBlockIndex - StartBlockIndex;

        public static Address DeriveAddress(Address address, int slotIndex) =>
            address.Derive(string.Format(
                CultureInfo.InvariantCulture,
                DeriveFormat,
                slotIndex));

        public CombinationSlotState(Address address, int unlockStage) : base(address)
        {
            UnlockStage = unlockStage;
        }

        public CombinationSlotState(Dictionary serialized) : base(serialized)
        {
            UnlockBlockIndex = serialized["unlockBlockIndex"].ToLong();
            UnlockStage = serialized["unlockStage"].ToInteger();
            if (serialized.TryGetValue((Text) "result", out var result))
            {
                Result = AttachmentActionResult.Deserialize((Dictionary) result);
            }

            if (serialized.TryGetValue((Text) "startBlockIndex", out var value))
            {
                StartBlockIndex = value.ToLong();
            }

            if (serialized.TryGetValue((Text) "petId", out var petId))
            {
                PetId = petId.ToNullableInteger();
            }
        }

        public bool Validate(AvatarState avatarState, long blockIndex)
        {
            if (avatarState is null)
            {
                return false;
            }

            return avatarState.worldInformation != null &&
                   avatarState.worldInformation.IsStageCleared(UnlockStage) &&
                   blockIndex >= UnlockBlockIndex;
        }

        public void Update(AttachmentActionResult result, long blockIndex, long unlockBlockIndex, int? petId = null)
        {
            Result = result;
            StartBlockIndex = blockIndex;
            UnlockBlockIndex = unlockBlockIndex;
            PetId = petId;
        }

        public void Update(long blockIndex)
        {
            UnlockBlockIndex = blockIndex;
            Result.itemUsable.Update(blockIndex);
        }

        public void Update(long blockIndex, Material material, int count)
        {
            Update(blockIndex);
            var result = new RapidCombination0Result((Dictionary) Result.Serialize())
            {
                cost = new Dictionary<Material, int> {[material] = count}
            };
            Result = result;
        }

        public void UpdateV2(long blockIndex, Material material, int count)
        {
            Update(blockIndex);
            var result = new RapidCombination5Result((Dictionary) Result.Serialize())
            {
                cost = new Dictionary<Material, int> {[material] = count}
            };
            Result = result;
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "unlockBlockIndex"] = UnlockBlockIndex.Serialize(),
                [(Text) "unlockStage"] = UnlockStage.Serialize(),
                [(Text) "startBlockIndex"] = StartBlockIndex.Serialize(),
            };

            if (!(Result is null))
            {
                values.Add((Text)"result", Result.Serialize());
            }

            if (!(PetId is null))
            {
                values.Add((Text)"petId", PetId.Serialize());
            }
#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }
    }
}

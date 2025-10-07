using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Types.Assets;

namespace Lib9c.Model.Mail
{
    /// <summary>
    /// Represents a worldboss reward mail that contains rewards such as fungible assets and items.
    /// </summary>
    public class WorldBossRewardMail : Mail
    {
        /// <summary>
        /// The type of mail.
        /// </summary>
        public override MailType MailType => MailType.System;

        /// <summary>
        /// A list of fungible asset values included in the reward.
        /// </summary>
        public List<FungibleAssetValue> FungibleAssetValues = new ();

        /// <summary>
        /// A list of item rewards where each entry contains the item ID and count.
        /// </summary>
        public List<(int id, int count)> Items = new ();

        /// <summary>
        /// Constructor for creating a WorldBossRewardMail with specified parameters.
        /// </summary>
        /// <param name="blockIndex">The block index at which the mail is created.</param>
        /// <param name="id">The unique identifier for the mail.</param>
        /// <param name="requiredBlockIndex">The block index required to claim the mail.</param>
        /// <param name="fungibleAssetValues">The list of fungible asset rewards.</param>
        /// <param name="items">The list of item rewards.</param>
        public WorldBossRewardMail(long blockIndex, Guid id, long requiredBlockIndex,
            List<FungibleAssetValue> fungibleAssetValues, List<(int id, int count)> items)
            : base(blockIndex, id, requiredBlockIndex)
        {
            FungibleAssetValues = fungibleAssetValues;
            Items = items;
        }

        /// <summary>
        /// Constructor for deserializing a WorldBossRewardMail from a Bencodex dictionary.
        /// </summary>
        /// <param name="serialized">The serialized dictionary representation of the mail.</param>
        public WorldBossRewardMail(Dictionary serialized) : base(serialized)
        {
            if (serialized.ContainsKey("f"))
            {
                FungibleAssetValues = serialized["f"].ToList(StateExtensions.ToFungibleAssetValue);
            }

            if (serialized.ContainsKey("i"))
            {
                Items = serialized["i"].ToList<(int, int)>(v =>
                {
                    var list = (List) v;
                    return ((Integer)list[0], (Integer)list[1]);
                });
            }
        }

        /// <summary>
        /// Reads the mail contents into the specified mail object.
        /// </summary>
        /// <param name="mail">The mail object to populate with this mail's data.</param>
        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        /// <summary>
        /// The type identifier for the WorldBossRewardMail class.
        /// </summary>
        protected override string TypeId => nameof(WorldBossRewardMail);

        /// <summary>
        /// Serializes the WorldBossRewardMail into a Bencodex-compatible format.
        /// </summary>
        /// <returns>A serialized representation of the mail.</returns>
        public override IValue Serialize()
        {
            var dict = (Dictionary)base.Serialize();
            if (FungibleAssetValues.Any())
            {
                dict = dict.SetItem("f", new List(FungibleAssetValues.Select(f => f.Serialize())));
            }

            if (Items.Any())
            {
                dict = dict.SetItem("i",
                    new List(Items.Select(tuple => List.Empty.Add(tuple.id).Add(tuple.count))));
            }

            return dict;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.BattleStatus
{
    /// <summary>
    /// Event that represents a character receiving rewards after battle.
    /// Supports both item rewards and fungible asset rewards.
    /// </summary>
    [Serializable]
    public class GetReward : EventBase
    {
        /// <summary>
        /// List of item rewards received by the character.
        /// </summary>
        public readonly List<ItemBase> Rewards;

        /// <summary>
        /// Dictionary of fungible asset rewards (e.g., NCG, Crystal) received by the character.
        /// </summary>
        public readonly Dictionary<string, int> FungibleAssetRewards;

        /// <summary>
        /// Initializes a new instance of the GetReward class with item rewards only.
        /// </summary>
        /// <param name="character">The character receiving the rewards.</param>
        /// <param name="rewards">List of item rewards.</param>
        public GetReward(CharacterBase character, List<ItemBase> rewards) : base(character)
        {
            Rewards = rewards;
            FungibleAssetRewards = new Dictionary<string, int>();
        }

        /// <summary>
        /// Initializes a new instance of the GetReward class with both item and fungible asset rewards.
        /// </summary>
        /// <param name="character">The character receiving the rewards.</param>
        /// <param name="rewards">List of item rewards.</param>
        /// <param name="fungibleAssetRewards">Dictionary of fungible asset rewards.</param>
        public GetReward(CharacterBase character, List<ItemBase> rewards, Dictionary<string, int> fungibleAssetRewards) : base(character)
        {
            Rewards = rewards;
            FungibleAssetRewards = fungibleAssetRewards ?? new Dictionary<string, int>();
        }

        /// <summary>
        /// Executes the reward event by giving rewards to the character.
        /// </summary>
        /// <param name="stage">The stage context for reward processing.</param>
        /// <returns>Coroutine enumerator for reward execution.</returns>
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetReward(Rewards, FungibleAssetRewards);
        }
    }
}

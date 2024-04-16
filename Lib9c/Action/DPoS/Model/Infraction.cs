namespace Nekoyume.Action.DPoS.Model
{
    public enum Infraction : byte
    {
        /// <summary>
        /// an empty infraction.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// a validator that double-signs a block.
        /// </summary>
        DoubleSign = 1,

        /// <summary>
        /// a validator that missed signing too many blocks.
        /// </summary>
        Downtime = 2,
    }
}

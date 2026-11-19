using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    /// <summary>
    /// A stage was entered before its hard world segment opened.
    /// </summary>
    [Serializable]
    public class StageNotOpenedException : InvalidStageException
    {
        /// <summary>
        /// Creates the exception for <paramref name="stageId"/> of <paramref name="worldId"/>.
        /// </summary>
        public StageNotOpenedException(int stageId, int worldId)
            : base($"stage {stageId} of world {worldId} is not opened yet")
        {
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected StageNotOpenedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

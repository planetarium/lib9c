using System;
using Bencodex.Types;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class ArenaRecord : IState
    {
        protected bool Equals(ArenaRecord other)
        {
            return Win == other.Win && Lose == other.Lose && Draw == other.Draw;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ArenaRecord)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Win;
                hashCode = (hashCode * 397) ^ Lose;
                hashCode = (hashCode * 397) ^ Draw;
                return hashCode;
            }
        }

        public int Win;
        public int Lose;
        public int Draw;

        public ArenaRecord()
        {
        }

        public ArenaRecord(List serialized)
        {
            Win = serialized[0].ToInteger();
            Lose = serialized[1].ToInteger();
            Draw = serialized[2].ToInteger();
        }

        public IValue Serialize() =>
            List.Empty
                .Add(Win.Serialize())
                .Add(Lose.Serialize())
                .Add(Draw.Serialize());
    }
}

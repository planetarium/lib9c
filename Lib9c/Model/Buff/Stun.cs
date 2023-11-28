using System;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Stun : Buff
    {
        public Stun(BuffInfo buffInfo) : base(buffInfo)
        {

        }

        protected Stun(Buff value) : base(value)
        {
        }

        public override object Clone()
        {
            return new Stun(this);
        }
    }
}

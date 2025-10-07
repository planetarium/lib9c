using System;
using System.Text;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.Module;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif

namespace Lib9c.Action
{
    [Serializable]
    public abstract class ActionBase : IAction
    {
        public abstract IValue PlainValue { get; }
        public abstract void LoadPlainValue(IValue plainValue);
        public abstract IWorld Execute(IActionContext context);

        /// <summary>
        /// returns "[Signer Address, AvatarState Address, ...]"
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="addresses"></param>
        /// <returns></returns>
        protected string GetSignerAndOtherAddressesHex(IActionContext ctx, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{ctx.Signer.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        protected bool TryGetAdminState(IActionContext ctx, out AdminState state)
        {
            state = default;

            IValue rawState = ctx.PreviousState.GetLegacyState(AdminState.Address);
            if (rawState is Bencodex.Types.Dictionary asDict)
            {
                state = new AdminState(asDict);
                return true;
            }

            return false;
        }

        protected void CheckPermission(IActionContext ctx)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            return;
#endif
            if (TryGetAdminState(ctx, out AdminState policy))
            {
                if (ctx.BlockIndex > policy.ValidUntil)
                {
                    throw new PolicyExpiredException(policy, ctx.BlockIndex);
                }

                if (policy.AdminAddress != ctx.Signer)
                {
                    throw new PermissionDeniedException(policy, ctx.Signer);
                }
            }
        }

        protected void CheckObsolete(long obsoleteIndex, IActionContext ctx)
        {
            if (ctx.BlockIndex > obsoleteIndex)
            {
                throw new ActionObsoletedException();
            }
        }
    }
}

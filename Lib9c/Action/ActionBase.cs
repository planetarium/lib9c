using System;
using System.Text;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Serilog;
using Nekoyume.Model.State;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class ActionBase : IAction
    {
        public static readonly IValue MarkChanged = Null.Value;

        // FIXME GoldCurrencyState 에 정의된 것과 다른데 괜찮을지 점검해봐야 합니다.
        protected static readonly Currency GoldCurrencyMock = new Currency();

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

        protected IWorld LogError(IActionContext context, string message, params object[] values)
        {
            string actionType = GetType().Name;
            object[] prependedValues = new object[values.Length + 2];
            prependedValues[0] = context.BlockIndex;
            prependedValues[1] = context.Signer;
            values.CopyTo(prependedValues, 2);
            string msg = $"#{{BlockIndex}} {actionType} (by {{Signer}}): {message}";
            Log.Error(msg, prependedValues);
            return context.PreviousState;
        }

        protected bool TryGetAdminState(IActionContext ctx, out AdminState state)
        {
            state = default;

            IValue rawState = ctx.PreviousState.GetAccount(ReservedAddresses.LegacyAccount).GetState(AdminState.Address);
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

        protected bool UseV100291Sheets(long blockIndex)
        {
            return blockIndex < ActionObsoleteConfig.V100301ExecutedBlockIndex;
        }

        protected void CheckActionAvailable(long startedIndex, IActionContext ctx)
        {
            if (ctx.BlockIndex <= startedIndex)
            {
                throw new ActionUnavailableException();
            }
        }
    }
}

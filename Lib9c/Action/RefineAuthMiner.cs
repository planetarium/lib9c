using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("refine_auth_miner")]
    public class RefineAuthMiner : ActionBase
    {
        private long _interval;
        private List<Address> _miners;
        private long _validUntil;

        public RefineAuthMiner(
            long interval,
            long validUntil,
            IEnumerable<Address> miners
            )
        {
            _interval = interval;
            _validUntil = validUntil;
            _miners = miners.ToList();
        }

        public override IValue PlainValue {
            get
            {
                var values = new Dictionary<IKey, IValue>
                {
                    [(Text) nameof(_miners)] = new List(_miners.OrderBy(m => m).Select(m => m.Serialize())),
                    [(Text) nameof(_interval)] = _interval.Serialize(),
                    [(Text) nameof(_validUntil)] = _validUntil.Serialize(),
                };

                return new Dictionary(values);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            _miners = asDict[nameof(_miners)].ToList(m => m.ToAddress());
            _interval = asDict[nameof(_interval)].ToLong();
            _validUntil = asDict[nameof(_validUntil)].ToLong();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta state = context.PreviousStates;
            if (context.Rehearsal)
            {
                return state
                    .SetState(AuthorizedMinersState.Address, MarkChanged);
            }
            CheckPermission(context);

            var authMinersState = new AuthorizedMinersState(_miners.ToList(), _interval, _validUntil);
            
            return state.SetState(
                AuthorizedMinersState.Address,
                authMinersState.Serialize()
            );
        }
    }
}
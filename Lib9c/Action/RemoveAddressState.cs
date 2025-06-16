using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Action to remove state for multiple addresses.
    /// </summary>
    [ActionType(TypeId)]
    public class RemoveAddressState : ActionBase
    {
        /// <summary>
        /// The type identifier of the action.
        /// </summary>
        public const string TypeId = "remove_address_state";

        /// <summary>
        /// List of tuples (account address, target address) to remove state for.
        /// </summary>
        public IReadOnlyList<(Address accountAddress, Address targetAddress)> Removals { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RemoveAddressState()
        {
        }

        /// <summary>
        /// Constructor that initializes with a list of removals.
        /// </summary>
        /// <param name="removals">List of tuples to remove state for</param>
        public RemoveAddressState(IReadOnlyList<(Address accountAddress, Address targetAddress)> removals)
        {
            Removals = removals;
        }

        /// <inheritdoc />
        public override IValue PlainValue =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                { (Text)"type_id", (Text)TypeId },
                {
                    (Text)"values", Dictionary.Empty
                    .Add("r", new List(Removals.Select(r =>
                        List.Empty
                            .Add(r.accountAddress.Serialize())
                            .Add(r.targetAddress.Serialize()))))
                }
            });

        /// <inheritdoc />
        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)plainValue;
            var values = (Dictionary)dictionary["values"];
            Removals = ((List)values["r"])
                .Select(r =>
                {
                    var list = (List)r;
                    return (list[0].ToAddress(), list[1].ToAddress());
                })
                .ToList();
        }

        /// <summary>
        /// Executes the action to remove state for the specified addresses.
        /// </summary>
        /// <param name="context">Action context</param>
        /// <returns>The updated world state</returns>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            CheckPermission(context);

            foreach (var (accountAddress, targetAddress) in Removals)
            {
                var account = states.GetAccount(accountAddress);
                account = account.RemoveState(targetAddress);
                states = states.SetAccount(accountAddress, account);
            }
            return states;
        }
    }
}

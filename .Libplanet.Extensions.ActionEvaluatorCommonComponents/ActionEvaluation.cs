using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class ActionEvaluation : IActionEvaluation
{
    public ActionEvaluation(
        IValue action,
        ActionContext inputContext,
        World outputState,
        Exception? exception)
    {
        Action = action;
        InputContext = inputContext;
        OutputState = outputState;
        Exception = exception;
    }

    public IValue Action { get; }
    public ActionContext InputContext { get; }
    IActionContext IActionEvaluation.InputContext => InputContext;
    public World OutputState { get; }
    IWorld IActionEvaluation.OutputState => OutputState;
    public Exception? Exception { get; }
}

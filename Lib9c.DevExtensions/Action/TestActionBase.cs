using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Assets;

namespace Lib9c.DevExtensions.Action;

[Serializable]
public abstract class TestActionBase : IAction
{
    public static readonly IValue MarkChanged = Null.Value;

    // FIXME GoldCurrencyState 에 정의된 것과 다른데 괜찮을지 점검해봐야 합니다.
    protected static readonly Currency GoldCurrencyMock = new Currency();

    public abstract IValue PlainValue { get; }
    public abstract void LoadPlainValue(IValue plainValue);

    public abstract IAccountStateDelta Execute(IActionContext context);
}

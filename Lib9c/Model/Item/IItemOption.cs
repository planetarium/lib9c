using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public interface IItemOption : IState
    {
        ItemOptionType Type { get; }

        int Grade { get; }
        
        /// <param name="ratio">(0...1)</param>
        void Enhance(decimal ratio);
    }
}

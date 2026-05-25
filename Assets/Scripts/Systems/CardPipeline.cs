using System.Collections.Generic;
using CardFramework;

namespace GreenPrince
{
    public interface ICardModifier
    {
        void Modify(CardInstance instance, object context);
    }

    public class CardPipeline
    {
        readonly List<ICardModifier> m_Modifiers = new();

        public void AddModifier(ICardModifier modifier) => m_Modifiers.Add(modifier);
        public bool RemoveModifier(ICardModifier modifier) => m_Modifiers.Remove(modifier);

        public void Process(CardInstance instance, object context)
        {
            foreach (var mod in m_Modifiers)
                mod.Modify(instance, context);
        }
    }
}

using System.Collections.Generic;

namespace GreenPrince
{
    /// <summary>
    /// Routes keyboard/gamepad input to the topmost registered UI surface.
    /// </summary>
    public static class UiInputFocus
    {
        static readonly Stack<IUiInputHandler> s_Stack = new();

        public static IUiInputHandler Current => s_Stack.Count > 0 ? s_Stack.Peek() : null;

        public static bool HasFocus(IUiInputHandler handler) =>
            handler != null && s_Stack.Count > 0 && ReferenceEquals(s_Stack.Peek(), handler);

        public static void Push(IUiInputHandler handler)
        {
            if (handler == null) return;
            if (s_Stack.Count > 0 && ReferenceEquals(s_Stack.Peek(), handler))
                return;
            s_Stack.Push(handler);
        }

        public static void Pop(IUiInputHandler handler)
        {
            if (s_Stack.Count == 0) return;
            if (handler != null && !ReferenceEquals(s_Stack.Peek(), handler))
                return;
            s_Stack.Pop();
        }

        public static void Clear() => s_Stack.Clear();
    }

    public interface IUiInputHandler
    {
        void OnUiInput();
    }
}

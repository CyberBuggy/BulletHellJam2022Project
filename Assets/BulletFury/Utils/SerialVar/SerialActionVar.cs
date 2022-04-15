using System;

namespace Ktyl.Util
{
    public abstract class SerialActionVar<T> : SerialVar<Action<T>>
    {
        public void Invoke(T val)
        {
            Value?.Invoke(val);
        }
    }
}
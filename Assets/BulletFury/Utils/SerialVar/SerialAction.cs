using System;
using UnityEngine;

namespace Ktyl.Util
{
    [CreateAssetMenu(menuName = "Create SerialAction", fileName = "SerialAction")]
    public class SerialAction : SerialVar<Action>
    {
        public void Invoke()
        {
            Value?.Invoke();
        }
    }
}
using System;
using UnityEngine.Events;

namespace BulletFury.Data
{
    /// <summary>
    /// Custom serializable unity even for bullet death
    /// Doing this means it'll show up as an event in the inspector
    /// </summary>
    [Serializable]
    public class BulletDiedEvent : UnityEvent<int, BulletContainer, bool>
    { }
}
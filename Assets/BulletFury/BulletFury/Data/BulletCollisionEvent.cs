using System;
using UnityEngine.Events;

namespace BulletFury.Data
{
    /// <summary>
    /// Custom serializable unity even for bullet collision
    /// Doing this means it'll show up as an event in the inspector
    /// </summary>
    [Serializable]
    public class BulletCollisionEvent : UnityEvent<BulletContainer, BulletCollider>
    {}
}

namespace BulletFury.Data
{
    /// <summary>
    /// Small enum to describe the possible methods of deciding which direction to spawn the bullet
    /// </summary>
    public enum SpawnDir { Directional, Randomised, Spherised  }
}
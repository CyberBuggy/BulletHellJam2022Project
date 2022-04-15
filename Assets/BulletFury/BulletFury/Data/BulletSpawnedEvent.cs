using System;
using UnityEngine.Events;

namespace BulletFury.Data
{
    [Serializable]
    public class BulletSpawnedEvent : UnityEvent<int, BulletContainer>
    {}
}
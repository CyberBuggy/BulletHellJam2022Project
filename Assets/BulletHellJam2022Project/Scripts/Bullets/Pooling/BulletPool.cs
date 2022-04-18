using System;
using UnityEngine;

namespace CyberBuggy.Bullets.Pooling
{
    [Serializable]
    public class BulletPool
    {
        public Action<BulletPool> OnDispose;
        public GameObject[] bullets;

        public void Init(int count)
        {
            bullets = new GameObject[count];
        }

        public void Dispose()
        {
            foreach (var bullet in bullets)
            {
                if(bullet == null)
                    continue;
                var bulletScript = bullet.GetComponent<Bullet>();
                MonoBehaviour.Destroy(bullet);
            }
            OnDispose?.Invoke(this);
        }

    }
}

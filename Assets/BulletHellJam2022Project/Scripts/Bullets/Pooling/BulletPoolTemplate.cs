using System;
using UnityEngine;

namespace CyberBuggy.Bullets.Pooling
{
    [Serializable]
    public class BulletPoolTemplate
    {
        public GameObject bulletPrefab;
        public int bulletCount;

        public BulletPoolTemplate(GameObject prefab, int count)
        {
            bulletPrefab = prefab;
            bulletCount = count;
        }
    }
}

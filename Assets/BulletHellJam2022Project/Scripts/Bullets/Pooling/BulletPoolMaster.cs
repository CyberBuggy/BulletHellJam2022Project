using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CyberBuggy
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
    public class BulletPoolMaster : MonoBehaviour
    {
        public static BulletPoolMaster Instance {get; private set;}

        [SerializeField] private Transform _bulletsParent;
        [SerializeField] private List<BulletPool> pooledBullets;
        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            pooledBullets = new List<BulletPool>();
        }
        
        public BulletPool AddPool(BulletPoolTemplate pool)
        {
            BulletPool instantiatedBullets = new BulletPool();
            var bulletPrefab = pool.bulletPrefab;
            instantiatedBullets.Init(pool.bulletCount);

            for (int j = 0; j < pool.bulletCount; j++)
            {
                var bullet = Instantiate(bulletPrefab, Vector2.zero, Quaternion.identity, _bulletsParent);
                instantiatedBullets.bullets[j] = bullet;
            }
            instantiatedBullets.OnDispose += RemoveFromPool;
            pooledBullets.Add(instantiatedBullets);
                
            return instantiatedBullets;
        }
        public List<BulletPool> AddPools(List<BulletPoolTemplate> pools)
        {
            List<BulletPool> poolList = new List<BulletPool>();
            for (int i = 0; i < pools.Count; i++)
            {
                var instantiatedBullets = AddPool(pools[i]);
                poolList.Add(instantiatedBullets);
            }
            return poolList;
        }

        private void RemoveFromPool(BulletPool pool)
        {
            pooledBullets.Remove(pool);

        }
    }
}

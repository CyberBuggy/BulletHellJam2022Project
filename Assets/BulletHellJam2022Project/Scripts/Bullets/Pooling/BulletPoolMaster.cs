using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CyberBuggy.Bullets.Pooling
{
    public class BulletPoolMaster : MonoBehaviour
    {
        public static BulletPoolMaster Instance {get; private set;}

        [SerializeField] private Transform _bulletsParent;
        [SerializeField] private List<BulletPool> _pooledBullets;
        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _pooledBullets = new List<BulletPool>();
        }
        
        public BulletPool AddPool(BulletPoolTemplate poolTemplate)
        {
            var instantiatedPool = new BulletPool();
            var bulletPrefab = poolTemplate.bulletPrefab;
            instantiatedPool.Init(poolTemplate.bulletCount);

            for (int j = 0; j < poolTemplate.bulletCount; j++)
            {
                var bullet = Instantiate(bulletPrefab, Vector2.zero, Quaternion.identity, _bulletsParent);
                instantiatedPool.bullets[j] = bullet;
            }
            instantiatedPool.OnDispose += RemoveFromPool;
            _pooledBullets.Add(instantiatedPool);
                
            return instantiatedPool;
        }
        public List<BulletPool> AddPools(List<BulletPoolTemplate> poolTemplates)
        {
            var poolList = new List<BulletPool>();
            for (int i = 0; i < poolTemplates.Count; i++)
            {
                var instantiatedPool = AddPool(poolTemplates[i]);
                poolList.Add(instantiatedPool);
            }
            return poolList;
        }

        private void RemoveFromPool(BulletPool pool)
        {
            _pooledBullets.Remove(pool);
        }
    }
}

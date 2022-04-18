using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Jobs;
using CyberBuggy.Bullets.Pooling;

namespace CyberBuggy.Bullets.Patterns
{
    public class BulletPatternBehaviour : MonoBehaviour
    {
        [SerializeField] private bool _shootingEnabled;
        public bool ShootingEnabled {get => _shootingEnabled; set => _shootingEnabled = value;}
        [SerializeField] private bool _automaticallyShootsOnFixedUpdate;
        [SerializeField] private BulletPatternMaster _patternMaster;
        public Transform BulletFirePoint { get => _bulletFirePoint; set => _bulletFirePoint = value; }
        [SerializeField] private Transform _bulletFirePoint;
        [SerializeField] private Transform _bulletOwner;
        public Transform BulletOwner { get => _bulletOwner; set => _bulletOwner = value; }
        private Transform _bulletTarget;
        public Transform BulletTarget { get => _bulletTarget; set => _bulletTarget = value; }

        private BulletDataBuilder _bulletBuilder;
        private TransformAccessArray[] _bulletTransformCollection;
        private NativeArray<BulletData>[] _bulletDataCollection;
        private List<BulletPool> _bulletPools;
        private List<BulletPoolTemplate> _bulletPoolTemplates;
        private int[] _bulletIndexes;
        private BulletPatternState[] _bulletPatternStates;

        private void Start()
        {
            InitiatePatterns();
        }
        private void InitiatePatterns()
        {
            int[] maxBulletsPooledPatterns = new int[_patternMaster.patterns.Count];
            for (int i = 0; i < _patternMaster.patterns.Count; i++)
            {
                var pattern = _patternMaster.patterns[i];
                maxBulletsPooledPatterns[i] = pattern.maxBulletsPooled;
            }

            _bulletBuilder = new BulletDataBuilder();

            _bulletDataCollection = _bulletBuilder.GetDataCollections(maxBulletsPooledPatterns, _patternMaster.patterns.Count, Allocator.Persistent);
            _bulletTransformCollection = _bulletBuilder.GetTransformAccessArrays(maxBulletsPooledPatterns, _patternMaster.patterns.Count);
            _bulletPoolTemplates = _bulletBuilder.GetBulletPoolTemplates(_patternMaster);
            
            _bulletPools = new List<BulletPool>(_patternMaster.patterns.Count);

            _bulletPools = BulletPoolMaster.Instance.AddPools(_bulletPoolTemplates);
            _bulletIndexes = new int[_patternMaster.patterns.Count];

            for (int j = 0; j < _bulletPools.Count; j++)
            {
                var pool = _bulletPools[j];
                for (int k = 0; k < pool.bullets.Length; k++)
                {
                    var bulletTransform = pool.bullets[k].transform;
                    _bulletTransformCollection[j].Add(bulletTransform);
                    
                    var bulletScript = bulletTransform.GetComponent<Bullet>();

                    bulletScript.BulletOwner = _bulletOwner;
                    bulletScript.bulletData.MovementInput.MoveVector = bulletTransform.transform.right;
                }
                
            }

            _bulletPatternStates = new BulletPatternState[_patternMaster.patterns.Count];

            for (int i = 0; i < _patternMaster.patterns.Count; i++)
            {
                var pattern = _patternMaster.patterns[i];
                var bulletPatternState = new BulletPatternState(pattern.burstDuration, pattern.burstShotCount, 0);
                _bulletPatternStates[i] = bulletPatternState;
                if(pattern.usesBurst)
                {
                    StartCoroutine(Co_SetInterval(bulletPatternState.BurstResetTime, (intervalTime) => {bulletPatternState.BurstResetTime = intervalTime;}));
                }
            }
        }

        private void FixedUpdate()
        {
            MoveBulletsWithJobs();

            if(!_automaticallyShootsOnFixedUpdate)
                return;

            SpawnBullets();
        }
        public void SpawnBullets()
        {
            if (!_shootingEnabled)
                return;
            
            for (int i = 0; i < _patternMaster.patterns.Count; i++)
            {
                var bulletPatternState = _bulletPatternStates[i];
                var isWaitingInterval = bulletPatternState.BulletInterval > 0;

                if (isWaitingInterval)
                    continue;

                var pattern = _patternMaster.patterns[i];
                var originalBulletRotation = _bulletFirePoint.localEulerAngles;

                bulletPatternState.bulletSpin += pattern.spinRate * Time.deltaTime;
                pattern.ApplySpin(ref originalBulletRotation, bulletPatternState);

                if (_bulletTarget != null && pattern.useLockOn)
                {
                    var targetDirection = _bulletTarget.position - _bulletFirePoint.position;
                    pattern.ApplyLockOn(ref originalBulletRotation, targetDirection);
                }

                if (pattern.usesBurst)
                {
                    if (bulletPatternState.BurstResetTime <= 0)
                    {
                        bulletPatternState.BurstResetTime = pattern.burstDuration;
                        bulletPatternState.BurstShotCount = pattern.burstShotCount;

                        StartCoroutine(Co_SetInterval(bulletPatternState.BurstResetTime, (intervalTime) => { bulletPatternState.BurstResetTime = intervalTime; }));
                    }

                    if (bulletPatternState.BurstShotCount <= 0)
                        continue;

                    bulletPatternState.BurstShotCount--;
                }

                ShootDistributedBullets(i, _bulletFirePoint.position, originalBulletRotation);
                StartCoroutine(Co_SetInterval(pattern.fireRate, (intervalTime) => { bulletPatternState.BulletInterval = intervalTime; }));

            }
        }

        private void MoveBulletsWithJobs()
        {
            for (int i = 0; i < _patternMaster.patterns.Count; i++)
            {
                var bulletJob = new BulletJob
                {
                    deltaTime = Time.deltaTime,
                    bulletData = this._bulletDataCollection[i]
                };

                var jobHandle = bulletJob.Schedule(_bulletTransformCollection[i]);
                jobHandle.Complete();
            }
            
        }
        private void ShootDistributedBullets(int patternIndex, Vector3 point, Vector3 eulerRotation)
        {
            var pattern = _patternMaster.patterns[patternIndex];
            for (int i = 0; i < pattern.bulletsPerShot; i++)
            {
                var firstBullet = i == 0;

                if(firstBullet)
                {
                    ShootBullet(patternIndex, 
                    point, 
                    Quaternion.Euler(eulerRotation));
                    continue;
                }

                var arrayBulletRotation = Vector3.zero;
                pattern.ApplyBulletDistribution(ref arrayBulletRotation, i);

                ShootBullet(patternIndex,
                point, 
                Quaternion.Euler(eulerRotation + arrayBulletRotation));
                
            }
        }
        private void ShootBullet(int patternIndex, Vector3 point, Quaternion rotation)
        {
            var bulletIndex = _bulletIndexes[patternIndex] = (_bulletIndexes[patternIndex] += 1) % _patternMaster.patterns[patternIndex].maxBulletsPooled;

            var bullet = _bulletPools[patternIndex].bullets[bulletIndex];

            bullet.SetActive(true);
            bullet.transform.position = point;
            bullet.transform.rotation = rotation;

            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.BulletOwner = _bulletOwner;

            bulletScript.bulletData.MovementInput.MoveVector = bullet.transform.right;
            _bulletDataCollection[patternIndex][bulletIndex] = bulletScript.bulletData;
            _bulletTransformCollection[patternIndex][bulletIndex].position = bullet.transform.position;
            _bulletTransformCollection[patternIndex][bulletIndex].rotation = bullet.transform.rotation;
            
        }
        private IEnumerator Co_SetInterval(float intervalTime, Action<float> callback)
        {
            while(intervalTime > 0)
            {
                intervalTime -= Time.deltaTime;
                callback?.Invoke(intervalTime);
                yield return null;
            }
            intervalTime = 0;
        }

        private void OnDestroy()
        {
            _bulletBuilder.Dispose();
            foreach (var pool in _bulletPools) pool.Dispose();
        }
    }
}

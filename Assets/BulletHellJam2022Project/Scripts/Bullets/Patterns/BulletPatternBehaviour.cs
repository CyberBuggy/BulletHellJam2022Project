using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;


namespace CyberBuggy
{
    public class BulletPatternBehaviour : MonoBehaviour, IShootBehaviour
    {
        [SerializeField] private bool shootingEnabled;
        [SerializeField] private BulletPatternMaster patternMaster;
        public bool ShootingEnabled {get => shootingEnabled; set => shootingEnabled = value;}
        private List<BulletPool> bulletPools;
        private List<BulletPoolTemplate> bulletPoolTemplates;
        private TransformAccessArray[] bulletTransformCollection;
        private BulletDataBuilder bulletBuilder;
        private NativeArray<BulletData>[] bulletDataCollection;
        private int[] bulletIndex;
        [SerializeField] private Transform bulletFirePoint;
        [SerializeField] private Transform bulletOwner;
        private Transform bulletTarget;

        private BulletPatternState[] bulletPatternStates;
        public void OnShootTrigger(Transform bulletFirePoint, Transform holder, Transform target = null)
        {
            bulletOwner = holder;
            if(target != null)
                bulletTarget = target;
        }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            int[] maxBulletsPooledPatterns = new int[patternMaster.patterns.Count];
            for (int i = 0; i < patternMaster.patterns.Count; i++)
            {
                var pattern = patternMaster.patterns[i];
                maxBulletsPooledPatterns[i] = pattern.maxBulletsPooled;
            }

            bulletBuilder = new BulletDataBuilder();

            bulletDataCollection = bulletBuilder.GetDataCollections(maxBulletsPooledPatterns, patternMaster.patterns.Count, Allocator.Persistent);
            bulletTransformCollection = bulletBuilder.GetTransformAccessArrays(maxBulletsPooledPatterns, patternMaster.patterns.Count);
            bulletPoolTemplates = bulletBuilder.GetBulletPoolTemplates(patternMaster);
            
            bulletPools = new List<BulletPool>(patternMaster.patterns.Count);

            bulletPools = BulletPoolMaster.Instance.AddPools(bulletPoolTemplates);
            bulletIndex = new int[patternMaster.patterns.Count];

            for (int j = 0; j < bulletPools.Count; j++)
            {
                var pool = bulletPools[j];
                for (int k = 0; k < pool.bullets.Length; k++)
                {
                    var bulletTransform = pool.bullets[k].transform;
                    bulletTransformCollection[j].Add(bulletTransform);
                    
                    var bulletScript = bulletTransform.GetComponent<Bullet>();

                    bulletScript.BulletOwner = bulletOwner;
                    bulletScript.bulletData.MovementInput.MoveVector = bulletTransform.transform.right;
                }
                
            }

            bulletPatternStates = new BulletPatternState[patternMaster.patterns.Count];

            for (int i = 0; i < patternMaster.patterns.Count; i++)
            {
                var pattern = patternMaster.patterns[i];
                var bulletPatternState = new BulletPatternState(pattern.burstDuration, pattern.BurstShotCount, 0);
                bulletPatternStates[i] = bulletPatternState;
                if(pattern.isBurst)
                {
                    StartCoroutine(IntervalShot(bulletPatternState.BurstResetTime, (intervalTime) => {bulletPatternState.BurstResetTime = intervalTime;}));
                }
            }
        }

        private void FixedUpdate()
        {
            MoveWithJobs();

            if(!shootingEnabled)
                return;
            
            for (int i = 0; i < patternMaster.patterns.Count; i++)
            {
                var bulletPatternState = bulletPatternStates[i];
                if(bulletPatternState.BulletInterval > 0)
                    continue;
                
                var pattern = patternMaster.patterns[i];

                Vector3 originalBulletRotation = bulletFirePoint.localEulerAngles;
                    
                bulletPatternState.bulletSpin += pattern.spinRate * Time.deltaTime;
                pattern.ApplySpin(ref originalBulletRotation, bulletPatternState);

                if(pattern.useLockOn && bulletTarget != null)
                {
                    Vector3 targetDirection = bulletTarget.position - bulletFirePoint.position;
                    pattern.ApplyLockOn(ref originalBulletRotation, targetDirection);
                }
                
                if(!pattern.isBurst)
                {
                    IndividualBulletShot(i, bulletFirePoint.position, originalBulletRotation);
                    StartCoroutine(IntervalShot(pattern.fireRate, (intervalTime) => {
                        bulletPatternState.BulletInterval = intervalTime;}));
                }
                    
                else
                {
                    if(bulletPatternState.BurstResetTime <= 0)
                    {
                        bulletPatternState.BurstResetTime = pattern.burstDuration;
                        bulletPatternState.BurstShotCount = pattern.BurstShotCount;

                        StartCoroutine(IntervalShot(bulletPatternState.BurstResetTime, (intervalTime) => {bulletPatternState.BurstResetTime = intervalTime;}));
                    }

                    if(bulletPatternState.BurstShotCount <= 0)
                        continue;

                    bulletPatternState.BurstShotCount--;
                    IndividualBulletShot(i, bulletFirePoint.position, originalBulletRotation);
                    StartCoroutine(IntervalShot(pattern.fireRate, (intervalTime) => {bulletPatternState.BulletInterval = intervalTime;}));
                }    
                
            }
            
        }

        private void MoveWithJobs()
        {
            for (int i = 0; i < patternMaster.patterns.Count; i++)
            {
                BulletJob bulletJob = new BulletJob
                {
                    deltaTime = Time.deltaTime,
                    bulletData = this.bulletDataCollection[i]
                };

                var jobHandle = bulletJob.Schedule(bulletTransformCollection[i]);
                jobHandle.Complete();
            }
            
        }
        private void IndividualBulletShot(int patternIndex, Vector3 point, Vector3 eulerRotation)
        {
            var pattern = patternMaster.patterns[patternIndex];
            for (int i = 0; i < pattern.bulletsPerShot; i++)
            {
                bool firstBullet = i == 0;
                    if(firstBullet)
                    {
                        ShootBullet(patternIndex, 
                        point, 
                        Quaternion.Euler(eulerRotation),
                         bulletOwner);
                        continue;
                    }

                    Vector3 arrayBulletRotation = Vector3.zero;
                    pattern.ApplyBulletDistribution(ref arrayBulletRotation, i);

                    ShootBullet(patternIndex,
                    point, 
                    Quaternion.Euler(eulerRotation + arrayBulletRotation), 
                    bulletOwner);
            }
        }
        private void ShootBullet(int patternIndex, Vector3 point, Quaternion rotation, Transform bulletOwner)
        {
            int index = bulletIndex[patternIndex]++ % patternMaster.patterns[patternIndex].maxBulletsPooled;

            var bullet = bulletPools[patternIndex].bullets[index];
            bullet.SetActive(true);
            bullet.transform.position = point;
            bullet.transform.rotation = rotation;

            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.BulletOwner = bulletOwner;

            bulletScript.bulletData.MovementInput.MoveVector = bullet.transform.right;
            bulletDataCollection[patternIndex][index] = bulletScript.bulletData;
            bulletTransformCollection[patternIndex][index].position = bullet.transform.position;
            bulletTransformCollection[patternIndex][index].rotation = bullet.transform.rotation;
            
        }
        private IEnumerator IntervalShot(float intervalTime, Action<float> callback)
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
            bulletBuilder.Dispose();
            foreach (var pool in bulletPools)
            {
                pool.Dispose();
                
            }
            foreach (var bulletTransforms in bulletTransformCollection)
            {
                //bulletTransforms.Dispose();
            }
            foreach (var bulletData in bulletDataCollection)
            {
                //bulletData.Dispose();
            }
            
        }
    }

    public class BulletDataBuilder 
    {
        private TransformAccessArray[] bulletTransformCollection;
        private NativeArray<BulletData>[] bulletDataCollection;
        private List<BulletPoolTemplate> bulletPoolTemplates;
        public TransformAccessArray[] GetTransformAccessArrays(int[] individualCapacity, int totalCapacity)
        {
            bulletTransformCollection = new TransformAccessArray[totalCapacity];

            for (int i = 0; i < bulletTransformCollection.Length; i++)
            {
                bulletTransformCollection[i] = new TransformAccessArray(individualCapacity[i]);
            }
            return bulletTransformCollection;
        }

        public NativeArray<BulletData>[] GetDataCollections(int[] individualCapacity, int totalCapacity, Allocator allocatorType)
        {
            bulletDataCollection = new NativeArray<BulletData>[totalCapacity];

            for (int i = 0; i < bulletDataCollection.Length; i++)
            {
                bulletDataCollection[i] = new NativeArray<BulletData>(individualCapacity[i], allocatorType);
            }
            
            return bulletDataCollection;
        }

        public List<BulletPoolTemplate> GetBulletPoolTemplates(BulletPatternMaster patternMaster)
        {
            bulletPoolTemplates = new List<BulletPoolTemplate>(patternMaster.patterns.Count);

            for (int i = 0; i < patternMaster.patterns.Count; i++)
            {
                var pattern = patternMaster.patterns[i];
                var poolTemplate = new BulletPoolTemplate(pattern.bulletPrefab, pattern.maxBulletsPooled);
                bulletPoolTemplates.Add(poolTemplate);
            }

            return bulletPoolTemplates;
        }

        public void Dispose()
        {
            foreach (var transformAccessArray in bulletTransformCollection)
            {
                transformAccessArray.Dispose();
            }
            foreach (var nativeArray in bulletDataCollection)
            {
                nativeArray.Dispose();
            }
        }
    }
}

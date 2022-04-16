using UnityEngine;
using System.Collections.Generic;
using System;

namespace CyberBuggy
{
    [CreateAssetMenu(fileName = "New Bullet Pattern", menuName = "Scriptable Objects/Bullet Pattern")]
    public class BulletPatternMaster : ScriptableObject
    {
        public List<BulletPattern> patterns; 

    }

    [Serializable]
    public class BulletPattern 
    {
        [Header("Common")]
        public GameObject bulletPrefab;
        public bool useLockOn;
        public float fireRate;
        public float spinRate;
        public float initialRotation;

        [Header("Accuracy Options")]
        [Range(0,1)] public float accuracy = 1;
        public Vector2 accuracyAngleRange;
        
        [Header("Bullet Distribution")]
        public int bulletsPerShot = 1;
        [Range(0, 360)]
        public float maxArrayAngleRange = 360;


        [Header("Burst Options")]
        public bool isBurst;
        public float burstDuration;
        public int BurstShotCount;

        [Header("Pooling")]
        public int maxBulletsPooled = 500;
        public void ApplySpin(ref Vector3 eulerRotation, in BulletPatternState patternState)
        {
            var convertedAccuracy = accuracy.Remap(0, 1, 1, 0);

            eulerRotation.z += 
            initialRotation 
            + patternState.bulletSpin
            + UnityEngine.Random.Range(accuracyAngleRange.x * convertedAccuracy, + accuracyAngleRange.y * convertedAccuracy);
        }

        public void ApplyLockOn(ref Vector3 eulerRotation, in Vector3 targetDirection)
        {
            var lockedOnRotation = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            eulerRotation.z += lockedOnRotation;
        }

        public void ApplyBulletDistribution(ref Vector3 eulerRotation, in int index)
        {
            if(maxArrayAngleRange == 360)
                eulerRotation = new Vector3(0, 0, maxArrayAngleRange / bulletsPerShot * index);
            else
                eulerRotation = new Vector3(0, 0, (maxArrayAngleRange / (bulletsPerShot - 1)) * index);
        }
    }

    [Serializable]
    public class BulletPatternState
    {
        public float BurstResetTime;
        public int BurstShotCount;
        public float BulletInterval;
        public float bulletSpin;
        public BulletPatternState(float resetTime, int shotCount, float interval)
        {
            BurstResetTime = resetTime;
            BurstShotCount = shotCount;
            BulletInterval = interval;
        }
    }


}
using UnityEngine;
using System;

namespace CyberBuggy.Bullets.Patterns
{
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
        [Range(0,1)] public float accuracyWeight = 1;
        public Vector2 spreadArcRange;
        
        [Header("Bullet Distribution")]
        public int bulletsPerShot = 1;
        [Range(0, 360)]
        public float arcRange = 360;


        [Header("Burst Options")]
        public bool usesBurst;
        public float burstDuration;
        public int burstShotCount;

        [Header("Pooling")]
        public int maxBulletsPooled = 500;
        public void ApplyLockOn(ref Vector3 eulerRotation, in Vector3 targetDirection)
        {
            var lockedOnRotation = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            eulerRotation.z += lockedOnRotation;
        }
        public void ApplySpin(ref Vector3 eulerRotation, in BulletPatternState patternState)
        {
            var convertedAccuracy = accuracyWeight.Remap(0, 1, 1, 0);

            eulerRotation.z += 
            initialRotation 
            + patternState.bulletSpin
            + UnityEngine.Random.Range(spreadArcRange.x * convertedAccuracy, + spreadArcRange.y * convertedAccuracy);
        }


        public void ApplyBulletDistribution(ref Vector3 eulerRotation, in int index)
        {
            if(arcRange == 360)
                eulerRotation = new Vector3(0, 0, arcRange / bulletsPerShot * index);
            else
                eulerRotation = new Vector3(0, 0, (arcRange / (bulletsPerShot - 1)) * index);
        }
    }


}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace CyberBuggy
{
    [Serializable] 
    public struct BulletSettings
    {
        public int bulletDamage;
        public float BulletLifeTime;

        [SerializeField]
        public MovementSettings MovementSettings;
    }

    [Serializable] 
    public struct BulletState
    {
        public MovementState MovementState;
    }

    [Serializable]
    public struct BulletData
    {
        public BulletState BulletState;
        public BulletSettings BulletSettings;

        public MovementInput MovementInput;
    }
    public class Bullet : MonoBehaviour
    {

        [Header("Settings")]

        [SerializeField] private bool usePooling;
        [SerializeField] private bool destroysOtherBullets;
        public BulletData bulletData;
        private Transform bulletOwner;
        public Transform BulletOwner { get => bulletOwner; set => bulletOwner = value;}
        private Coroutine bulletLifetimeCoroutine;
        private bool wasEnabledLastFrame;
        private bool enabledOnce;
        private float lifetime;
        private void Start()
        {
            lifetime = bulletData.BulletSettings.BulletLifeTime;
            if(!usePooling)
                bulletLifetimeCoroutine = StartCoroutine(LifetimeCountdown(lifetime));
            
        }
        private void Explode()
        {
            enabledOnce = true;

            if(bulletLifetimeCoroutine != null)
                StopCoroutine(bulletLifetimeCoroutine);
            
            if(!usePooling)
            {
                Destroy(gameObject);
                return;
            }
            gameObject.SetActive(false);
                
        }
        private IEnumerator LifetimeCountdown(float seconds)
        {
            while(seconds > 0)
            {
                seconds -= Time.deltaTime;
                lifetime = seconds;
                yield return null;
            }
            
            Explode();
        }
        private void OnTriggerEnter2D(Collider2D collider)
        {
            if(collider.isTrigger)
                return;
            
            if(collider.transform == bulletOwner)
                return;
            
            var hasCollidedWithAnotherBullet = collider.TryGetComponent(out Bullet collidedBullet);

            if(hasCollidedWithAnotherBullet)
            {
                if(!destroysOtherBullets)
                    return;
                
                if(collidedBullet.bulletOwner == bulletOwner)
                    return;
                
                collidedBullet.Explode();
                Explode();

                return;
            }

            var hasCollidedWithDamageable = collider.TryGetComponent(out IDamageable receiver);
            if(hasCollidedWithDamageable)
            {
                if (bulletOwner != null)
                {
                    var actor = bulletOwner.GetComponent<IActor>();

                    receiver.TryTakeDamage(bulletData.BulletSettings.bulletDamage, actor);
                }
                else
                    receiver.TryTakeDamage(bulletData.BulletSettings.bulletDamage, null);

            }

            if(bulletLifetimeCoroutine != null)
                StopCoroutine(bulletLifetimeCoroutine);
            Explode();
        }
        private void OnEnable()
        {
            if(usePooling)
            {
                if(bulletLifetimeCoroutine != null)
                    StopCoroutine(bulletLifetimeCoroutine);
                lifetime = bulletData.BulletSettings.BulletLifeTime;
                bulletLifetimeCoroutine = StartCoroutine(LifetimeCountdown(lifetime));
                
            }
            
            wasEnabledLastFrame = true;
        }

        private void OnDisable()
        {    
            wasEnabledLastFrame = false;
        }

        
    }

    [BurstCompile]
    public struct BulletJob : IJobParallelForTransform 
    {
        [ReadOnly] public float deltaTime;
        public NativeArray<BulletData> bulletData;
        public void Execute(int index, TransformAccess transform)
        {
            var data = bulletData[index];
            Movement.Move(ref data.BulletState.MovementState, data.BulletSettings.MovementSettings, data.MovementInput.MoveVector, deltaTime);
            transform.position += (Vector3)data.BulletState.MovementState.Velocity;
            Movement.SetPosition(ref data.BulletState.MovementState, transform.position);

        }
    }
}

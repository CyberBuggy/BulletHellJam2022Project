using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace CyberBuggy.Bullets
{
    public class Bullet : MonoBehaviour
    {

        [Header("Settings")]
        [SerializeField] private bool _usePooling;
        [SerializeField] private bool _destroysOtherBullets;
        [SerializeField] private bool _damagesOwner;
        public BulletData bulletData;
        private Transform _bulletOwner;
        public Transform BulletOwner { get => _bulletOwner; set => _bulletOwner = value;}
        private Coroutine _bulletLifetimeCoroutine;
        private float lifetime;
        private void Start()
        {
            lifetime = bulletData.BulletSettings.BulletLifeTime;
            if(!_usePooling)
                _bulletLifetimeCoroutine = StartCoroutine(Co_LifetimeCountdown(lifetime));
            
        }
        private void OnEnable()
        {
            if(_usePooling)
            {
                if(_bulletLifetimeCoroutine != null)
                    StopCoroutine(_bulletLifetimeCoroutine);
                lifetime = bulletData.BulletSettings.BulletLifeTime;
                _bulletLifetimeCoroutine = StartCoroutine(Co_LifetimeCountdown(lifetime));  
            }
        }
        private void Explode()
        {
            if(_bulletLifetimeCoroutine != null)
                StopCoroutine(_bulletLifetimeCoroutine);
            
            if(!_usePooling)
            {
                Destroy(gameObject);
                return;
            }
            gameObject.SetActive(false);
                
        }
        private IEnumerator Co_LifetimeCountdown(float seconds)
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
            
            if(_damagesOwner && collider.transform == _bulletOwner)
                return;
            
            var hasCollidedWithAnotherBullet = collider.TryGetComponent(out Bullet collidedBullet);

            if(hasCollidedWithAnotherBullet)
            {
                if(!_destroysOtherBullets)
                    return;
                
                if(collidedBullet.BulletOwner == _bulletOwner)
                    return;
                
                collidedBullet.Explode();
                Explode();

                return;
            }

            var hasCollidedWithDamageable = collider.TryGetComponent(out IDamageable receiver);
            if(hasCollidedWithDamageable)
            {
                if (_bulletOwner != null)
                {
                    var actor = _bulletOwner.GetComponent<IActor>();

                    receiver.TryTakeDamage(bulletData.BulletSettings.bulletDamage, actor);
                }
                else
                    receiver.TryTakeDamage(bulletData.BulletSettings.bulletDamage, null);

            }

            Explode();
        }
    }
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

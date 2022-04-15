using System.Collections.Generic;
using BulletFury.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BulletFury
{
    public enum ColliderShape {Sphere, AABB, OBB, Triangle}
    /// <summary>
    /// Tell an object to collide with bullets
    /// </summary>
    public class BulletCollider : MonoBehaviour
    {
        // the set of bullets this collider should collide with
        private List<BulletManager> _hitByBullets = new List<BulletManager>();
        
        [SerializeField] private LayerMask bulletLayersToBeHitBy;

        [SerializeField] private ColliderShape shape;
        
        // the radius of the sphere that describes this collider
        [SerializeField] private float radius = .5f;

        // the bounding box that describes this collider
        [SerializeField] private Vector3 size = Vector3.one;

        [SerializeField] private bool destroyBullet = true;
        // the offset of the collider
        [SerializeField] private Vector3 center;
        
        // the three points that make up the triangle
        [SerializeField] private Vector3 pointA;
        [SerializeField] private Vector3 pointB;
        [SerializeField] private Vector3 pointC;
        
        // Unity Event that fires when a bullet collides with this collider, can be set in the inspector like a button 
        // ReSharper disable once InconsistentNaming
        [SerializeField] private BulletCollisionEvent OnCollide;

        public BulletCollisionEvent OnCollideEvent => OnCollide;
        // cached job and job handle
        private BulletDistanceJob _bulletJobCircle;
        private BulletAABBJob _bulletJobAABB;
        private BulletOBBJob _bulletJobOBB;
        private BulletTriangleJob _bulletJobTriangle;
        
        private JobHandle _handle;

        // an array of bullets
        private BulletContainer[] _bullets;

        private static List<BulletCollider> _colliders;

        private void Awake()
        {
            if (_colliders == null)
                _colliders = new List<BulletCollider>();
            _colliders.Add(this);
        }

        private void OnEnable()
        {
            var managers = BulletManager.GetAllManagers();
            if (managers == null)
                return;
            for (int i = managers.Count - 1; i >= 0; i--)
            {
                if (managers[i] == null) continue;
                
                if (bulletLayersToBeHitBy == (bulletLayersToBeHitBy | (1 << managers[i].gameObject.layer)) && !_hitByBullets.Contains(managers[i]))
                    AddManagerToBullets(managers[i]);
            }
        }

        private void OnDisable()
        {
            var managers = BulletManager.GetAllManagers();
            if (managers == null)
                return;
            for (int i = managers.Count - 1; i >= 0; i--)
            {
                if (managers[i] == null)
                    continue;
                
                if (bulletLayersToBeHitBy == (bulletLayersToBeHitBy | (1 << managers[i].gameObject.layer)) && _hitByBullets.Contains(managers[i]))
                    RemoveManagerFromBullets(managers[i]);
            }
        }

        /// <summary>
        /// Unity function, called every frame
        /// Run the job, tell the bullet manager that the bullet has been collided with
        /// </summary>
        private void FixedUpdate()
        {
            if (!gameObject.activeInHierarchy) return;
            
            for (var index = _hitByBullets.Count - 1; index >= 0; index--)
            {
                if (index >= _hitByBullets.Count)
                    return;
                var manager = _hitByBullets[index];
                if (manager == null || !manager.enabled || !manager.gameObject.activeSelf ||
                    manager.GetBullets() == null)
                    continue;
                // grab the bullets in the bullet manager
                _bullets = manager.GetBullets();

                if (shape == ColliderShape.Sphere)
                {
                    // create the job
                    _bulletJobCircle = new BulletDistanceJob
                    {
                        In = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        Out = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        Distance = radius,
                        Position = transform.position + center
                    };

                    // run the job
                    _handle = _bulletJobCircle.Schedule(_bullets.Length, 256);
                    // make sure the job finished this frame
                    _handle.Complete();
                    // grab the results
                    _bulletJobCircle.Out.CopyTo(_bullets);
                    // dispose the native arrays
                    _bulletJobCircle.In.Dispose();
                    _bulletJobCircle.Out.Dispose();
                }
                else if (shape == ColliderShape.AABB)
                {
                    var bounds = new Bounds(transform.position + center, Vector3.Scale(transform.localScale, size));
                    // create the job
                    _bulletJobAABB = new BulletAABBJob
                    {
                        In = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        Out = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        BoxMin = bounds.min,
                        BoxMax = bounds.max
                    };

                    // run the job
                    _handle = _bulletJobAABB.Schedule(_bullets.Length, 256);
                    // make sure the job finished this frame
                    _handle.Complete();
                    
                    // grab the results
                    _bulletJobAABB.Out.CopyTo(_bullets);
                    // dispose the native arrays
                    _bulletJobAABB.In.Dispose();
                    _bulletJobAABB.Out.Dispose();
                } else if (shape == ColliderShape.OBB)
                {
                    var scaledSize = Vector3.Scale(transform.localScale, size);
                    
                    _bulletJobOBB = new BulletOBBJob
                    {
                        In = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        Out = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        BoxHalfExtents = new NativeArray<float>(new[] {scaledSize.x, scaledSize.y, scaledSize.z},
                            Allocator.TempJob),
                        BoxAxes = new NativeArray<float3>(
                            new float3[] {transform.right, transform.up, transform.forward}, Allocator.TempJob),
                        BoxCentre = transform.position + center
                    };
                    
                    // run the job
                    _handle = _bulletJobOBB.Schedule(_bullets.Length, 256);
                    // make sure the job finished this frame
                    _handle.Complete();
                    
                    // grab the results
                    _bulletJobOBB.Out.CopyTo(_bullets);
                    // dispose the native arrays
                    _bulletJobOBB.In.Dispose();
                    _bulletJobOBB.Out.Dispose();
                    _bulletJobOBB.BoxHalfExtents.Dispose();
                    _bulletJobOBB.BoxAxes.Dispose();
                }else if (shape == ColliderShape.Triangle)
                {
                    var scaledSize = Vector3.Scale(transform.localScale, size);
                    
                    _bulletJobTriangle = new BulletTriangleJob
                    {
                        In = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        Out = new NativeArray<BulletContainer>(_bullets, Allocator.TempJob),
                        a = LocalPointToWorld(pointA),
                        b = LocalPointToWorld(pointB),
                        c = LocalPointToWorld(pointC)
                    };
                    
                    // run the job
                    _handle = _bulletJobTriangle.Schedule(_bullets.Length, 256);
                    // make sure the job finished this frame
                    _handle.Complete();
                    
                    // grab the results
                    _bulletJobTriangle.Out.CopyTo(_bullets);
                    // dispose the native arrays
                    _bulletJobTriangle.In.Dispose();
                    _bulletJobTriangle.Out.Dispose();
                }

                // loop through the bullets, if there was a collision this frame - tell the bullet manager and anything else that needs to know
                for (int i = 0; i < _bullets.Length; i++)
                {
                    if (_bullets[i].Dead == 0 && _bullets[i].Collided == 1 && _bullets[i].CurrentSize > 0)
                    {
                        if (destroyBullet)
                            manager.HitBullet(i);
                        OnCollide?.Invoke(_bullets[i], this);
                    }
                }
            }
        }

        public void AddManagerToBullets(BulletManager manager)
        {
            _hitByBullets.Add(manager);
        }

        public void RemoveManagerFromBullets(BulletManager manager)
        {
            _hitByBullets.Remove(manager);
        }

        public static void AddManagerToColliders(BulletManager manager)
        {
            if (_colliders == null)
                return;
            foreach (var collider in _colliders)
            {
                if (collider.bulletLayersToBeHitBy == (collider.bulletLayersToBeHitBy | (1 << manager.gameObject.layer)) && !collider._hitByBullets.Contains(manager))
                    collider.AddManagerToBullets(manager);
            }
        }

        public static void RemoveManagerFromColliders(BulletManager manager)
        {
            if (_colliders == null)
                return;
            foreach (var collider in _colliders)
            {
                if (collider.bulletLayersToBeHitBy == (collider.bulletLayersToBeHitBy | (1 << manager.gameObject.layer)) && collider._hitByBullets.Contains(manager))
                    collider.RemoveManagerFromBullets(manager);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (shape == ColliderShape.Sphere)
                Gizmos.DrawWireSphere(transform.position + center, radius);
            else if (shape == ColliderShape.AABB)
                Gizmos.DrawWireCube(transform.position + center, Vector3.Scale(transform.localScale, size));
            else if (shape == ColliderShape.OBB)
            {
                var matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(center, Vector3.Scale(transform.localScale, size));
            } else if (shape == ColliderShape.Triangle)
            {
                var wPosA = LocalPointToWorld(pointA);
                var wPosB = LocalPointToWorld(pointB);
                var wPosC = LocalPointToWorld(pointC);
                
                Gizmos.DrawLine(wPosA, wPosB);
                Gizmos.DrawLine(wPosB, wPosC);
                Gizmos.DrawLine(wPosC, wPosA);
            }
        }

        private Vector3 LocalPointToWorld(Vector3 point)
        {
            return transform.position + transform.rotation * Vector3.Scale(point, transform.localScale);
        }
    }
}
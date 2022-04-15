using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace BulletFury.Data
{
    public enum BulletPlane {XY, XZ}
    
    /// <summary>
    /// Container for bullet settings
    /// </summary>
    [CreateAssetMenu(menuName = "BulletFury/Bullet Settings")]
    public class BulletSettings : ScriptableObject
    {
        #region Fields

        [SerializeField, Tooltip("the mesh to use for the bullet")] private Mesh mesh = null;
        // public accessor for the mesh - doing it this way means we can never set the mesh in code
        public Mesh Mesh => mesh;
        

        [SerializeField, Tooltip("the material to use for the bullet")] 
        private Material material = null;
        // public accessor for the material - doing it this way means we can never set the material in code
        public Material Material => material;

        [SerializeField] private BulletPlane plane = BulletPlane.XY;
        public BulletPlane Plane => plane;
        

        [SerializeField, Tooltip("wait until the bullet manager says go - used for bullet tracing")]
        private bool waitToStart = false;
        
 
        [SerializeField, Tooltip("the amount, in seconds, of the bullet's animation that should play before the bullet stops\nusing this means the bullet will spawn with an animation, rather than just not doing anything til the manager says go")] 
        private float timeToPlayWhileWaiting;
        

        [SerializeField, Tooltip("the amount of time, in seconds, that the bullet should stay alive")] 
        private float lifetime = 1;

        public float Lifetime => lifetime;
        [SerializeField, Tooltip("how quickly the bullet should move, in unity units per second")] 
        private float speed = 10;
        
        [SerializeField, Tooltip("move all of the bullets with the current object")] 
        private bool moveWithTransform = false;
        
        [FormerlySerializedAs("extraData")] [SerializeField] private float damage = 1;

        [SerializeField, Tooltip("how big, in unity units, the bullet should be\nthis affects scale - so a value of 1 correlates to a scale of (1,1,1)")] 
        private float size = 1;
        [SerializeField, Tooltip("how big, as a percentage of size, the bullet's collider should be.")] 
        private float colliderSize = 1;

        public float Size => size;
        public float ColliderSize => colliderSize;
        
        [SerializeField, Tooltip("the amount the bullet should rotate, in degrees per second")] 
        private float angularVelocity = 0;
        

        [SerializeField, Tooltip("the start colour of the bullet"), ColorUsage(true, true)] 
        private Color startColor = Color.white;

        public Color StartColor => startColor;

        [SerializeField, Tooltip("colour the bullet over time (multiplied with the start colour)")] 
        private bool useColorOverTime = false;
        [SerializeField, Tooltip("Gradient's length is the total lifetime of the bullet, or a set time")]
        private CurveUsage colorCurveUsage;

        [SerializeField, Tooltip("Length of time the gradient describes")] 
        private float colorCurveTime;
        [SerializeField, GradientUsage(true)] private Gradient colorOverTime = null;


        [SerializeField, Tooltip("rotate the bullet over time")] private bool useRotationOverTime = false;
        [SerializeField, Tooltip("Curve's length is the total lifetime of the bullet, or a set time")]
        private CurveUsage rotationCurveUsage;

        [SerializeField, Tooltip("Length of time the curve describes")] 
        private float rotationCurveTime;
        [SerializeField] private AnimationCurve rotationOverTime = null;
        [SerializeField] private bool useRotationForDirection = true;

        [SerializeField, Tooltip("scale the bullet over time")] private bool useSizeOverTime = false;
        [SerializeField, Tooltip("Curve's length is the total lifetime of the bullet, or a set time")]
        private CurveUsage sizeCurveUsage;

        [SerializeField, Tooltip("Length of time the curve describes")] 
        private float sizeCurveTime;
        [SerializeField] private AnimationCurve sizeOverTime = null;
        

        [SerializeField, Tooltip("change velocity over time")] 
        private bool useVelocityOverTime = false;
        [SerializeField, Tooltip("local: use the bullet's forward/up/right axes rather than world axes\nworld: use the world forward/up/right axes rather than the object's local axes")] 
        private ForceSpace velocitySpace = ForceSpace.Local;

        [SerializeField, Tooltip("scale with speed or use the absolute value of the animation curve")] 
        private bool scaleWithSpeed = false;

        [SerializeField, Tooltip("Curve's length is the total lifetime of the bullet, or a set time")]
        private CurveUsage velocityCurveUsage;

        [SerializeField, Tooltip("Length of time the curve describes")] 
        private float velocityCurveTime;
        [SerializeField] 
        private Vector3 velocityScaleInDirection = Vector3.one;
        [SerializeField] private AnimationCurve velocityOverTimeX = AnimationCurve.Linear(0,0,1, 0);
        [SerializeField] private AnimationCurve velocityOverTimeY = AnimationCurve.Linear(0,0,1, 0);
        [SerializeField] private AnimationCurve velocityOverTimeZ = AnimationCurve.Linear(0,0,1, 0);

        [SerializeField] private bool trackObject;
        [SerializeField] private string trackedObjectTag;
        [SerializeField] private float turnSpeed;
        
        [SerializeField, Tooltip("add force over time. N.B. this is ADDITIVE, so it'll get fast quickly")] 
        private bool useForceOverTime = false;
        [SerializeField, Tooltip("Curve's length is the total lifetime of the bullet, or a set time")]
        private CurveUsage forceCurveUsage;

        [SerializeField, Tooltip("")] 
        private float forceCurveTime;
        [SerializeField] 
        private Vector3 forceScaleInDirection = Vector3.one;
        [SerializeField, Tooltip("local: use the bullet's forward/up/right axes rather than world axes\nworld: use the world forward/up/right axes rather than the object's local axes")] 
        private ForceSpace forceSpace = ForceSpace.Local;
        [SerializeField] private AnimationCurve forceOverTimeX = AnimationCurve.Linear(0,0,1, 0);
        [SerializeField] private AnimationCurve forceOverTimeY = AnimationCurve.Linear(0,0,1, 0);
        [SerializeField] private AnimationCurve forceOverTimeZ = AnimationCurve.Linear(0,0,1, 0);
#if UNITY_EDITOR
        [SerializeField] private bool isExpanded;
#endif
        #endregion

        private Dictionary<int, Transform> _trackedObjects = new Dictionary<int, Transform>();
        
        public void SetTrackedObject(ref BulletContainer b, Transform obj)
        {
            if (!_trackedObjects.ContainsKey(b.Id))
                _trackedObjects.Add(b.Id, obj);
            else
                _trackedObjects[b.Id] = obj;
        }

        public void Setup()
        {
            _trackedObjects = new Dictionary<int, Transform>();
        }
        
        /// <summary>
        /// Initialise the bullet
        /// </summary>
        /// <param name="position">the starting value, as a percentage of the number of bullets spawned this cycle</param>
        /// <param name="bullet">a reference to the current bullet</param>
        public void Init(ref BulletContainer bullet)
        {
            bullet.Lifetime = lifetime;
            bullet.AngularVelocity = angularVelocity;
            bullet.Speed = speed;
            bullet.CurrentSpeed = speed;
            bullet.StartSize = size;
            bullet.CurrentSize = bullet.StartSize;
            bullet.StartColor = startColor;
            bullet.Color = bullet.StartColor;
            bullet.RotationChangedThisFrame = 0;
            bullet.Waiting = waitToStart ? (byte) 1 : (byte) 0;
            bullet.TimeToWait = timeToPlayWhileWaiting;
            bullet.Forward = bullet.Rotation * Vector3.forward;
            bullet.Right = bullet.Rotation * Vector3.right;
            bullet.Up = bullet.Rotation * Vector3.up;
            if (trackObject)
            {
                if (!_trackedObjects.ContainsKey(bullet.Id))
                    _trackedObjects.Add(bullet.Id, null);
                
                _trackedObjects[bullet.Id] = GameObject.FindWithTag(trackedObjectTag)?.transform;
            }
            bullet.TrackObject = trackObject ? (byte) 1 : (byte) 0;
            bullet.Damage = damage;
        }

        
        /// <summary>
        /// Set the values of the bullet
        /// </summary>
        /// <param name="bullet">the current bullet</param>
        /// <param name="deltaTime">cached Time.deltaTime</param>
        public void SetValues(ref BulletContainer bullet, float deltaTime, Vector3 position, Vector3 previousPosition, bool managerIsActive)
        {
            // if the bullet is dead or waiting, don't do anything
            if (bullet.Dead == 1 || (bullet.Waiting == 1 && bullet.CurrentLifeSeconds > bullet.TimeToWait))
                return;

            if (!managerIsActive)
                bullet.Dead = 1;

            // change the colour over time
            if (useColorOverTime)
            {
                
                bullet.Color = bullet.StartColor * colorOverTime.Evaluate(colorCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % colorCurveTime / colorCurveTime);
            }

            // if we've got some extra angular velocity, rotate the bullet over time and mark the rotation as changed
            if (useRotationOverTime)
            {
                // if we've got some angular velocity, rotate the bullet over time and mark the rotation as changed
                if (Mathf.Abs(bullet.AngularVelocity) > 0)
                {
                    
                    if (plane == BulletPlane.XY)
                        bullet.Rotation *= Quaternion.Euler(0, 0, bullet.AngularVelocity * deltaTime);
                    else
                        bullet.Rotation *= Quaternion.Euler(0, bullet.AngularVelocity * deltaTime, 0);
                }
                    
                if (plane == BulletPlane.XY)
                    bullet.Rotation *= Quaternion.Euler(0, 0, rotationOverTime.Evaluate(rotationCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % rotationCurveTime / rotationCurveTime) * deltaTime);
                else
                    bullet.Rotation *= Quaternion.Euler(0, rotationOverTime.Evaluate(rotationCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % rotationCurveTime / rotationCurveTime) * deltaTime, 0);
                
                bullet.RotationChangedThisFrame = 1;
            }

            // if the bullet's rotation has changed this frame, calculate the forward/right/up axes for the bullet
            if (bullet.RotationChangedThisFrame == 1)
            {
                bullet.Forward = bullet.Rotation * Vector3.forward;
                bullet.Right = bullet.Rotation * Vector3.right;
                bullet.Up = bullet.Rotation * Vector3.up;
            }

            if (useRotationOverTime && bullet.TrackObject == 1 && _trackedObjects.ContainsKey(bullet.Id) && _trackedObjects[bullet.Id] != null)
            {
                var target = _trackedObjects[bullet.Id].position - (Vector3)bullet.Position;
                if (plane == BulletPlane.XY)
                    bullet.Up = Vector3.RotateTowards(bullet.Up, target, turnSpeed * deltaTime, 0.0f);
                else 
                    bullet.Forward = Vector3.RotateTowards(bullet.Forward, target, turnSpeed * deltaTime, 0.0f);

                bullet.Rotation = Quaternion.LookRotation(bullet.Forward, bullet.Up);
            }

            // change the size over time
            if (useSizeOverTime)
                bullet.CurrentSize = bullet.StartSize * sizeOverTime.Evaluate(sizeCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % sizeCurveTime / sizeCurveTime);

            bullet.ColliderSize = bullet.CurrentSize * colliderSize / 2f;
 
            // move the bullet forwards
            if (useRotationForDirection)
                bullet.Velocity = bullet.Rotation * (Plane == BulletPlane.XY ? Vector3.up : Vector3.forward);
            else
                bullet.Velocity = bullet.Direction * (Plane == BulletPlane.XY ? Vector3.up : Vector3.forward);

            if (moveWithTransform)
                bullet.Position += (float3)(position - previousPosition);

            // change the velocity over time
            if (useVelocityOverTime)
            {
                // if the force space is local, use the bullet's local axes
                if (velocitySpace == ForceSpace.Local)
                {
                    if (useRotationForDirection)
                    {
                        bullet.Velocity = bullet.Right * velocityOverTimeX.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.x +
                                          bullet.Up * velocityOverTimeY.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.y +
                                          bullet.Forward * velocityOverTimeZ.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.z;
                    }
                    else
                    {
                        bullet.Velocity = (bullet.Direction * Vector3.right) * velocityOverTimeX.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.x +
                                          (bullet.Direction * Vector3.up) * velocityOverTimeY.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.y +
                                          (bullet.Direction * Vector3.forward) * velocityOverTimeZ.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.z;
                    }
                }
                else // if the force space is world, use the world axes
                {
                    bullet.Velocity = new float3(
                        velocityOverTimeX.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.x,
                        velocityOverTimeY.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.y,
                        velocityOverTimeZ.Evaluate(velocityCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % velocityCurveTime / velocityCurveTime) * velocityScaleInDirection.z
                    );
                }

                // if we're scaling with the speed value, do that
                if (scaleWithSpeed)
                    bullet.Velocity *= bullet.CurrentSpeed;
            }
            else // if we're not using velocity over time, we definitely want to scale with speed;
                bullet.Velocity *= bullet.CurrentSpeed;

            // add force over time
            if (useForceOverTime)
            {
                // if the force space is local, use the bullet's local axes 
                if (forceSpace == ForceSpace.Local)
                {
                    bullet.Force += bullet.Right * forceOverTimeX.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.x +
                                   bullet.Up * forceOverTimeY.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.y +
                                   bullet.Forward * forceOverTimeZ.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.z;
                }
                else // if the force space is world, use the world axes
                {
                    bullet.Force += new float3(
                        forceOverTimeX.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.x,
                        forceOverTimeY.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.y,
                        forceOverTimeZ.Evaluate(forceCurveUsage == CurveUsage.Lifetime ? bullet.CurrentLifePercent : bullet.CurrentLifeSeconds % forceCurveTime / forceCurveTime) * forceScaleInDirection.z
                    );
                }
            }
            
            bullet.RotationChangedThisFrame = 0;
        }
    }
}
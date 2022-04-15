using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace BulletFury.Data
{
    /// <summary>
    /// Container for bullet data, to be used for rendering
    /// </summary>
    #if !UNITY_EDITOR
    [BurstCompile]
    #endif
    public struct BulletContainer
    {
        public int Id;
        public float3 Position;
        public float CurrentSize;
        public float ColliderSize;
        public float StartSize;
        public byte Waiting;
        public float TimeToWait;
        public byte Collided;
        public Color Color;
        public Color StartColor;
        public Quaternion Rotation;
        public byte RotationChangedThisFrame;
        public float3 Forward;
        public float3 Right;
        public float3 Up;
        public float CurrentLifePercent;
        public float CurrentLifeSeconds;
        public byte Dead;
        public byte EndOfLife;
        public float Lifetime;
        public float AngularVelocity;
        public float Speed;
        public float CurrentSpeed;
        public float3 Velocity;
        public float3 Force;
        public float Damage;
        public byte TrackObject;
        public Quaternion Direction;
    }
}
using BulletFury.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BulletFury
{
    /// <summary>
    /// A C# job that checks for collisions between bullet colliders and bullets
    /// </summary>
#if !UNITY_EDITOR
    [BurstCompile]
#endif
    public struct BulletDistanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BulletContainer> In;
        [WriteOnly] public NativeArray<BulletContainer> Out;
        [ReadOnly] public float Distance;
        [ReadOnly] public float3 Position;
        
        public void Execute(int index)
        {
            var container = In[index];
            if (container.Dead == 1)
            {
                Out[index] = container;
                return;
            }

            var dist = math.distancesq(Position, container.Position); 

            container.Collided = dist <= (Distance + container.ColliderSize) * (Distance + container.ColliderSize) ? (byte) 1 : (byte) 0;
            Out[index] = container;
        }
    }

#if !UNITY_EDITOR
    [BurstCompile]
#endif
    public struct BulletAABBJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BulletContainer> In;
        [WriteOnly] public NativeArray<BulletContainer> Out;
        [ReadOnly] public float3 BoxMin;
        [ReadOnly] public float3 BoxMax;

        public void Execute(int index)
        {
            var container = In[index];
            if (container.Dead == 1)
            {
                Out[index] = container;
                return;
            }

            var x = math.max(BoxMin.x, math.min(container.Position.x, BoxMax.x));
            var y = math.max(BoxMin.y, math.min(container.Position.y, BoxMax.y));
            var z = math.max(BoxMin.z, math.min(container.Position.z, BoxMax.z));

            var sqrDist = (x - container.Position.x) * (x - container.Position.x) +
                          (y - container.Position.y) * (y - container.Position.y) +
                          (z - container.Position.z) * (z - container.Position.z);

            container.Collided = sqrDist <= (container.ColliderSize * container.ColliderSize) ? (byte) 1 : (byte) 0;
            Out[index] = container;
        }
    }
    
#if !UNITY_EDITOR
    [BurstCompile]
#endif
    public struct BulletOBBJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BulletContainer> In;
        [WriteOnly] public NativeArray<BulletContainer> Out;
        [ReadOnly] public float3 BoxCentre;
        [ReadOnly] public NativeArray<float> BoxHalfExtents;
        [ReadOnly] public NativeArray<float3> BoxAxes;
        public void Execute(int index)
        {
            var container = In[index];
            if (container.Dead == 1)
            {
                Out[index] = container;
                return;
            }

            var point = ClosestPoint(container.Position);
            float3 v = point - container.Position;
            container.Collided = math.dot(v, v) <= (container.ColliderSize * container.ColliderSize) ? (byte) 1 : (byte) 0;
            Out[index] = container;
        }

        private float3 ClosestPoint(float3 pos)
        {
            float3 d = pos - BoxCentre;
            var q = BoxCentre;

            for (int i = 0; i < 3; i++)
            {
                float dist = math.dot(d, BoxAxes[i]);
                var boxHalfExtent = BoxHalfExtents[i]/2f;
                if (dist > boxHalfExtent) dist = boxHalfExtent;
                if (dist < -boxHalfExtent) dist = -boxHalfExtent;

                q += dist * BoxAxes[i];
            }

            return q;
        }
    }
    
#if !UNITY_EDITOR
    [BurstCompile]
#endif
    public struct BulletTriangleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BulletContainer> In;
        [WriteOnly] public NativeArray<BulletContainer> Out;
        [ReadOnly] public float3 a;
        [ReadOnly] public float3 b;
        [ReadOnly] public float3 c;
        
        public void Execute(int index)
        {
            var container = In[index];
            if (container.Dead == 1)
            {
                Out[index] = container;
                return;
            }

            var point = ClosestPoint(container.Position);
            float3 v = point - container.Position;
            container.Collided = math.dot(v, v) <= (container.ColliderSize * container.ColliderSize) ? (byte) 1 : (byte) 0;
            Out[index] = container;
        }

        private float3 ClosestPoint(float3 p)
        {
            var ab = b - a;
            var ac = c - a;
            var bc = c - b;

            float snom = math.dot(p - a, ab);
            float sdenom = math.dot(p - b, a - b);

            float tnom = math.dot(p - a, ac);
            float tdenom = math.dot(p - c, a - c);

            if (snom <= 0.0f && tnom <= 0.0f) return a;

            float unom = math.dot(p - b, bc);
            float udenom = math.dot(p - c, b - c);

            if (sdenom <= 0.0f && unom <= 0.0f) return b;
            if (tdenom <= 0.0f && udenom <= 0.0f) return c;

            var n = math.cross(b - a, c - a);
            var vc = math.dot(n, math.cross(a - p, b - p));

            if (vc <= 0 && snom >= 0.0f && sdenom >= 0.0f)
                return a + snom / (snom + sdenom) * ab;

            float va = math.dot(n, math.cross(b - p, c - p));

            if (va <= 0.0f && unom >= 0.0f && udenom >= 0.0f)
                return b + unom / (unom + udenom) * bc;

            float vb = math.dot(n, math.cross(c - p, a - p));

            if (vb <= 0.0f && tnom >= 0.0f && tdenom >= 0.0f)
                return a + tnom / (tnom + tdenom) * ac;

            float u = va / (va + vb + vc);
            float v = vb / (va + vb + vc);
            float w = 1.0f - u - v;
            return u * a + v * b + w * c;
        }
    }
}
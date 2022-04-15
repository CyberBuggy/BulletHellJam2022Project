using BulletFury.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BulletFury
{
    /// <summary>
    /// A C# job that moves all bullets based on their velocity and current force
    /// </summary>
#if !UNITY_EDITOR
    [BurstCompile]
#endif
    public struct BulletJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BulletContainer> In;
        [WriteOnly] public NativeArray<BulletContainer> Out;
        [ReadOnly] public float DeltaTime;
        
        public void Execute(int index)
        {
            var container = In[index];

            if (container.Dead == 1 || container.Waiting == 1 && container.CurrentLifeSeconds > container.TimeToWait)
                return;

            container.CurrentLifeSeconds += DeltaTime;
            if (container.CurrentLifeSeconds > container.Lifetime)
            {
                container.Dead = 1;
                container.EndOfLife = 1;

                Out[index] = container;
                return;
            }

            container.CurrentLifePercent = container.CurrentLifeSeconds / container.Lifetime;
            container.Position += container.Velocity * DeltaTime +
                                  container.Force * DeltaTime;

           
            container.Rotation =  math.normalize(container.Rotation);
            Out[index] = container;
        }
    }
}
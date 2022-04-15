using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BulletFury.Rendering
{
    public class BulletFuryRenderPass : ScriptableRenderPass
    {
        // ReSharper disable once InconsistentNaming
        public static SortedList<int, CommandBuffer> Buffers;

        public BulletFuryRenderPass(RenderPassEvent pass)
        {
            renderPassEvent = pass;
            if (Buffers == null)
                Buffers = new SortedList<int, CommandBuffer>();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < Buffers.Count; i++)
            {
                context.ExecuteCommandBuffer(Buffers[Buffers.Keys[i]]);
                Buffers[Buffers.Keys[i]].Release();
            }
            Buffers.Clear();
        }
    }
}
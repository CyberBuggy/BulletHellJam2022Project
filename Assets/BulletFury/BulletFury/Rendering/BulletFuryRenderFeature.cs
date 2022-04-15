using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BulletFury.Rendering
{
    public class BulletFuryRenderFeature : ScriptableRendererFeature
    {
        [SerializeField, Tooltip("When to render the bullets - default is \"Before rendering post processing\" so the bullets are drawn on top of everything else.")] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public override void Create()
        {
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(new BulletFuryRenderPass(renderPassEvent));
        }
    }
}
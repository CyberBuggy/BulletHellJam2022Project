using BulletFury.Data;
using UnityEngine;

namespace BulletFury
{
    public class ParticleOnBulletSpawned : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particle;

        public void OnBulletSpawned(int idx, BulletContainer bullet)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = bullet.Position
            };
            
            particle.Emit(emit, 1);
        }
    }
}
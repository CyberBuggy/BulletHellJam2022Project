using UnityEngine;

namespace Utils
{
    public class SoundOnBulletSpawn : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private AudioSource source;

        public void BulletSpawn()
        {
            if (source.enabled)
                source.PlayOneShot(clip);
        }
    }
}

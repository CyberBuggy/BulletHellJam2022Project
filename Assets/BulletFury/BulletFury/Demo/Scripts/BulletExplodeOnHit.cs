using System.Collections;
using BulletFury.Data;
using UnityEngine;

namespace BulletFury.Demo
{
    public class BulletExplodeOnHit : MonoBehaviour
    { 
        [SerializeField] private Transform explosionPrefab = null;
        [SerializeField] private float timeToShrink = 0.5f;
        [SerializeField] private Vector3 scaleMin = Vector3.one, scaleMax = Vector3.one;
        [SerializeField] private float minRandomRange = 0.25f, maxRandomRange = 0.5f;
        public void OnBulletDie(int idx, BulletContainer bullet, bool endOfLife)
        {
            if (endOfLife) return;
            
            StartCoroutine(PlayExplosion(bullet.Position));
        }

        private IEnumerator PlayExplosion(Vector3 position)
        {
            var explosion = Instantiate(explosionPrefab, transform);
            explosion.localScale = explosionPrefab.localScale;
            explosion.rotation = explosionPrefab.rotation;

            var rndScale = Random.Range(0f, 1f);
            var scale = Vector3.Lerp(scaleMin, scaleMax, rndScale);
            
            var rnd = Random.onUnitSphere * Random.Range(minRandomRange, maxRandomRange);
            rnd.y = 0;
            explosion.transform.position = position + rnd;
            
            var timer = 0f;

            while (timer < timeToShrink)
            {
                var t = timer / timeToShrink;
                explosion.transform.localScale = Vector3.Lerp(scale, Vector3.zero, t*t*t);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            Destroy(explosion.gameObject);

        }
    }
}
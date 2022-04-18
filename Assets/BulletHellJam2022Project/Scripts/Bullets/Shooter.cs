using CyberBuggy.Bullets.Patterns;
using UnityEngine;

namespace CyberBuggy.Bullets
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private BulletPatternBehaviour _bulletPatternBehaviour;

        [SerializeField] private Transform _firePoint;
        [SerializeField] private Transform _testTarget;

        private void Start()
        {
            _bulletPatternBehaviour.BulletFirePoint = _firePoint;
            _bulletPatternBehaviour.BulletTarget = _testTarget;
        }

        private void FixedUpdate()
        {
            _bulletPatternBehaviour.SpawnBullets();
        }
    }
}

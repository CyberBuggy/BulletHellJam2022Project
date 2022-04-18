using System.Collections.Generic;
using CyberBuggy.Bullets.Pooling;
using Unity.Collections;
using UnityEngine.Jobs;

namespace CyberBuggy.Bullets.Patterns
{
    public class BulletDataBuilder 
    {
        private TransformAccessArray[] _bulletTransformCollection;
        private NativeArray<BulletData>[] _bulletDataCollection;
        private List<BulletPoolTemplate> _bulletPoolTemplates;
        public TransformAccessArray[] GetTransformAccessArrays(int[] individualCapacity, int totalCapacity)
        {
            _bulletTransformCollection = new TransformAccessArray[totalCapacity];

            for (int i = 0; i < _bulletTransformCollection.Length; i++)
            {
                _bulletTransformCollection[i] = new TransformAccessArray(individualCapacity[i]);
            }
            return _bulletTransformCollection;
        }

        public NativeArray<BulletData>[] GetDataCollections(int[] individualCapacity, int totalCapacity, Allocator allocatorType)
        {
            _bulletDataCollection = new NativeArray<BulletData>[totalCapacity];

            for (int i = 0; i < _bulletDataCollection.Length; i++)
            {
                _bulletDataCollection[i] = new NativeArray<BulletData>(individualCapacity[i], allocatorType);
            }
            
            return _bulletDataCollection;
        }

        public List<BulletPoolTemplate> GetBulletPoolTemplates(BulletPatternMaster _patternMaster)
        {
            _bulletPoolTemplates = new List<BulletPoolTemplate>(_patternMaster.patterns.Count);

            for (int i = 0; i < _patternMaster.patterns.Count; i++)
            {
                var pattern = _patternMaster.patterns[i];
                var poolTemplate = new BulletPoolTemplate(pattern.bulletPrefab, pattern.maxBulletsPooled);
                _bulletPoolTemplates.Add(poolTemplate);
            }

            return _bulletPoolTemplates;
        }

        public void Dispose()
        {
            foreach (var transformAccessArray in _bulletTransformCollection) transformAccessArray.Dispose();
            foreach (var nativeArray in _bulletDataCollection) nativeArray.Dispose();
        }
    }
}

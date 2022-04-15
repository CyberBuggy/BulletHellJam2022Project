using System;
using BulletFury.Utils;
using UnityEngine;

namespace BulletFury.Data
{
    /// <summary>
    /// Container for the bullet spawning settings
    /// </summary>
    [CreateAssetMenu(menuName = "BulletFury/Spawn Settings")]
    public class SpawnSettings : ScriptableObject
    {
        
        [SerializeField, Tooltip("how often the bullet should fire, in seconds")] private float fireRate = 0.1f;
        // public accessor for the fire rate, so we can't change it in code
        public float FireRate => fireRate;

        
        [SerializeField, Tooltip("the number of bursts each shot should fire")] private int burstCount = 1;
        // public accessor
        public int BurstCount => burstCount;
        
        
        [SerializeField, Tooltip("the delay between burst shots")] private float burstDelay = 0.1f;
        // public accessor
        public float BurstDelay => burstDelay;
        
        
        [SerializeField, Tooltip("the method by which we decide what direction to spawn the bullets in")] 
        private SpawnDir spawnDir;

        [SerializeField, Range(0, 360), Tooltip("The arc to use for random directional spawning")]
        private float directionArc;

        [SerializeField, Tooltip("spawn points randomly rather than in a specific shape?")]
        private bool randomise;
        
        [SerializeField, Tooltip("spawn all the random points at the radius of the shape?")] 
        private bool onEdge;
        
         
        [SerializeField, Tooltip("the amount of sides the spawn shape should have")] private int numSides;
        
        [SerializeField, Tooltip("the number of bullets per side the shape should have")] private int numPerSide;
        
        [SerializeField, Tooltip("the radius of the shape")] private float radius;

        
        // the arc of the shape
        [SerializeField, Range(0, 360)] private float arc;
        #if UNITY_EDITOR
        [SerializeField] private bool isExpanded;
        #endif
        /// <summary>
        /// Get a point based on the spawning settings
        /// </summary>
        /// <param name="onGetPoint"> a function to run for every point that has been found </param>
        public void Spawn(Action<Vector2, Vector2> onGetPoint, Squirrel3 rnd)
        {
            // initialise the array
            var points = new Vector2[numSides];
            // take a first pass and add some points to every side

            var offset = arc / (2 * numSides) - ((0.5f * arc)) + 90f;
            var anglePerSide = arc / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = (!randomise ? i * anglePerSide : rnd.Next() * arc) + offset;  

                angle *= Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // set the direction based on the spawnDir enum
                Vector2 dir;
                switch (spawnDir)
                {
                    case SpawnDir.Directional:
                        dir = points[i];
                        break;
                    case SpawnDir.Randomised:
                        dir = Vector2.up;
                        break;
                    case SpawnDir.Spherised:
                        dir = points[i];
                        break;
                    default:
                        dir = Vector2.up;
                        break;
                }

                if (spawnDir == SpawnDir.Randomised)
                {
                    var rndAngle = rnd.Next() * directionArc * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(rndAngle), Mathf.Sin(rndAngle));
                }
                
                if ((randomise || spawnDir == SpawnDir.Randomised) && !onEdge)
                    points[i] *= rnd.Next();
                    

                if (numPerSide == 1)
                    onGetPoint?.Invoke(points[i] * radius, dir);
            }

            if (numPerSide == 1)
                return;

            // for every side
            for (int i = 0; i < numSides; ++i)
            {
                // get the next position
                var next = i + 1;
                if (next == numSides)
                    next = 0;

                // the normal of the current side
                var direction = Vector2.Lerp(points[i], points[next], 0.5f).normalized;

                // for every bullet we should spawn on this side of the shape
                for (int j = 0; j < numPerSide; ++j)
                {
                    // position the current point a percentage of the way between each end of the side
                    var t = j / (float) numPerSide;
                    t += (1f / numPerSide) / 2f;
                    var point = Vector2.Lerp(points[i], points[next], t);
                    point *= radius;

                    Vector2 dir;
                    switch (spawnDir)
                    {
                        case SpawnDir.Directional:
                            dir = direction;
                            break;
                        case SpawnDir.Randomised:
                            var rndAngle = rnd.Next() * directionArc * Mathf.Deg2Rad;
                            dir = new Vector2(Mathf.Cos(rndAngle), Mathf.Sin(rndAngle));
                            break;
                        case SpawnDir.Spherised:
                            dir = point.normalized;
                            break;
                        default:
                            dir = Vector2.up;
                            break;
                    }
                    
                    // tell function what the point and direction is 
                    onGetPoint?.Invoke(point, dir);
                }
            }
        }
    }
}
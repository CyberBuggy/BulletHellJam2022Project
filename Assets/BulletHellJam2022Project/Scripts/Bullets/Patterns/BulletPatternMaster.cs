using UnityEngine;
using System.Collections.Generic;

namespace CyberBuggy.Bullets.Patterns
{
    [CreateAssetMenu(fileName = "New Bullet Pattern", menuName = "Scriptable Objects/Bullet Pattern")]
    public class BulletPatternMaster : ScriptableObject
    {
        public List<BulletPattern> patterns; 
    }

}
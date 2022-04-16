using UnityEngine;
using System.Collections.Generic;

namespace CyberBuggy
{
    public interface IShootBehaviour
    {
        public bool ShootingEnabled {get; set;}
        void OnShootTrigger(Transform bulletFirePoint, Transform holder, Transform target = null);


    }
}

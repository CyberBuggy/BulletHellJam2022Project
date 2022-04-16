using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KaitoCo
{
    public class Weapon : MonoBehaviour
    {
        public Action OnWeaponUse;

        private bool isFiring;
        public bool IsFiring { get => isFiring; set => isFiring = value; }

        public virtual void Use(Transform target = null)
        {
            
        }


        public virtual void Stop()
        {

        }
    }
}

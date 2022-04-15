using System;
using System.Collections;
using UnityEngine;

namespace Utils
{
    public static class WaitUtils
    {
        public static void Wait(float time, bool scaled, Action onComplete)
        {
            WaitIE(time, scaled, onComplete).Run();
        }

        public static IEnumerator WaitIE(float time, bool scaled, Action onComplete)
        {
            if (scaled)
                yield return new WaitForSeconds(time);
            else
                yield return new WaitForSecondsRealtime(time);
            
            onComplete.Invoke();
        }
    }
}
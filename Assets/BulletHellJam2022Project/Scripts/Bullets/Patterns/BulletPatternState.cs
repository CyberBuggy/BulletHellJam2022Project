using System;

namespace CyberBuggy.Bullets.Patterns
{
    [Serializable]
    public class BulletPatternState
    {
        public float BurstResetTime;
        public int BurstShotCount;
        public float BulletInterval;
        public float bulletSpin;
        public BulletPatternState(float resetTime, int shotCount, float interval)
        {
            BurstResetTime = resetTime;
            BurstShotCount = shotCount;
            BulletInterval = interval;
        }
    }


}
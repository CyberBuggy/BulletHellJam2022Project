using System.Collections;

namespace Utils
{
    public static class EnumeratorExtensions
    {
        /// <summary>
        ///     Shorthand to run a coroutine: IEnumeratorFunction().Run(); 
        /// </summary>
        public static void Run(this IEnumerator toRun)
        {
            Runner.Run(toRun);
        }
    }
}
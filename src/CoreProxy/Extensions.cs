using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace jnm2.CoreProxy
{
    internal static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this Task task) { }
    }
}

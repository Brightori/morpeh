﻿#if MORPEH_BURST
namespace Morpeh.Native {
    using System.Runtime.CompilerServices;
    using Morpeh;

    public static class NativeComponentsCacheExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeCache<TNative> AsNative<TNative>(this ComponentsCache<TNative> cache) where TNative : unmanaged, IComponent {
            var nativeCache = new NativeCache<TNative> {
                components = cache.components.AsNative(),
            };
            return nativeCache;
        }
    }
}
#endif
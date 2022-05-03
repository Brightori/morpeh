﻿#if MORPEH_BURST
namespace Morpeh.Native {
    using System.Runtime.CompilerServices;
    using Collections;

    public static class NativeIntHashMapExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe NativeIntHashMap<TNative> AsNative<TNative>(this IntHashMap<TNative> hashMap) where TNative : unmanaged {
            var nativeIntHashMap = new NativeIntHashMap<TNative>();
            
            fixed (int* lengthPtr = &hashMap.length)
            fixed (int* capacityPtr = &hashMap.capacity)
            fixed (int* capacityMinusOnePtr = &hashMap.capacityMinusOne)
            fixed (int* lastIndexPtr = &hashMap.lastIndex)
            fixed (int* freeIndexPtr = &hashMap.freeIndex)
            fixed (TNative* dataPtr = &hashMap.data[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0])
            fixed (IntHashMapSlot* slotsPtr = &hashMap.slots[0]){
                nativeIntHashMap.lengthPtr           = lengthPtr;
                nativeIntHashMap.capacityPtr         = capacityPtr;
                nativeIntHashMap.capacityMinusOnePtr = capacityMinusOnePtr;
                nativeIntHashMap.lastIndexPtr        = lastIndexPtr;
                nativeIntHashMap.freeIndexPtr        = freeIndexPtr;
                nativeIntHashMap.data                = dataPtr;
                nativeIntHashMap.buckets             = bucketsPtr;
                nativeIntHashMap.slots               = slotsPtr;
            }

            return nativeIntHashMap;
        }
    }
}
#endif
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class NativeSliceExtension
{
    public static unsafe void CopyToFast<T>(this NativeSlice<T> nativeSlice, NativeArray<T> target) where T : struct
    {
        if (target == null)
        {
            throw new NullReferenceException(nameof(target) + " is null");
        }
        int nativeArrayLength = nativeSlice.Length;
        if (target.Length < nativeArrayLength)
        {
            throw new IndexOutOfRangeException(
                nameof(target) + " is shorter than " + nameof(nativeSlice));
        }
        int byteLength = nativeSlice.Length * UnsafeUtility.SizeOf<T>();
        void* managedBuffer = target.GetUnsafePtr();
        void* nativeBuffer = nativeSlice.GetUnsafePtr();
        UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
    }

}
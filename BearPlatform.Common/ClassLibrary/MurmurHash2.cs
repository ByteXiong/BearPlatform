using System;
using System.Runtime.InteropServices;

namespace BearPlatform.Common.ClassLibrary;

public class MurmurHash2
{
    public static UInt32 Hash(byte[] data)
    {
        return Hash(data, 0xc58f1a7b);
    }

    const UInt32 M = 0x5bd1e995;
    const Int32 R = 24;

    [StructLayout(LayoutKind.Explicit)]
    struct BytetoUInt32Converter
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public UInt32[] UInts;
    }

    public static UInt32 Hash(byte[] data, UInt32 seed)
    {
        Int32 length = data.Length;
        if (length == 0)
            return 0;
        UInt32 h = seed ^ (UInt32)length;
        Int32 currentIndex = 0;
        // array will be length of Bytes but contains Uints
        // therefore the currentIndex will jump with +1 while length will jump with +4
        UInt32[] hackArray = new BytetoUInt32Converter { Bytes = data }.UInts;
        while (length >= 4)
        {
            UInt32 k = hackArray[currentIndex++];
            k *= M;
            k ^= k >> R;
            k *= M;

            h *= M;
            h ^= k;
            length -= 4;
        }

        currentIndex *= 4; // fix the length
        switch (length)
        {
            case 3:
                h ^= (UInt16)(data[currentIndex++] | data[currentIndex++] << 8);
                h ^= (UInt32)data[currentIndex] << 16;
                h *= M;
                break;
            case 2:
                h ^= (UInt16)(data[currentIndex++] | data[currentIndex] << 8);
                h *= M;
                break;
            case 1:
                h ^= data[currentIndex];
                h *= M;
                break;
        }

        // Do a few final mixes of the hash to ensure the last few
        // bytes are well-incorporated.

        h ^= h >> 13;
        h *= M;
        h ^= h >> 15;

        return h;
    }
}

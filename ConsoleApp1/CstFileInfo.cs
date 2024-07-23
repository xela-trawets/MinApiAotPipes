using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 60)]
    public unsafe struct CstHeaderStruct
    {
        public fixed byte Signature[8];
        public byte ImageType;//0 = projection, 1 = projection set, 2 = volume
        public byte DataType;//0 = Float32, 1 = UInt16
        public ushort Width;
        public ushort Height;
        public ushort Depth;
        public float Angle;
        public UInt32 Checksum;
        public UInt32 DataByteOffset;
        public UInt32 reserved;
        public byte StorageDirection;
        public byte BeamMode;
        public byte IsAdjusted;
        public byte BitsUsed;
        public float NormChamber;
        public float TubeVoltage;
        public float TubeCurrent;
        public float PulseWidth;
        public float AdjustmentOffset;
        public float AdjustmentScale;
    }
    public record CstInfo(int Width, int Height, int Depth, TypeCode DataType, int DataByteOffset);
    public class CstHeader
    {
        public static CstInfo GetFileInfo(string fileName)
        {
            CstHeaderStruct hdrStruct = default;
            //ref byte firstByte = ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref hdrStruct, 1)));
            var hdrByteSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref hdrStruct, 1));
            using var fs = File.OpenRead(fileName);
            fs.ReadExactly(hdrByteSpan);
            TypeCode tc = hdrStruct.DataType switch
            {
                0 => TypeCode.Single,
                1 => TypeCode.UInt16,
                _ => throw new("Unknown data type in version 2.3.0.39 of cst doc"),
            };
            int dataByteOffset = (int)hdrStruct.DataByteOffset;
            Debug.Assert(dataByteOffset >= 0);
            if (dataByteOffset < 60) dataByteOffset = 60;
            return new(hdrStruct.Width, hdrStruct.Height, hdrStruct.Depth, tc, dataByteOffset);
        }
    }
}

using System.Runtime.InteropServices;

using CstFileInfo;

namespace PulsePileUpOptimizationTest
{
    internal class Program
    {
        static double FastExp0(double xd)
        {
            int n2p = 6;
            float x = (float)xd;
            float n2 = (1 << n2p);
            x = x + x * x * 0.5f / n2 +
                //x * x * x * 1.0f / (6 * n2 * n2) +
                //x * x * x * x * 1.0 / (24 * n2 * n2 * n2) +
                //x * x * x * x * x * 1.0 / (120 * n2 * n2 * n2 * n2) +
                0;
            x = 1 + (x / (1 << n2p));
            for (int np = 0; np < n2p; np++)
            {
                x = x * x;
            }
            double y = x;
            return y;
        }
        
        const int wTile = 128;
        const int hTile = 128;
        const int nxTile = 3;
        const int nyTile = 3;
        const int wImg = 384;
        const int hImg = 384;
        const int pixPerFrame = 384 * 384;
        const int cfiDepth = 4;
        const float CountScaleOutput = (float)1.0;
        const float CountScaleInput = (float)(1.0 / CountScaleOutput);
        static float[] coeffDataFrames = new float[pixPerFrame * cfiDepth];
        static int FrameCount = 1000;
        const int repeatCount = 1000;//for timing
        const int npix = pixPerFrame * repeatCount;
        const double megaPixels = npix * 1.0e-6;

        static float[] cor = new float[pixPerFrame];
        static float[] sum4 = new float[pixPerFrame];
        static float[] tmp4 = new float[pixPerFrame];
        static float[] gain4 = new float[pixPerFrame];
        static float[] gain4a = new float[pixPerFrame];
        static float[] frame = new float[pixPerFrame];
        static float[] frameLog = new float[pixPerFrame];

        static void Main(string[] args)
        {
            var singleFrameData = ReadRawData();
            float[] multiFrameData = new float[pixPerFrame * FrameCount];
            for (int f = 0; f < FrameCount; f++)
            {
                singleFrameData.AsSpan().CopyTo(multiFrameData.AsSpan(pixPerFrame * f, pixPerFrame));
            }
            ReadCoeff();
        }
        static float[] ReadCoeff()
        {
            const string cstPath = @"C:\tmp\saveOld\PulsePileupCalibration.ppc";//"C:\tmp\PulsePileupCalibration.ppc";
                                                                                // "c:\tmp\coeff231104-01.csts";

            var cfi = CstHeader.GetFileInfo(cstPath);
            Console.WriteLine(cfi);
            if (cfi.Depth != cfiDepth)
            {
                throw new("Test not supported due to mismatch coeff depth");
            }

            var coeffDataFrames = new float[pixPerFrame * cfi.Depth];

            using (var fs = File.OpenRead(cstPath))
            {
                fs.Seek(cfi.DataByteOffset, SeekOrigin.Begin);
                fs.ReadExactly(MemoryMarshal.AsBytes(coeffDataFrames.AsSpan()));
            }
            return coeffDataFrames;
        }
        static float[] ReadRawData()
        {
            string fnTest = @"C:\tmp\saveOld\mA_sweep_OpenField_240mA_high_sensitivity_2023-11-04_172835.avg.raw";
            var frame = new float[pixPerFrame];
            File.OpenRead(fnTest).ReadExactly(MemoryMarshal.AsBytes(frame.AsSpan()));
            return frame;
        }
    }
}

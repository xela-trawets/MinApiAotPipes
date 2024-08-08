using System.Diagnostics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace ConsoleApp1 //pulse pile-up
{
    using TOutput = float;
    using TInput = float;
    internal class Program
    {
        long millisecs = 0;
        double rate = 0;
        static void RunTestLog(int start, int count)
        {
            var rawLn = frame.AsSpan(start, count);
            var dest = cor.AsSpan(start, count);

            var sum = sum4.AsSpan(start, count);
            var tmp = tmp4.AsSpan(start, count);
            var gain = gain4.AsSpan(start, count);
            //    Vector256<>

            //var pf0 = coeffDataFrames.AsSpan(pixPerFrame + 0, pixPerFrame);
            //var pf1 = coeffDataFrames.AsSpan(pixPerFrame + 512, pixPerFrame);
            //var pf2 = coeffDataFrames.AsSpan(pixPerFrame + 1024, pixPerFrame);
            //var pf3 = coeffDataFrames.AsSpan(pixPerFrame + 1536, pixPerFrame);
            var pf0 = coeffDataFrames.AsSpan(pixPerFrame * 0 + start, count);
            var pf1 = coeffDataFrames.AsSpan(pixPerFrame * 1 + start, count);
            var pf2 = coeffDataFrames.AsSpan(pixPerFrame * 2 + start, count);
            var pf3 = coeffDataFrames.AsSpan(pixPerFrame * 3 + start, count);

            //            var logGain = ((((pf[3] * t) + pf[2]) * t) + pf[1]) * t + pf[0];
            {
                TensorPrimitives.MultiplyAdd(pf3, rawLn, pf2, sum);
                TensorPrimitives.MultiplyAdd(sum, rawLn, pf1, tmp);
                TensorPrimitives.MultiplyAdd(tmp, rawLn, pf0, sum);
                //TensorPrimitives.Add(rawLn, sum, dest);
                TensorPrimitives.Exp(sum, gain);
                TensorPrimitives.Multiply(rawLn, gain, dest);
            }


            //TensorPrimitives.MultiplyAdd(pf3, rawLn, pf2, sum);
            //TensorPrimitives.MultiplyAdd(sum, rawLn, pf1, sum);
            //TensorPrimitives.MultiplyAdd(sum, rawLn, pf0, sum);
            for (int nrep = 0; nrep < 1000; nrep++)
            {
                //TensorPrimitives.MultiplyAdd(pf3, rawLn, pf2, sum);
                TensorPrimitives.FusedMultiplyAdd(pf3, rawLn, pf2, sum);
                TensorPrimitives.MultiplyAdd(sum, rawLn, pf1, tmp);
                TensorPrimitives.MultiplyAdd(tmp, rawLn, pf0, sum);
                TensorPrimitives.Add(rawLn, sum, dest);
                //TensorPrimitives.Exp(sum, gain);
                //TensorPrimitives.Multiply(rawLn, gain, dest);
            }
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
        const int repeatCount = 1000;
        const int npix = pixPerFrame * repeatCount;
        const double megaPixels = npix * 1.0e-6;

        static float[] cor = new float[pixPerFrame];
        static float[] sum4 = new float[pixPerFrame];
        static float[] tmp4 = new float[pixPerFrame];
        static float[] gain4 = new float[pixPerFrame];
        static float[] gain4a = new float[pixPerFrame];
        static float[] frame = new float[pixPerFrame];
        static float[] frameLog = new float[pixPerFrame];


        static Stopwatch sw = new();
        unsafe static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var a = new Single[1000];
            var b = new Single[1000];
            TensorPrimitives.Abs(a.AsSpan(), b.AsSpan());
            //benchmark.net
            //allignment
            //Lock the pages down
            //giant pages
            // Log vs Exp
            ///Multi pass
            /// memcpy speed
            //var unmanagedMemory = new UnmanagedMemoryManager<float>(1000);//pipeline sockets unofficial
            //SpanHelpers.
            //Buffer.MemoryCopy(frame, frameLog, pixPerFrame * sizeof(float), pixPerFrame * sizeof(float));
            void* pa = NativeMemory.AlignedAlloc(1000*4, 64);

            ReadCoeff();
            ReadRawData();

            //Thread T0 = new(() => RunTestLog(0, pixPerFrame / 2));
            RunTestLog(0, pixPerFrame);
            RunTestLog(0, pixPerFrame);
            RunTestLog(0, pixPerFrame);
            sw.Restart();
            RunTestLog(0, pixPerFrame);
            sw.Stop();
            long millisecs = sw.ElapsedMilliseconds;
            double rate = megaPixels * 1000.0 / (double)millisecs;
            Console.WriteLine($"Basic {rate:####0.0} :{megaPixels} megapixels in {millisecs} milliseconds");
        }
        static void ReadCoeff()
        {
            Console.WriteLine("Hello, World! - Run per tile through all images to stay ing cache");
            Console.WriteLine("Use Flat Gig AOT");
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
            //List<double[]> pixFitList = new();
            //for (int ipx = 0; ipx < pixPerFrame; ipx++)
            //{
            //    var fit = new double[cfi.Depth];
            //    for (int ima = 0; ima < cfi.Depth; ima++)
            //    {
            //        fit[ima] = coeffDataFrames[pixPerFrame * ima + ipx];
            //    }
            //    pixFitList.Add(fit);
            //}
        }
        static void ReadRawData()
        {
            string fnTest = @"C:\tmp\saveOld\mA_sweep_OpenField_240mA_high_sensitivity_2023-11-04_172835.avg.raw";
            float[] cor = Array.Empty<float>();
            var frame = new float[pixPerFrame];
            File.OpenRead(fnTest).ReadExactly(MemoryMarshal.AsBytes(frame.AsSpan()));
            //for (int ipx = 0; ipx < pixPerFrame; ipx++)
            //{
            //    var pf = pixFitList[ipx];
            //    var raw = frame[ipx];

            //    //var t = raw * (1.0 / (1000 * 5000.0));
            //    var t = raw;

            //    var logGain = ((((pf[3] * t) + pf[2]) * t) + pf[1]) * t + pf[0];

            //    var gain = MathF.Exp((float)logGain);

            //    cor[ipx] *= gain;
            //    cor[ipx] *= CountScaleOutput;
            //}
            cor = frame.ToArray();
            frameLog = frame.ToArray();
        }
        static unsafe void Vectorized256(ref TInput xRef, ref TOutput dRef, nuint remainder)
        {
            ref TOutput dRefBeg = ref dRef;

            // Preload the beginning and end so that overlapping accesses don't negatively impact the data

            Vector256<TOutput> beg = Invoke(Vector256.LoadUnsafe(ref xRef));
            Vector256<TOutput> end = Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

            // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
            // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

            fixed (TInput* px = &xRef)
            fixed (TOutput* pd = &dRef)
            {
                TInput* xPtr = px;
                TOutput* dPtr = pd;
                {
                    Vector256<TOutput> vector1;
                    Vector256<TOutput> vector2;
                    Vector256<TOutput> vector3;
                    Vector256<TOutput> vector4;

                    //if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(TInput))) && canAlign)
                    {
                        while (remainder >= (uint)(Vector256<TInput>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 0)));
                            vector2 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 1)));
                            vector3 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 2)));
                            vector4 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 3)));

                            vector1.Store(dPtr + (uint)(Vector256<TOutput>.Count * 0));
                            vector2.Store(dPtr + (uint)(Vector256<TOutput>.Count * 1));
                            vector3.Store(dPtr + (uint)(Vector256<TOutput>.Count * 2));
                            vector4.Store(dPtr + (uint)(Vector256<TOutput>.Count * 3));

                            // We load, process, and store the next four vectors

                            vector1 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 4)));
                            vector2 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 5)));
                            vector3 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 6)));
                            vector4 = Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 7)));

                            vector1.Store(dPtr + (uint)(Vector256<TOutput>.Count * 4));
                            vector2.Store(dPtr + (uint)(Vector256<TOutput>.Count * 5));
                            vector3.Store(dPtr + (uint)(Vector256<TOutput>.Count * 6));
                            vector4.Store(dPtr + (uint)(Vector256<TOutput>.Count * 7));

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector256<TInput>.Count * 8);
                            dPtr += (uint)(Vector256<TOutput>.Count * 8);

                            remainder -= (uint)(Vector256<TInput>.Count * 8);
                        }
                    }
                }
            }
        }
        //unsafe static void Vectorized256<T>(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
        //{
        //    ref T dRefBeg = ref dRef;

        //    // Preload the beginning and end so that overlapping accesses don't negatively impact the data

        //    Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
        //                                               Vector256.LoadUnsafe(ref yRef),
        //                                               Vector256.LoadUnsafe(ref zRef));
        //    Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
        //                                               Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
        //                                               Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

        //    if (remainder > (uint)(Vector256<T>.Count * 8))
        //    {
        //        // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
        //        // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

        //        fixed (T* px = &xRef)
        //        fixed (T* py = &yRef)
        //        fixed (T* pz = &zRef)
        //        fixed (T* pd = &dRef)
        //        {
        //            T* xPtr = px;
        //            T* yPtr = py;
        //            T* zPtr = pz;
        //            T* dPtr = pd;

        //            // We need to the ensure the underlying data can be aligned and only align
        //            // it if it can. It is possible we have an unaligned ref, in which case we
        //            // can never achieve the required SIMD alignment.

        //            bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

        //            if (canAlign)
        //            {
        //                // Compute by how many elements we're misaligned and adjust the pointers accordingly
        //                //
        //                // Noting that we are only actually aligning dPtr. This is because unaligned stores
        //                // are more expensive than unaligned loads and aligning both is significantly more
        //                // complex.

        //                nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (nuint)sizeof(T);

        //                xPtr += misalignment;
        //                yPtr += misalignment;
        //                zPtr += misalignment;
        //                dPtr += misalignment;

        //                Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

        //                remainder -= misalignment;
        //            }

        //            Vector256<T> vector1;
        //            Vector256<T> vector2;
        //            Vector256<T> vector3;
        //            Vector256<T> vector4;

        //            if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
        //            {
        //                // This loop stores the data non-temporally, which benefits us when there
        //                // is a large amount of data involved as it avoids polluting the cache.

        //                while (remainder >= (uint)(Vector256<T>.Count * 8))
        //                {
        //                    // We load, process, and store the first four vectors

        //                    vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
        //                    vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
        //                    vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
        //                    vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

        //                    vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
        //                    vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
        //                    vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
        //                    vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

        //                    // We load, process, and store the next four vectors

        //                    vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
        //                    vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
        //                    vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
        //                    vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
        //                                                      Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
        //                                                      Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

        //                    vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
        //                    vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
        //                    vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
        //                    vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

        //                    // We adjust the source and destination references, then update
        //                    // the count of remaining elements to process.

        //                    xPtr += (uint)(Vector256<T>.Count * 8);
        //                    yPtr += (uint)(Vector256<T>.Count * 8);
        //                    zPtr += (uint)(Vector256<T>.Count * 8);
        //                    dPtr += (uint)(Vector256<T>.Count * 8);

        //                    remainder -= (uint)(Vector256<T>.Count * 8);
        //                }
        //            }
        //        }
        //    }
        //}
        private const uint V_ARG_MAX = 0x42AE0000;

        private const float V_EXPF_MIN = -103.97208f;
        private const float V_EXPF_MAX = +88.72284f;

        private const double V_EXPF_HUGE = 6755399441055744;
        private const double V_TBL_LN2 = 1.4426950408889634;

        private const double C1 = 1.0000000754895704;
        private const double C2 = 0.6931472254087585;
        private const double C3 = 0.2402210737432219;
        private const double C4 = 0.05550297297702539;
        private const double C5 = 0.009676036358193323;
        private const double C6 = 0.001341000536524434;

        //[MethodImpl(MethodImplOptions.AggressiveInlining
        //    |MethodImplOptions.AggressiveOptimization)
        //    ]
        public static Vector256<float> Invoke(Vector256<float> x)
        {
            // Convert x to double precision
            (Vector256<double> xl, Vector256<double> xu) = Vector256.Widen(x);

            // x * (64.0 / ln(2))
            Vector256<double> v_tbl_ln2 = Vector256.Create(V_TBL_LN2);

            Vector256<double> zl = xl * v_tbl_ln2;
            Vector256<double> zu = xu * v_tbl_ln2;

            Vector256<double> v_expf_huge = Vector256.Create(V_EXPF_HUGE);

            Vector256<double> dnl = zl + v_expf_huge;
            Vector256<double> dnu = zu + v_expf_huge;

            // n = (int)z
            Vector256<ulong> nl = dnl.AsUInt64();
            Vector256<ulong> nu = dnu.AsUInt64();

            // dn = (double)n
            dnl -= v_expf_huge;
            dnu -= v_expf_huge;

            // r = z - dn
            Vector256<double> c1 = Vector256.Create(C1);
            Vector256<double> c2 = Vector256.Create(C2);
            Vector256<double> c3 = Vector256.Create(C3);
            Vector256<double> c4 = Vector256.Create(C4);
            Vector256<double> c5 = Vector256.Create(C5);
            Vector256<double> c6 = Vector256.Create(C6);

            Vector256<double> rl = zl - dnl;

            Vector256<double> rl2 = rl * rl;
            Vector256<double> rl4 = rl2 * rl2;

            Vector256<double> polyl = (c4 * rl + c3) * rl2
                                   + ((c6 * rl + c5) * rl4
                                    + (c2 * rl + c1));


            Vector256<double> ru = zu - dnu;

            Vector256<double> ru2 = ru * ru;
            Vector256<double> ru4 = ru2 * ru2;

            Vector256<double> polyu = (c4 * ru + c3) * ru2
                                   + ((c6 * ru + c5) * ru4
                                    + (c2 * ru + c1));

            // result = (float)(poly + (n << 52))
            Vector256<float> ret = Vector256.Narrow(
                (polyl.AsUInt64() + (nl << 52)).AsDouble(),
                (polyu.AsUInt64() + (nu << 52)).AsDouble()
            );

            // Check if -103 < |x| < 88
            if (Vector256.GreaterThanAny(Vector256.Abs(x).AsUInt32(), Vector256.Create(V_ARG_MAX)))
            {
                // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
                Vector256<float> infinityMask = Vector256.GreaterThan(x, Vector256.Create(V_EXPF_MAX));

                ret = Vector256.ConditionalSelect(
                    infinityMask,
                    Vector256.Create(float.PositiveInfinity),
                    ret
                );

                // (x < V_EXPF_MIN) ? 0 : x
                ret = Vector256.AndNot(ret, Vector256.LessThan(x, Vector256.Create(V_EXPF_MIN)));
            }

            return ret;
        }
    }
}

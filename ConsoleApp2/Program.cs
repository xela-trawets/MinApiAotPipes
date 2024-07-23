// See https://aka.ms/new-console-template for more information
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
Console.WriteLine("Hello, World!");
//var a = new Vector256<float>[1000];
//var b = new Vector256<float>[1000];
//TensorPrimitives.Abs(MemoryMarshal.Cast<Vector256<float>, float>(a.AsSpan()), MemoryMarshal.Cast<Vector256<float>, float>(b.AsSpan()));

var af = new float[1000];
var bf = new float[1000];
TensorPrimitives.Abs<float>(af.AsSpan(), bf.AsSpan());

var ad = new double[1000];
var bd = new double[1000];
TensorPrimitives.Abs<double>(ad.AsSpan(), bd.AsSpan());

Console.WriteLine("We never see this text");

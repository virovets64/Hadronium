using System;
using System.Runtime.InteropServices;
using System.Windows;
#if Model3D
using System.Windows.Media.Media3D;
#endif

namespace Hadronium
{
  class Engine
  {
    public Engine()
    {
      parameters.Viscosity = 10;
      parameters.ParticleAttraction = -1;
      parameters.ParticlePower = -2;
      parameters.LinkAttraction = 10;
      parameters.LinkPower = -1;
      parameters.StretchAttraction = 0;
      parameters.Accuracy = 50;
      parameters.TimeScale = 1;
      parameters.StepElapsedTime = 0; // in msec
      parameters.RealTimeScale = 1;
      parameters.StepCount = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Particle
    {
#if Model3D
      public Point3D Position;
      public Vector3D Velocity;
#else
      public Point Position;
      public Vector Velocity;
#endif
      public double Mass;
      public bool Fixed;
    }

    public Particle[] particles;

    [StructLayout(LayoutKind.Sequential)]
    public struct Parameters
    {
      public double Viscosity;
      public double ParticleAttraction;
      public double ParticlePower;
      public double LinkAttraction;
      public double LinkPower;
      public double StretchAttraction;
      public double Accuracy;
      public double TimeScale;
      public double StepElapsedTime; // in msec
      public double RealTimeScale;
      public long StepCount;
    }

    public Parameters parameters;

    private IntPtr handle = IntPtr.Zero;
    
    public void Start(Link[] links)
    {
      handle = EngineStart(ref parameters, particles, particles.Length, links, links.Length);
    }

    public void Stop()
    {
      EngineStop(handle);
      handle = IntPtr.Zero;
    }

    public bool Active
    {
      get { return handle != IntPtr.Zero; }
    }

    public long ActualStepCount
    {
      get { return Active ? EngineStepCount(handle) : 0; }
    }

    public Parameters Sync()
    {
      var outputParams = parameters;
      EngineSync(handle, ref outputParams, ref particles, particles.Length);
      return outputParams;
    }

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr EngineStart(
        ref Parameters parameters,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Particle[] particles,
        long particleCount,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] Link[] links,
        long linkCount);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineSync(
        IntPtr engine,
        ref Parameters parameters,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ref Particle[] particles,
        long particleCount);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern long EngineStepCount(
        IntPtr engine);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineStop(
        IntPtr engine);

  }
}

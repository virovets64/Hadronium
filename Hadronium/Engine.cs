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
      parameters.In.Viscosity = 10;
      parameters.In.ParticleAttraction = -1;
      parameters.In.ParticlePower = -2;
      parameters.In.LinkAttraction = 10;
      parameters.In.LinkPower = -1;
      parameters.In.StretchAttraction = 0;
      parameters.In.Gravity = 0;
      parameters.In.Accuracy = 50;
      parameters.In.TimeScale = 1;
      parameters.Out.StepElapsedTime = 0; // in msec
      parameters.Out.RealTimeScale = 1;
      parameters.Out.StepCount = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Link
    {
      public int A;
      public int B;
      public double Strength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleInfo
    {
      public double Mass;
      public bool Fixed;
    }

    private double[] particleData;
    private ParticleInfo[] particleInfos;
    private Link[] links;

    [StructLayout(LayoutKind.Sequential)]
    public struct Parameters
    {
      [StructLayout(LayoutKind.Sequential)]
      public struct Input
      {
        public double Viscosity;
        public double ParticleAttraction;
        public double ParticlePower;
        public double LinkAttraction;
        public double LinkPower;
        public double StretchAttraction;
        public double Gravity;
        public double Accuracy;
        public double TimeScale;
      }
      [StructLayout(LayoutKind.Sequential)]
      public struct Output
      {
        public double StepElapsedTime; // in msec
        public double RealTimeScale;
        public long StepCount;
      }
      public Input In;
      public Output Out;
    }

    public Parameters parameters;

    private IntPtr handle = IntPtr.Zero;
    
    public void Start(Model model)
    {
      particleData = new double[model.Particles.Count * model.Dimension * 2];
      particleInfos = new ParticleInfo[model.Particles.Count];
      links = new Link[model.Links.Count];
      int pIndex = 0;
      foreach(var particle in model.Particles)
      {
        particleData[pIndex++] = particle.Position.X;
        particleData[pIndex++] = particle.Position.Y;
        particleData[pIndex++] = particle.Velocity.X;
        particleData[pIndex++] = particle.Velocity.Y;
      }
      for (int i = 0; i < model.Particles.Count; i++)
        particleInfos[i].Mass = model.Particles[i].Mass;

      for (int i = 0; i < model.Links.Count; i++)
      {
        links[i].A = model.GetParticleIndex(model.Links[i].A);
        links[i].B = model.GetParticleIndex(model.Links[i].B);
        links[i].Strength = model.Links[i].Strength;
      }

      handle = EngineStart(ref parameters, particleData.Length, particleData, particleInfos.Length, particleInfos, links.Length, links);
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

    public void Sync(Model model)
    {
      for (int i = 0; i < model.Particles.Count; i++)
        particleInfos[i].Fixed = model.Particles[i].Fixed;

      int pIndex = 0;
      foreach (var particle in model.Particles)
      {
        if (particle.Fixed)
        {
          particleData[pIndex++] = particle.Position.X;
          particleData[pIndex++] = particle.Position.Y;
          particleData[pIndex++] = particle.Velocity.X;
          particleData[pIndex++] = particle.Velocity.Y;
        }
        else
          pIndex += model.Dimension * 2;
      }

      EngineSync(handle, ref parameters, particleData.Length, ref particleData, particleInfos.Length, particleInfos);

      pIndex = 0;
      foreach (var particle in model.Particles)
      {
        if (!particle.Fixed)
        {
          particle.Position.X = particleData[pIndex++];
          particle.Position.Y = particleData[pIndex++];
          particle.Velocity.X = particleData[pIndex++];
          particle.Velocity.Y = particleData[pIndex++];
        }
        else
          pIndex += model.Dimension * 2;
      }
    }

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr EngineStart(
        ref Parameters parameters,
        long particleDataSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double[] particleData,
        long particleInfoSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ParticleInfo[] particleInfos,
        long linkCount,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] Link[] links
        );

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineSync(
        IntPtr engine,
        ref Parameters parameters,
        long particleDataSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ref double[] particleData,
        long particleInfoSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] ParticleInfo[] particleInfos);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern long EngineStepCount(
        IntPtr engine);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineStop(
        IntPtr engine);

  }
}

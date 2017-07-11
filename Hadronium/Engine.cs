﻿using System;
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
    public struct Particle
    {
#if Model3D
      public Point3D Position;
      public Vector3D Velocity;
#else
      public Point Position;
      public Vector Velocity;
#endif
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

    public Particle[] particles;
    public ParticleInfo[] particleInfos;
    public Link[] links;

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
    
    public void Start()
    {
      handle = EngineStart(ref parameters, particles.Length, particles, particleInfos, links.Length, links);
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

    public void Sync()
    {
      EngineSync(handle, ref parameters, particles.Length, ref particles, particleInfos);
    }

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr EngineStart(
        ref Parameters parameters,
        long particleCount,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Particle[] particles,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ParticleInfo[] particleInfos,
        long linkCount,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] Link[] links
        );

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineSync(
        IntPtr engine,
        ref Parameters parameters,
        long particleCount,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ref Particle[] particles,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ParticleInfo[] particleInfos);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern long EngineStepCount(
        IntPtr engine);

    [DllImport("Engine.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void EngineStop(
        IntPtr engine);

  }
}

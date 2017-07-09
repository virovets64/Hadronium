using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
#if Model3D
using System.Windows.Media.Media3D;
#endif
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hadronium
{
  public class Utils
  {
#if Model3D
        public static void Zero(ref Point3D a)
        {
            a.X = a.Y = a.Z = 0;
        }
        public static void Zero(ref Vector3D a)
        {
            a.X = a.Y = a.Z = 0;
        }
#else
    public static void Zero(ref Point a)
    {
      a.X = a.Y = 0;
    }
    public static void Zero(ref Vector a)
    {
      a.X = a.Y = 0;
    }
#endif
  }


  public class Particle
  {
    public Particle()
    {
      Utils.Zero(ref Position);
      Utils.Zero(ref Velocity);
      Mass = 1;
      Fixed = false;
      FillColor = Color.FromRgb(100, 200, 100);
      StrokeColor = Color.FromRgb(0, 0, 0);
    }
#if Model3D
        public Point3D Position;
        public Vector3D Velocity;
#else
    public Point Position;
    public Vector Velocity;
#endif
    public double Mass;
    public bool Fixed;
    public string Name;
    public Color FillColor;
    public Color StrokeColor;
    public object Tag;

  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Link
  {
    public Link(int a, int b, double strength = 1)
    {
      A = a;
      B = b;
      Strength = strength;
    }
    public int A;
    public int B;
    public double Strength;
  }


  public class Model : INotifyPropertyChanged
  {
    public Model()
    {
    }

    public int FindParticleIndex(string name)
    {
      for (int i = 0; i < Particles.Count; i++)
      {
        if (Particles[i].Name == name)
          return i;
      }
      return -1;
    }
    public int GetParticleIndex(string name)
    {
      int result = FindParticleIndex(name);
      if (result == -1)
        throw new Exception("Threr is no particle with name " + name);
      return result;
    }

    private Engine engine = new Engine();

    public List<Particle> Particles = new List<Particle>();
    public List<Link> Links = new List<Link>();

    public event PropertyChangedEventHandler PropertyChanged;

    private void setProperty(string propertyName, ref double member, double value)
    {
      if (member != value)
      {
        lock (this)
        {
          member = value;
        }
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    private void setProperty(string propertyName, ref long member, long value)
    {
      if (member != value)
      {
        lock (this)
        {
          member = value;
        }
        if (PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    public double Viscosity
    {
      get { return engine.parameters.Viscosity; }
      set { setProperty("Viscosity", ref engine.parameters.Viscosity, value); }
    }
    public double ParticleAttraction
    {
      get { return engine.parameters.ParticleAttraction; }
      set { setProperty("ParticleAttraction", ref engine.parameters.ParticleAttraction, value); }
    }
    public double LinkAttraction
    {
      get { return engine.parameters.LinkAttraction; }
      set { setProperty("LinkAttraction", ref engine.parameters.LinkAttraction, value); }
    }
    public double StretchAttraction
    {
      get { return engine.parameters.StretchAttraction; }
      set { setProperty("StretchAttraction", ref engine.parameters.StretchAttraction, value); }
    }
    public double Accuracy
    {
      get { return engine.parameters.Accuracy; }
      set { setProperty("Accuracy", ref engine.parameters.Accuracy, value); }
    }
    //public double VelocityLimit
    //{
    //    get { return velocityLimit; }
    //    set { velocityLimit = value; }
    //}
    //public double AccelerationLimit
    //{
    //    get { return accelerationLimit; }
    //    set { accelerationLimit = value; }
    //}
    public double TimeScale
    {
      get { return engine.parameters.TimeScale; }
      set { setProperty("TimeScale", ref engine.parameters.TimeScale, value); }
    }


    public long StepCount
    {
      get { return engine.parameters.StepCount; }
      set { setProperty("StepCount", ref engine.parameters.StepCount, value); }
    }

    public long ActualStepCount
    {
      get { return engine.ActualStepCount; }
    }

    public double StepElapsedTime
    {
      get { return engine.parameters.StepElapsedTime; }
      set { setProperty("StepElapsedTime", ref engine.parameters.StepElapsedTime, value); }
    }

    public double RealTimeScale
    {
      get { return engine.parameters.RealTimeScale; }
      set { setProperty("RealTimeScale", ref engine.parameters.RealTimeScale, value); }
    }

    public bool Active
    {
      get { return engine.Active; }
      set
      {
        if (value != Active)
        {
          if (value)
            Start();
          else Stop();
        }
      }
    }

    public void Start()
    {
      if (!Active)
      {
        engine.particles = new Engine.Particle[Particles.Count];
        for (int i = 0; i < Particles.Count; i++)
        {
          engine.particles[i].Position = Particles[i].Position;
          engine.particles[i].Velocity = Particles[i].Velocity;
          engine.particles[i].Mass = Particles[i].Mass;
        }
        engine.Start(Links.ToArray());
      }
    }

    public void Stop()
    {
      engine.Stop();
    }

    public void Clear()
    {
      Particles.Clear();
      Links.Clear();
    }


#if Model3D
    public void AddRandomParticles(int particleCount, int linkCount, Rect3D zone)
#else
    public void AddRandomParticles(int particleCount, int linkCount, Rect zone)
#endif
    {
      int maxLinkCount = particleCount * (particleCount - 1) / 2;
      if (linkCount > maxLinkCount)
        throw new Exception(string.Format("{0} particles cannot have more than {1} links", particleCount, maxLinkCount));
      var random = new Random();
      var newParticles = new List<Particle>();
      for (int i = 0; i < particleCount; i++)
      {
        Particle particle = new Particle();
        newParticles.Add(particle);
      }
      randomizeParticlePositions(newParticles, zone);
      var firstNewParticleIndex = Particles.Count;
      Particles.AddRange(newParticles);
      for (int i = 0; i < linkCount; )
      {
        int nA = firstNewParticleIndex + random.Next(particleCount);
        int nB = firstNewParticleIndex + random.Next(particleCount);
        if (AddLink(nA, nB))
          i++;
      }
    }


#if Model3D
    private void randomizeParticlePositions(List<Particle> particles, Rect3D zone)
    {
      if (particles.Count == 0)
        return;
      double averageDist = Math.Pow(zone.SizeX * zone.SizeY * zone.SizeZ / Particles.Count, (double)1/3);
      var random = new Random();
      foreach (var p in particles)
      {
        Utils.Zero(ref p.Velocity);
        while (true)
        {
          p.Position.X = random.NextDouble() * zone.SizeX + zone.X;
          p.Position.Y = random.NextDouble() * zone.SizeY + zone.Y;
          p.Position.Z = random.NextDouble() * zone.SizeZ + zone.Z;

          bool isFarEnough = true;
          foreach (var p2 in particles)
          {
            if (p2 != p)
            {
              if ((p2.Position - p.Position).Length < averageDist / 2)
              {
                isFarEnough = false;
                break;
              }
            }
          }
          if (isFarEnough)
            break;
        }
      }
    }
#else
    private void randomizeParticlePositions(List<Particle> particles, Rect zone)
    {
      if (particles.Count == 0)
        return;
      double averageDist = Math.Sqrt(zone.Height * zone.Width / particles.Count);
      var random = new Random();
      foreach (var p in particles)
      {
        Utils.Zero(ref p.Velocity);
        while (true)
        {
          p.Position.X = random.NextDouble() * zone.Width + zone.Left;
          p.Position.Y = random.NextDouble() * zone.Height + zone.Top;

          bool isFarEnough = true;
          foreach (var p2 in particles)
          {
            if (p2 != p)
            {
              if ((p2.Position - p.Position).Length < averageDist / 2)
              {
                isFarEnough = false;
                break;
              }
            }
          }
          if (isFarEnough)
            break;
        }
      }
    }
#endif



#if Model3D
    public void RandomizePositions(Rect3D zone)
    {
      randomizeParticlePositions(Particles, zone);
    }
#else
    public void RandomizePositions(Rect zone)
    {
      randomizeParticlePositions(Particles, zone);
    }
#endif


    private bool AddLink(int a, int b)
    {
      if (a == b)
        return false;
      foreach (var l in Links)
      {
        if (l.A == a && l.B == b || l.A == b && l.B == a)
          return false;
      }
      Links.Add(new Link(a, b));
      return true;
    }


    unsafe public void Refresh()
    {
      if (!Active)
        return;
      for (int i = 0; i < engine.particles.Length; i++)
      {
        engine.particles[i].Fixed = Particles[i].Fixed;
        if (Particles[i].Fixed)
        {
          engine.particles[i].Position = Particles[i].Position;
          engine.particles[i].Velocity = Particles[i].Velocity;
        }
      }
      var tmp = engine.Sync();
      for (int i = 0; i < engine.particles.Length; i++)
      {
        if (!Particles[i].Fixed)
        {
          Particles[i].Position = engine.particles[i].Position;
          Particles[i].Velocity = engine.particles[i].Velocity;
        }
      }
      StepElapsedTime = tmp.StepElapsedTime;
      StepCount = tmp.StepCount;
      RealTimeScale = tmp.RealTimeScale;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace Hadronium
{
  public class Particle
  {
    public Particle(int dimension)
    {
      Position = new double[dimension];
      Velocity = new double[dimension];
      Mass = 1;
      Fixed = false;
      FillColor = Color.FromRgb(100, 200, 100);
      StrokeColor = Colors.Transparent;
    }
    public double[] Position;
    public double[] Velocity;
    public double Mass;
    public bool Fixed;
    public string Name;
    public Color FillColor;
    public Color StrokeColor;
    public object Tag;
  }

  public class Link
  {
    public Link(Particle a, Particle b, double strength = 1)
    {
      A = a;
      B = b;
      Strength = strength;
    }
    public Particle A;
    public Particle B;
    public double Strength;
  }

  public class Box
  {
    public Box(int dimension)
    {
      P1 = new double[dimension];
      P2 = new double[dimension];
    }
    public Box(double[] p1, double[] p2)
    {
      P1 = p1;
      P2 = p2;
    }
    public double[] P1;
    public double[] P2;
  }

  public class Model : INotifyPropertyChanged
  {
    public Model(int dimension)
    {
      this.dimension = dimension;
    }

    private int dimension;
    public int Dimension 
    {
      get { return dimension; }
    }

    public double Distance(double[] p1, double[] p2)
    {
      double sum = 0;
      for (int i = 0; i < p1.Length; i++)
      {
        var d = p2[i] - p1[i];
        sum += d * d;
      }
      return Math.Sqrt(sum);
    }

    public Particle FindParticle(string name)
    {
      foreach (var p in Particles)
      {
        if (p.Name == name)
          return p;
      }
      return null;
    }

    public Particle GetParticle(string name)
    {
      return Particles.Find(x => x.Name == name);
    }

    public int GetParticleIndex(Particle p)
    {
      int result = Particles.IndexOf(p); 
      if (result == -1)
        throw new Exception("Particle is not found");
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
    private Engine.Parameters.Output statistics;

    public double Viscosity
    {
      get { return engine.parameters.In.Viscosity; }
      set { setProperty("Viscosity", ref engine.parameters.In.Viscosity, value); }
    }
    public double ParticleAttraction
    {
      get { return engine.parameters.In.ParticleAttraction; }
      set { setProperty("ParticleAttraction", ref engine.parameters.In.ParticleAttraction, value); }
    }
    public double LinkAttraction
    {
      get { return engine.parameters.In.LinkAttraction; }
      set { setProperty("LinkAttraction", ref engine.parameters.In.LinkAttraction, value); }
    }
    public double StretchAttraction
    {
      get { return engine.parameters.In.StretchAttraction; }
      set { setProperty("StretchAttraction", ref engine.parameters.In.StretchAttraction, value); }
    }
    public double Gravity
    {
      get { return engine.parameters.In.Gravity; }
      set { setProperty("Gravity", ref engine.parameters.In.Gravity, value); }
    }
    public double Accuracy
    {
      get { return engine.parameters.In.Accuracy; }
      set { setProperty("Accuracy", ref engine.parameters.In.Accuracy, value); }
    }
    public double TimeScale
    {
      get { return engine.parameters.In.TimeScale; }
      set { setProperty("TimeScale", ref engine.parameters.In.TimeScale, value); }
    }

    public long StepCount
    {
      get { return statistics.StepCount; }
      set { setProperty("StepCount", ref statistics.StepCount, value); }
    }

    public long ActualStepCount
    {
      get { return engine.ActualStepCount; }
    }

    public double StepElapsedTime
    {
      get { return statistics.StepElapsedTime; }
      set { setProperty("StepElapsedTime", ref statistics.StepElapsedTime, value); }
    }

    public double RealTimeScale
    {
      get { return statistics.RealTimeScale; }
      set { setProperty("RealTimeScale", ref statistics.RealTimeScale, value); }
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
        engine.Start(this);
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

    public void AddRandomParticles(int particleCount, int linkCount, Box zone)
    {
      int maxLinkCount = particleCount * (particleCount - 1) / 2;
      if (linkCount > maxLinkCount)
        throw new Exception(string.Format("{0} particles cannot have more than {1} links", particleCount, maxLinkCount));
      var random = new Random();
      var newParticles = new List<Particle>();
      for (int i = 0; i < particleCount; i++)
      {
        Particle particle = new Particle(Dimension);
        newParticles.Add(particle);
      }
      randomizeParticlePositions(newParticles, zone);
      var firstNewParticleIndex = Particles.Count;
      Particles.AddRange(newParticles);
      for (int i = 0; i < linkCount; )
      {
        var a = newParticles[random.Next(particleCount)];
        var b = newParticles[random.Next(particleCount)];
        if (AddLink(a, b))
          i++;
      }
    }


    private void randomizeParticlePositions(List<Particle> particles, Box zone)
    {
      if (particles.Count == 0)
        return;

      double volume = 1;
      for (int i = 0; i < Dimension; i++)
        volume *= (zone.P2[i] - zone.P1[i]);

      double averageDist = Math.Pow(volume, 1.0 / Dimension) / particles.Count;
      var random = new Random();
      foreach (var p in particles)
      {
        Array.Clear(p.Velocity, 0, Dimension);
        while (true)
        {
          for (int i = 0; i < Dimension; i++)
            p.Position[i] = random.NextDouble() * (zone.P2[i] - zone.P1[i]) + zone.P1[i];

          bool isFarEnough = true;
          foreach (var p2 in particles)
          {
            if (p2 != p)
            {
              if ( Distance(p2.Position, p.Position) < averageDist / 2)
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



    public void RandomizePositions(Box zone)
    {
      randomizeParticlePositions(Particles, zone);
    }

    private Link findLink(Particle a, Particle b)
    {
      foreach (var l in Links)
        if (l.A == a && l.B == b || l.A == b && l.B == a)
          return l;
      return null;
    }

    public bool AddLink(Particle a, Particle b)
    {
      if (a == b)
        return false;
      if(findLink(a, b) != null)
        return false;
      Links.Add(new Link(a, b));
      return true;
    }

    public bool RemoveLink(Particle a, Particle b)
    {
      var link = findLink(a, b);
      if(link == null)
        return false;
      Links.Remove(link);
      return true;
    }

    public void RemoveParticle(Particle p)
    {
      Links.RemoveAll(x => x.A == p || x.B == p);
      Particles.Remove(p);
    }

    unsafe public void Refresh()
    {
      if (!Active)
        return;
      engine.Sync(this);
      StepCount = engine.parameters.Out.StepCount;
      StepElapsedTime = engine.parameters.Out.StepElapsedTime;
      RealTimeScale = engine.parameters.Out.RealTimeScale;
    }
  }
}

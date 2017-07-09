#pragma once

#include "Model.h"
#include "Solver.h"
#include "StopWatch.h"

#include <mutex>
#include <thread>

template<typename Number, int Dim>
class Engine
{
public:
  Engine(Parameters& parameters, Particle<Number, Dim>* particles, long particleCount, Link* links, long linkCount)
  {
    Params = parameters;
    BarrierParams = parameters;
    ParticleCount = particleCount;
    LinkCount = linkCount;
    Items.reset(new Item[particleCount]);
    Extras.reset(new Extra[particleCount]);
    Links.reset(new Link[linkCount]);
    Barrier = new Particle<Number, Dim>[particleCount];
    for (int i = 0; i < ParticleCount; i++)
    {
      Items[i].Position = particles[i].Position;
      Items[i].Velocity = particles[i].Velocity;
      Extras[i].Mass = particles[i].Mass;
      Extras[i].Fixed = particles[i].Fixed;
      Barrier[i] = particles[i];
    }
    for (int i = 0; i < LinkCount; i++)
    {
      Links[i].A = links[i].A;
      Links[i].B = links[i].B;
      Links[i].Strength = links[i].Strength;
    }
    Solver.Initialize(particleCount * 4, &Items[0].Position.Data[0], [this](const Number* y, Number* fy) 
    { 
      return Calculate((const Item*)y, (Item*)fy); 
    });
    ShouldStop = false;

    WorkerThread = std::thread([this]()
    { 
      Run(); 
    });
  }
  ~Engine()
  {
    ShouldStop = true;
    WorkerThread.join();
  }
  void Sync(Parameters& parameters, Particle<Number, Dim>* particles)
  {
    std::lock_guard<std::mutex> lock(Mutex);

    for (int i = 0; i < ParticleCount; i++)
    {
      Barrier[i].Fixed = particles[i].Fixed;
      if (Extras[i].Fixed)
      {
        Barrier[i].Position = particles[i].Position;
        Barrier[i].Velocity = particles[i].Velocity;
      }
      else
      {
        particles[i].Position = Barrier[i].Position;
        particles[i].Velocity = Barrier[i].Velocity;
      }
    }

    memcpy(&BarrierParams.In, &parameters.In, sizeof(Params.In));
    memcpy(&parameters.Out, &BarrierParams.Out, sizeof(Params.Out));
  }

  __int64 GetStepCount()
  {
    return Params.Out.StepCount;
  }
private:
  EulerSolver<Number> Solver;
  //	RungeKuttaSolver<Number> Solver;
  struct Item
  {
    Vector<Number, Dim> Position;
    Vector<Number, Dim> Velocity;
  };

  struct Extra
  {
    double Mass;
    bool Fixed;
  };
  std::unique_ptr<Item[]> Items;
  std::unique_ptr<Link[]> Links;
  std::unique_ptr<Extra[]> Extras;

  Particle<Number, Dim>* Barrier;

  long ParticleCount;
  long LinkCount;

  Parameters Params;
  Parameters BarrierParams;
  std::thread WorkerThread;
  std::mutex Mutex;
  bool ShouldStop;

  void Calculate(const Item* inputs, Item* outputs)
  {
    for (int i = ParticleCount - 1; i >= 0; i--)
    {
      outputs[i].Velocity = {};
    }
    for (int i = ParticleCount - 1; i >= 0; i--)
    {
      for (int j = i - 1; j >= 0; j--)
      {
        auto v = (inputs[j].Position - inputs[i].Position);
        auto dist = v.Length();
        v *= Params.In.ParticleAttraction * pow(dist, Params.In.ParticlePower - 1);
        outputs[i].Velocity += v * Extras[j].Mass;
        outputs[j].Velocity -= v * Extras[i].Mass;
      }
      outputs[i].Velocity -= inputs[i].Velocity * Params.In.Viscosity;
    }

    for (int i = 0; i < LinkCount; i++)
    {
      auto& link = Links[i];
      auto v = inputs[link.B].Position - inputs[link.A].Position;
      auto dist = v.Length();
      v *= Params.In.LinkAttraction * link.Strength / pow(dist, Params.In.LinkPower - 1);
      outputs[link.A].Velocity += v * Extras[link.B].Mass;
      outputs[link.B].Velocity -= v * Extras[link.A].Mass;
      outputs[link.A].Velocity.Data[0] -= Params.In.StretchAttraction;
      outputs[link.B].Velocity.Data[0] += Params.In.StretchAttraction;
    }
    for (int i = ParticleCount - 1; i >= 0; i--)
    {
      outputs[i].Position = inputs[i].Velocity;
    }
  }

  void Run()
  {
    StopWatch stopwatchSync;
    StopWatch stopwatch;
    while (!ShouldStop)
    {
      if (stopwatchSync.Seconds() > 0.030)
      {
        {
          std::lock_guard<std::mutex> lock(Mutex);

          for (int i = 0; i < ParticleCount; i++)
          {
            Extras[i].Fixed = Barrier[i].Fixed;
            if (Barrier[i].Fixed)
            {
              Items[i].Position = Barrier[i].Position;
              Items[i].Velocity = Barrier[i].Velocity;
            }
            else
            {
              Barrier[i].Position = Items[i].Position;
              Barrier[i].Velocity = Items[i].Velocity;
            }
          }
          memcpy(&Params.In, &BarrierParams.In, sizeof(Params.In));
          memcpy(&BarrierParams.Out, &Params.Out, sizeof(Params.Out));
        }
        stopwatchSync.Reset();
      }

      double dt = stopwatch.Seconds();
      if (dt == 0)
        continue;
      stopwatch.Reset();
      Params.Out.RealTimeScale = Solver.Step(dt * Params.In.TimeScale, Params.In.Accuracy) / dt;
      Params.Out.StepCount++;
      Params.Out.StepElapsedTime = stopwatch.Seconds();
    }
  }
};

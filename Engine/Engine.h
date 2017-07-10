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
  using MyParticle = Particle<Number, Dim>;

  Engine(Parameters& parameters, 
    long particleCount,
    MyParticle* particles,
    ParticleInfo* particleInfos,
    long linkCount,
    LinkInfo* links)
  {
    Params = parameters;
    BarrierParams = parameters;
    ParticleCount = particleCount;
    LinkCount = linkCount;
    WorkingParticles.reset(new MyParticle[particleCount]);
    ParticleInfos.reset(new ParticleInfo[particleCount]);
    Links.reset(new LinkInfo[linkCount]);
    BarrierParticles.reset(new MyParticle[particleCount]);
    for (int i = 0; i < ParticleCount; i++)
    {
      WorkingParticles[i] = particles[i];
      ParticleInfos[i] = particleInfos[i];
      BarrierParticles[i] = particles[i];
    }
    for (int i = 0; i < LinkCount; i++)
    {
      Links[i] = links[i];
    }
    Solver.Initialize(particleCount * 4, &WorkingParticles[0].Position.Data[0], [this](const Number* y, Number* fy) 
    { 
      return Calculate((const MyParticle*)y, (MyParticle*)fy);
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
  void Sync(Parameters& parameters, Particle<Number, Dim>* particles, ParticleInfo* particleInfos)
  {
    std::lock_guard<std::mutex> lock(Mutex);

    for (int i = 0; i < ParticleCount; i++)
    {
      ParticleInfos[i].Fixed = particleInfos[i].Fixed;
      if (ParticleInfos[i].Fixed)
      {
        BarrierParticles[i].Position = particles[i].Position;
        BarrierParticles[i].Velocity = particles[i].Velocity;
      }
      else
      {
        particles[i].Position = BarrierParticles[i].Position;
        particles[i].Velocity = BarrierParticles[i].Velocity;
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

  std::unique_ptr<MyParticle[]> WorkingParticles;
  std::unique_ptr<LinkInfo[]> Links;
  std::unique_ptr<ParticleInfo[]> ParticleInfos;

  std::unique_ptr<MyParticle[]> BarrierParticles;

  long ParticleCount;
  long LinkCount;

  Parameters Params;
  Parameters BarrierParams;
  std::thread WorkerThread;
  std::mutex Mutex;
  bool ShouldStop;

  void Calculate(const MyParticle* inputs, MyParticle* outputs)
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
        outputs[i].Velocity += v * ParticleInfos[j].Mass;
        outputs[j].Velocity -= v * ParticleInfos[i].Mass;
      }
      outputs[i].Velocity -= inputs[i].Velocity * Params.In.Viscosity;
      outputs[i].Velocity.Data[1] += Params.In.Gravity;
    }

    for (int i = 0; i < LinkCount; i++)
    {
      auto& link = Links[i];
      auto v = inputs[link.B].Position - inputs[link.A].Position;
      auto dist = v.Length();
      v *= Params.In.LinkAttraction * link.Strength / pow(dist, Params.In.LinkPower - 1);
      outputs[link.A].Velocity += v * ParticleInfos[link.B].Mass;
      outputs[link.B].Velocity -= v * ParticleInfos[link.A].Mass;
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
            if (ParticleInfos[i].Fixed)
            {
              WorkingParticles[i].Position = BarrierParticles[i].Position;
              WorkingParticles[i].Velocity = BarrierParticles[i].Velocity;
            }
            else
            {
              BarrierParticles[i].Position = WorkingParticles[i].Position;
              BarrierParticles[i].Velocity = WorkingParticles[i].Velocity;
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

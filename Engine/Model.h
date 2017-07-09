#pragma once

#include "Vector.h"

template<typename Number, int Dim>
struct Particle
{
  Vector<Number, Dim> Position;
  Vector<Number, Dim> Velocity;
};

struct ParticleInfo
{
  double Mass;
  bool Fixed;
};


struct LinkInfo
{
  int A;
  int B;
  double Strength;
};


struct Parameters
{
  struct
  {
    double Viscosity;
    double ParticleAttraction;
    double ParticlePower;
    double LinkAttraction;
    double LinkPower;
    double StretchAttraction;
    double Accuracy;
    double TimeScale;
  } In;
  struct
  {
    double StepElapsedTime;
    double RealTimeScale;
    __int64 StepCount;
  } Out;
};

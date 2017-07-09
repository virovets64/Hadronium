#include "Engine.h"
#include "Model.h"


typedef double ValType;
const int Dimension = 2;

extern "C" __declspec(dllexport) void* EngineStart(
  Parameters* parameters, 
  __int64 particleCount, 
  Particle<ValType, Dimension>* particles, 
  ParticleInfo* particleInfos,
  __int64 linkCount, 
  LinkInfo* links)
{
  return new Engine<ValType, Dimension>(*parameters, particleCount, particles, particleInfos, linkCount, links);
}

extern "C" __declspec(dllexport) void EngineSync(
  void* engine, 
  Parameters* parameters, 
  __int64 particleCount, 
  Particle<ValType, Dimension>*& particles,
  ParticleInfo* particleInfos)
{
  ((Engine<ValType, Dimension>*)engine)->Sync(*parameters, particles, particleInfos);
}

extern "C" __declspec(dllexport) __int64 EngineStepCount(void* engine)
{
  return ((Engine<ValType, Dimension>*)engine)->GetStepCount();
}

extern "C" __declspec(dllexport) void EngineStop(void* engine)
{
  delete (Engine<ValType, Dimension>*)engine;
}

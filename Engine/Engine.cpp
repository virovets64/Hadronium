#include "Engine.h"
#include "Model.h"


typedef double ValType;
const int Dimension = 2;

extern "C" __declspec(dllexport) void* EngineStart(Parameters* parameters, Particle<ValType, Dimension>* particles, __int64 particleCount, Link* links, __int64 linkCount)
{
  return new Engine<ValType, Dimension>(*parameters, particles, particleCount, links, linkCount);
}

extern "C" __declspec(dllexport) void EngineSync(void* engine, Parameters* parameters, Particle<ValType, Dimension>*& particles, __int64)
{
  ((Engine<ValType, Dimension>*)engine)->Sync(*parameters, particles);
}

extern "C" __declspec(dllexport) __int64 EngineStepCount(void* engine)
{
  return ((Engine<ValType, Dimension>*)engine)->GetStepCount();
}

extern "C" __declspec(dllexport) void EngineStop(void* engine)
{
  delete (Engine<ValType, Dimension>*)engine;
}

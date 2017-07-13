#include "Engine.h"
#include "Model.h"



extern "C" __declspec(dllexport) void* EngineStart(
  Parameters* parameters, 
  int dimension,
  __int64 particleDataSize, 
  double* particleData, 
  __int64 particleCount,
  ParticleInfo* particleInfos,
  __int64 linkCount, 
  LinkInfo* links)
{
  EngineBase* engine;
  switch (dimension)
  {
    case 1:
      engine = new Engine<double, 1>();
      break;
    case 2:
      engine = new Engine<double, 2>();
      break;
    case 3:
      engine = new Engine<double, 3>();
      break;
    default:
      throw std::runtime_error("Invalid dimension value");
  }
  engine->Start(*parameters, particleCount, particleData, particleInfos, linkCount, links);
  return engine;
}

extern "C" __declspec(dllexport) void EngineSync(
  void* engine, 
  Parameters* parameters, 
  __int64 particleDataSize,
  double*& particleData,
  __int64 particleCount,
  ParticleInfo* particleInfos)
{
  ((EngineBase*)engine)->Sync(*parameters, particleData, particleInfos);
}

extern "C" __declspec(dllexport) __int64 EngineStepCount(void* engine)
{
  return ((EngineBase*)engine)->GetStepCount();
}

extern "C" __declspec(dllexport) void EngineStop(void* engine)
{
  delete (EngineBase*)engine;
}

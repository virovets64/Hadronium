#pragma once

#include <chrono>

class StopWatch
{
public:
  StopWatch()
  {
    Reset();
  }
  void Reset()
  {
    TimePoint = std::chrono::high_resolution_clock::now();
  }
  __int64 Microseconds() const
  {
    return std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::high_resolution_clock::now() - TimePoint).count();
  }
  double Seconds() const
  {
    return double(Microseconds()) / 1e6;
  }

private:
  std::chrono::high_resolution_clock::time_point TimePoint;
};



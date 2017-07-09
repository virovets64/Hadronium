#pragma once

#include <memory>
#include <functional>

template<typename Number>
class BasicSolver
{
public:
  typedef std::function<void(const Number*, Number*)> CalcFunction;
  virtual void Initialize(int n, Number* y, CalcFunction func)
  {
    N = n;
    Y = y;
    Function = func;
  }
  virtual double Step(double dt, double accuracy) = 0;
protected:
  int N;
  Number* Y;
  CalcFunction Function;

  Number Distance(const Number* x1, const Number* x2)
  {
    Number result = 0;
    for (int i = 0; i < N; i++)
    {
      Number x = x1[i] - x2[i];
      result += x * x;
    }
    return sqrt(result);
  }
};


template<typename Number>
class EulerSolver : public BasicSolver < Number >
{
public:
  virtual void Initialize(int n, Number* y, CalcFunction func)
  {
    BasicSolver::Initialize(n, y, func);
    FY.reset(new Number[N]);
    Y1.reset(new Number[N]);
    FY1.reset(new Number[N]);
    LastDt = 0.001;
  }
  virtual double Step(double dt, double accuracy)
  {
    if (dt > LastDt * 2)
      dt = LastDt * 2;

    Function(Y, FY.get());
    for (;;)
    {
      for (int i = 0; i < N; i++)
        Y1[i] = Y[i] + FY[i] * dt;
      Function(Y1.get(), FY1.get());

      if (Distance(FY1.get(), FY.get()) < accuracy)
        break;
      dt /= 2;
    }
    LastDt = dt;
    for (int i = 0; i < N; i++)
      Y[i] += (FY[i] + FY1[i]) / 2 * dt;
    return dt;
  }
protected:
  std::unique_ptr<Number[]> Y1;
  std::unique_ptr<Number[]> FY;
  std::unique_ptr<Number[]> FY1;
  double LastDt;
};

template<typename Number>
class RungeKuttaSolver : public BasicSolver < Number >
{
public:
  virtual void Initialize(int n, Number* y, CalcFunction func)
  {
    BasicSolver::Initialize(n, y, func);
    Y1.reset(new Number[N]);
    Y2.reset(new Number[N]);
    Y3.reset(new Number[N]);
    Y4.reset(new Number[N]);
    Tmp.reset(new Number[N]);
  }
  virtual double Step(double dt, double accuracy)
  {
    Function(Y, Y1.get());
    for (;;)
    {
      for (int i = 0; i < N; i++)
        Y2[i] = Y[i] + Y1[i] * dt;
      Function(Y2.get(), Y3.get());

      if (Distance(Y3.get(), Y1.get()) < accuracy)
        break;
      dt /= 2;
    }

    //		Function(Y, Y1.get());
    for (int i = 0; i < N; i++)
      Tmp[i] = Y[i] + Y1[i] * dt / 2.0;
    Function(Tmp.get(), Y2.get());
    for (int i = 0; i < N; i++)
      Tmp[i] = Y[i] + Y2[i] * dt / 2.0;
    Function(Tmp.get(), Y3.get());
    for (int i = 0; i < N; i++)
      Tmp[i] = Y[i] + Y3[i] * dt;
    Function(Tmp.get(), Y4.get());
    for (int i = 0; i < N; i++)
      Y[i] = Y[i] + dt / 6.0 * (Y1[i] + 2.0 * Y2[i] + 2.0 * Y3[i] + Y4[i]);
    return dt;
  }
protected:
  std::unique_ptr<Number[]> Y1;
  std::unique_ptr<Number[]> Y2;
  std::unique_ptr<Number[]> Y3;
  std::unique_ptr<Number[]> Y4;
  std::unique_ptr<Number[]> Tmp;
};


#include <math.h>
#include <process.h>
#include <Windows.h>
#include <float.h>
#include <memory>
#include <functional>
#include "StopWatch.h"
#include <amp.h>
#include <amp_math.h>

using namespace std;
using namespace concurrency;
using namespace concurrency::fast_math;

struct Vector 
{
	double X;
	double Y;
	Vector() restrict(amp, cpu)
		: X(0.), Y(0.)
	{}
	Vector(double x, double y) restrict(amp, cpu)
		: X(x), Y(y)
	{}
	double Length()
	{
		return sqrt(X * X + Y * Y);
	}
	double Length() restrict(amp)
	{
		return sqrt(X * X + Y * Y);
	}
	double LengthSquared()
	{
		return X * X + Y * Y;
	}
	double LengthSquared() restrict(amp)
	{
		return X * X + Y * Y;
	}
};

inline Vector operator + (Vector a, Vector b) 
{
	return Vector(a.X + b.X, a.Y + b.Y);		
}
inline Vector operator + (Vector a, Vector b) restrict(amp)
{
	return Vector(a.X + b.X, a.Y + b.Y);		
}
inline Vector operator - (Vector a, Vector b) 
{
	return Vector(a.X - b.X, a.Y - b.Y);		
}
inline Vector operator - (Vector a, Vector b) restrict(amp)
{
	return Vector(a.X - b.X, a.Y - b.Y);		
}
inline void operator += (Vector& a, Vector b)
{
	a.X += b.X;
	a.Y += b.Y;
}
inline void operator += (Vector& a, Vector b) restrict(amp)
{
	a.X += b.X;
	a.Y += b.Y;
}
inline void operator -= (Vector& a, Vector b)
{
	a.X -= b.X;
	a.Y -= b.Y;
}
inline void operator -= (Vector& a, Vector b) restrict(amp)
{
	a.X -= b.X;
	a.Y -= b.Y;
}
inline Vector operator * (Vector a, double k)
{
	return Vector(a.X * k, a.Y * k);		
}
inline Vector operator * (Vector a, double k) restrict(amp)
{
	return Vector(a.X * k, a.Y * k);		
}
inline Vector operator * (double k, Vector a) restrict(amp)
{
	return Vector(a.X * k, a.Y * k);		
}
inline Vector operator * (double k, Vector a)
{
	return Vector(a.X * k, a.Y * k);		
}
inline void operator *= (Vector& a, double k)
{
	a.X *= k;
	a.Y *= k;
}
inline void operator *= (Vector& a, double k) restrict(amp)
{
	a.X *= k;
	a.Y *= k;
}


struct Particle
{
    Vector Position;
    Vector Velocity;
    double Mass;
    bool Fixed;
};


struct Link
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

typedef function<double(const double*, double*)> CalcFunction;

class BasicSolver
{
public:
	virtual void Initialize(int n, double* y, CalcFunction func)
	{
		N = n;
		Y = y;
		Function = func;
	}
	virtual double Step(double dt, double accuracy) = 0;
protected:
	int N;
	double* Y;
	CalcFunction Function;

	double Distance(const double* x1, const double* x2)
	{
		double result = 0;
		for(int i = 0; i < N; i++)
		{
			double x = x1[i] - x2[i];
			result += x * x;
		}
		return sqrt(result);
	}
};


class EulerSolver: public BasicSolver
{
public:
	virtual void Initialize(int n, double* y, CalcFunction func)
	{
		BasicSolver::Initialize(n, y, func);
		FY.reset(new double[N]);
		Y1.reset(new double[N]);
		FY1.reset(new double[N]);
		LastDt = 0.001;
	}
	virtual double Step(double dt, double accuracy)
	{
		if(dt > LastDt * 2)
			dt = LastDt * 2;

		Function(Y, FY.get());
		for(;;)
		{
			for(int i = 0; i < N; i++)
				Y1[i] = Y[i] + FY[i] * dt;
			Function(Y1.get(), FY1.get());

			if(Distance(FY1.get(), FY.get()) < accuracy)
				break;
			dt /= 2;
		}
		LastDt = dt;
		for(int i = 0; i < N; i++)
			Y[i] += FY[i] * dt;
		return dt;
	}
protected:
	unique_ptr<double[]> Y1;
	unique_ptr<double[]> FY;
	unique_ptr<double[]> FY1;
	double LastDt;
};

class RungeKuttaSolver: public BasicSolver
{
public:
	virtual void Initialize(int n, double* y, CalcFunction func)
	{
		BasicSolver::Initialize(n, y, func);
		Y1.reset(new double[N]);
		Y2.reset(new double[N]);
		Y3.reset(new double[N]);
		Y4.reset(new double[N]);
		Tmp.reset(new double[N]);
	}
	virtual double Step(double dt, double accuracy)
	{
		Function(Y, Y1.get());
		for(;;)
		{
			for(int i = 0; i < N; i++)
				Y2[i] = Y[i] + Y1[i] * dt;
			Function(Y2.get(), Y3.get());

			if(Distance(Y3.get(), Y1.get()) < accuracy)
				break;
			dt /= 2;
		}

//		Function(Y, Y1.get());
        for (int i = 0; i < N; i++)
            Tmp[i] = Y[i] + Y1[i] * dt / 2.0;
		Function(Tmp.get(), Y2.get());
		for(int i = 0; i < N; i++)
			Tmp[i] = Y[i] + Y2[i] * dt / 2.0;
		Function(Tmp.get(), Y3.get());
		for(int i = 0; i < N; i++)
			Tmp[i] = Y[i] + Y3[i] * dt;
		Function(Tmp.get(), Y4.get());
		for(int i = 0; i < N; i++)
			Y[i] = Y[i] + dt / 6.0 * (Y1[i] + 2.0 * Y2[i] + 2.0 * Y3[i] + Y4[i]); 
		return dt;
	}
protected:
	unique_ptr<double[]> Y1;
	unique_ptr<double[]> Y2;
	unique_ptr<double[]> Y3;
	unique_ptr<double[]> Y4;
	unique_ptr<double[]> Tmp;
};


class Engine 
{
public:
	Engine(Parameters& parameters, Particle* particles, long particleCount, Link* links, long linkCount)
	{
		Params = parameters;
		BarrierParams = parameters;
		ParticleCount = particleCount;
		LinkCount = linkCount;
		Items = new Item[particleCount];
		Extras = new Extra[particleCount];
		Links = new Link[linkCount];
		Barrier = new Particle[particleCount];
		for(int i = 0; i < ParticleCount; i++)
		{
			Items[i].Position = particles[i].Position;
			Items[i].Velocity = particles[i].Velocity;
			Extras[i].Mass = particles[i].Mass;
			Extras[i].Fixed = particles[i].Fixed;
			Barrier[i] = particles[i];
		}
		for(int i = 0; i < LinkCount; i++)
		{
			Links[i].A = links[i].A;
			Links[i].B = links[i].B;
			Links[i].Strength = links[i].Strength;
		}
		Solver.Initialize(particleCount * 4, &Items[0].Position.X, [this](const double* y, double* fy) { return Calculate((const Item*)y, (Item*)fy); });
		ShouldStop = false;
		InitializeCriticalSection(&CriticalSection);
		ThreadHandle = (HANDLE)_beginthreadex(NULL, 0, ThreadProc, this, 0, NULL);	
	}
	~Engine()
	{
		ShouldStop = true;
		WaitForSingleObject(ThreadHandle, INFINITE);
		DeleteCriticalSection(&CriticalSection);
		delete [] Links; 
		delete [] Items; 
		delete [] Extras; 
	}
	void Sync(Parameters& parameters, Particle* particles)
	{
		EnterCriticalSection(&CriticalSection);

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

		LeaveCriticalSection(&CriticalSection);
	}
	__int64 GetStepCount()
	{
		return Params.Out.StepCount;
	}
private:
    accelerator default_device;
	EulerSolver Solver;
//	RungeKuttaSolver Solver;
	struct Item
	{
	    Vector Position;
		Vector Velocity;
	};

	struct Extra
	{
	    double Mass;
		bool Fixed;
	};
	Item* Items;
	Extra* Extras;
    Particle* Barrier;

	long ParticleCount;
	long LinkCount;

    Link* Links;
	Parameters Params;
	Parameters BarrierParams;
	HANDLE ThreadHandle;
	CRITICAL_SECTION CriticalSection;
	bool ShouldStop;
	static unsigned __stdcall ThreadProc(void *arg)
	{
		((Engine*)arg)->Run();
		return 0;
	}


	double Calculate(const Item* inputs, Item* outputs)
	{

		array_view<const Item, 1> in(ParticleCount, inputs);
		array_view<Item, 1> out(ParticleCount, outputs);

		out.discard_data();

		int particleCount = ParticleCount;
		double particleAttraction = Params.In.ParticleAttraction;
		double particlePower = Params.In.ParticlePower;
		double viscosity = Params.In.Viscosity;

		parallel_for_each( 
			out.extent, 
			[=](index<1> i) restrict(amp)
		{
			out[i].Velocity.X = out[i].Velocity.Y = 0;
			for (int j = particleCount - 1; j >= 0;  j--)
			{
				if(i[0] != j)
				{
					Vector v = in[j].Position - in[i].Position;
					double dist = v.Length();
					v *= particleAttraction * pow(dist, particlePower - 1);
					out[i].Velocity += v /* * Extras[j].Mass*/;
				}
            }
            out[i].Velocity -= in[i].Velocity * viscosity;
		}
		);

		out.synchronize();

		auto endLink = Links + LinkCount;
        for (auto l = Links; l != endLink; l++)
        {
            auto v = Items[l->B].Position - Items[l->A].Position;
            double dist = v.Length();
            v *= Params.In.LinkAttraction * l->Strength / pow(dist, Params.In.LinkPower - 1);
            outputs[l->A].Velocity += v * Extras[l->B].Mass;
            outputs[l->B].Velocity -= v * Extras[l->A].Mass;
            outputs[l->A].Velocity.X -= Params.In.StretchAttraction;
            outputs[l->B].Velocity.X += Params.In.StretchAttraction;
        }
		for (int i = ParticleCount - 1; i >= 0; i--)
        {
			outputs[i].Position = inputs[i].Velocity;
        }
		return 0;
    }

	void Run()
	{
        double dt = 0.001;
        TStopWatch stopwatchSync;
        TStopWatch stopwatch;
        while (!ShouldStop)
        {
			if (stopwatchSync.GetSeconds() > 0.030)
            {
				EnterCriticalSection(&CriticalSection);
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
				LeaveCriticalSection(&CriticalSection);
                stopwatchSync.Reset();
            }
			dt = stopwatch.GetSeconds();
            stopwatch.Reset();
			Params.Out.RealTimeScale = Solver.Step(dt * Params.In.TimeScale, Params.In.Accuracy) / dt;
			Params.Out.StepCount++;
			Params.Out.StepElapsedTime = stopwatch.GetSeconds();
//			if(Params.Out.StepElapsedTime < 0.010)
//				Sleep(10);
        }
	}
};

extern "C" __declspec(dllexport) void* EngineStart(Parameters* parameters, Particle* particles, __int64 particleCount, Link* links, __int64 linkCount)
{
	return new Engine(*parameters, particles, particleCount, links, linkCount);
}

extern "C" __declspec(dllexport) void EngineSync(void* engine, Parameters* parameters, Particle*& particles, __int64)
{
	((Engine*)engine)->Sync(*parameters, particles);
}

extern "C" __declspec(dllexport) __int64 EngineStepCount(void* engine)
{
	return ((Engine*)engine)->GetStepCount();
}

extern "C" __declspec(dllexport) void EngineStop(void* engine)
{
	delete (Engine*)engine;
}

extern "C" __declspec(dllexport) void Test1(Particle*& values, __int64 count)
{
	for(__int64 i = 0; i < count; i++)
		values[i].Position.X = 5;
}


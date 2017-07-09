#pragma once

#include <array>

template<typename Number, int Dim>
struct Vector
{
  std::array<Number, Dim> Data;
  Vector()
    : Data{}
  {}

  Number LengthSquared()
  {
    Number result{};
    for (auto d : Data)
      result += d * d;
    return result;
  }

  Number Length()
  {
    return sqrt(LengthSquared());
  }

  inline Vector<Number, Dim> operator += (const Vector<Number, Dim>& other)
  {
    for (int i = 0; i < Dim; i++)
      Data[i] += other.Data[i];
    return *this;
  }

  inline Vector<Number, Dim> operator -= (const Vector<Number, Dim>& other)
  {
    for (int i = 0; i < Dim; i++)
      Data[i] -= other.Data[i];
    return *this;
  }

  inline Vector<Number, Dim> operator *= (Number k)
  {
    for (int i = 0; i < Dim; i++)
      Data[i] *= k;
    return *this;
  }
};

template<typename Number, int Dim>
inline Vector<Number, Dim> operator + (const Vector<Number, Dim>& a, const Vector<Number, Dim>& b)
{
  auto tmp(a);
  return tmp += b;
}

template<typename Number, int Dim>
inline Vector<Number, Dim> operator - (const Vector<Number, Dim>& a, const Vector<Number, Dim>& b)
{
  auto tmp(a);
  return tmp -= b;
}

template<typename Number, int Dim>
inline Vector<Number, Dim> operator * (const Vector<Number, Dim>& a, Number k)
{
  auto tmp(a);
  return tmp *= k;
}

template<typename Number, int Dim>
inline Vector<Number, Dim> operator * (Number k, const Vector<Number, Dim>& a)
{
  auto tmp(a);
  return tmp *= k;
}

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public struct Chunk
{
  public string pathName;
  public Vector3 position;
  public Matrix4x4 Matrix;
  public Bounds bounds;
  public Chunk(string _pathName, Vector3 _position, Matrix4x4 _Matrix, Bounds _bounds)
  {
    pathName = _pathName;
    position = _position;
    Matrix = _Matrix;
    bounds = _bounds;
  }
}
[Serializable]
public struct Grass_Cate
{
  public Material mat;
  public Mesh mesh;
  public int CountPerMeter;
}
public struct Chunk_To_Cam
{
  public float dis;
  public int ID;
}
[Serializable]
public struct Grass
{
  public Vector3 position;
}
[Serializable]
public struct GrassInfo
{
  public Matrix4x4 transform;
  public Vector3 center;
  public float radius;
  public GrassInfo(Matrix4x4 _transform, Vector3 _center, float _radius)
  {
    transform = _transform;
    center = _center;
    radius = _radius;
  }
}
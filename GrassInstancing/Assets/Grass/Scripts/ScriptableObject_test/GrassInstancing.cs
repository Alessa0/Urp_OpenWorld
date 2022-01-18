using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Linq;

public class GrassInstancing
{

  public GrassChunkData chunk;

  private Material _grassMaterial;
  private Mesh _instanceMesh;
  private Vector2 _grassQuadSize = new Vector2(1.0f, 1.0f);
  private int _maxGrassCount = 10000;
  private int _subMeshIndex = 0;
  int _cachedInstanceCount = -1;
  int _cachedSubMeshIndex = -1;
  uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
  private ComputeShader Cullingcompute;
  private int _kernel;
  ComputeBuffer _objectIndirectBuffer;
  ComputeBuffer _objectBoundBuffer;
  ComputeBuffer _visibilityIDBuffer;//RW
  ComputeBuffer _drawCommandBuffer;//RW
  private int _grassCount;
  private List<Vector3> pos = new List<Vector3>();

  public GrassInstancing(Grass_Cate grass_Cate, ComputeShader Culling, GrassChunkData data)
  {
    Cullingcompute = Culling;
    _instanceMesh = grass_Cate.mesh;
    _grassMaterial = grass_Cate.mat;
    chunk = data;
    _drawCommandBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    _visibilityIDBuffer = new ComputeBuffer(_maxGrassCount, sizeof(uint));
    _kernel = Cullingcompute.FindKernel("CSMain");
    UpdateBufferData();
    _actives.Add(this);
  }
  public void Dispose()
  {
    _actives.Remove(this);

    _drawCommandBuffer?.Release();
    _drawCommandBuffer = null;

    _objectBoundBuffer?.Release();
    _objectBoundBuffer = null;

    _objectIndirectBuffer?.Release();
    _objectIndirectBuffer = null;

    _visibilityIDBuffer?.Release();
    _visibilityIDBuffer = null;
  }
  private void ReleaseComputeBuffer(ComputeBuffer computeBuffer)
  {
    computeBuffer.Release();
    computeBuffer = null;
  }
  public void Render()
  {

    if (_cachedInstanceCount != _grassCount || _cachedSubMeshIndex != _subMeshIndex)
    {
      UpdateBufferData();
    }
    return;
  }



  //private void UpdateBuffer()
  //{
  //  if (_objectIndirectBuffer != null)
  //  {
  //    return;
  //  }
  //  GameObject gameObject = GameObject.Find(chunk.pathName);
  //  var terrianMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
  //  var terrianPos = chunk.position;
  //  var grassIndex = 0;
  //  List<GrassInfo> grassBound = new List<GrassInfo>();
  //  List<uint> Indirect = new List<uint>();
  //  var indices = terrianMesh.triangles;
  //  var vertices = terrianMesh.vertices;

  //  for (int j = 0; j < indices.Length / 3; j++)
  //  {
  //    var index1 = indices[j * 3];
  //    var index2 = indices[j * 3 + 1];
  //    var index3 = indices[j * 3 + 2];
  //    var v1 = vertices[index1];
  //    var v2 = vertices[index2];
  //    var v3 = vertices[index3];

  //    //面得到法向
  //    var normal = Util.GetFaceNormal(v1, v2, v3);

  //    //计算up到faceNormal的旋转四元数
  //    var upToNormal = Quaternion.FromToRotation(Vector3.up, normal);

  //    //三角面积
  //    var arena = Util.GetAreaOfTriangle(v1, v2, v3);

  //    //计算在该三角面中，需要种植的数量
  //    var countPerTriangle = Mathf.Max(1, _grassCountPerMeter * arena);
  //    for (int i = 0; i < countPerTriangle; i++)
  //    {
  //      var positionInTerrian = Util.RandomPointInsideTriangle(v1, v2, v3);
  //      float rot = UnityEngine.Random.Range(0, 180);
  //      var localToTerrian = Matrix4x4.TRS(positionInTerrian, upToNormal * Quaternion.Euler(0, rot, 0), Vector3.one);

  //      Vector2 texScale = Vector2.one;
  //      Vector2 texOffset = Vector2.zero;
  //      float3 _center = terrianPos + positionInTerrian;
  //      pos.Add(_center);
  //      var grassInfo = new GrassInfo()
  //      {
  //        transform = localToTerrian,
  //        center = _center,
  //        radius = 0.6f
  //      };
  //      grassBound.Add(grassInfo);
  //      Indirect.Add((uint)grassIndex);
  //      grassIndex++;
  //      if (grassIndex >= _maxGrassCount)
  //      {
  //        break;
  //      }
  //    }
  //    if (grassIndex >= _maxGrassCount)
  //    {
  //      break;
  //    }
  //  }
  //  //foreach (var v in terrianMesh.vertices)
  //  //{
  //  //  var vertexPosition = v;
  //  //  for (var j = 0; j < _grassCountPerMeter; j++)
  //  //  {
  //  //    Vector3 offset = vertexPosition + new Vector3(UnityEngine.Random.Range(0, 1f), 0, UnityEngine.Random.Range(0, 1f));
  //  //    float rot = UnityEngine.Random.Range(0, 180);
  //  //    var localToTerrian = Matrix4x4.TRS(offset, Quaternion.Euler(0, rot, 0), Vector3.one);
  //  //    float3 _center = terrianPos + offset;
  //  //    pos.Add(_center);
  //  //    var grassInfo = new GrassInfo()
  //  //    {
  //  //      transform = localToTerrian,
  //  //      center = _center,
  //  //      radius = 0.6f
  //  //    };
  //  //    grassBound.Add(grassInfo);
  //  //    Indirect.Add((uint)grassIndex);
  //  //    grassIndex++;
  //  //    if (grassIndex >= _maxGrassCount)
  //  //    {
  //  //      break;
  //  //    }
  //  //  }
  //  //  if (grassIndex >= _maxGrassCount)
  //  //  {
  //  //    break;
  //  //  }
  //  //}
  //  _grassCount = grassIndex;
  //  _cachedInstanceCount = _grassCount;
  //  _objectIndirectBuffer = new ComputeBuffer(_grassCount, sizeof(uint));
  //  _objectIndirectBuffer.SetData(Indirect);
  //  _objectBoundBuffer = new ComputeBuffer(_grassCount, 64 + 4 * sizeof(float));
  //  _objectBoundBuffer.SetData(grassBound);
  //}
  private void UpdateBufferData()
  {
    if (_objectIndirectBuffer != null)
    {
      return;
    }
    List<GrassInfo> grass = ReadData(chunk);
    _grassCount = grass.Count;
    _cachedInstanceCount = _grassCount;
    _objectIndirectBuffer = new ComputeBuffer(_grassCount, sizeof(uint));
    _objectIndirectBuffer.SetData(chunk.indirect);
    _objectBoundBuffer = new ComputeBuffer(_grassCount, 64 + 4 * sizeof(float));
    _objectBoundBuffer.SetData(grass);
  }
  public void CreatCmd(CommandBuffer cmd, Camera cam)
  {
    var index = 0;
    foreach (var grass in actives)
    {
      if (grass == null)
      {
        continue;
      }
      if (!grass._grassMaterial)
      {
        continue;
      }
      ExecuteCulling(cmd, cam);
      var visibilityIDBuffer = _visibilityIDBuffer;
      cmd.SetGlobalMatrix("_LocalToWorld", chunk.localToWorld);
      cmd.SetGlobalVector("_GrassQuadSize", _grassQuadSize);
      cmd.SetGlobalBuffer("_GrassInfos", _objectBoundBuffer);
      cmd.SetGlobalBuffer("_VisibilityIDBuffer", visibilityIDBuffer);
      cmd.DrawMeshInstancedIndirect(grass._instanceMesh, grass._subMeshIndex, grass._grassMaterial, 0, grass._drawCommandBuffer);
      index++;
    }
  }
  private List<GrassInstancing> _actives = new List<GrassInstancing>();
  public IReadOnlyCollection<GrassInstancing> actives
  {
    get
    {
      return _actives;
    }
  }



  // Indirect args
  public void ClearDrawCommands()
  {
    var instanceMesh = _instanceMesh;
    args = new uint[5] { 0, 0, 0, 0, 0 };
    if (_instanceMesh != null)
    {
      args[0] = (uint)instanceMesh.GetIndexCount(_subMeshIndex);
      args[1] = 0;
      args[2] = (uint)instanceMesh.GetIndexStart(_subMeshIndex);
      args[3] = (uint)instanceMesh.GetBaseVertex(_subMeshIndex);
      args[4] = 0;
    }
    else
      args[0] = args[1] = args[2] = args[3] = 0;
    _drawCommandBuffer.SetData(args);
    _cachedSubMeshIndex = _subMeshIndex;
  }

  public static float4 NormalizePlane(float4 p)
  {
    float l = math.length(p.xyz);
    return p * (1.0f / l);
  }

  public void ExecuteCulling(CommandBuffer cmd, Camera camera)
  {
    float4x4 projection = camera.projectionMatrix;
    float4x4 projectionT = transpose(projection);
    float4 frustumX = NormalizePlane(projectionT[3] + projectionT[0]);
    float4 frustumY = NormalizePlane(projectionT[3] + projectionT[1]);
    float4 frustum = float4(frustumX.x, frustumX.z, frustumY.y, frustumY.z);
    cmd.SetComputeFloatParam(Cullingcompute, "_NearClipPlane", camera.nearClipPlane);
    cmd.SetComputeFloatParam(Cullingcompute, "_FarClipPlane", camera.farClipPlane);
    cmd.SetComputeMatrixParam(Cullingcompute, "_ViewMatrix", camera.worldToCameraMatrix);
    cmd.SetComputeVectorParam(Cullingcompute, "_GPUCullingFrustum", frustum);
    cmd.SetComputeBufferParam(Cullingcompute, _kernel, "_ObjectBoundBuffer", _objectBoundBuffer);
    cmd.SetComputeBufferParam(Cullingcompute, _kernel, "_ObjectIndirectBuffer", _objectIndirectBuffer);
    cmd.SetComputeBufferParam(Cullingcompute, _kernel, "_VisibilityIDBuffer", _visibilityIDBuffer);
    cmd.SetComputeBufferParam(Cullingcompute, _kernel, "_DrawCommandBuffer", _drawCommandBuffer);
    int groupX = Mathf.CeilToInt(_grassCount / 64f);
    cmd.DispatchCompute(Cullingcompute, _kernel, groupX, 1, 1);
  }
  List<GrassInfo> ReadData(GrassChunkData dataObject)
  {
    int instanceCount = 0;
    instanceCount += dataObject.grass.Count;
    Debug.Log("data count : " + dataObject.grass.Count);
    List<GrassInfo> grass = new List<GrassInfo>(instanceCount);
    int itemCount = dataObject.grass.Count;
    for (int j = 0; j < itemCount; j++)
    {
      grass.Add(dataObject.grass[j]);
    }
    return grass;
  }

}

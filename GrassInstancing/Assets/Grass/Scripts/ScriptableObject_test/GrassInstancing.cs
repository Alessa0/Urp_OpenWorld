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
  ComputeBuffer _visibilityIDBuffer;
  ComputeBuffer _drawCommandBuffer;
  private int _grassCount;

  public GrassInstancing( ComputeShader Culling, GrassChunkData data)
  {
    Cullingcompute = Culling;
    _instanceMesh = data.grassCate.mesh;
    _grassMaterial = data.grassCate.mat;
    chunk = data;
    _drawCommandBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    _visibilityIDBuffer = new ComputeBuffer(_maxGrassCount, sizeof(uint));
    _kernel = Cullingcompute.FindKernel("CSMain");
    UpdateBufferData();
  }
  public void Dispose()
  {
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
  public void Render(CommandBuffer cmd, Camera cam)
  {
    if (_cachedInstanceCount != _grassCount || _cachedSubMeshIndex != _subMeshIndex)
    {
      UpdateBufferData();
    }
    ClearDrawCommands();
    CreatCmd(cmd, cam);
    return;
  }
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
    ExecuteCulling(cmd, cam);
    var visibilityIDBuffer = _visibilityIDBuffer;
    cmd.SetGlobalMatrix("_LocalToWorld", chunk.localToWorld);
    cmd.SetGlobalVector("_GrassQuadSize", _grassQuadSize);
    cmd.SetGlobalBuffer("_GrassInfos", _objectBoundBuffer);
    cmd.SetGlobalBuffer("_VisibilityIDBuffer", visibilityIDBuffer);
    cmd.DrawMeshInstancedIndirect(_instanceMesh, _subMeshIndex, _grassMaterial, 0, _drawCommandBuffer);
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
    //Debug.Log("data count : " + dataObject.grass.Count);
    List<GrassInfo> grass = new List<GrassInfo>(instanceCount);
    int itemCount = dataObject.grass.Count;
    for (int j = 0; j < itemCount; j++)
    {
      grass.Add(dataObject.grass[j]);
    }
    return grass;
  }

}

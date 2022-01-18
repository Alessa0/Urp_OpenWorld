using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Linq;


public class MeshGrass : MonoBehaviour
{
  private GrassRenderFeature feature;
  public struct GrassInfo
  {
    public Matrix4x4 transform;
    public float3 center;
    public float radius;
  }

  [SerializeField] private Material _grassMaterial;
  [SerializeField] private Mesh _instanceMesh;
  [SerializeField] private int _grassCountPerMeter = 2;
  private int _maxGrassCount = 10000;
  private int _subMeshIndex = 0;
  int _cachedInstanceCount = -1;
  int _cachedSubMeshIndex = -1;
  uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
  public ComputeShader Cullingcompute;
  private int _kernel;
  ComputeBuffer _objectIndirectBuffer;
  ComputeBuffer _objectBoundBuffer;
  ComputeBuffer _visibilityIDBuffer;//RW
  ComputeBuffer _drawCommandBuffer;//RW
  private int _grassCount;

  //---------------------------------------------------------------------------------
  private List<Vector3> pos = new List<Vector3>();

  void Start()
  {
    _drawCommandBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    _visibilityIDBuffer = new ComputeBuffer(_maxGrassCount, sizeof(uint));
    UpdateBuffer();
    _kernel = Cullingcompute.FindKernel("CSMain");
  }
  private void Update()
  {
    Camera cam = Camera.main;
    if (_cachedInstanceCount != _grassCount || _cachedSubMeshIndex != _subMeshIndex)
    {
      UpdateBuffer();
    }
    ClearDrawCommands();
    //CMD
    CommandBuffer cmd = CommandBufferPool.Get("Grass");
    cmd.Clear();
    CreatCmd(cmd, cam);
    feature.AddCommandbuffer(cmd);
  }

  private void CreatCmd(CommandBuffer cmd, Camera cam)
  {
    var index = 0;
    foreach (var grass in actives)
    {
      if (!grass)
      {
        continue;
      }
      if (!grass._grassMaterial)
      {
        continue;
      }
      ExecuteCulling(cmd, cam);
      var visibilityIDBuffer = _visibilityIDBuffer;
      cmd.SetGlobalMatrix("_LocalToWorld", transform.localToWorldMatrix);
      cmd.SetGlobalBuffer("_GrassInfos", _objectBoundBuffer);
      cmd.SetGlobalBuffer("_VisibilityIDBuffer", visibilityIDBuffer);
      cmd.DrawMeshInstancedIndirect(grass._instanceMesh, grass._subMeshIndex, grass._grassMaterial, 0, grass._drawCommandBuffer);
      index++;
    }
  }

  private void OnValidate()
  {
    if (feature == null)
    {
      try
      {
        var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        var scriptableRendererData = (ScriptableRendererData)urpAsset.GetType().GetProperty("scriptableRendererData"
            , BindingFlags.NonPublic | BindingFlags.Instance).GetValue(urpAsset);
        feature = (GrassRenderFeature)scriptableRendererData.rendererFeatures.FirstOrDefault(x => x is GrassRenderFeature);
      }
      catch (System.Exception)
      {
        throw;
      }
    }
  }

  private void UpdateBuffer()
  {
    if (_objectIndirectBuffer != null)
    {
      return;
    }
    var filter = GetComponent<MeshFilter>();
    var terrianMesh = filter.sharedMesh;
    var grassIndex = 0;
    List<GrassInfo> grassBound = new List<GrassInfo>();
    List<uint> Indirect = new List<uint>();
    foreach (var v in terrianMesh.vertices)
    {
      var vertexPosition = v;

      for (var j = 0; j < _grassCountPerMeter; j++)
      {
        Vector3 offset = vertexPosition + new Vector3(UnityEngine.Random.Range(0, 1f), 0, UnityEngine.Random.Range(0, 1f));
        float rot = UnityEngine.Random.Range(0, 180);
        var localToTerrian = Matrix4x4.TRS(offset, Quaternion.Euler(0, rot, 0), Vector3.one);
        float3 _center = transform.position + offset;
        pos.Add(_center);
        var grassInfo = new GrassInfo()
        {
          transform = localToTerrian,
          center = _center,
          radius = 0.6f
        };
        grassBound.Add(grassInfo);
        Indirect.Add((uint)grassIndex);
        grassIndex++;
        if (grassIndex >= _maxGrassCount)
        {
          break;
        }
      }
      if (grassIndex >= _maxGrassCount)
      {
        break;
      }
    }
    _grassCount = grassIndex;
    _cachedInstanceCount = _grassCount;
    _objectIndirectBuffer = new ComputeBuffer(_grassCount, sizeof(uint));
    _objectIndirectBuffer.SetData(Indirect);
    _objectBoundBuffer = new ComputeBuffer(_grassCount, 64 + 4 * sizeof(float));
    _objectBoundBuffer.SetData(grassBound);
  }

  private static List<MeshGrass> _actives = new List<MeshGrass>();
  public static IReadOnlyCollection<MeshGrass> actives
  {
    get
    {
      return _actives;
    }
  }

  void OnEnable()
  {
    _actives.Add(this);
  }

  void OnDisable()
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


}



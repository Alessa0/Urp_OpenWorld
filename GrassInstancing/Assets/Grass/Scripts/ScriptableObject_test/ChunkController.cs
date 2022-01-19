using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class ChunkController : MonoBehaviour
{
  private GrassRenderFeature _feature;

  //public Material[] _grassMaterial;
  //public Mesh[] _instanceMesh;
  public ComputeShader _cullingcompute;
  public float _visibleRange = 40f;
  private List<GrassChunkData> _dataObjects;
  private List<GrassInstancing> _visibleGrass;
  private List<int> _visibleID;
  private GameObject[] _chunks;
  private Plane[] _planes;
  private Vector3 _camPos;
  int _visibleCount;
  public static ChunkController instance = null;
  void Awake()
  {
    if (instance != null && instance != this)
      Destroy(gameObject);
    instance = this;
  }

  void Start()
  {
    _dataObjects = new List<GrassChunkData>();
    GameObject[] terrainGOs = Util.CollectDataInScene("Chunks");
    for (int i = 0; i < terrainGOs.Length; i++)
    {
      Object obj = Resources.Load("GrassChunksData/GPUData_" + terrainGOs[i].name);
      GrassChunkData data = ScriptableObject.Instantiate(obj) as GrassChunkData;
      _dataObjects.Add(data);
    }
    _visibleGrass = new List<GrassInstancing>();
    _visibleID = new List<int>();
    _planes = new Plane[6];
    _chunks = terrainGOs;
    for (int i = 0; i < _dataObjects.Count; i++)
    {
      _visibleID.Add(i);
      GrassInstancing grass = new GrassInstancing(_cullingcompute, _dataObjects[i]);
      _visibleGrass.Add(grass);
    }
  }

  void Update()
  {
    _camPos = GameObject.Find("Main Camera").transform.position;
    Camera cam = Camera.main;
   _visibleCount = FindChunksInRange();
    Draw(cam);
  }
  private void Draw(Camera cam)
  {
    CommandBuffer _cmd = CommandBufferPool.Get("Grass");
    _cmd.Clear();
    for (int i = 0; i < _visibleCount; i++)
    {
      _visibleGrass[_visibleID[i]].Render(_cmd, cam);
    }
    _feature.AddCommandbuffer(_cmd);

    GeometryUtility.CalculateFrustumPlanes(cam, _planes);
    for (var index = 0; index < _chunks.Length; index++)
    {
      var result = GeometryUtility.TestPlanesAABB(_planes, _chunks[index].GetComponent<MeshRenderer>().bounds)
        && Util.CalculateDis(_chunks[index].transform.position, _camPos)<=_visibleRange;
      _chunks[index].SetActive(result);
    }
  }

  private void OnDestroy()
  {
    for (int i = 0; i < _dataObjects.Count; i++)
    {
      _visibleGrass[i].Dispose();
    }
  }

  private void OnValidate()
  {
    if (_feature == null)
    {
      try
      {
        var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        var scriptableRendererData = (ScriptableRendererData)urpAsset.GetType().GetProperty("scriptableRendererData"
            , BindingFlags.NonPublic | BindingFlags.Instance).GetValue(urpAsset);
        _feature = (GrassRenderFeature)scriptableRendererData.rendererFeatures.FirstOrDefault(x => x is GrassRenderFeature);
      }
      catch (System.Exception)
      {
        throw;
      }
    }
  }
  private int FindChunksInRange()
  {
    var count = 0;
    for (int i = 0; i < _dataObjects.Count; i++)
    {
      float t = Util.CalculateDis(_chunks[i].transform.position, _camPos);
      if (t<=_visibleRange)
      {
        _visibleID[count] =(i);
        count++;
      }
    }
    return count;
  }

}

using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class ChunkController : MonoBehaviour
{
  private List<GrassChunkData> _DataObjects;
  public Material[] _grassMaterial;
  public Mesh[] _instanceMesh;
  public ComputeShader Cullingcompute;
  private GrassRenderFeature feature;
  private List<GrassInstancing> m_grassGenerator;
  private List<GrassInstancing> cur_grassGenerator;
  private List<List<GrassInstancing>> _grassGenerator;
  private int[,] _3X3ChunksID;
  private GameObject[] CullingTestObjects;
  private Plane[] planes;
  private Vector3 _CamPos;
  List<Grass_Cate> grass_Cates;
  float[,] chunk_dis;
  int _LastID;
  public static ChunkController instance = null;
  void Awake()
  {
    if (instance != null && instance != this)
      Destroy(gameObject);

    instance = this;
  }

  void Start()
  {
    _DataObjects = new List<GrassChunkData>();
    GameObject[] terrainGOs = Util.CollectDataInScene("Chunks");
    for (int i = 0; i < terrainGOs.Length; i++)
    {
      Object obj = Resources.Load("GrassChunksData/GPUData_" + terrainGOs[i].name);
      GrassChunkData data = ScriptableObject.Instantiate(obj) as GrassChunkData;

      _DataObjects.Add(data);
    }
    Debug.Log(_DataObjects.Count);
    _CamPos = GameObject.Find("Main Camera").transform.position;
    grass_Cates = new List<Grass_Cate>();

    planes = new Plane[6];
    _3X3ChunksID = Util.MatrixSplit();

    CullingTestObjects = terrainGOs;

    for (int i = 0; i < _grassMaterial.Length; i++)
    {
      Grass_Cate category = new Grass_Cate();
      category.mat = _grassMaterial[i];
      category.mesh = _instanceMesh[i];
      grass_Cates.Add(category);
    }
    m_grassGenerator = new List<GrassInstancing>();
    cur_grassGenerator = new List<GrassInstancing>(9);
    for (int i = 0; i < _DataObjects.Count; i++)
    {
      var r = Random.Range(0, 2);
      GrassInstancing grass = new GrassInstancing(grass_Cates[r], Cullingcompute, _DataObjects[i]);
      m_grassGenerator.Add(grass);
    }
    int ID = DistanceSort();
    for (int i = 0; i < 9; i++)
    {
      GrassInstancing grass = new GrassInstancing(grass_Cates[i % 2], Cullingcompute, _DataObjects[_3X3ChunksID[ID, i]]);
      cur_grassGenerator.Add(grass);
    }
    _LastID = ID;
  }

  void Update()
  {
    _CamPos = GameObject.Find("Main Camera").transform.position;
    Camera cam = Camera.main;
    GeometryUtility.CalculateFrustumPlanes(cam, planes);
    for (var index = 0; index < CullingTestObjects.Length; index++)
    {
      var result = GeometryUtility.TestPlanesAABB(planes, CullingTestObjects[index].GetComponent<MeshRenderer>().bounds);
      CullingTestObjects[index].SetActive(result);
    }

    int ID = DistanceSort();
    if (_LastID != ID)
    {
      for (int i = 0; i < 9; i++)
      {
        GrassInstancing grass = new GrassInstancing(grass_Cates[i % 2], Cullingcompute, _DataObjects[_3X3ChunksID[ID, i]]);
        cur_grassGenerator[i] = (grass);
      }
      _LastID = ID;
      Debug.Log(ID);
    }

    Draw(cam);

  }
  private void Draw(Camera cam)
  {
    //for (int i = 0; i < _DataObjects.Count; i++)
    //{
    //    m_grassGenerator[i].Render();
    //    m_grassGenerator[i].ClearDrawCommands();
    //}
    for (int i = 0; i < 9; i++)
    {
      cur_grassGenerator[i].Render();
      cur_grassGenerator[i].ClearDrawCommands();
    }
    CommandBuffer _cmd = CommandBufferPool.Get("Grass");
    _cmd.Clear();
    //for (int i = 0; i < _DataObjects.Count; i++)
    //{
    //    m_grassGenerator[i].CreatCmd(_cmd, cam);
    //}
    for (int i = 0; i < 9; i++)
    {
      cur_grassGenerator[i].CreatCmd(_cmd, cam);
    }
    feature.AddCommandbuffer(_cmd);
  }



  private void OnDestroy()
  {
    //for (int i = 0; i < _DataObjects.Count; i++)
    //{
    //    m_grassGenerator[i].Dispose();
    //}
    for (int i = 0; i < 9; i++)
    {
      cur_grassGenerator[i].Dispose();
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
  private int DistanceSort()
  {
    chunk_dis = new float[_3X3ChunksID.GetLength(0), 2];
    for (int i = 0; i < chunk_dis.GetLength(0); i++)
    {
      float t = Util.CalculateDis(CullingTestObjects[_3X3ChunksID[i, 5]].transform.position, _CamPos);
      chunk_dis[i, 0] = t;
      chunk_dis[i, 1] = i;
    }
    Util.BubbleSort(chunk_dis);
    return (int)chunk_dis[0, 1];
  }

}

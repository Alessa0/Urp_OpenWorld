using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CreateData : Editor
{
  private static int _grassCountPerMeter = 1;
  private static int _maxGrassCount = 10000;
  public static string TerrainRoot = "Chunks";
  [MenuItem("Assets/Create/ChunksData")]
  public static void CreateMyAsset()
  {
    GameObject[] terrainGOs = Util.CollectDataInScene(TerrainRoot);
    List<List<GrassInfo>> GPUGrass = new List<List<GrassInfo>>(terrainGOs.Length);
    List<List<int>> Indirect = new List<List<int>>(terrainGOs.Length);
    List<int> GrassCount = new List<int>(terrainGOs.Length);
    List<Matrix4x4> M = new List<Matrix4x4>(terrainGOs.Length);
    for (int i = 0; i < terrainGOs.Length; i++)
    {
      var terrianMesh = terrainGOs[i].GetComponent<MeshFilter>().sharedMesh;
      var terrianPos = terrainGOs[i].transform.position;
      var grassIndex = 0;
      List<GrassInfo> grassBound = new List<GrassInfo>();
      List<int> IndirectPerTriangles = new List<int>();
      var indices = terrianMesh.triangles;
      var vertices = terrianMesh.vertices;

      for (int j = 0; j < indices.Length / 3; j++)
      {
        var index1 = indices[j * 3];
        var index2 = indices[j * 3 + 1];
        var index3 = indices[j * 3 + 2];
        var v1 = vertices[index1];
        var v2 = vertices[index2];
        var v3 = vertices[index3];
        //面得到法向
        var normal = Util.GetFaceNormal(v1, v2, v3);
        //计算up到faceNormal的旋转四元数
        var upToNormal = Quaternion.FromToRotation(Vector3.up, normal);
        //三角面积
        var arena = Util.GetAreaOfTriangle(v1, v2, v3);
        //计算在该三角面中，需要种植的数量
        var countPerTriangle = Mathf.Max(1, _grassCountPerMeter * arena);
        for (int p = 0; p < countPerTriangle; p++)
        {
          var positionInTerrian = Util.RandomPointInsideTriangle(v1, v2, v3);
          float rot = UnityEngine.Random.Range(0, 180);
          var localToTerrian = Matrix4x4.TRS(positionInTerrian, upToNormal * Quaternion.Euler(0, rot, 0), Vector3.one);

          Vector2 texScale = Vector2.one;
          Vector2 texOffset = Vector2.zero;
          Vector3 _center = terrianPos + positionInTerrian;
          var grassInfo = new GrassInfo(localToTerrian, _center, 0.6f);
          grassBound.Add(grassInfo);
          IndirectPerTriangles.Add(grassIndex);
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
      Matrix4x4 m = terrainGOs[i].gameObject.transform.localToWorldMatrix;
      M.Add(m);
      GrassCount.Add(grassIndex);
      GPUGrass.Add(grassBound);
      Indirect.Add(IndirectPerTriangles);
    }
    for (int i = 0; i < terrainGOs.Length; i++)
    {
      GrassChunkData asset = ScriptableObject.CreateInstance<GrassChunkData>();
      asset.localToWorld = M[i];
      asset.grass = GPUGrass[i];
      asset.grassCount = GrassCount[i];
      asset.indirect = Indirect[i];

      string name = terrainGOs[i].name;
      AssetDatabase.CreateAsset(asset, "Assets/Resources/GrassChunksData/GPUData_" + name + ".asset");
      AssetDatabase.SaveAssets();

    }

  }



}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
  public static GameObject[] CollectDataInScene(string rootName)
  {
    GameObject rootGO = GameObject.Find(rootName);
    int childrenCount = rootGO.transform.childCount;
    GameObject[] children = new GameObject[childrenCount];
    for (int i = 0; i < childrenCount; ++i)
    {
      children[i] = rootGO.transform.GetChild(i).gameObject;
    }
    return children;
  }

  private static Mesh _grassMesh;
  public static Mesh CreateGrassMesh()
  {
    var grassMesh = new Mesh { name = "Grass Quad" };
    float width = 1f;
    float height = 1f;
    float halfWidth = width / 2;
    grassMesh.SetVertices(new List<Vector3>
            {
                new Vector3(-halfWidth, 0, 0.0f),
                new Vector3(-halfWidth,  height, 0.0f),
                new Vector3(halfWidth, 0, 0.0f),
                new Vector3(halfWidth,  height, 0.0f),

            });
    grassMesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1),
            });

    grassMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3, },
    MeshTopology.Triangles, 0, false);
    grassMesh.RecalculateNormals();
    grassMesh.UploadMeshData(true);
    return grassMesh;
  }

  public static Mesh unitMesh
  {
    get
    {
      if (_grassMesh != null)
      {
        return _grassMesh;
      }
      _grassMesh = CreateGrassMesh();
      return _grassMesh;
    }
  }


  /// <summary>
  /// 三角形内部，取平均分布的随机点
  /// </summary>
  public static Vector3 RandomPointInsideTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var x = Random.Range(0, 1f);
    var y = Random.Range(0, 1f);
    if (y > 1 - x)
    {
      //如果随机到了右上区域，那么反转到左下
      var temp = y;
      y = 1 - x;
      x = 1 - temp;
    }
    var vx = p2 - p1;
    var vy = p3 - p1;
    return p1 + x * vx + y * vy;
  }


  //计算三角形面积
  public static float GetAreaOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var vx = p2 - p1;
    var vy = p3 - p1;
    var dotvxy = Vector3.Dot(vx, vy);
    var sqrArea = vx.sqrMagnitude * vy.sqrMagnitude - dotvxy * dotvxy;
    return 0.5f * Mathf.Sqrt(sqrArea);
  }

  public static Vector3 GetFaceNormal(Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var vx = p2 - p1;
    var vy = p3 - p1;
    return Vector3.Cross(vx, vy);
  }
  //分割矩阵
  public static int[,] MatrixSplit()
  {
    int[,] M = new int[4, 4];
    for (int i = 0; i < 4; i++)
    {
      for (int j = 0; j < 4; j++)
      {
        M[i, j] = i * 4 + j;
      }
    }
    var l = 0;
    for (int i = 0; i+2 < M.GetLength(0); i++)
    {
      l = l + M.GetLength(1)-2;
    }
    int[,] result = new int[l,9];
    int count = 0;
    // int[] t = new int[9];
    for (int i = 0; i + 2 < M.GetLength(0); i++)
    {
      for (int j = 0; j + 2 < M.GetLength(1); j++)
      {
        int[] t = new int[9] {
        M[i,j],M[i,j+1],M[i,j+2],
        M[i+1,j],M[i+1,j+1],M[i+1,j+2],
        M[i+2,j],M[i+2,j+1],M[i+2,j+2]};
        //  int t = M[i + 1, j + 1];
        for (int p = 0; p < 9; p++)
        {
          result[count,p] = t[p];
        }

        count++;
      }
    }
    return result;
  }
  //计算距离
  public static float CalculateDis(Vector3 p1, Vector3 p2)
  {
    return (p1 - p2).magnitude;
  }
  //排序
  public static void BubbleSort(float[,] R)
  {
    int i, j; //交换标志 
    float[,] temp = new float[1,2];
    bool exchange;
    for (i = 0; i < R.GetLength(0); i++) //最多做R.Length-1趟排序 
    {
      exchange = false; //本趟排序开始前，交换标志应为假
      for (j = R.GetLength(0) - 2; j >= i; j--)
      {
        if (R[j + 1,0] < R[j,0]) //交换条件
        {
          temp[0,0] = R[j + 1,0];
          temp[0, 1] = R[j + 1, 1];
          R[j + 1,0] = R[j,0];
          R[j + 1, 1] = R[j, 1];
          R[j,0] = temp[0,0];
          R[j, 1] = temp[0, 1];
          exchange = true; //发生了交换，故将交换标志置为真 
        }
      }
      if (!exchange) //本趟排序未发生交换，提前终止算法 
      {
        break;
      }
    }
  }
  public static void QuickSort(float[][] list, int low, int high)
  {
    int mid;
    if (low < high)
    {
      mid = Partition(list, low, high);
      QuickSort(list, low, mid - 1);
      QuickSort(list, mid + 1, high);
    }
  }

  private static int Partition(float[][] list, int low, int high)
  {
    float[] temp0 = list[low];
   // float temp1 = list[low, 1];
    while (low < high)
    {
      while (low < high && temp0[0] < list[high][0])
      {
        high--;
      }
      list[low][0] = list[high][0];
      list[low][ 1] = list[high][ 1];
      while (low < high && temp0[0] > list[low][0])
      {
        low++;
      }
      list[high][0] = list[low][0];
      list[high][1] = list[low][ 1];
    }
    list[low]= temp0;
    return low;
  }
}

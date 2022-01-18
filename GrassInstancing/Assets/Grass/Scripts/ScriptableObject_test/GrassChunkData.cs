using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName ="Chunks", menuName = "Assets/Create/Chunks")]
public class GrassChunkData : ScriptableObject
{
  public Matrix4x4 localToWorld;
  public int grassCount;
  public List<int> indirect;
  public List<GrassInfo> grass;
}


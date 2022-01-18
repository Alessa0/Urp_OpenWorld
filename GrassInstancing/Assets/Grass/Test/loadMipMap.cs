using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class loadMipMap : MonoBehaviour
{
  public Texture2D Tex;

  private Color[] _tex;
  private int _mipCount;
  private int[] _mipWidth;
  private int[] _mipHeight;
  private int[] _mipOffset;
  private int _curMip = 0;
  void Start()
  {
    _mipCount = Tex.mipmapCount;
    _mipWidth = new int[_mipCount];
    _mipHeight = new int[_mipCount];
    _mipOffset = new int[_mipCount];
    InitArr();
    SaveTex();
  }

  void InitArr()
  {
    int len = 0;
    for (int i = 0; i < _mipCount; i++)
    {
      if (i == 0)
      {
        _mipWidth[i] = Tex.width;
        _mipHeight[i] = Tex.height;
        _mipOffset[i] = 0;
      }
      else
      {
        _mipWidth[i] = _mipWidth[i - 1] / 2;
        _mipHeight[i] = _mipHeight[i - 1] / 2;
        _mipOffset[i] = _mipOffset[i - 1] + _mipWidth[i - 1] * _mipHeight[i - 1];
      }
      len += (_mipWidth[i] * _mipHeight[i]);
    }

    _tex = new Color[len];
  }

  void SaveTex()
  {
    for (int mip = 0; mip < _mipCount; mip++)
    {
      Color[] cols = Tex.GetPixels(mip);

      for (int i = 0; i < _mipWidth[mip] * _mipHeight[mip]; i++)
      {
        _tex[_mipOffset[mip] + i] = cols[i];
        //tex1.Add(cols[i]);
      }
    }
  }
  Texture TestMip(int mip, int Scale)
  {
    Texture2D texture = new Texture2D(_mipWidth[mip] * Scale, _mipHeight[mip] * Scale);
    for (int i = 0; i < texture.width; i += Scale)
    {
      var offsetx = i / Scale;
      for (int j = 0; j < texture.height; j += Scale)
      {
        var offsety = j / Scale;
        Color[] cols = new Color[Scale * Scale];
        for (int a = 0; a < cols.Length; a++)
        {
          cols[a] = GetPixelFormTex(mip, offsetx, offsety);
        }
        texture.SetPixels(i, j, Scale, Scale, cols, 0);
      }
    }

    texture.Apply();
    return texture;
  }
  Color GetPixelFormTex(int mip, int x, int y)
  {
    int index = _mipOffset[mip] + y * _mipWidth[mip] + x;//mipOffset[mip - 1] >>( 0, 0 )
    return (Color)_tex[index];
  }

  private void OnGUI()
  {
    int Scale = 4;
    GUI.Label(new Rect(300, 0, 200, 30), "Mip: " + _curMip);
    _curMip = Mathf.Max(0, (int)(GUI.HorizontalSlider(new Rect(0, 0, 200, 30), _curMip, 0, _mipCount - 1)));
    GUI.DrawTexture(new Rect(500, 200, _mipWidth[_curMip] * Scale, _mipHeight[_curMip] * Scale), TestMip(_curMip, Scale));
  }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
  private GrassRenderPass _pass = null;
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
    var cameraData = renderingData.cameraData;
    if (cameraData.renderType == CameraRenderType.Base)
    {
      renderer.EnqueuePass(_pass);
    }
  }
  public override void Create()
  {
    _pass = new GrassRenderPass();
  }
  public static CommandBuffer _cmd;
  public void AddCommandbuffer(CommandBuffer cmd)
  {
    if (cmd != null)
    {
      _cmd = (cmd);
    }
  }

  public class GrassRenderPass : ScriptableRenderPass
  {
    public GrassRenderPass()
    {
      this.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
      if (_cmd != null)
      {
        context.ExecuteCommandBuffer(_cmd);
      }
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
      base.FrameCleanup(cmd);
    }
  }
}


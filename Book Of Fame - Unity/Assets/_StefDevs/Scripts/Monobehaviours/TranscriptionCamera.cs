using UnityEngine.Rendering;
using UnityEngine;

public class TranscriptionCamera : MonoBehaviour 
{
    internal new Camera camera;
	internal CommandBuffer depthMask_cmdBuffer;
	public Material depthMask_material;
    public Renderer depthMask_maxDepthValueRenderer;

    private void Awake()
    {
		camera = GetComponent<Camera> ();
		camera.targetTexture = new RenderTexture (Screen.width, Screen.height, 24);
		Shader.SetGlobalTexture("_TimeCrackTexture", camera.targetTexture);
		depthMask_cmdBuffer = new CommandBuffer (){ name = "Depth Masking" };

		#if UNITY_REVERSED_Z
		depthMask_cmdBuffer.ClearRenderTarget(true, true, Color.clear, 1);
		#else
		depthMask_cmdBuffer.ClearRenderTarget(true, true, Color.clear, 0);
		#endif

		depthMask_cmdBuffer.DrawRenderer (depthMask_maxDepthValueRenderer, depthMask_material);

		camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, depthMask_cmdBuffer);
    }
}

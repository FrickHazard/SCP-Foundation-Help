using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SCP099Effect : MonoBehaviour
{
    [Range(0, 1)]
    public float EyeSizeX = 0;

    [Range(0, 1)]
    public float EyeSizeY = 0;

    private Camera myCamera;
    private Material material;

    void Awake()
    {
        myCamera = GetComponent<Camera>();
        material = new Material(Shader.Find("Hidden/SCP099Effect"));
        myCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    // Postprocess the image
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!myCamera) return;
        material.SetFloat("_ViewportEyeSizeX", EyeSizeX);
        material.SetFloat("_ViewportEyeSizeY", EyeSizeY);
        material.SetInt("_EyeCount", 1);
        material.SetVectorArray("_EyePositions", new Vector4[10] {
           myCamera.WorldToViewportPoint(Vector3.zero),
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
            Vector4.zero,
        Vector4.zero });
        Graphics.Blit(source, destination, material);
    }
}
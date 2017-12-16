using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BlinkEffect : MonoBehaviour
{
    [Range(1.0f, 5.0f)]
    public float HorizontalScale = 2f;

    [Range(1.0f, 5.0f)]
    public float VerticalScale = 2.3f;

    public float TotalTime = 1f;

    private Material material;
    // use float instead of bool for shader, other wise this would dumb asf
    private float isBlinking;
    // counter for this blinks time
    private float currentBlinkTimeCounter;

    // Creates a private material used to the effect
    void Awake()
    {
        material = new Material(Shader.Find("Hidden/Blink"));
    }

    private void Update()
    {
        if (Input.GetKeyDown("space"))StartBlink();
        if (isBlinking == 1 && currentBlinkTimeCounter != TotalTime)
        {
            currentBlinkTimeCounter += Time.deltaTime;
            if (currentBlinkTimeCounter > TotalTime) currentBlinkTimeCounter = TotalTime;
        }
    }

    // main Api purely for display
    public void StartBlink()
    {
        isBlinking = 1;
        currentBlinkTimeCounter = 0;
    }

    private void CancelBlink()
    {
        isBlinking = 0;
    }

    private float GetBlinkAnimationPercentage()
    {
       float percentThroughtBlink = (currentBlinkTimeCounter / TotalTime);
        // opening eyelid again, since 0.5 is half way mark
        if (percentThroughtBlink > 0.5)
        {
            return 1f - (percentThroughtBlink - 0.5f);
        }
        // times 2 because eyecloses then opens
        return percentThroughtBlink * 2;
    }

    // Postprocess the image
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
     
        material.SetFloat("_IsBlinking", isBlinking);
        material.SetFloat("_EyeClosedPercent", GetBlinkAnimationPercentage());
        material.SetFloat("_HorizontalScale", HorizontalScale);
        material.SetFloat("_VerticalScale", VerticalScale);
        Graphics.Blit(source, destination, material);
    }
}
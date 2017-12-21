using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCP099Eye : MonoBehaviour {

    // average blink time is about 1/3 of a second
    public float TotalTime = 0.333f;

    // what sin y scale is considered to be fully opened
    public float MaxEyeOpenScale = 0.211f;

    private bool isBlinking = false;

    // counter for this blinks time
    private float currentBlinkTimeCounter;

    private Material material;

    void Awake () {
        material = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if (isBlinking == true && currentBlinkTimeCounter != TotalTime)
        {
            currentBlinkTimeCounter += Time.deltaTime;
            if (currentBlinkTimeCounter > TotalTime) currentBlinkTimeCounter = TotalTime;
            SetEyeClosedPercent(GetBlinkAnimationPercentage());
        }
    }

    public void StartBlinkCycle(float minSeconds, float maxSeconds)
    {
        StartCoroutine(BlinkCoroutine(minSeconds, maxSeconds));
    }

    public void StopBlinkCycle()
    {
        StopCoroutine("BlinkCoroutine");
    }

    private IEnumerator BlinkCoroutine(float minSeconds, float maxSeconds)
    {
        yield return new WaitForSeconds(Random.Range(minSeconds, maxSeconds));
        StartBlink();
        StartCoroutine(BlinkCoroutine(minSeconds, maxSeconds));
    }

    private void StartBlink()
    {
        isBlinking = true;
        currentBlinkTimeCounter = 0;
    }

    private float GetBlinkAnimationPercentage()
    {
        float percentThroughtBlink = (currentBlinkTimeCounter / TotalTime);
        // opening eyelid again, since 0.5 is half way mark
        if (percentThroughtBlink > 0.5)
        {
            return 1 - (1f - ((percentThroughtBlink - 0.5f) * 2)) ;
        }
        // times 2 because eye closes then opens
        return 1 - percentThroughtBlink * 2;
    }

    public void SetFade(float fade)
    {
        fade = Mathf.Clamp(fade, 0, 1.0f);
        material.SetFloat("_Fade", fade);
    }

    public void SetEyeCenter(Vector3 center)
    {
        material.SetFloat("_LookDirX", center.x);
        material.SetFloat("_LookDirY", center.y);
    }

    private void SetEyeClosedPercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0, 1.0f);
        // scale percent, make max eye amount respected 
        material.SetFloat("_EyeYScale", (percent * MaxEyeOpenScale));
    }
}

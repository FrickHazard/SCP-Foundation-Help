using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SCP099Eye : MonoBehaviour {

    // average blink time is about 1/3 of a second
    public float TotalBlinkTime = 0.333f;

    public float TotalSpawnDieTime = 2f;

    // what sin Y scale value is considered to be fully opened
    public float MaxEyeOpenScale = 0.211f;

    private bool isBlinking = false;

    private bool spawning = false;

    private bool dying = false;

    // counter for this blinks time
    private float currentBlinkTimeCounter;

    // counter for spawning effect of eye
    private float spawnDyingTimeCounter;

    private Material material;
    // shader properties, ids are faster than strings
    private int _Fade = -1;
    private int _IrisColor = -1;
    private int _LookDirX = -1;
    private int _LookDirY = -1;
    private int _EyeYScale = -1;

    void Awake () {
       material = GetComponent<MeshRenderer>().material;
       _Fade = Shader.PropertyToID("_Fade");
       _IrisColor = Shader.PropertyToID("_IrisColor");
       _LookDirX = Shader.PropertyToID("_LookDirX");
       _LookDirY = Shader.PropertyToID("_LookDirY");
       _EyeYScale = Shader.PropertyToID("_EyeYScale");
    }

    void Update()
    {
        if (isBlinking == true && currentBlinkTimeCounter != TotalBlinkTime)
        {
            currentBlinkTimeCounter += Time.deltaTime;
            if (currentBlinkTimeCounter > TotalBlinkTime)
            {
                currentBlinkTimeCounter = 0;
                isBlinking = false;
            }
            SetEyeOpenPercent(GetBlinkAnimationPercentage());
        }
        // fade effect if spawning or dying
        if (spawning || dying)
        {
            spawnDyingTimeCounter += Time.deltaTime;
            if (spawnDyingTimeCounter > TotalSpawnDieTime)
            {
                // if dying animation is finished disable object
                if (dying)
                {
                    transform.parent = null;
                    gameObject.SetActive(false);
                }
                spawnDyingTimeCounter = 0;
                spawning = false;
                dying = false;
            }
            if (spawning)
            {
                SetFade(1 - (spawnDyingTimeCounter / TotalSpawnDieTime));
            }
            else
            {
                SetFade(spawnDyingTimeCounter / TotalSpawnDieTime);
            }
        }
    }

    public void Spawn(Vector3 position, Vector3 normal, float scale, Transform parent = null)
    {
        transform.position = position + (normal * 0.01f);
        transform.LookAt(position + normal, Vector3.up);
        transform.localScale = new Vector3(scale, scale, scale);
        transform.parent = parent;
        spawning = true;
        dying = false;
        SetFade(1f);
        spawnDyingTimeCounter = 0f;
        SetIrisColor(Random.ColorHSV(0.2f, 0.8f, 0.5f, 0.8f, 0.1f, 0.8f));
        MaxEyeOpenScale = Random.Range(0.2f, 0.22f);
        SetEyeOpenPercent(1f);
        gameObject.SetActive(true);
    }

    public void Kill()
    {
        spawning = false;
        dying = true;
        spawnDyingTimeCounter = 0f;

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
        float percentThroughtBlink = (currentBlinkTimeCounter / TotalBlinkTime);
        // opening eyelid again, since 0.5 is half way mark
        if (percentThroughtBlink > 0.5)
        {
            return 1 - (1f - ((percentThroughtBlink - 0.5f) * 2)) ;
        }
        // times 2 because eye closes then opens
        return 1 - percentThroughtBlink * 2;
    }

    public void SetEyeCenter(Vector3 center)
    {
        material.SetFloat(_LookDirX, center.x);
        material.SetFloat(_LookDirY, center.y);
    }

    private void SetEyeOpenPercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0, 1.0f);
        // scale percent, make max eye amount respected 
        material.SetFloat(_EyeYScale, (percent * MaxEyeOpenScale));
    }

    private void SetFade(float fade)
    {
        fade = Mathf.Clamp(fade, 0, 1.0f);
        material.SetFloat(_Fade, fade);
    }

    private void SetIrisColor(Color color)
    {
        material.SetColor(_IrisColor, color);
    }

    private void DrawGizmo(bool selected)
    {
        var col = new Color(0.8f, 0.7f, 0.1f, 1.0f);
        col.a = selected ? 0.3f : 0.1f;
        Gizmos.color = col;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        col.a = selected ? 0.5f : 0.2f;
        Gizmos.color = col;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    public void OnDrawGizmos()
    {
        DrawGizmo(false);
    }

    public void OnDrawGizmosSelected()
    {
        DrawGizmo(true);
    }
}

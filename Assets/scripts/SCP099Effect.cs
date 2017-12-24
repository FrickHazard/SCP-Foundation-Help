using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SCP099Effect : MonoBehaviour
{
    public SCP099Eye EyePrefab;

    public int MaxAnamolyLevel = 10;

    public int MaxEyeCount;

    public float MinEyeDistanceFromEachOther = 5f;

    public float EyeScaleModifierPerLevel = 0.03f;

    public int EyesCountModifierPerLevel = 1;

    public float AnamolyOccurrenceDurationModifierPerLevel = 1f;

    public float AnamolyOccurrenceTimerModifierPerLevel = 1f;

    public float EyeBlinkTimerMin = 5;
    public float EyeBlinkTimerMax = 7;

    public float EyeScaleMin = 0.2f;
    public float EyeScaleMax = 0.3f;

    public float AnamolyOccurrenceDurationMin = 10;
    public float AnamolyOccurrenceDurationMax = 20;

    public float AnamolyOccurrenceTimerMin = 3;
    public float AnamolyOccurrenceTimerMax = 6;

    public LayerMask RayCastMask;

    public int AnamolyLevel { get { return anamolyLevel; } }
    public Camera MyCamera { get; set; }

    private int anamolyLevel = 0;
    private List<SCP099Eye> eyes = new List<SCP099Eye>();
    private int eyeIndex = 0;
    private bool inAnamolyOccurrence = false;
    private int eyesLeftToSpawn = 0;
    private Dictionary<Camera, CommandBuffer> m_Cameras = new Dictionary<Camera, CommandBuffer>();

    void Start()
    {
        MyCamera = GetComponent<Camera>();
        SetUpObjectPool();
    }

    public void IncreaseAnamolyLevel()
    {
        if (anamolyLevel == MaxAnamolyLevel) return;
        anamolyLevel++;
        // effect has started
        if (anamolyLevel == 1)
        {
            Debug.Log("SCP 099 Has Effected Player.");
            StartCoroutine(AnamolyOccurrenceCoroutine());
        }
        Debug.Log("SCP099 Anamoly Level has Increased to " + AnamolyLevel + ".");
    }

    public void EndAnamolyEffect()
    {
        anamolyLevel = 0;
        Debug.Log("SCP099 Anamoly Stopped.");
        StopCoroutine("AnamolyOccurrenceCoroutine");
        EndAnamolyOccurrence();
    }

    IEnumerator AnamolyOccurrenceCoroutine()
    {
        // max time based on anamoly level
        float anamolyMaxTime = Mathf.Max(AnamolyOccurrenceTimerMax - (AnamolyOccurrenceTimerModifierPerLevel * AnamolyLevel), AnamolyOccurrenceTimerMin);
        yield return new WaitForSeconds(Random.Range(AnamolyOccurrenceTimerMin, anamolyMaxTime));
        StartAnamolyOccurrence();
        float anamolyMaxDuration = Mathf.Max(AnamolyOccurrenceDurationMax + (AnamolyOccurrenceDurationModifierPerLevel * AnamolyLevel), AnamolyOccurrenceDurationMin);
        yield return new WaitForSeconds(Random.Range(AnamolyOccurrenceDurationMin, anamolyMaxDuration));
        EndAnamolyOccurrence();
        StartCoroutine(AnamolyOccurrenceCoroutine());
    }

    private void StartAnamolyOccurrence()
    {
        inAnamolyOccurrence = true;
        eyesLeftToSpawn = Mathf.Min(EyesCountModifierPerLevel * AnamolyLevel, MaxEyeCount);
        SpawnEyes();
    }

    private void EndAnamolyOccurrence()
    {
        inAnamolyOccurrence = false;
        eyes.ForEach(eye => { eye.Kill(); });
        eyesLeftToSpawn = 0;
    }

    private void SpawnEyes()
    {
        int validHitCount = 0;
        const int MAX_RAYCAST_ATTEMPTS = 100;
        const float MAX_RAYCAST_DISTANCE = 100f;
        Vector3[] hits = new Vector3[eyes.Count];
        RaycastHit hit;
        for (int i = 0; i < MAX_RAYCAST_ATTEMPTS; i++)
        {
            Vector3 direction = MyCamera.transform.worldToLocalMatrix.MultiplyVector(new Vector3(Random.Range(-1f, 1f),Random.Range(-0.7f, 0.7f),Random.Range(-1f, 1f))).normalized;
            if (Physics.Raycast(MyCamera.transform.position, direction, out hit, MAX_RAYCAST_DISTANCE, RayCastMask))
            {
                // make sure eyes are apart by set amount
                if(hits.Any(pos => {
                    return Vector3.Distance(pos, hit.point) < MinEyeDistanceFromEachOther;
                })) continue;

                // make sure angle of impact isnt more than 60(roughly) degrees
                if (Mathf.Abs(Vector3.Dot(hit.normal, direction)) < 0.333) continue;

                hits[validHitCount] = hit.point;
                eyesLeftToSpawn--;
                validHitCount++;
                SetNextEye(hit.point, Vector3.Lerp(-direction, hit.normal, 0.5f));
                if (eyesLeftToSpawn == 0) return;
            }
        }
        if (eyesLeftToSpawn > 0) StartCoroutine(ContinueSpawn());
    }

    IEnumerator ContinueSpawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnEyes();
    }

    private void SetUpObjectPool()
    {
        // clean up if object pool already exists
        if (eyes.Count > 0)
        {
            eyes.ForEach(eye => DestroyImmediate(eye));
            eyes.Clear();
        }

        eyeIndex = 0;
        for (int i = 0; i < MaxEyeCount; i++)
        {
            SCP099Eye eye = Instantiate(EyePrefab).GetComponent<SCP099Eye>();
            eye.gameObject.SetActive(false);
            eyes.Add(eye);
        }
    }

    private void SetNextEye(Vector3 postion, Vector3 normal)
    {
        float eyeScaleMax = Mathf.Max(EyeScaleMin, EyeScaleMax + (AnamolyLevel * EyeScaleModifierPerLevel) );
        eyes[eyeIndex].Spawn(postion, normal, Random.Range(EyeScaleMin, eyeScaleMax));
        eyes[eyeIndex].StartBlinkCycle(EyeBlinkTimerMin, EyeBlinkTimerMax);
        eyeIndex++;
        if (eyeIndex == eyes.Count) eyeIndex = 0;
    }

    void OnDisable()
    {
        foreach (var cam in m_Cameras)
        {
            if (cam.Key)
            {
                cam.Key.RemoveCommandBuffer(CameraEvent.BeforeLighting, cam.Value);
            }
        }
    }

    private void OnPreCull()
    {
        var act = gameObject.activeInHierarchy && enabled;
        if (!act)
        {
            OnDisable();
            return;
        }

        var cam = Camera.current;
        if (!inAnamolyOccurrence || !cam)
            return;

        CommandBuffer buf = null;
        if (m_Cameras.ContainsKey(cam))
        {
            buf = m_Cameras[cam];
            buf.Clear();
        }
        else
        {
            buf = new CommandBuffer();
            buf.name = "SCP099 Eye Command Buffer";
            m_Cameras[cam] = buf;

            // set this command buffer to be executed just before deferred lighting pass
            // in the camera
            cam.AddCommandBuffer(CameraEvent.BeforeLighting, buf);
        }

        eyes.ForEach(eye => {
            if (eye.gameObject.activeSelf)
            {
                Vector3 eyeLookPoint = eye.transform.worldToLocalMatrix * (MyCamera.transform.position - eye.transform.position);
                eye.SetEyeCenter(Vector3.ProjectOnPlane(eyeLookPoint.normalized, Vector3.forward).normalized);
            }
        });

        // copy g-buffer normals into a temporary RT
        var normalsID = Shader.PropertyToID("_WorldNormalsForEyes");
        buf.GetTemporaryRT(normalsID, -1, -1);
        buf.Blit(BuiltinRenderTextureType.GBuffer2, normalsID);
        // render depth normals decals into two MRTs, for eyes, bascially sets a global shader variable
        RenderTargetIdentifier[] mrt = { BuiltinRenderTextureType.GBuffer0, BuiltinRenderTextureType.GBuffer2 };
        buf.SetRenderTarget(mrt, BuiltinRenderTextureType.CameraTarget);
        buf.ReleaseTemporaryRT(normalsID);
    }

}
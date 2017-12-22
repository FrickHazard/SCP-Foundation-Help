using UnityEngine;
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

    private int anamolyLevel = 0;
    private Camera myCamera;
    private List<SCP099Eye> eyes = new List<SCP099Eye>();
    private int eyeIndex = 0;
    private bool inAnamolyOccurrence = false;

    void Start()
    {
        myCamera = GetComponent<Camera>();
        SetUpObjectPool();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
        IncreaseAnamolyLevel();
    }

    public void IncreaseAnamolyLevel()
    {
        if (anamolyLevel == MaxAnamolyLevel) return;
        anamolyLevel++;
        // effect has started
        if (anamolyLevel == 1) StartCoroutine(AnamolyOccurrenceCoroutine());
    }

    public void EndAnamolyEffect()
    {
        anamolyLevel = 0;
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
        SpawnEyes();
    }

    private void EndAnamolyOccurrence()
    {
        inAnamolyOccurrence = false;
        eyes.ForEach(eye => { eye.Kill(); });
    }

    private void SpawnEyes()
    {
        int validHitCount = 0;
        const int MAX_RAYCAST_ATTEMPTS = 100;
        const float MAX_RAYCAST_DISTANCE = 100f;
        RaycastHit hit;
        for (int i = 0; i < MAX_RAYCAST_ATTEMPTS; i++)
        {
            Vector3 direction = myCamera.transform.worldToLocalMatrix.MultiplyVector(new Vector3(Random.Range(-1f, 1f),Random.Range(-0.7f, 0.7f),Random.Range(-1f, 1f)));
            if (Physics.Raycast(myCamera.transform.position, direction, out hit, MAX_RAYCAST_DISTANCE, RayCastMask))
            {
                // make sure eyes are apart by set amount
                if(eyes.Any(eye => {
                    if (eye.gameObject.activeSelf) return false;
                    else return Vector3.Distance(eye.transform.position, hit.point) < MinEyeDistanceFromEachOther;
                })) continue;
                // some decal check here as well


                validHitCount++;
                SetNextEye(hit);
                if (validHitCount >= MaxEyeCount) return;
                if (validHitCount == EyesCountModifierPerLevel * AnamolyLevel) return;
            }
           
        }
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

    private void SetNextEye(RaycastHit rayHit)
    {
        float eyeScaleMax = Mathf.Max(EyeScaleMin, EyeScaleMax + (AnamolyLevel * EyeScaleModifierPerLevel) );
        eyes[eyeIndex].Spawn(rayHit, Random.Range(EyeScaleMin, eyeScaleMax));
        eyes[eyeIndex].StartBlinkCycle(EyeBlinkTimerMin, EyeBlinkTimerMax);
        eyeIndex++;
        if (eyeIndex == eyes.Count) eyeIndex = 0;
    }

    void Update()
    {
        if (!inAnamolyOccurrence || AnamolyLevel == 0) return;
        eyes.ForEach(eye => {
            if (eye.gameObject.activeSelf)
            {
                Vector3 eyeLookDir = Vector3.ProjectOnPlane(myCamera.transform.position, eye.transform.forward) - eye.transform.position;
                eye.SetEyeCenter((eye.transform.worldToLocalMatrix * eyeLookDir.normalized).normalized);
            }
        });
    }

}
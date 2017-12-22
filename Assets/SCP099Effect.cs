using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SCP099Effect : MonoBehaviour
{

    public SCP099Eye EyePrefab;

    public int MaxEyeCount;

    public int EyesCountModifierPerLevel = 1;

    public float AnamolyOccurrenceDurationModifierPerLevel = 1f;

    public float AnamolyOccurrenceTimerModifierPerLevel = 1f;

    public float EyeLifeSpanModifierPerLevel = 1f;

    public float EyeBlinkTimerMin = 5;
    public float EyeBlinkTimerMax = 7;

    public float AnamolyOccurrenceDurationMin = 10;
    public float AnamolyOccurrenceDurationMax = 20;

    public float AnamolyOccurrenceTimerMin = 3;
    public float AnamolyOccurrenceTimerMax = 6;

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
    }

    public void IncreaseAnamolyLevel()
    {
        anamolyLevel++;
        // effect has started
        if (anamolyLevel == 1) StartCoroutine(AnamolyOccurrenceCoroutine());
    }

    IEnumerator AnamolyOccurrenceCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(AnamolyOccurrenceTimerMin, AnamolyOccurrenceTimerMax));
        StartAnamolyOccurrence();
        yield return new WaitForSeconds(Random.Range(AnamolyOccurrenceDurationMin, AnamolyOccurrenceDurationMax));
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
        SetNextEye(new Vector3(0, 0, -15), new Vector3(0, 0, -1), Vector3.up);
        SetNextEye(new Vector3(4, 2, -15), new Vector3(0, 0f, -1), Vector3.up);
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

    private void SetNextEye(Vector3 position, Vector3 normal, Vector3 up)
    {
        eyes[eyeIndex].Spawn(position, normal, up);
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
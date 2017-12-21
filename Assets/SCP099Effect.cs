using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SCP099Effect : MonoBehaviour
{

    [Range(0, 15)]
    public int EyeCount;

    public SCP099Eye EyePrefab;

    public float BlinkTimerMin = 5;
    public float BlinkTimerMax = 7;

    public float FadeDistance = 10;
    public float FadeEndOffset = 6;

    private Camera myCamera;
    private List<SCP099Eye> eyes = new List<SCP099Eye>();
    private int indexer = 0;

    void Awake()
    {
        myCamera = GetComponent<Camera>();
        SetUpObjectPool();
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

        indexer = 0;
        for (int i = 0; i < EyeCount; i++)
        {
            SCP099Eye eye = Instantiate(EyePrefab).GetComponent<SCP099Eye>();
            eye.gameObject.SetActive(false);
            eyes.Add(eye);
        }
    }

    public void SetNextEye(Vector3 position, Vector3 normal, Vector3 up)
    {

        eyes[indexer].transform.position = position;
        eyes[indexer].transform.LookAt(position + normal, up);
        eyes[indexer].gameObject.SetActive(true);
        eyes[indexer].StartBlinkCycle(BlinkTimerMin, BlinkTimerMax);
        indexer++;
        if (indexer == eyes.Count) indexer = 0;
    }

    void Update()
    {
        eyes.ForEach(eye => {
            if (eye.gameObject.activeSelf)
            {
                float distance = Vector3.Distance(myCamera.transform.position, eye.transform.position);
                float percent =  (distance - FadeEndOffset) / FadeDistance;
                eye.SetFade(1 - percent);
                Vector3 eyeLookDir = Vector3.ProjectOnPlane(myCamera.transform.position, eye.transform.forward) - eye.transform.position;
                eye.SetEyeCenter(eye.transform.worldToLocalMatrix * eyeLookDir.normalized);
            }
        });
    }

}
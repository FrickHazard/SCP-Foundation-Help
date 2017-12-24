using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCP099Canvas : MonoBehaviour {

    public float TimeSeenPerAnamolyLevelIncrease = 5f;
    public float RadiusOfEffect = 10f;
    public Transform playerTransform;
    public SCP099Effect Effect;

    private float timeSeenSinceLastAnamolyLevelIncrease = 0f;
    private bool isVisible = false;

    private void Update()
    {
        if (isVisible && Vector3.Distance(playerTransform.position, transform.position) <= RadiusOfEffect)
        {
            timeSeenSinceLastAnamolyLevelIncrease += Time.deltaTime;
            if (timeSeenSinceLastAnamolyLevelIncrease > TimeSeenPerAnamolyLevelIncrease)
            {
                Effect.IncreaseAnamolyLevel();
                timeSeenSinceLastAnamolyLevelIncrease = 0f;
            }
            isVisible = false;
        }
    }

    private void OnWillRenderObject()
    {
        if (Camera.current != Effect.MyCamera) return;
        isVisible = true;
    }
}

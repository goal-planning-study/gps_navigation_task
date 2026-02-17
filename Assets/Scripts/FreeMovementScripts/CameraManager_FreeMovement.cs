using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraManager_FreeMovement : MonoBehaviour
{
    public Camera mainCamera;
    public RewardManager_FreeMovement rewardManager;
    public GameObject player;

    [Header("Camera Positions")]
    public Vector3 allocentricPosition = new Vector3(5f,30f,15f);
    public Vector3 allocentricRotation = new Vector3(90f,0f,0f);
    public Vector3 egocentricOffset = new Vector3(0f,3f,-4f);
    public float egocentricPitch = 18f;

    [Header("Timing")]
    public int memorizationRepetitions = 2;
    public float rewardDisplayTime = 1.5f;
    public float pauseBetweenRewards = 0.5f;
    public float pauseBetweenSeq = 1f;
    public float transitionDuration = 2.0f;
    public float pauseBeforeDrop = 0.8f;
    public bool autoPlayMemorization = true; 

    // runtime
    Vector3 targetEgocentricPosition;
    Quaternion targetEgocentricRotation;

    void Start()
    {
        if (mainCamera==null) mainCamera = Camera.main;
        if (rewardManager==null) rewardManager = Object.FindAnyObjectByType<RewardManager_FreeMovement>();
    }

    public void StartNewConfiguration(int configIndex)
    {
        if (rewardManager != null) rewardManager.LoadConfiguration(configIndex);

        if (player!=null)
        {
            var rend = player.GetComponent<Renderer>(); if (rend!=null) rend.enabled = false;
            var mover = player.GetComponentInChildren<FreeContinuousMovement>(); if (mover!=null) mover.enabled = false;
        }

        SetupAllocentricView();
        if (autoPlayMemorization)
        {
            StartCoroutine(PlayMemorizationAndDrop());
        }
    }

    // Made public so external controllers can trigger the allocentric view directly
    public void SetupAllocentricView()
    {
        if (mainCamera==null) return;
        mainCamera.transform.SetParent(null);
        mainCamera.transform.position = allocentricPosition;
        mainCamera.transform.eulerAngles = allocentricRotation;
    }

    IEnumerator PlayMemorizationAndDrop()
    {
        int count = rewardManager != null ? rewardManager.GetCurrentRewardCount() : 0;
        if (count == 0) { Debug.LogWarning("[FreeCamera] No rewards to show."); yield break; }

        string trialType = rewardManager != null ? rewardManager.GetCurrentTrialType() : "forw";
        List<int> order = new List<int>();
        if (trialType == "backw")
        {
            for (int i = count - 1; i >= 0; i--) order.Add(i);
        }
        else
        {
            for (int i = 0; i < count; i++) order.Add(i);
        }

        for (int rep = 0; rep < memorizationRepetitions; rep++)
        {
            Debug.Log($"[FreeCamera] Showing memorization repetition {rep+1}/{memorizationRepetitions}");
            foreach (int idx in order)
            {
                rewardManager.ShowReward(idx);
                yield return new WaitForSeconds(rewardDisplayTime);
                rewardManager.HideReward(idx);
                yield return new WaitForSeconds(pauseBetweenRewards);
            }

            if (rep < memorizationRepetitions - 1)
                yield return new WaitForSeconds(pauseBetweenSeq);
        }

        yield return new WaitForSeconds(pauseBeforeDrop);
        Debug.Log("[FreeCamera] Memorization complete, beginning drop to egocentric.");
        BeginDrop();
    }

    public void BeginDrop()
    {
        StartCoroutine(DropRoutine());
    }

    void CalculateEgocentricTarget()
    {
        targetEgocentricPosition = player.transform.position + player.transform.TransformDirection(egocentricOffset);
        float pitch = egocentricPitch;
        float yaw = player.transform.eulerAngles.y;
        targetEgocentricRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    IEnumerator DropRoutine()
    {
        if (mainCamera==null || player==null) yield break;

        player.GetComponent<Renderer>().enabled = true;
        CalculateEgocentricTarget();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f,1f,t);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetEgocentricPosition, smoothT);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetEgocentricRotation, smoothT);
            yield return null;
        }

        mainCamera.transform.position = targetEgocentricPosition;
        mainCamera.transform.rotation = targetEgocentricRotation;

        // parent camera to player and set local pose
        mainCamera.transform.SetParent(player.transform, true);
        mainCamera.transform.localPosition = egocentricOffset;
        mainCamera.transform.localRotation = Quaternion.Euler(egocentricPitch, 0f, 0f);

        // enable free movement component on player
        var mover = player.GetComponent<FreeContinuousMovement>();
        if (mover!=null) mover.enabled = true;

        // notify reward manager to start trial logging
        if (rewardManager!=null) rewardManager.StartNewTrial(player.transform.position);
    }
}

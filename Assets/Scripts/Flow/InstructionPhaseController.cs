using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InstructionPhaseController : MonoBehaviour
{
    public RewardManager_FreeMovement rewardManager;
    public CameraManager_FreeMovement cameraManager;
    public GameObject player;

    [Header("InstructionPhase settings")]
    public int requiredStreak = 3;
    public float displayDuration = 3.0f;      // how long the reward is visible from birds-eye
    public float pauseAfterHide = 0.15f;      // short pause after hiding before drop
    public float waitForPressTimeout = 30f;   // optional timeout for player response (seconds)
    public float feedbackDuration = 2.0f;     // show feedback before returning to birds-eye
    public float pauseBetweenTrials = 0.5f;

    private bool waitingForSpace = false;
    private bool firstTry = true;
    private Vector3 originalPlayerPosition;

    void Start()
    {
        if (rewardManager == null) rewardManager = Object.FindAnyObjectByType<RewardManager_FreeMovement>();
        if (cameraManager == null) cameraManager = Object.FindAnyObjectByType<CameraManager_FreeMovement>();
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p;
        }

        // store player's original tile so we can reset each trial to identical start
        if (player != null) originalPlayerPosition = player.transform.position;

        // ensure configuration loaded
        if (rewardManager != null) rewardManager.LoadConfiguration(0);

        // ensure SceneSequenceManager exists
        if (SceneSequenceManager.Instance == null)
        {
            var go = new GameObject("SceneSequenceManager");
            go.AddComponent<SceneSequenceManager>();
        }

        StartCoroutine(RunInstructionLoop());
    }

    IEnumerator RunInstructionLoop()
    {
        while (true)
        {
            int rewardCount = rewardManager != null ? rewardManager.GetCurrentRewardCount() : 0;
            if (rewardCount == 0) { Debug.LogWarning("[Instruction] no rewards available"); yield break; }

            int idx = Random.Range(0, rewardCount);

            // ensure camera at birds-eye and player hidden/locked
            rewardManager.HideAllRewards();
            cameraManager.SetupAllocentricView();

            if (player != null)
            {
                var rend = player.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
                var mover = player.GetComponentInChildren<FreeContinuousMovement>();
                if (mover != null) mover.enabled = false;

                // ensure player is reset to the same starting tile each trial
                player.transform.position = originalPlayerPosition;
            }

            // show the selected reward from birds-eye for displayDuration seconds
            rewardManager.ShowReward(idx);
            yield return new WaitForSeconds(displayDuration);

            // hide the shown reward and pause briefly
            rewardManager.HideReward(idx);

            // inform reward manager which index we are targeting for this trial
            rewardManager.instructionTargetIdx = idx;

            yield return new WaitForSeconds(pauseAfterHide);

            // drop to player (camera manager handles enabling movement at end of drop)
            cameraManager.BeginDrop();

            // wait for the drop to finish
            yield return new WaitForSeconds(cameraManager.transitionDuration + 0.1f);

            // wait for a single space press (first-try matters). single attempt per trial.
            waitingForSpace = true;
            firstTry = true;
            bool trialSucceeded = false;
            float elapsed = 0f;

            while (waitingForSpace)
            {
                elapsed += Time.deltaTime;

                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                {
                    Vector3 pressPos = player != null ? player.transform.position : Vector3.zero;

                    // single authoritative call to RecordSpacePress — it returns whether correct
                    bool correct = rewardManager.RecordSpacePress(pressPos);

                    if (correct && firstTry) trialSucceeded = true;
                    else trialSucceeded = false;

                    waitingForSpace = false;
                    break;
                }

                // optional timeout to avoid infinite wait
                if (elapsed >= waitForPressTimeout)
                {
                    Debug.Log("[Instruction] Wait-for-press timed out; treating as incorrect.");
                    waitingForSpace = false;
                    trialSucceeded = false;
                    break;
                }

                // after the first frame without pressing, subsequent presses are no longer first-try
                firstTry = false;
                yield return null;
            }

            // show feedback for a short duration (reward remains shown if correct; otherwise you can display an icon)
            if (trialSucceeded)
            {
                Debug.Log("[Instruction] Correct first-try — showing positive feedback.");
                // keep the revealed reward visible (RecordSpacePress already called ShowReward)
            }
            else
            {
                Debug.Log("[Instruction] Incorrect — showing negative feedback (you can replace with icon).");
                // optionally show an incorrect icon here (not implemented)
            }

            // wait feedbackDuration so participant sees the result
            yield return new WaitForSeconds(feedbackDuration);

            // update streak safely
            var mgr = SceneSequenceManager.Instance;
            if (trialSucceeded)
            {
                if (mgr != null) mgr.IncrementInstructionStreak();
            }
            else
            {
                if (mgr != null) mgr.ResetInstructionStreak();
            }

            // check end condition
            if (mgr != null && mgr.instructionCorrectStreak >= requiredStreak)
            {
                // tidy up: hide any revealed reward, small pause, then go to main scene
                rewardManager.HideAllRewards();
                cameraManager.SetupAllocentricView();
                yield return new WaitForSeconds(0.8f);
                mgr.GoToMainScene();
                yield break;
            }

            // prepare for next trial: hide any revealed reward and move camera back to birds-eye
            rewardManager.HideAllRewards();
            cameraManager.SetupAllocentricView();

            // small pause before next trial
            yield return new WaitForSeconds(pauseBetweenTrials);
        }
    }

    int GetLastShown()
    {
        if (rewardManager == null) return -1;
        var t = rewardManager.GetType();
        var fi = t.GetField("lastShownRewardIdx", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (fi != null)
        {
            return (int)fi.GetValue(rewardManager);
        }
        return -1;
    }
}


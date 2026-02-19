// InstructionPhaseController.cs
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class InstructionPhaseController : MonoBehaviour
{
    public RewardManager_FreeMovement rewardManager;
    public CameraManager_FreeMovement cameraManager;
    public GameObject player;

    [Header("Settings")]
    public int requiredStreak = 3;
    public float displayDuration = 3.0f;
    public float pauseAfterHide = 0.15f;
    public float waitForPressTimeout = 30f;
    public float feedbackDuration = 2.0f;
    public float pauseBetweenTrials = 0.5f;

    private Vector3 originalPlayerPosition;
    private bool spaceHandled = false;

    void Start()
    {
        Debug.Log("[Instruction] Starting InstructionPhase");

        if (rewardManager == null) rewardManager = Object.FindAnyObjectByType<RewardManager_FreeMovement>();
        if (cameraManager == null) cameraManager = Object.FindAnyObjectByType<CameraManager_FreeMovement>();
        if (player == null) player = GameObject.FindWithTag("Player");

        if (player != null) originalPlayerPosition = player.transform.position;

        if (rewardManager != null) rewardManager.LoadConfiguration(0);

        if (SceneSequenceManager.Instance == null)
        {
            var go = new GameObject("SceneSequenceManager");
            go.AddComponent<SceneSequenceManager>();
            Debug.Log("[Instruction] Created SceneSequenceManager");
        }

        StartCoroutine(RunInstructionLoop());
    }

    IEnumerator RunInstructionLoop()
    {
        while (true)
        {
            int rewardCount = rewardManager != null ? rewardManager.GetCurrentRewardCount() : 0;
            if (rewardCount == 0) { Debug.LogWarning("[Instruction] No rewards available"); yield break; }

            int idx = Random.Range(0, rewardCount);
            Debug.Log($"[Instruction] Trial start - target reward: {(char)('A' + idx)}");

            // Setup birds-eye view
            rewardManager.HideAllRewards();
            cameraManager.SetupAllocentricView();

            if (player != null)
            {
                var rend = player.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
                var moverComponent = player.GetComponentInChildren<FreeContinuousMovement>();
                if (moverComponent != null) moverComponent.enabled = false;
                player.transform.position = originalPlayerPosition;
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // Show reward from birds-eye
            rewardManager.ShowReward(idx);
            yield return new WaitForSeconds(displayDuration);

            rewardManager.HideReward(idx);
            rewardManager.instructionTargetIdx = idx;

            yield return new WaitForSeconds(pauseAfterHide);

            // Drop to egocentric view
            cameraManager.BeginDrop();
            yield return new WaitForSeconds(cameraManager.transitionDuration + 0.1f);

            // Wait for single space press (FreeContinuousMovement stays enabled for movement)
            // Use pressDetected flag to ensure we only count the FIRST press
            bool pressDetected = false;
            bool trialSucceeded = false;
            float elapsed = 0f;

            while (!pressDetected)
            {
                elapsed += Time.deltaTime;

                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame)
                {
                    pressDetected = true;
                    Vector3 pressPos = player != null ? player.transform.position : Vector3.zero;
                    
                    // Call RecordSpacePress only ONCE
                    bool correct = rewardManager.RecordSpacePress(pressPos);
                    
                    Debug.Log($"[Instruction] Space pressed - correct={correct}");
                    trialSucceeded = correct;
                    break;
                }

                if (elapsed >= waitForPressTimeout)
                {
                    Debug.Log("[Instruction] Timeout - treating as incorrect");
                    pressDetected = true;
                    trialSucceeded = false;
                    break;
                }

                yield return null;
            }

            // Show feedback
            if (trialSucceeded)
            {
                Debug.Log("[Instruction] CORRECT - showing positive feedback");
            }
            else
            {
                Debug.Log("[Instruction] INCORRECT - showing negative feedback");
            }

            yield return new WaitForSeconds(feedbackDuration);

            // Update streak
            var mgr = SceneSequenceManager.Instance;
            if (trialSucceeded)
            {
                if (mgr != null) mgr.IncrementInstructionStreak();
            }
            else
            {
                if (mgr != null) mgr.ResetInstructionStreak();
            }

            // Check completion
            if (mgr != null && mgr.instructionCorrectStreak >= requiredStreak)
            {
                Debug.Log($"[Instruction] Required streak reached ({requiredStreak}) - transitioning to main scene");
                rewardManager.HideAllRewards();
                cameraManager.SetupAllocentricView();
                yield return new WaitForSeconds(0.8f);
                mgr.GoToFreeMovementScene();
                yield break;
            }

            // Reset for next trial
            rewardManager.HideAllRewards();
            cameraManager.SetupAllocentricView();
            yield return new WaitForSeconds(pauseBetweenTrials);
        }
    }
}
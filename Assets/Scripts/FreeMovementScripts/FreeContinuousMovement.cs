// File: FreeContinuousMovement.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeContinuousMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;      // units per second
    public float rotationSpeed = 120f;  // degrees per second (yaw)
    public bool useCharacterController = false;

    [Header("References")]
    public RewardManager_FreeMovement rewardManager; // assign or auto-find
    public CameraManager_FreeMovement cameraManager; // optional

    CharacterController cc;

    void Awake()
    {
        if (useCharacterController)
        {
            cc = GetComponent<CharacterController>();
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();
        }

        if (rewardManager == null) rewardManager = Object.FindAnyObjectByType<RewardManager_FreeMovement>();
        if (cameraManager == null) cameraManager = Object.FindAnyObjectByType<CameraManager_FreeMovement>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ===== FORWARD / BACKWARD MOVEMENT =====
        Vector3 move = Vector3.zero;

        if (keyboard.upArrowKey.isPressed)
            move += transform.forward;

        if (keyboard.downArrowKey.isPressed)
            move -= transform.forward;

        if (move.sqrMagnitude > 0.001f)
        {
            Vector3 delta = move.normalized * moveSpeed * Time.deltaTime;
            if (useCharacterController && cc != null) 
            {
                cc.Move(delta);
            }
            else 
            {
                transform.position += delta;
            }

            // Notify reward manager that the player moved (so it can hide revealed rewards if the player left the square)
            if (rewardManager != null)
            {
                rewardManager.CheckHideRewardOnMove(transform.position);
            }
        }

        // ===== ROTATION (TURNING) =====
        float turn = 0f;

        if (keyboard.leftArrowKey.isPressed)
            turn = -1f;

        if (keyboard.rightArrowKey.isPressed)
            turn = 1f;

        if (Mathf.Abs(turn) > 0.001f)
            transform.Rotate(Vector3.up, turn * rotationSpeed * Time.deltaTime);

        // ===== SPACE PRESS =====
        // ONLY handle space press if NOT in instruction mode (instructionTargetIdx == -1)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Vector3 pos = transform.position;
            if (rewardManager != null)
            {
                // Check if we're in instruction mode
                if (rewardManager.instructionTargetIdx == -1)
                {
                    // Normal gameplay mode - handle space press
                    rewardManager.RecordSpacePress(pos);
                }
                // If instructionTargetIdx >= 0, InstructionPhaseController handles it
            }
        }
    }
}

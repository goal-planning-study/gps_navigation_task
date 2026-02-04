using UnityEngine;
using UnityEngine.InputSystem; 
public class moveplayer : MonoBehaviour
{
    public float gridStepSize = 10.3f;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 100f;

    public rewardManager rewardManager;
    public CameraManager cameraManager;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    private bool isRotating = false;
    
    void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }
    
    void Update()
    {
        if (isRotating) //V: first check if we are rotating/supposed to be rotating
        {
            RotateToTarget();
        }
        else if (!isMoving)
        {
            CheckInput();
            rewardManager.RewardFound(transform.position, "");
        }
        else if (isMoving)
        {
            MoveToTarget(); 
        }
    }
    
    void CheckInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        string keyPressed = "";
        
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            keyPressed = "ArrowUp";
            Vector3 potentialTarget = transform.position + (transform.forward * gridStepSize);
            if (WithinBounds(potentialTarget)){
                targetPosition = potentialTarget;
                isMoving = true;
            }
            cameraManager.DisableMiniMap();
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            keyPressed = "ArrowDown";
            SetTarget(180f);
            cameraManager.DisableMiniMap();
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            keyPressed = "ArrowLeft";
            SetTarget(-90f);
            cameraManager.DisableMiniMap();
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            keyPressed = "ArrowRight";
            SetTarget(90f);
            cameraManager.DisableMiniMap();
        }

        // Check for reward with key info
        if (!string.IsNullOrEmpty(keyPressed))
        {
            rewardManager.RewardFound(transform.position, keyPressed);
        }
    }
    void SetTarget(float relativeYRotation) //V: calculate rotation target relative to current position and set isRotating to true
    {
        float currentYRotation = transform.rotation.eulerAngles.y;
        float newYRotation = currentYRotation + relativeYRotation;
        targetRotation = Quaternion.Euler(0, newYRotation, 0);
        isRotating = true;

    }

    void RotateToTarget()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime 
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.01f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    bool WithinBounds(Vector3 position) //V: check that we are within grid boundaries
    {
        float leftBound = -5.3f;
        float rightBound = 15.3f;
        float upBound = 25.6f; //V: for upper bounds we use z coordinates
        float bottomBound = 5f;
        float tolerance = 0.1f;

        return position.x > leftBound - tolerance && 
        position.x < rightBound + tolerance && 
        position.z < upBound + tolerance && 
        position.z > bottomBound - tolerance;
    }
    
    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime //V: Time.deltaTime = time since last frame; ensures moving time is constant despite ≠ computers may have ≠ updating speed
        );
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f) //V: if the distance between current and target position = 0.01, then snap to target 
        {
            transform.position = targetPosition;
            isMoving = false;

            rewardManager.RewardFound(transform.position, "");
        }
    }
}
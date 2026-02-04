using UnityEngine;
using UnityEngine.InputSystem; 

public class ContinuousMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 50f;
    public rewardManager rewardManager;
    public CameraManager cameraManager;
    
    private Vector3 lastPosition;
    private bool hasMoved = false;

    void Start()
    {
        lastPosition = transform.position; //V: set starting position as the current position of the player
    }

    void Update()
    {
        CheckContinuousInput();
        
        //V: if we have stopped moving, then call function to check if we have found a reward
        if (Vector3.Distance(transform.position, lastPosition) < 0.001f && hasMoved) // FIXED: position
        {
            rewardManager.RewardFound(transform.position, "");
            hasMoved = false;
        }
        
        lastPosition = transform.position;
    }

    void CheckContinuousInput()
    {
        Keyboard keyboard = Keyboard.current; 
        if (keyboard == null) return;
        
        bool inputReceived = false;
        
        if (keyboard.upArrowKey.isPressed) //V: keep moving as long as the key is being pressed
        {
            Vector3 potentialTarget = transform.position + (transform.forward * moveSpeed * Time.deltaTime);
            
            if (WithinBounds(potentialTarget))
            {
                transform.position = potentialTarget;
                hasMoved = true;
                inputReceived = true;
            }
        }
        else if (keyboard.leftArrowKey.isPressed)
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime / 90f);
            hasMoved = true;
            inputReceived = true;
        }
        else if (keyboard.rightArrowKey.isPressed)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime / 90f);
            hasMoved = true;
            inputReceived = true;
        }
        else if (keyboard.downArrowKey.isPressed)
        {
            transform.Rotate(Vector3.up, 180f * rotationSpeed * Time.deltaTime / 90f);
            hasMoved = true;
            inputReceived = true;
        }
        
        if (inputReceived) //V: disable minimap camera at first input
        {
            cameraManager.DisableMiniMap();
        }
    }

    bool WithinBounds(Vector3 position)
    {
        float leftBound = -5.3f;
        float rightBound = 15.3f;
        float upBound = 25.6f;
        float bottomBound = 5f;
        float tolerance = 0.1f;
        
        return position.x > leftBound - tolerance && 
               position.x < rightBound + tolerance && 
               position.z < upBound + tolerance && 
               position.z > bottomBound - tolerance;
    }
}
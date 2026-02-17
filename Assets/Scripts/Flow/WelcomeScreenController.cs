// Assets/Scripts/Flow/WelcomeScreenController.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class WelcomeScreenController : MonoBehaviour
{
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (SceneSequenceManager.Instance != null)
                SceneSequenceManager.Instance.GoToInstructionPhase();
            else
                Debug.LogWarning("SceneSequenceManager not found.");
        }
    }
}


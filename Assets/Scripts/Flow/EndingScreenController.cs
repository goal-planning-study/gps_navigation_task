// Assets/Scripts/Flow/EndingScreenController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EndingScreenController : MonoBehaviour
{
    public Text pointsText;
    public DataLogger dataLogger; // optional

    void Start()
    {
        if (dataLogger == null) dataLogger = Object.FindAnyObjectByType<DataLogger>();
        if (pointsText != null && SceneSequenceManager.Instance != null)
            pointsText.text = $"Points: {SceneSequenceManager.Instance.points}";
        // attempt to flush/send log via reflection if a suitable method exists
        if (dataLogger != null)
        {
            var mi = dataLogger.GetType().GetMethod("SendAllData");
            if (mi == null) mi = dataLogger.GetType().GetMethod("FlushAndSend");
            if (mi != null)
            {
                try { mi.Invoke(dataLogger, null); Debug.Log("[Ending] DataLogger send invoked."); }
                catch { Debug.LogWarning("[Ending] DataLogger send invocation failed."); }
            }
            else Debug.Log("[Ending] No DataLogger send method found; data will remain local until your existing flow sends it.");
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[Ending] Space pressed - quitting play mode (or application).");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

using UnityEngine;
using System.Runtime.InteropServices;

/// Initialises DataLogger with participant info from JavaScript/sessionStorage
public class ParticipantInfoReader : MonoBehaviour
{
    public DataLogger dataLogger;

    void Start()
    {
        if (dataLogger == null)
        {
            dataLogger = GetComponent<DataLogger>();
        }

        // Set default values - will be overwritten when parent sends message
        #if UNITY_EDITOR
        dataLogger.SetParticipantInfo("TEST_PARTICIPANT|TEST_STUDY|TEST_SESSION");
        Debug.Log("Editor mode: Using test participant info");
        #else
        Debug.Log("Waiting for participant info from parent window...");
        #endif
    }
}



















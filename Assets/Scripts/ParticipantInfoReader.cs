using UnityEngine;
using System.Runtime.InteropServices;

/// Initialises DataLogger with participant info from JavaScript/sessionStorage
public class ParticipantInfoReader : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string GetSessionStorageItem(string key);

    public DataLogger dataLogger;

    void Start()
    {
        if (dataLogger == null)
        {
            dataLogger = GetComponent<DataLogger>();
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        ReadParticipantInfo();
        #else
        // For testing in Editor
        dataLogger.SetParticipantInfo("TEST_PARTICIPANT|TEST_STUDY|TEST_SESSION");
        Debug.Log("Editor mode: Using test participant info");
        #endif
    }

    void ReadParticipantInfo()
    {
        try
        {
            string participantId = GetSessionStorageItem("participant_id");
            string studyId = GetSessionStorageItem("study_id");
            string sessionId = GetSessionStorageItem("session_id");

            // Fallback values if not found
            if (string.IsNullOrEmpty(participantId)) participantId = "UNKNOWN_PARTICIPANT";
            if (string.IsNullOrEmpty(studyId)) studyId = "UNKNOWN_STUDY";
            if (string.IsNullOrEmpty(sessionId)) sessionId = "UNKNOWN_SESSION";

            string combinedInfo = $"{participantId}|{studyId}|{sessionId}";
            dataLogger.SetParticipantInfo(combinedInfo);

            Debug.Log($"Participant info loaded from sessionStorage: {combinedInfo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to read participant info: {e.Message}");
            dataLogger.SetParticipantInfo("ERROR|ERROR|ERROR");
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

/// Logs ABCD task data & sends it to JavaScript for Pavlovia storage
public class DataLogger : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void SendDataToJS(string jsonData);

    [System.Serializable]
    public class TrialStartData
    {
        public string event_type = "trial_start";
        public string participant;
        public string study_id;
        public string session_id;
        public string session = "001";
        public string date;
        public int round;
        public int rep;
        public double start_ABCD_screen;
        public string trial_type;
        public string sequence;
        public float start_loc_x;
        public float start_loc_y;
        public float start_loc_z;
    }

    [System.Serializable]
    public class KeyPressData
    {
        public string event_type = "key_press";
        public string participant;
        public string study_id;
        public string session_id;
        public string session = "001";
        public string date;
        public int round;
        public int rep;
        public double t_step_press_global;
        public double t_step_press_curr_run;
        public string key_pressed;
        public int key_index;
    }

    [System.Serializable]
    public class MovementData
    {
        public string event_type = "movement";
        public string participant;
        public string study_id;
        public string session_id;
        public string session = "001";
        public string date;
        public int round;
        public int rep;
        public bool movement_complete = true;
        public float curr_loc_x;
        public float curr_loc_y;
        public float curr_loc_z;
        public float from_x;
        public float from_y;
        public float from_z;
        public double t_step_from_start_currrun;
        public double t_step_end_global;
        public double t_step_tglobal;
        public double length_step;
        public string direction;
        public float curr_rew_x;
        public float curr_rew_y;
        public float curr_rew_z;
        public string type;
        public string state;
        public bool found_reward;
        public int movement_index;
    }

    [System.Serializable]
    public class RewardData
    {
        public string event_type = "reward";
        public string participant;
        public string study_id;
        public string session_id;
        public string session = "001";
        public string date;
        public int round;
        public int rep;
        public float rew_loc_x;
        public float rew_loc_y;
        public float rew_loc_z;
        public double t_reward_start;
        public double t_reward_start_global;
        public string reward_letter;
        public int reward_index;
        public string state;
        public int moves_to_find;
    }

    // Participant info (set from JavaScript on startup)
    private string participantId;
    private string studyId;
    private string sessionId;
    
    // Trial tracking
    private double trialStartTime;
    private double experimentStartTime;
    
    void Awake()
    {
        experimentStartTime = GetUnixTimestamp();
    }

    /// Called from JavaScript to initialise participant info
    /// e.g., unityInstance.SendMessage('DataLogger', 'SetParticipantInfo', 'PID|STUDY|SESSION');
    public void SetParticipantInfo(string info)
    {
        string[] parts = info.Split('|');
        if (parts.Length >= 3)
        {
            participantId = parts[0];
            studyId = parts[1];
            sessionId = parts[2];
            Debug.Log($"DataLogger initialised: PID={participantId}, STUDY={studyId}, SESSION={sessionId}");
        }
    }

    public void LogTrialStart(int round, int rep, string trialType, string sequence, Vector3 startPos)
    {
        trialStartTime = GetUnixTimestamp();
        
        var data = new TrialStartData
        {
            participant = participantId,
            study_id = studyId,
            session_id = sessionId,
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            round = round,
            rep = rep,
            start_ABCD_screen = trialStartTime,
            trial_type = trialType,
            sequence = sequence,
            start_loc_x = startPos.x,
            start_loc_y = startPos.y,
            start_loc_z = startPos.z
        };

        SendToJavaScript(data);
    }

    public void LogKeyPress(int round, int rep, string key, int keyIndex)
    {
        double now = GetUnixTimestamp();
        
        var data = new KeyPressData
        {
            participant = participantId,
            study_id = studyId,
            session_id = sessionId,
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            round = round,
            rep = rep,
            t_step_press_global = now,
            t_step_press_curr_run = now - trialStartTime,
            key_pressed = key,
            key_index = keyIndex
        };

        SendToJavaScript(data);
    }

    public void LogMovement(int round, int rep, Vector3 fromPos, Vector3 toPos, string direction,
                           Vector3 targetPos, string trialType, string state, bool foundReward, int movementIndex)
    {
        double now = GetUnixTimestamp();
        double stepStart = now - 0.1;
        
        var data = new MovementData
        {
            participant = participantId,
            study_id = studyId,
            session_id = sessionId,
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            round = round,
            rep = rep,
            curr_loc_x = toPos.x,
            curr_loc_y = toPos.y,
            curr_loc_z = toPos.z,
            from_x = fromPos.x,
            from_y = fromPos.y,
            from_z = fromPos.z,
            t_step_from_start_currrun = stepStart - trialStartTime,
            t_step_end_global = now,
            t_step_tglobal = stepStart,
            length_step = now - stepStart,
            direction = direction,
            curr_rew_x = targetPos.x,
            curr_rew_y = targetPos.y,
            curr_rew_z = targetPos.z,
            type = trialType,
            state = state,
            found_reward = foundReward,
            movement_index = movementIndex
        };

        SendToJavaScript(data);
    }

    public void LogReward(int round, int rep, Vector3 rewardPos, string rewardLetter, 
                         int rewardIndex, string state, int movesToFind)
    {
        double now = GetUnixTimestamp();
        
        var data = new RewardData
        {
            participant = participantId,
            study_id = studyId,
            session_id = sessionId,
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            round = round,
            rep = rep,
            rew_loc_x = rewardPos.x,
            rew_loc_y = rewardPos.y,
            rew_loc_z = rewardPos.z,
            t_reward_start = now - trialStartTime,
            t_reward_start_global = now,
            reward_letter = rewardLetter,
            reward_index = rewardIndex,
            state = state,
            moves_to_find = movesToFind
        };

        SendToJavaScript(data);
    }

    private void SendToJavaScript(object data)
    {
        string json = JsonUtility.ToJson(data);

        Debug.Log("[WEBGL_DATA] " + json);
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            SendDataToJS(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send data to JS: {e.Message}");
        }
        #else
        Debug.Log($"[WEBGL DATA]: {json}");
        #endif
    }

    private double GetUnixTimestamp()
    {
        return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}

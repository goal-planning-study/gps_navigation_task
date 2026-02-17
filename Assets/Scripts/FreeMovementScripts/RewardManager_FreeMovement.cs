// File: RewardManager_FreeMovement.cs
using UnityEngine;
using System.Collections.Generic;

public class RewardManager_FreeMovement : MonoBehaviour
{
    [System.Serializable]
    public class GridPosition
    {
        public float x;  // Unity X (left/right)
        public float y;  // Unity Y (height)
        public float z;  // Unity Z (forward/back)
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class RewardConfiguration
    {
        public string configName;
        public string trialType; // "forw" or "backw"
        public List<GridPosition> rewardPositions;
    }

    [System.Serializable]
    public class ConfigurationData
    {
        public List<RewardConfiguration> configurations;
        public int trialsPerConfig;
    }

    [Header("Configuration File")]
    public TextAsset configurationFile;

    [Header("Reward Prefab")]
    public GameObject rewardPrefab;

    [Header("Data Logger")]
    public DataLogger dataLogger;

    [Header("Memorization / Free-Move Settings")]
    public int memorizationRepetitions = 2;
    public float rewardDisplayTime = 1.5f;
    public float pauseBetweenRewards = 0.5f;
    public float pauseBeforeDrop = 0.8f;
    public float uncoverThreshold = 0.5f; // world units tolerance for RecordSpacePress

    public bool autoStartConfiguration = true;
    public int instructionTargetIdx = -1;

    private ConfigurationData configData;
    private GameObject[] currentRewardObjects;
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;

    // logging
    private int keyPressIndex = 0;
    private int movementIndex = 0;
    private Vector3 playerStartPosition;

    private float lastSpaceTime = -10f;
    private const float spaceDebounce = 0.25f;

    private bool experimentFinished = false;
    private bool trialCompleted = false;

    void Awake()
    {
        LoadConfigurationsFromFile();

        if (configData != null && configData.configurations.Count > 0)
        {
            LoadConfiguration(0);
            Debug.Log("[Free] Awake: rewards created and hidden");
        }
        else
        {
            Debug.LogError("[Free] No configurations loaded!");
        }
    }

    void Start()
    {
        if (configData != null && configData.configurations.Count > 0)
        {
            Debug.Log($"[Free] Starting {configData.configurations[currentConfigIdx].configName}");
        }

        // auto-bind datalogger if not set
        if (dataLogger == null)
        {
            dataLogger = Object.FindAnyObjectByType<DataLogger>();
            if (dataLogger != null) Debug.Log("[Free] Bound DataLogger via FindAnyObjectByType");
        }

        // Only auto-start the first configuration if the inspector flag allows it.
        // InstructionPhase will set this flag to false in the Inspector to take manual control.
        if (autoStartConfiguration)
        {
            StartNextConfiguration_Free();
        }
    }

    public void LoadConfigurationsFromFile()
    {
        if (configurationFile == null)
        {
            Debug.LogError("[Free] Configuration file not assigned!");
            return;
        }

        try
        {
            configData = JsonUtility.FromJson<ConfigurationData>(configurationFile.text);
            Debug.Log($"[Free] Loaded {configData.configurations.Count} configurations from file");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Free] Failed to load configuration file: {e.Message}");
        }
    }

    public void LoadConfiguration(int index)
    {
        if (configData == null || index < 0 || index >= configData.configurations.Count)
        {
            Debug.LogError("[Free] Invalid configuration index");
            return;
        }

        currentConfigIdx = index;

        if (currentRewardObjects != null)
        {
            foreach (GameObject reward in currentRewardObjects)
                if (reward != null) Destroy(reward);
        }

        List<GridPosition> positions = configData.configurations[index].rewardPositions;
        if (positions == null || positions.Count == 0)
        {
            Debug.LogError("[Free] No positions in configuration " + index);
            return;
        }

        currentRewardObjects = new GameObject[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 worldPos = positions[i].ToVector3();
            currentRewardObjects[i] = Instantiate(rewardPrefab, worldPos, Quaternion.identity);
            currentRewardObjects[i].name = $"Reward_{(char)('A' + i)}_{configData.configurations[index].configName}";
            var rend = currentRewardObjects[i].GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
            Debug.Log($"[Free] Reward {(char)('A' + i)} at world position: {worldPos}");
        }

        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        HideAllRewards();
        trialCompleted = false;
        Debug.Log($"[Free] Loaded {configData.configurations[index].configName} with {positions.Count} rewards");
    }

    public void StartNewTrial(Vector3 playerPosition)
    {
        playerStartPosition = playerPosition;
        keyPressIndex = 0;
        movementIndex = 0;

        if (dataLogger != null)
        {
            string sequence = GetCurrentSequence();
            string trialType = GetCurrentTrialType();
            var mi = dataLogger.GetType().GetMethod("LogTrialStart");
            if (mi != null)
            {
                try { mi.Invoke(dataLogger, new object[] { currentConfigIdx, repsCompleted, trialType, sequence, playerStartPosition }); }
                catch { Debug.LogWarning("[Free] DataLogger.LogTrialStart invocation failed."); }
            }
        }

        Debug.Log($"[Free] Trial started cfg={currentConfigIdx} rep={repsCompleted} startPos={playerStartPosition}");
    }

    private string GetCurrentSequence()
    {
        RewardConfiguration config = configData.configurations[currentConfigIdx];
        return config.trialType == "backw" ? "D-C-B-A" : "A-B-C-D";
    }

    // made public so CameraManager_FreeMovement can read it
    public string GetCurrentTrialType() => configData.configurations[currentConfigIdx].trialType;

    public int GetCurrentRewardCount() => currentRewardObjects != null ? currentRewardObjects.Length : 0;
    public string GetCurrentConfigName() => configData.configurations[currentConfigIdx].configName;

    public bool RecordSpacePress(Vector3 worldPos)
    {
        // debounce duplicate calls (same real press may be routed twice)
        if (Time.time - lastSpaceTime < spaceDebounce)
        {
            Debug.Log("[Free] RecordSpacePress ignored due to debounce.");
            return false;
        }
        lastSpaceTime = Time.time;

        Debug.Log($"[Free] Space press at ({worldPos.x:F3},{worldPos.y:F3},{worldPos.z:F3}) cfg={currentConfigIdx} rep={repsCompleted}");

        // Log raw press coordinates if DataLogger supports it
        if (dataLogger != null)
        {
            var mi = dataLogger.GetType().GetMethod("LogSpacePress");
            if (mi != null)
            {
                try { mi.Invoke(dataLogger, new object[] { currentConfigIdx, repsCompleted, worldPos }); }
                catch { Debug.LogWarning("[Free] DataLogger.LogSpacePress invocation failed."); }
            }
        }

        if (experimentFinished || trialCompleted || currentRewardObjects == null) return false;

        // choose the intended target: instructionTargetIdx (if set) otherwise the sequence nextRewardIdx
        int targetIdx = (instructionTargetIdx >= 0) ? instructionTargetIdx : nextRewardIdx;

        if (targetIdx < 0 || targetIdx >= currentRewardObjects.Length)
        {
            Debug.LogWarning("[Free] RecordSpacePress: invalid target index.");
            // clear instruction target in case it was invalid to avoid stuck state
            if (instructionTargetIdx >= 0) instructionTargetIdx = -1;
            return false;
        }

        Vector3 rewardCenter = currentRewardObjects[targetIdx].transform.position;

        // compute cell half-sizes robustly (same code as before)
        List<float> xs = new List<float>();
        List<float> zs = new List<float>();
        foreach (var go in currentRewardObjects)
        {
            if (go == null) continue;
            xs.Add(go.transform.position.x);
            zs.Add(go.transform.position.z);
        }
        xs.Sort(); zs.Sort();

        float defaultSpacing = 10.3f;
        float minDx = float.MaxValue;
        for (int i = 1; i < xs.Count; i++)
        {
            float d = Mathf.Abs(xs[i] - xs[i - 1]);
            if (d > 0f && d < minDx) minDx = d;
        }
        float minDz = float.MaxValue;
        for (int i = 1; i < zs.Count; i++)
        {
            float d = Mathf.Abs(zs[i] - zs[i - 1]);
            if (d > 0f && d < minDz) minDz = d;
        }

        float cellWidth = (minDx == float.MaxValue) ? defaultSpacing : minDx;
        float cellDepth = (minDz == float.MaxValue) ? defaultSpacing : minDz;
        float halfSizeX = Mathf.Max(cellWidth * 0.5f - 0.001f, 0.01f);
        float halfSizeZ = Mathf.Max(cellDepth * 0.5f - 0.001f, 0.01f);

        bool insideX = worldPos.x >= (rewardCenter.x - halfSizeX) && worldPos.x <= (rewardCenter.x + halfSizeX);
        bool insideZ = worldPos.z >= (rewardCenter.z - halfSizeZ) && worldPos.z <= (rewardCenter.z + halfSizeZ);

        if (insideX && insideZ)
        {
            Debug.Log($"[Free] Correct uncover for reward {(char)('A' + targetIdx)} (center={rewardCenter}, press={worldPos})");

            // show the revealed reward
            ShowReward(targetIdx);
            lastShownRewardIdx = targetIdx;

            // If this was an instruction check, clear the instructionTargetIdx but do NOT advance the sequence
            if (instructionTargetIdx >= 0)
            {
                instructionTargetIdx = -1;
                // don't advance nextRewardIdx — instruction trials are separate from normal sequence
            }
            else
            {
                // Normal sequence gameplay: advance and log reward
                nextRewardIdx++;
                movementIndex++;

                if (dataLogger != null)
                {
                    var mi = dataLogger.GetType().GetMethod("LogReward");
                    if (mi != null)
                    {
                        try
                        {
                            string state = ((char)('A' + (nextRewardIdx - 1))).ToString();
                            mi.Invoke(dataLogger, new object[] {
                                currentConfigIdx,
                                repsCompleted,
                                rewardCenter,
                                state,
                                nextRewardIdx - 1,
                                state,
                                movementIndex
                            });
                        }
                        catch { /* ignore */ }
                    }
                }

                if (nextRewardIdx >= currentRewardObjects.Length)
                {
                    repsCompleted++;
                    trialCompleted = true;
                    CompleteTrial_Free();
                }
            }

            return true;
        }
        else
        {
            float dx = Mathf.Abs(worldPos.x - rewardCenter.x);
            float dz = Mathf.Abs(worldPos.z - rewardCenter.z);
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            Debug.Log($"[Free] Incorrect uncover (dist={dist:F3}) - player pressed at {worldPos}, expected square centered at {rewardCenter}");

            if (dataLogger != null)
            {
                var mi = dataLogger.GetType().GetMethod("LogSpaceMiss");
                if (mi != null)
                {
                    try { mi.Invoke(dataLogger, new object[] { currentConfigIdx, repsCompleted, worldPos, dist }); }
                    catch { /* ignore */ }
                }
            }

            // if instruction trial, clear instruction target so controller can proceed
            if (instructionTargetIdx >= 0) instructionTargetIdx = -1;

            return false;
        }
    }


    public void CheckHideRewardOnMove(Vector3 playerPos)
    {
        if (lastShownRewardIdx < 0 || currentRewardObjects == null) return;
        if (lastShownRewardIdx >= currentRewardObjects.Length) { lastShownRewardIdx = -1; return; }

        // compute cell half-sizes the same way as in RecordSpacePress
        List<float> xs = new List<float>();
        List<float> zs = new List<float>();
        foreach (var go in currentRewardObjects)
        {
            if (go == null) continue;
            xs.Add(go.transform.position.x);
            zs.Add(go.transform.position.z);
        }
        xs.Sort();
        zs.Sort();

        float defaultSpacing = 10.3f;
        float minDx = float.MaxValue;
        for (int i = 1; i < xs.Count; i++)
        {
            float d = Mathf.Abs(xs[i] - xs[i - 1]);
            if (d > 0f && d < minDx) minDx = d;
        }
        float minDz = float.MaxValue;
        for (int i = 1; i < zs.Count; i++)
        {
            float d = Mathf.Abs(zs[i] - zs[i - 1]);
            if (d > 0f && d < minDz) minDz = d;
        }

        float cellWidth = (minDx == float.MaxValue) ? defaultSpacing : minDx;
        float cellDepth = (minDz == float.MaxValue) ? defaultSpacing : minDz;
        float halfSizeX = cellWidth * 0.5f;
        float halfSizeZ = cellDepth * 0.5f;

        float eps = 0.001f;
        halfSizeX = Mathf.Max(halfSizeX - eps, 0.01f);
        halfSizeZ = Mathf.Max(halfSizeZ - eps, 0.01f);

        Vector3 rewardCenter = currentRewardObjects[lastShownRewardIdx].transform.position;
        bool insideX = playerPos.x >= (rewardCenter.x - halfSizeX) && playerPos.x <= (rewardCenter.x + halfSizeX);
        bool insideZ = playerPos.z >= (rewardCenter.z - halfSizeZ) && playerPos.z <= (rewardCenter.z + halfSizeZ);

        if (!(insideX && insideZ))
        {
            // player left the square — hide the revealed reward
            HideReward(lastShownRewardIdx);
            Debug.Log($"[Free] Player left square for reward {(char)('A' + lastShownRewardIdx)}; hiding reward.");
            lastShownRewardIdx = -1;
        }
    }

    void CompleteTrial_Free()
    {
        Debug.Log($"[Free] CompleteTrial_Free: repsCompleted={repsCompleted}");
        if (repsCompleted < configData.trialsPerConfig)
        {
            Invoke(nameof(ResetTrial), 2f);
            return;
        }

        if (currentConfigIdx < configData.configurations.Count - 1)
        {
            currentConfigIdx++;
            repsCompleted = 0;
            Invoke(nameof(StartNextConfiguration_Free), 2f);
        }
        else
        {
            experimentFinished = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    public void StartNextConfiguration_Free()
    {
        LoadConfiguration(currentConfigIdx);

        var cam = Object.FindAnyObjectByType<CameraManager_FreeMovement>();
        if (cam != null) cam.StartNewConfiguration(currentConfigIdx);

        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
    }

    void ResetTrial()
    {
        HideAllRewards();
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        trialCompleted = false;
    }

    public void ShowReward(int index)
    {
        if (index >= 0 && currentRewardObjects != null && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
            currentRewardObjects[index].GetComponent<Renderer>().enabled = true;
    }

    public void HideReward(int index)
    {
        if (index >= 0 && currentRewardObjects != null && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
            currentRewardObjects[index].GetComponent<Renderer>().enabled = false;
    }

    public void HideAllRewards()
    {
        if (currentRewardObjects == null) return;
        foreach (var r in currentRewardObjects)
            if (r != null)
                r.GetComponent<Renderer>().enabled = false;
    }

    // Compatibility shim (optional)
    public bool RewardFound(Vector3 playerPosition, string keyPressed)
    {
        RecordSpacePress(playerPosition);
        return true;
    }
}

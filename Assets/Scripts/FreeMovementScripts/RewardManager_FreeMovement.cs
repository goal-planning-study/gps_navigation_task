// File: RewardManager_FreeMovement.cs
using UnityEngine;
using System.Collections.Generic;

public class RewardManager_FreeMovement : MonoBehaviour
{
    [System.Serializable]
    public class GridPosition
    {
        public float x;
        public float y;
        public float z;
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class RewardConfiguration
    {
        public string configName;
        public string trialType;
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

    [Header("Settings")]
    public float cellTolerance = 0.45f; // fraction of cell size (0.5 = full cell)

    public bool autoStartConfiguration = true;
    public int instructionTargetIdx = -1;

    private ConfigurationData configData;
    private GameObject[] currentRewardObjects;
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;

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

        if (dataLogger == null)
        {
            dataLogger = Object.FindAnyObjectByType<DataLogger>();
            if (dataLogger != null) Debug.Log("[Free] Bound DataLogger");
        }

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
            Debug.Log($"[Free] Loaded {configData.configurations.Count} configurations");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Free] Failed to load config: {e.Message}");
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
        }

        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        HideAllRewards();
        trialCompleted = false;
        
        // Reset player to starting position and north-facing rotation
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        
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

        Debug.Log($"[Free] Trial started cfg={currentConfigIdx} rep={repsCompleted}");
    }

    private string GetCurrentSequence()
    {
        RewardConfiguration config = configData.configurations[currentConfigIdx];
        return config.trialType == "backw" ? "D-C-B-A" : "A-B-C-D";
    }

    public string GetCurrentTrialType() => configData.configurations[currentConfigIdx].trialType;
    public int GetCurrentRewardCount() => currentRewardObjects != null ? currentRewardObjects.Length : 0;
    public string GetCurrentConfigName() => configData.configurations[currentConfigIdx].configName;

    // Calculate grid cell size from reward positions
    private (float cellWidth, float cellDepth) GetCellDimensions()
    {
        if (currentRewardObjects == null || currentRewardObjects.Length < 2)
            return (10.3f, 10.3f); // default

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

        float minDx = float.MaxValue;
        for (int i = 1; i < xs.Count; i++)
        {
            float d = Mathf.Abs(xs[i] - xs[i - 1]);
            if (d > 0.01f && d < minDx) minDx = d;
        }

        float minDz = float.MaxValue;
        for (int i = 1; i < zs.Count; i++)
        {
            float d = Mathf.Abs(zs[i] - zs[i - 1]);
            if (d > 0.01f && d < minDz) minDz = d;
        }

        float cellWidth = (minDx == float.MaxValue) ? 10.3f : minDx;
        float cellDepth = (minDz == float.MaxValue) ? 10.3f : minDz;

        return (cellWidth, cellDepth);
    }

    public bool RecordSpacePress(Vector3 worldPos)
    {
        if (Time.time - lastSpaceTime < spaceDebounce)
        {
            return false;
        }
        lastSpaceTime = Time.time;

        Debug.Log($"[Free] Space press at ({worldPos.x:F3},{worldPos.z:F3}) cfg={currentConfigIdx} rep={repsCompleted}");

        if (dataLogger != null)
        {
            var mi = dataLogger.GetType().GetMethod("LogSpacePress");
            if (mi != null)
            {
                try { mi.Invoke(dataLogger, new object[] { currentConfigIdx, repsCompleted, worldPos }); }
                catch { }
            }
        }

        if (experimentFinished || trialCompleted || currentRewardObjects == null) return false;

        int targetIdx = (instructionTargetIdx >= 0) ? instructionTargetIdx : nextRewardIdx;

        if (targetIdx < 0 || targetIdx >= currentRewardObjects.Length)
        {
            if (instructionTargetIdx >= 0) instructionTargetIdx = -1;
            return false;
        }

        Vector3 rewardCenter = currentRewardObjects[targetIdx].transform.position;
        var (cellWidth, cellDepth) = GetCellDimensions();

        // Use tolerance as fraction of cell size
        float halfSizeX = cellWidth * cellTolerance;
        float halfSizeZ = cellDepth * cellTolerance;

        float dx = Mathf.Abs(worldPos.x - rewardCenter.x);
        float dz = Mathf.Abs(worldPos.z - rewardCenter.z);

        bool insideCell = (dx <= halfSizeX) && (dz <= halfSizeZ);

        Debug.Log($"[Free] Target={targetIdx}, RewardCenter=({rewardCenter.x:F2},{rewardCenter.z:F2}), dx={dx:F2}, dz={dz:F2}, halfX={halfSizeX:F2}, halfZ={halfSizeZ:F2}, inside={insideCell}");

        if (insideCell)
        {
            Debug.Log($"[Free] CORRECT uncover for reward {(char)('A' + targetIdx)}");

            ShowReward(targetIdx);
            lastShownRewardIdx = targetIdx;

            if (instructionTargetIdx >= 0)
            {
                // Instruction trial - don't advance sequence
                instructionTargetIdx = -1;
            }
            else
            {
                // Normal gameplay - advance sequence
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
                        catch { }
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
            Debug.Log($"[Free] INCORRECT - outside cell (dx={dx:F2} > {halfSizeX:F2} OR dz={dz:F2} > {halfSizeZ:F2})");

            if (dataLogger != null)
            {
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                var mi = dataLogger.GetType().GetMethod("LogSpaceMiss");
                if (mi != null)
                {
                    try { mi.Invoke(dataLogger, new object[] { currentConfigIdx, repsCompleted, worldPos, dist }); }
                    catch { }
                }
            }

            if (instructionTargetIdx >= 0) instructionTargetIdx = -1;
            return false;
        }
    }

    public void CheckHideRewardOnMove(Vector3 playerPos)
    {
        if (lastShownRewardIdx < 0 || currentRewardObjects == null) return;
        if (lastShownRewardIdx >= currentRewardObjects.Length) { lastShownRewardIdx = -1; return; }

        Vector3 rewardCenter = currentRewardObjects[lastShownRewardIdx].transform.position;
        var (cellWidth, cellDepth) = GetCellDimensions();

        float halfSizeX = cellWidth * cellTolerance;
        float halfSizeZ = cellDepth * cellTolerance;

        float dx = Mathf.Abs(playerPos.x - rewardCenter.x);
        float dz = Mathf.Abs(playerPos.z - rewardCenter.z);

        bool insideCell = (dx <= halfSizeX) && (dz <= halfSizeZ);

        if (!insideCell)
        {
            HideReward(lastShownRewardIdx);
            Debug.Log($"[Free] Player left square for reward {(char)('A' + lastShownRewardIdx)}");
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
}
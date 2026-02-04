using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class rewardManager : MonoBehaviour
{
    [System.Serializable]
    public class GridPosition
    {
        public float x;  // Unity X (left/right)
        public float y;  // Unity Y (height)
        public float z;  // Unity Z (forward/back)
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
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
    
    private ConfigurationData configData;
    private GameObject[] currentRewardObjects; //V: array containing sequence of rewards
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;

    // Movement tracking
    private int keyPressIndex = 0;
    private int movementIndex = 0;
    private Vector3 playerStartPosition;

    private bool experimentFinished = false;
    private bool trialCompleted = false;


    public int GetCurrentRewardCount()
    {
        return currentRewardObjects != null ? currentRewardObjects.Length : 0;
    }
    
    void Awake() //V: Awake() takes precedence over any Start() in any of the scripts, so makes sure all rewards are hidden before starting 
    {
        LoadConfigurationsFromFile();
        
        if (configData != null && configData.configurations.Count > 0)
        {
            LoadConfiguration(0);
            Debug.Log("Awake complete - rewards created and hidden");
        }
        else
        {
            Debug.LogError("No configurations loaded!");
        }
    }

    void Start()
    {
        if (configData != null && configData.configurations.Count > 0)
        {
            Debug.Log($"Starting {configData.configurations[currentConfigIdx].configName}");
            Debug.Log($"Total configurations loaded: {configData.configurations.Count}");
        }
    }

    void LoadConfigurationsFromFile()
    {
        if (configurationFile == null)
        {
            Debug.LogError("Configuration file not assigned!");
            return;
        }
        
        try
        {
            configData = JsonUtility.FromJson<ConfigurationData>(configurationFile.text);
            Debug.Log($"Loaded {configData.configurations.Count} configurations from file");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load configuration file: {e.Message}");
        }
    }

    public void LoadConfiguration(int index)
    {
        if (configData == null || index < 0 || index >= configData.configurations.Count)
        {
            Debug.LogError("Invalid configuration index");
            return;
        }

        currentConfigIdx = index;

        // Destroy old rewards
        if (currentRewardObjects != null)
        {
            foreach (GameObject reward in currentRewardObjects)
                if (reward != null) Destroy(reward);
        }

        List<GridPosition> positions = configData.configurations[index].rewardPositions;
        if (positions == null || positions.Count == 0)
        {
            Debug.LogError("No positions in configuration " + index);
            return;
        }

        // create array sized to positions
        currentRewardObjects = new GameObject[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 worldPos = positions[i].ToVector3();
            currentRewardObjects[i] = Instantiate(rewardPrefab, worldPos, Quaternion.identity);
            currentRewardObjects[i].name = $"Reward_{(char)('A' + i)}_{configData.configurations[index].configName}";
            var rend = currentRewardObjects[i].GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
            Debug.Log($"Reward {(char)('A' + i)} at world position: {worldPos}");
        }

        // reset indices for new configuration
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        HideAllRewards();
        trialCompleted = false;

        Debug.Log($"Loaded {configData.configurations[index].configName} with {positions.Count} rewards");
        Debug.Log($"Starting trial {repsCompleted + 1}/{configData.trialsPerConfig} of Config {currentConfigIdx}");
    }

    public void StartNewTrial(Vector3 playerPosition)
    {
        playerStartPosition = playerPosition;
        keyPressIndex = 0;
        movementIndex = 0;
        
        // Log trial start
        if (dataLogger != null)
        {
            string sequence = GetCurrentSequence();
            string trialType = GetCurrentTrialType();
            dataLogger.LogTrialStart(currentConfigIdx, repsCompleted, trialType, sequence, playerStartPosition);
        }
    }

    private string GetCurrentSequence()
    {
        RewardConfiguration config = configData.configurations[currentConfigIdx];
        if (config.trialType == "backw")
            return "D-C-B-A";
        else
            return "A-B-C-D";
    }

    private string GetCurrentTrialType()
    {
        return configData.configurations[currentConfigIdx].trialType;
    }

    
    public int GetTotalConfigurations()
    {
        return configData.configurations.Count;
    }
    
    public string GetCurrentConfigName()
    {
        return configData.configurations[currentConfigIdx].configName;
    }
    

    public bool RewardFound(Vector3 playerPosition, string keyPressed)
    {
        if (experimentFinished || trialCompleted)
        {
            return false;
        }

        // Log key press
        if (dataLogger != null && !string.IsNullOrEmpty(keyPressed))
        {
            dataLogger.LogKeyPress(currentConfigIdx, repsCompleted, keyPressed, keyPressIndex);
            keyPressIndex++;
        }

        // Debug.Log($"=== RewardFound called === nextRewardIdx={nextRewardIdx}");
        if (currentRewardObjects == null || nextRewardIdx >= currentRewardObjects.Length)
        {
            return false;
        }

        GameObject currReward = currentRewardObjects[nextRewardIdx];
        if (currReward == null) return false;

        Vector3 previousPosition = playerPosition; // Store for movement logging
        float distance = Vector3.Distance(playerPosition, currReward.transform.position);
        const float uncoverThreshold = 0.05f;

        if (distance < uncoverThreshold)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("spacebar was pressed");
                Debug.Log($"Reward {nextRewardIdx + 1}/{currentRewardObjects.Length} found!");

                // Log movement that found reward
                if (dataLogger != null)
                {
                    string state = ((char)('A' + nextRewardIdx)).ToString();
                    dataLogger.LogMovement(
                        currentConfigIdx, 
                        repsCompleted,
                        previousPosition,
                        playerPosition,
                        keyPressed,
                        currReward.transform.position,
                        GetCurrentTrialType(),
                        state,
                        true,
                        movementIndex
                    );
                    movementIndex++;
                    
                    // Log reward found
                    dataLogger.LogReward(
                        currentConfigIdx,
                        repsCompleted,
                        currReward.transform.position,
                        state,
                        nextRewardIdx,
                        state,
                        movementIndex
                    );
                }

                ShowReward(nextRewardIdx);
                lastShownRewardIdx = nextRewardIdx;
                nextRewardIdx++;

                if (nextRewardIdx >= currentRewardObjects.Length)
                {
                    repsCompleted++;
                    Debug.Log($"Last reward found! Trial {repsCompleted}/{configData.trialsPerConfig} complete");
                    trialCompleted = true;
                    CompleteTrial();
                }
                return true;
            }
            return false;
        }
        else
        {
            // Log movement without finding reward (if there was actual movement)
            if (dataLogger != null && !string.IsNullOrEmpty(keyPressed) && 
                Vector3.Distance(previousPosition, playerPosition) > 0.01f)
            {
                string state = ((char)('A' + nextRewardIdx)).ToString();
                dataLogger.LogMovement(
                    currentConfigIdx,
                    repsCompleted,
                    previousPosition,
                    playerPosition,
                    keyPressed,
                    currReward.transform.position,
                    GetCurrentTrialType(),
                    state,
                    false,
                    movementIndex
                );
                movementIndex++;
            }

            if (lastShownRewardIdx >= 0 && lastShownRewardIdx < currentRewardObjects.Length)
            {
                GameObject lastReward = currentRewardObjects[lastShownRewardIdx];
                if (lastReward != null)
                {
                    float distanceToLast = Vector3.Distance(playerPosition, lastReward.transform.position);
                    if (distanceToLast > uncoverThreshold * 1.5f)
                    {
                        HideReward(lastShownRewardIdx);
                        lastShownRewardIdx = -1;
                    }
                }
            }
        }
        return false;
    }
    

    private void FinishAllConfigurations()
    {
        Debug.Log("FinishAllConfigurations: all configs completed.");
        experimentFinished = true;

        // disable player movement
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            var mover = playerObj.GetComponent<moveplayer>();
            if (mover != null) mover.enabled = false;
            var rend = playerObj.GetComponent<Renderer>();
            if (rend != null) rend.enabled = false; // optional: hide player
        }

        // stop any further reward checks quickly
        nextRewardIdx = currentRewardObjects != null ? currentRewardObjects.Length : 0;

        // Editor convenience: stop Play mode when running inside the Editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

 
        
    void CompleteTrial()
    {
        Debug.Log($"CompleteTrial() called: repsCompleted={repsCompleted}, trialsPerConfig={configData.trialsPerConfig}, currentConfigIdx={currentConfigIdx}/{configData.configurations.Count-1}");

        // If we still need more repetitions for this configuration, reset for the next repetition
        if (repsCompleted < configData.trialsPerConfig)
        {
            Debug.Log($"Moving on to repetition {repsCompleted + 1}/{configData.trialsPerConfig}");
            Invoke(nameof(ResetTrial), 2f);
            return;
        }

        // We finished the required repetitions for the current configuration
        if (currentConfigIdx < configData.configurations.Count - 1)
        {
            Debug.Log($"{configData.configurations[currentConfigIdx].configName} complete! Advancing to next config.");
            currentConfigIdx++;
            repsCompleted = 0;
            Invoke(nameof(StartNextConfiguration), 2f);
        }
        else
        {
            Debug.Log("All configurations completed!");
            FinishAllConfigurations();
        }
    }

    void StartNextConfiguration()
    {
        // instantiate rewards for the new currentConfigIdx and reset trial state
        LoadConfiguration(currentConfigIdx);

        // ensure CameraManager shows the top-down/sequence for the new config
        var cam = FindObjectOfType<CameraManager>();
        if (cam != null)
        {
            Debug.Log($"Telling CameraManager to start config {currentConfigIdx}");
            cam.StartNewConfiguration(currentConfigIdx);
        }
        else
        {
            Debug.LogWarning("CameraManager not found; sequence won't be shown.");
        }

        repsCompleted = 0;
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
    }
    
    void ResetTrial()
    {
        HideAllRewards();
        nextRewardIdx = 0;
        lastShownRewardIdx = -1;
        trialCompleted = false;
        Debug.Log($"Starting trial {repsCompleted + 1}/{configData.trialsPerConfig} of Config {currentConfigIdx}");
    }

    public void ShowReward(int index)
    {
        Debug.Log($"ShowReward called with index: {index}");
        
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            Debug.Log($"Showing reward at index {index}, name: {currentRewardObjects[index].name}");            
            currentRewardObjects[index].GetComponent<Renderer>().enabled = true;
        }
        else
        {
            Debug.LogError($"Cannot show reward at index {index}!");
        }
    }

    public void HideReward(int index)
    {
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            currentRewardObjects[index].GetComponent<Renderer>().enabled = false;
        }
    }
    
    void HideAllRewards()
    {
        if (currentRewardObjects != null)
        {
            foreach (GameObject reward in currentRewardObjects)
            {
                if (reward != null)
                {
                    reward.GetComponent<Renderer>().enabled = false;
                }
            }
        }
    }
}

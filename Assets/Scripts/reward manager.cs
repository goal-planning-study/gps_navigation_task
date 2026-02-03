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
    
    private ConfigurationData configData;
    private GameObject[] currentRewardObjects; //V: array containing sequence of rewards
    private int currentConfigIdx = 0;
    private int nextRewardIdx = 0;
    private int repsCompleted = 0;
    private int lastShownRewardIdx = -1;

    // near the other public helpers
    public int GetCurrentRewardCount()
    {
        return currentRewardObjects != null ? currentRewardObjects.Length : 0;
    }

    private bool experimentFinished = false;

    
    void Awake() //V: Awake() takes precedence over any Start() in any of the scripts, so we make sure all rewards are hidden before starting 
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

        Debug.Log($"Loaded {configData.configurations[index].configName} with {positions.Count} rewards");
        Debug.Log($"Starting trial {repsCompleted + 1}/{configData.trialsPerConfig} of Config {currentConfigIdx}");
    }

    
    public int GetTotalConfigurations()
    {
        return configData.configurations.Count;
    }
    
    public string GetCurrentConfigName()
    {
        return configData.configurations[currentConfigIdx].configName;
    }
    

    public bool RewardFound(Vector3 playerPosition)
    {
        if (experimentFinished)
        {
            return false;
        }

        Debug.Log($"=== RewardFound called === nextRewardIdx={nextRewardIdx}");
        if (currentRewardObjects == null || nextRewardIdx >= currentRewardObjects.Length)
        {
            return false;
        }

        GameObject currReward = currentRewardObjects[nextRewardIdx];
        if (currReward == null) return false;

        float distance = Vector3.Distance(playerPosition, currReward.transform.position);

        // increase tolerance a little — 0.01 is extremely small
        const float uncoverThreshold = 0.05f;

        if (distance < uncoverThreshold)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("spacebar was pressed");
                Debug.Log($"Reward {nextRewardIdx + 1}/{currentRewardObjects.Length} found!");
                ShowReward(nextRewardIdx);
                lastShownRewardIdx = nextRewardIdx;
                nextRewardIdx++;

                if (nextRewardIdx >= currentRewardObjects.Length)
                {
                    repsCompleted++;
                    Debug.Log($"Last reward found! Trial {repsCompleted}/{configData.trialsPerConfig} complete");
                    CompleteTrial();
                }
                return true;
            }
            return false;
        }
        else
        {
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

 
     
    void CompleteTrial() //V: check if we have completed all repetitions of the current trial and switch to next configuration if appropriate
    {
        if (repsCompleted >= configData.trialsPerConfig)  
        {
            if (currentConfigIdx < configData.configurations.Count - 1)
            {
                Debug.Log($"{configData.configurations[currentConfigIdx].configName} complete!");
                currentConfigIdx++;
                repsCompleted = 0;

                Invoke("StartNextConfiguration", 2f); //V: have top down view of the next configuration start a few seconds after trial is completed
  
            }
            else
            {
                Debug.Log("All configurations completed!");
            }
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
        Debug.Log($"Starting trial {repsCompleted + 1}/{configData.trialsPerConfig} of Config {currentConfigIdx}");
    }

    public void ShowReward(int index)
    {
        Debug.Log($"ShowReward called with index: {index}");
        
        if (index >= 0 && index < currentRewardObjects.Length && currentRewardObjects[index] != null)
        {
            Debug.Log($"Showing reward at index {index}, name: {currentRewardObjects[index].name}");
            Debug.Log($"Renderer before: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");
            
            currentRewardObjects[index].GetComponent<Renderer>().enabled = true;
            
            Debug.Log($"Renderer after: {currentRewardObjects[index].GetComponent<Renderer>().enabled}");
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

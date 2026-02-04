using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    //V: create all necessary cameras
    public Camera firstPersonCamera;
    public Camera miniMapCamera;
    
    //V: create reward manager object for showing rewards
    public rewardManager rewardManager;
    
    //V: create player object
    public GameObject player;
    
    //V: specify timing variables
    public float rewardDisplayTime = 1.5f;
    public float pauseBetweenRewards = 0.5f;
    public float pauseBetweenSeq = 1f;
    
    [Header("Memorization Settings")]
    public int memorizationRepetitions = 2;  //V: how many times to show the sequence
    
    void Start()
    {
        //V: Start with first configuration (index 0)
        StartNewConfiguration(0);
    }
    
    //V: Called when starting a new configuration (at start and after completing trials)
    public void StartNewConfiguration(int configIndex)
    {
        //V: Load the new configuration in reward manager
        rewardManager.LoadConfiguration(configIndex);
        
        //V: Hide player and disable movement initially
        player.GetComponent<Renderer>().enabled = false;
        player.GetComponent<moveplayer>().enabled = false;
        
        //V: Setup camera for memorization phase
        SetupMemorizationCamera();
        
        Debug.Log($"Memorizing {rewardManager.GetCurrentConfigName()}: Watch the reward sequence!");
        
        //V: Start the coroutine to show rewards
        StartCoroutine(ShowRewardSequence());
    }
    
    void SetupMemorizationCamera()
    {
        firstPersonCamera.enabled = false;
        miniMapCamera.enabled = true;
        
        //V: Put camera as full screen to show rewards
        miniMapCamera.rect = new Rect(0, 0, 1, 1);
        miniMapCamera.depth = 0;
    }
    
    void SetupGameplayCameras()
    {
        firstPersonCamera.enabled = true;
        miniMapCamera.enabled = true;
        
        //V: Mini-map in top-right corner
        miniMapCamera.rect = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        miniMapCamera.depth = 1; 
    }
    
    IEnumerator ShowRewardSequence()
    {
        int rewardCount = rewardManager.GetCurrentRewardCount();
        if (rewardCount == 0)
        {
            Debug.LogError("ShowRewardSequence: no rewards to show (rewardCount == 0). Aborting sequence.");
            StartGamePhase();
            yield break;
        }

        for (int repetition = 0; repetition < memorizationRepetitions; repetition++)
        {
            Debug.Log($"Showing sequence {repetition + 1}/{memorizationRepetitions}");
            for (int i = 0; i < rewardCount; i++)
            {
                Debug.Log($"Showing reward index {i}");
                rewardManager.ShowReward(i);
                yield return new WaitForSeconds(rewardDisplayTime);
                rewardManager.HideReward(i);
                yield return new WaitForSeconds(pauseBetweenRewards);
            }

            if (repetition < memorizationRepetitions - 1)
                yield return new WaitForSeconds(pauseBetweenSeq);
        }

        Debug.Log("Memorization complete! Starting game...");
        yield return new WaitForSeconds(1f);
        StartGamePhase();
    }
    
    void StartGamePhase()
    {
        SetupGameplayCameras();
        
        player.GetComponent<Renderer>().enabled = true;
        player.GetComponent<moveplayer>().enabled = true;
        
        // Notify rewardManager that trial is starting
        var rewardMgr = FindObjectOfType<rewardManager>();
        if (rewardMgr != null)
        {
            rewardMgr.StartNewTrial(player.transform.position);
        }
        
        Debug.Log("Find the rewards in order: A → B → C → D");
    }

    public void DisableMiniMap()
    {
        
        if (miniMapCamera != null && miniMapCamera.enabled)
        {
            miniMapCamera.enabled = false;
        }
    }
}
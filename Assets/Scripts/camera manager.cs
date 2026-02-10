using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public Camera mainCamera;
    public rewardManager rewardManager;
    public GameObject player;
    
    [Header("Camera Positions")]
    public Vector3 allocentricPosition = new Vector3(5f, 30f, 15f);
    public Vector3 allocentricRotation = new Vector3(90f, 0f, 0f);
    public Vector3 egocentricOffset = new Vector3(0f, 3f, -4f);
    public float egocentricPitch = 18f;
    
    [Header("Timing")]
    public float rewardDisplayTime = 1.5f;
    public float pauseBetweenRewards = 0.5f;
    public float pauseBetweenSeq = 1f;
    public float transitionDuration = 2.5f;
    
    [Header("Memorization Settings")]
    public int memorizationRepetitions = 2;
    
    private Vector3 targetEgocentricPosition;
    private Quaternion targetEgocentricRotation;
    
    void Start()
    {
        StartNewConfiguration(0);
    }
    
    public void StartNewConfiguration(int configIndex)
    {
        rewardManager.LoadConfiguration(configIndex);
        
        player.GetComponent<Renderer>().enabled = false;
        player.GetComponent<moveplayer>().enabled = false;
        
        SetupAllocentricView();
        
        Debug.Log($"Memorizing {rewardManager.GetCurrentConfigName()}: Watch the reward sequence!");
        
        StartCoroutine(ShowRewardSequence());
    }
    
    void SetupAllocentricView()
    {
        mainCamera.transform.position = allocentricPosition;
        mainCamera.transform.eulerAngles = allocentricRotation;
        mainCamera.fieldOfView = 60f;
    }
    
    void CalculateEgocentricTarget()
    {
        // Position behind & above player using the egocentricOffset
        targetEgocentricPosition = player.transform.position + player.transform.TransformDirection(egocentricOffset);

        float pitch = egocentricPitch; // use inspector-tunable value
        float yaw = player.transform.eulerAngles.y;
        targetEgocentricRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    
    IEnumerator ShowRewardSequence()
    {
        int rewardCount = rewardManager.GetCurrentRewardCount();
        if (rewardCount == 0)
        {
            Debug.LogError("ShowRewardSequence: no rewards to show. Aborting.");
            StartGamePhase();
            yield break;
        }

        for (int repetition = 0; repetition < memorizationRepetitions; repetition++)
        {
            Debug.Log($"Showing sequence {repetition + 1}/{memorizationRepetitions}");
            for (int i = 0; i < rewardCount; i++)
            {
                rewardManager.ShowReward(i);
                yield return new WaitForSeconds(rewardDisplayTime);
                rewardManager.HideReward(i);
                yield return new WaitForSeconds(pauseBetweenRewards);
            }

            if (repetition < memorizationRepetitions - 1)
                yield return new WaitForSeconds(pauseBetweenSeq);
        }

        Debug.Log("Memorization complete! Transitioning to first-person view...");
        yield return new WaitForSeconds(1f);
        StartCoroutine(TransitionToEgocentric());
    }
    
    IEnumerator TransitionToEgocentric()
    {
        player.GetComponent<Renderer>().enabled = true;

        CalculateEgocentricTarget();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startPos, targetEgocentricPosition, smoothT);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetEgocentricRotation, smoothT);

            yield return null;
        }

        mainCamera.transform.position = targetEgocentricPosition;
        mainCamera.transform.rotation = targetEgocentricRotation;

        StartGamePhase();
    }
    
    void StartGamePhase()
    {
        // allow player movement
        player.GetComponent<moveplayer>().enabled = true;

        // Parent camera to player but KEEP world transform so the camera doesn't snap.
        mainCamera.transform.SetParent(player.transform, true);

        // Apply the inspector-tunable local offset and pitch so the camera sits behind & above the player.
        // Make sure you have a public float egocentricPitch defined (degrees) and egocentricOffset set in Inspector.
        mainCamera.transform.localPosition = egocentricOffset;
        mainCamera.transform.localRotation = Quaternion.Euler(egocentricPitch, 0f, 0f);

        var rewardMgr = FindObjectOfType<rewardManager>();
        if (rewardMgr != null)
        {
            rewardMgr.StartNewTrial(player.transform.position);
        }

        Debug.Log("Find the rewards in order: A → B → C → D");
    }
}
// SceneSequenceManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SceneSequenceManager : MonoBehaviour
{
    public static SceneSequenceManager Instance { get; private set; }

    [Header("Scene Names")]
    public string instructionScene = "InstructionPhase";
    public string mainScene = "My Scene";
    public string freeMovementScene = "FreeMovement";
    public string endingScene = "EndingScreen";

    [Header("Player State")]
    public int points = 0;
    public int instructionCorrectStreak = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SceneSequence] Manager initialized");
    }

    public void GoToInstructionPhase()
    {
        Debug.Log("[SceneSequence] Loading InstructionPhase");
        LoadScene(instructionScene);
    }

    public void GoToMainScene()
    {
        Debug.Log("[SceneSequence] Loading MainScene");
        LoadScene(mainScene);
    }

    public void GoToFreeMovementScene()
    {
        Debug.Log("[SceneSequence] Loading FreeMovementScene");
        LoadScene(freeMovementScene);
    }

    public void GoToEndingScene()
    {
        Debug.Log("[SceneSequence] Loading EndingScene");
        LoadScene(endingScene);
    }

    private void LoadScene(string sceneName)
    {
#if UNITY_EDITOR
        // In Editor: use EditorSceneManager (works without Build Settings)
        EditorSceneManager.LoadSceneInPlayMode(
            $"Assets/Scenes/{sceneName}.unity",
            new LoadSceneParameters(LoadSceneMode.Single)
        );
#else
        // In Build: use standard SceneManager (requires Build Settings)
        SceneManager.LoadScene(sceneName);
#endif
    }

    public void AddPoints(int v) { points += v; }
    public void ResetInstructionStreak() { instructionCorrectStreak = 0; }
    public void IncrementInstructionStreak()
    {
        instructionCorrectStreak++;
        Debug.Log($"[SceneSequence] Instruction streak: {instructionCorrectStreak}");
    }
}
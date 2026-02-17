// Assets/Scripts/Flow/SceneSequenceManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneSequenceManager : MonoBehaviour
{
    public static SceneSequenceManager Instance { get; private set; }

    [Header("Scene Order")]
    public string welcomeScene = "WelcomeScreen";
    public string instructionScene = "InstructionPhase";
    public string mainScene = "My Scene";
    public string freeMovementScene = "FreeMovement";
    public string endingScene = "EndingScreen";

    [Header("Player state")]
    public int points = 0;

    // internal
    public int instructionCorrectStreak = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // If already in another scene, ensure first scene is WelcomeScreen
        if (SceneManager.GetActiveScene().name != welcomeScene)
        {
            StartCoroutine(LoadSceneAsync(welcomeScene));
        }
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
    }

    public void GoToInstructionPhase() => StartCoroutine(LoadSceneAsync(instructionScene));
    public void GoToMainScene() => StartCoroutine(LoadSceneAsync(mainScene));
    public void GoToFreeMovementScene() => StartCoroutine(LoadSceneAsync(freeMovementScene));
    public void GoToEndingScene() => StartCoroutine(LoadSceneAsync(endingScene));

    // scoring helpers
    public void AddPoints(int v) { points += v; }
    public void ResetInstructionStreak() { instructionCorrectStreak = 0; }
    public void IncrementInstructionStreak() { instructionCorrectStreak++; }
}

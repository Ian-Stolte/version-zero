using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Stats")]
    public int runNum;
    public int lastLevelReached;

    public int health;
    public int levelDmg;
    public int levelKills;

    [Header("Timers")]
    public float rawTimer;
    public float gameplayTimer;
    public float levelTime;


    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        levelTime = 0;
        levelKills = 0;
        levelDmg = 0;
    }

    private void Update()
    {
        rawTimer += Time.deltaTime;
        if (GameManager.Instance != null)
            if (!GameManager.Instance.pauseGame)
            {
                gameplayTimer += Time.deltaTime;
                levelTime += Time.deltaTime;
            }
    }
    
    public void LoadGame(bool newGame)
    {
        runNum = (newGame) ? 1 : 2;
        StartCoroutine(LoadGameCor());
    }

    private IEnumerator LoadGameCor()
    {
        Fader.Instance.FadeIn(2);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("Startup UI");
    }

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }
}

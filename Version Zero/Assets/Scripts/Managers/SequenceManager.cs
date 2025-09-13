using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Dialogue")]
    public List<Dictionary<string, int>> timesPlayed = new List<Dictionary<string, int>>();

    [Header("Stats")]
    public int runNum;
    [HideInInspector] public bool skipIntro;
    public int lastLevelReached;

    public int health;
    public int levelDmg;
    public int levelKills;
    public ProgramData programData;

    [Header("Timers")]
    public float rawTimer;
    public float gameplayTimer;
    public float levelTime;

    [Header("Boss Progression")]
    public int[] boss1Kills = new int[3];
    public int[] boss2Kills = new int[3];


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
        skipIntro = !newGame;
        StartCoroutine(LoadGameCor());
    }

    private IEnumerator LoadGameCor()
    {
        Time.timeScale = 1.1f;
        StartCoroutine(AudioManager.Instance.StartFade("Title", 1.5f, 0f));
        Fader.Instance.FadeIn(2);
        yield return new WaitForSeconds(2);
        //SceneManager.LoadScene("Startup UI");
        SceneManager.LoadScene("Level 1");
    }

    public void Quit()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }


    public void ViewFunctions()
    {
        GameObject bg = GameObject.Find("View Functions");
        foreach (Transform child in bg.transform)
            if (child.name != "Back Button")
                Destroy(child.gameObject);
        bg.GetComponent<CanvasGroup>().alpha = 1;
        bg.GetComponent<CanvasGroup>().blocksRaycasts = true;


        int numSpawned = 0;
        foreach (GameObject g in programData.baseBlocks)
        {
            SpawnFunction(g, ref numSpawned, bg);
        }
        foreach (GameObject g in programData.effectBlocks)
        {
            SpawnFunction(g, ref numSpawned, bg);
        }
        foreach (GameObject g in programData.modBlocks)
        {
            if (g.name != "Aura" && g.name != "Auto")
            {
                SpawnFunction(g, ref numSpawned, bg);
            }
        }
    }

    private void SpawnFunction(GameObject g, ref int numSpawned, GameObject parent)
    {
        GameObject obj = Instantiate(g, Vector3.zero, Quaternion.identity, parent.transform);
        Symbol s = obj.GetComponent<Block>().symbol;
        s.GetComponent<Image>().enabled = true;
        s.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);
        s.enabled = false;
        obj.GetComponent<Block>().enabled = false;
        obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-780 + (numSpawned % 7) * 260, 390 - (numSpawned / 7) * 280);
        numSpawned++;
    }

    public void CloseFunctions()
    {
        GameObject bg = GameObject.Find("View Functions");
        foreach (Transform child in bg.transform)
            if (child.name != "Back Button")
                Destroy(child.gameObject);
        bg.GetComponent<CanvasGroup>().alpha = 0;
        bg.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Bools")]
    public bool scifiNames;
    public bool skipDialogue;
    public bool noSpawn;
    [HideInInspector] public bool pauseGame;
    [HideInInspector] public bool playerPaused;
    [HideInInspector] public bool loadingLevel;
    
    [Header("Rooms")]
    [SerializeField] private TextMeshProUGUI areaText;
    [SerializeField] private LayerMask terrainLayer;
    
    [Header("Enemy Spawn")]
    [SerializeField] private List<string> enemyPrefabs; //TODO: change to struct w/ spawn pct, weight, etc
    [SerializeField] private string[] enemyTypes;
    private string enemyType = "Logic";
    [SerializeField] private Transform enemyParent;
    [SerializeField] private List<int> waves = new List<int>();

    public Transform enemyTimer;
    private float spawnTimer;
    private float totalSpawn;
    private bool spawningEnemies;
    private float minSpawn = 20;
    private float maxSpawn = 30;

    [Header("Terminals")]
    [SerializeField] private GameObject terminalBar;
    [HideInInspector] public Image bar;
    [HideInInspector] public Terminal currentTerminal;
    [HideInInspector] public int numTerminals;
    public KeyCode terminalBind;
    [SerializeField] private Transform terminalIcons;
    [SerializeField] private GameObject terminalIcon;
    [SerializeField] private Material terminalGreen;

    [Header("Barrier")]
    [SerializeField] private Color unlockTextColor;
    [SerializeField] private Material barrierGreen;
    [SerializeField] private Material barrierUnlockGreen;

    [Header("Misc")]
    public GameObject bossUI;
    [SerializeField] private GameObject loadingText;
    [SerializeField] private GameObject gameOver;
    private GameObject canvas;
    private Transform player;


    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Playtest Options" || scene.name == "Startup UI")
        {
            Destroy(canvas);
            Destroy(gameObject);
        }
        else if (scene.name != "End Screen")
        {
            if (player == null)
                player = GameObject.Find("Player").transform;
            enemyParent = GameObject.Find("Enemies").transform;

            //create an icon for each terminal in the level
            foreach (Transform child in terminalIcons)
                Destroy(child.gameObject);
            
            numTerminals = 0;
            foreach (GameObject g in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (g.layer == LayerMask.NameToLayer("Terminal") && g.hideFlags == HideFlags.None && g.scene.IsValid())
                    numTerminals++;
            }
            for (int i = 0; i < numTerminals; i++)
            {
                GameObject icon = Instantiate(terminalIcon, Vector2.zero, terminalIcon.transform.rotation, terminalIcons);
                icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-810, 450 - 130*i - areaText.preferredHeight);
            }

            //set spawn pct & enemies available by level (15, 25 by default)
            enemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
            if (scene.name == "Level 4")
            {
                enemyPrefabs.Add("Artillerist");
                minSpawn = 15;
                maxSpawn = 25;
            }
            else if (scene.name == "Level 5")
            {
                minSpawn = 10;
                maxSpawn = 20;
            }
            else if (scene.name.Contains("Final"))
            {
                minSpawn = 1;
                maxSpawn = 3;
            }
        
            //replace enemies with chosen type
            if (scene.name != "Level 6")
            {
                List<GameObject> newEnemies = new List<GameObject>();
                foreach (Transform child in enemyParent)
                {
                    for (int i = 0; i < child.name.Length; i++)
                    {
                        if (child.name[i] == '_')
                        {
                            string name = child.name.Substring(0, i) + "_" + enemyType;
                            GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + name);
                            
                            if (prefab != null && child.gameObject.activeSelf)
                            {
                                newEnemies.Add(Instantiate(prefab, child.position, child.rotation));
                            }
                            break;
                        }
                    }
                }
                foreach (Transform child in enemyParent)
                    Destroy(child.gameObject);

                foreach (GameObject g in newEnemies)
                    g.transform.parent = enemyParent;
            }
        }

        int runNum = 2;
        if (SequenceManager.Instance != null)
            runNum = SequenceManager.Instance.runNum;

        //set up specific levels
        if (scene.name == "Level 1")
        {
            StartCoroutine(DialogueManager.Instance.IntroDialogue());
        }
        else if (scene.name == "Level 2")
        {
            Terminal terminal = GameObject.Find("Terminal").GetComponent<Terminal>();
            if (runNum == 1)
            {
                foreach (GameObject g in terminal.hiddenRoom)
                    g.SetActive(!g.activeSelf);
            }
            else
            {
                terminal.complete = true;
                Destroy(terminalIcons.GetChild(1).gameObject);
                numTerminals--;
            }
        }
        
        //check if spawning enemies
        if (scene.name != "End Screen")
        {
            int sceneNum = int.Parse(SceneManager.GetActiveScene().name.Substring(6));
            if (((sceneNum > 3 && sceneNum != 6) || (sceneNum == 3 && runNum > 1) || scene.name.Contains("Final")) && !noSpawn)
            {
                enemyTimer.gameObject.SetActive(true);
                player.GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
                spawningEnemies = true;
                spawnTimer = Random.Range(minSpawn / 2f, maxSpawn / 2f);
                totalSpawn = spawnTimer;
            }
            else
            {
                spawningEnemies = false;
                enemyTimer.gameObject.SetActive(false);
                if (scene.name != "Level 6")
                {
                    foreach (Transform child in enemyParent)
                       Destroy(child.gameObject);
                }
            }
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(LoadNextLevel(GameObject.Find("End Elevator").GetComponent<Elevator>().nextArea));
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            int killed = enemyParent.childCount;
            foreach (Transform child in enemyParent)
                Destroy(child.gameObject);
        }

        if (spawningEnemies && !pauseGame && !loadingLevel)
        {
            spawnTimer -= Time.deltaTime;
            enemyTimer.GetChild(2).GetComponent<TextMeshProUGUI>().text = Mathf.Round(spawnTimer * 10)/10f + "s";
            enemyTimer.GetChild(4).GetComponent<Image>().fillAmount = 1 - spawnTimer / totalSpawn;
            if (spawnTimer < 0)
            {
                StartCoroutine(WaveEnemies(1));
                spawnTimer = Random.Range(minSpawn, maxSpawn);
                totalSpawn = spawnTimer;
            }
        }
    }


    private void SetupWaves(int n)
    {
        int maxPerWave = Mathf.Max(2, (int)Mathf.Round(n*3/5));
        waves.Clear();
        foreach (Transform child in enemyParent)
        {
            Destroy(child.gameObject);
        }
        while (n > 0)
        {
            int numToAdd = Random.Range(2, Mathf.Min(n, maxPerWave)+1);
            if (n - numToAdd == 1)
                numToAdd--;
            waves.Add(numToAdd);
            n -= numToAdd;
        }
    }

    public IEnumerator WaveEnemies(int n, Vector3 setPos = default)
    {
        if (loadingLevel)
            yield break;
        for (int i = 0; i < n; i++)
        {
            string name = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)] + "_" + enemyType;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + name);
            if (prefab != null)
            {
                int repeats = name.Contains("Swarm") ? 2 : 1;
                for (int j = 0; j < repeats; j++)
                {
                    if (loadingLevel)
                        yield break;
                    if (setPos != Vector3.zero)
                    {
                        GameObject enemy = Instantiate(prefab, setPos + new Vector3(0, 15, 0), Quaternion.identity, enemyParent);
                        enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
                    }
                    else
                    {
                        float minDist = 5;
                        float maxDist = 10;
                        Vector3 offset = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized * Random.Range(5, 10) + new Vector3(0, 1, 0);
                        int attempts = 0;
                        //while pos overlaps something or doesn't touch the ground, regenerate
                        float checkSize = (prefab.name.Contains("Tank")) ? 1.5f : 0.5f;
                        while (Physics.OverlapSphere(player.position + offset, checkSize).Length > 0 || Physics.OverlapSphere(player.position + offset + new Vector3(0, -1.5f, 0), 1f, LayerMask.GetMask("Ground")).Length == 0)
                        {
                            offset = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized * Random.Range(minDist, maxDist) + new Vector3(0, 1, 0);
                            attempts++;
                            if (attempts == 10) //fail to find open spot
                            {
                                minDist++;
                                maxDist++;
                                attempts = 0;
                                if (maxDist > 20)
                                {
                                    Debug.Log("NO OPEN SPOT :(");
                                    break;
                                }
                            }
                        }
                        if (maxDist < 20)
                        {
                            GameObject enemy = Instantiate(prefab, player.position + offset + new Vector3(0, 15, 0), Quaternion.identity, enemyParent);
                            enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
                        }
                    }
                    yield return new WaitForSeconds(0.5f);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }


    public IEnumerator UseTerminal()
    {
        playerPaused = true;
        bar = Instantiate(terminalBar, player.transform.position + new Vector3(0, 1.3f, 0), Quaternion.identity).transform.GetChild(1).GetComponent<Image>();
        AudioManager.Instance.Play("Terminal Charge");
        float elapsed = 0;
        while (elapsed < 4)
        {
            if (bar == null)
                yield break;
            bar.fillAmount = elapsed/4f;
            yield return null;
            elapsed += Time.deltaTime;
        }
        currentTerminal.complete = true;
        Destroy(bar.transform.parent.gameObject);
        playerPaused = false;
        AudioManager.Instance.Play("Terminal Activate");
        AudioManager.Instance.Stop("Terminal Charge");
        if (currentTerminal.screen != null)
        {
            var mats = currentTerminal.screen.materials;
            mats[1] = terminalGreen;
            currentTerminal.screen.materials = mats;
            foreach (MeshRenderer m in currentTerminal.bars)
                m.material = terminalGreen;
        }
        DialogueManager.Instance.PlayTerminal(currentTerminal.ID);
        FinishTerminalIcon();
        numTerminals--;
        
        //disable barrier &/or show hidden room
        if (currentTerminal.barrier != null)
            UnlockBarrier(currentTerminal.barrier);
        foreach (GameObject g in currentTerminal.hiddenRoom)
            g.SetActive(!g.activeSelf);
    }

    public void FinishTerminalIcon()
    {
        Transform iconToChange = terminalIcons.GetChild(terminalIcons.childCount - numTerminals);
        iconToChange.GetChild(0).gameObject.SetActive(true);
        //if (numTerminals < terminalIcons.childCount)
        //    iconToChange.GetChild(1).gameObject.SetActive(true);
    }

    public void UnlockBarrier(Transform barrier)
    {
        int numLocks = 0;
        Transform locks = barrier.GetChild(0);
        foreach (Transform child in locks)
        {
            if (child.GetComponent<MeshRenderer>().material.name.Contains("Blue") || child.GetComponent<MeshRenderer>().material.name.Contains("Red"))
                numLocks++;
        }
        locks.GetChild(locks.childCount - numLocks).GetComponent<MeshRenderer>().material = barrierUnlockGreen;

        if (numLocks <= 1)
        {
            locks.gameObject.SetActive(false);
            barrier.GetChild(1).gameObject.SetActive(false);
            barrier.GetChild(2).GetComponent<MeshRenderer>().material = barrierGreen;
            barrier.GetChild(3).GetComponent<MeshRenderer>().material = barrierGreen;
            TextMeshProUGUI txt = barrier.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>();
            txt.text = "Welcome, AUTH_USER!";
            txt.color = unlockTextColor;
        }
    }


    public IEnumerator LoadNextLevel(string nextArea)
    {
        loadingLevel = true;
        AudioManager.Instance.Play("Elevator Down");
        foreach (Transform child in enemyParent)
            Destroy(child.gameObject);
        yield return new WaitForSeconds(0.5f);
        Fader.Instance.FadeIn(1.2f, true);
        yield return new WaitForSeconds(1.2f);
        yield return new WaitForSeconds(1.5f);
        loadingText.GetComponent<TextMeshProUGUI>().text = "Now approaching: \n" + nextArea;
        loadingText.SetActive(true);
        Color c = loadingText.GetComponent<TextMeshProUGUI>().color;
        loadingText.GetComponent<TextMeshProUGUI>().color = new Color(c.r, c.g, c.b, 1);
        yield return new WaitForSeconds(2f);

        float elapsed = 1;
        StartCoroutine(ElevatorSounds());
        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            yield return null;
            loadingText.GetComponent<TextMeshProUGUI>().color = new Color(c.r, c.g, c.b, elapsed);
        }
        loadingText.SetActive(false);
        int levelNum = int.Parse(SceneManager.GetActiveScene().name.Substring(6))+1;
        areaText.text = nextArea;
        if (levelNum < 7)
            SceneManager.LoadScene("Level " + levelNum);
        else
        {
            SequenceManager.Instance.health = player.GetComponent<PlayerMovement>().health;
            Destroy(player.gameObject);
            SceneManager.LoadScene("End Screen");
        }
        loadingLevel = false;
    }

    private IEnumerator ElevatorSounds()
    {
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(AudioManager.Instance.StartFade("Elevator Down", 0, 0.5f));
        yield return new WaitForSeconds(0.5f);
        AudioManager.Instance.Play("Elevator Stop");
        AudioManager.Instance.Stop("Elevator Down");
    }

    public IEnumerator FinalNextLevel(string scene)
    {
        //show dissolve/teleport effect?
        Fader.Instance.FadeInOut(0.5f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(scene);
    }


    public IEnumerator GameOver()
    {
        DialogueManager.Instance.StopCoroutines();
        player.GetComponent<PlayerPrograms>().enabled = false;
        pauseGame = true;
        StartCoroutine(AudioManager.Instance.FadeOutAll(0));
        AudioManager.Instance.Play("Static");
        AudioManager.Instance.Play("Game Over");
        Camera.main.GetComponent<GlitchManager>().ShowGlitch(2, 1);

        yield return new WaitForSeconds(2);
        AudioManager.Instance.Stop("Static");
        gameOver.SetActive(true);
        yield return new WaitForSeconds(1);

        TMPro.TextMeshProUGUI txt = gameOver.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        for (int i = 0; i < 4; i++)
        {
            txt.text = "_";
            yield return new WaitForSeconds(0.5f);
            txt.text = "";
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(1);
        string message = "Program Terminated";
        foreach (char c in message)
        {
            txt.text += c;
            if (c == ' ')
                yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1.5f);
        StartCoroutine(AudioManager.Instance.StartFade("Game Over", 2, 0));
        for (float i = 0; i < 1; i += 0.01f)
        {
            gameOver.transform.GetChild(1).GetComponent<CanvasGroup>().alpha = i;
            gameOver.transform.GetChild(2).GetComponent<CanvasGroup>().alpha = i;
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void Reset()
    {
        if (!loadingLevel)
            StartCoroutine(ResetCor());
    }

    private IEnumerator ResetCor()
    {
        loadingLevel = true;
        SequenceManager.Instance.runNum++;
        if (SceneManager.GetActiveScene().name != "End Screen")
            SequenceManager.Instance.lastRoom = int.Parse(SceneManager.GetActiveScene().name.Substring(6));
        else
            SequenceManager.Instance.lastRoom = 7;
        Fader.Instance.FadeIn(1.5f);
        yield return new WaitForSeconds(2);
        gameOver.SetActive(false);
        if (player == null)
            Destroy(GameObject.Find("Computer"));
        else
        {
            Destroy(player.GetComponent<PlayerMovement>().computer.gameObject);
            Destroy(player.gameObject);
        }
        canvas = GameObject.Find("Canvas");
        SceneManager.LoadScene("Startup UI");
        loadingLevel = false;
    }

    public void Quit()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}


[System.Serializable]
public class Room
{
    public string name;
    public bool active;
    public float weight;
    //tags like encounter type, etc.
}
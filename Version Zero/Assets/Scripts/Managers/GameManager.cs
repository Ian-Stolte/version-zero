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
    public bool skipDialogue;
    public bool noSpawn;
    [HideInInspector] public bool pauseGame;
    [HideInInspector] public bool playerPaused;
    [HideInInspector] public bool loadingLevel;

    [Header("Enemy Spawn")]
    [SerializeField] private string[] enemyPrefabs; //TODO: change to struct w/ spawn pct, weight, etc
    [SerializeField] private string[] enemyTypes;
    private string enemyType = "Logic";
    [SerializeField] private Transform enemyParent;

    public Transform enemyTimer;
    private float spawnTimer;
    private float totalSpawn;
    private bool spawningEnemies;
    [SerializeField] private Vector3[] spawnDelays;
    private float minSpawn = 15;
    private float maxSpawn = 25;

    [Header("Enemy Percents")]
    [SerializeField] private int[] clearPars; //instinct
    [SerializeField] private int[] killPars; //memory
    private float instinctLvl;
    private float logicLvl;
    private float memoryLvl;

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
    [SerializeField] private TextMeshProUGUI areaText;
    [SerializeField] private LayerMask terrainLayer;
    public GameObject bossUI;
    [SerializeField] private GameObject loadingText;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private GameObject finalVFX;
    private GameObject canvas;
    private Transform player;

    [Header("Debug")]
    [SerializeField] private GameObject testSphereGreen;
    [SerializeField] private GameObject testSphereRed;



    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int levelNum = 0;
        int.TryParse(SceneManager.GetActiveScene().name.Substring(6), out levelNum);

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
                icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-810, 450 - 130 * i - areaText.preferredHeight);
            }

            //set enemy type for the level
            if (levelNum == 6 || levelNum == 12)
            {
                //if boss, pick highest pct
                float maxLvl = Mathf.Max(logicLvl, memoryLvl, instinctLvl);
                if (maxLvl == logicLvl)
                    enemyType = "Logic";
                else if (maxLvl == memoryLvl)
                    enemyType = "Memory";
                else
                    enemyType = "Instinct";
            }
            else
            {
                //if normal level, use enemy pcts to randomly pick
                float total = logicLvl + instinctLvl + memoryLvl;
                if (total == 0)
                    enemyType = new[] { "Logic", "Instinct", "Memory" }[Random.Range(0, 3)];
                else
                {
                    float r = Random.Range(0f, total);
                    if (r < logicLvl)
                        enemyType = "Logic";
                    else if (r < logicLvl + instinctLvl)
                        enemyType = "Instinct";
                    else
                        enemyType = "Memory";
                }
            }

            //set enemies available by level
            if (!scene.name.Contains("Final"))
            {
                if (levelNum == 2 || levelNum == 3)
                    enemyPrefabs = new string[] { "Swarm", "Tank" };
                else if (levelNum == 4)
                    enemyPrefabs = new string[] { "Swarm", "Tank", "Artillery" };
                else if (levelNum == 7)
                    enemyPrefabs = new string[] { "Aggro" };
                else if (levelNum == 8)
                    enemyPrefabs = new string[] { "Evasive" };
                else if (levelNum == 9)
                    enemyPrefabs = new string[] { "Aggro", "Evasive" };
                else if (levelNum == 13)
                    enemyPrefabs = new string[] { "Landmine", "Charge" };
                else if (levelNum == 14)
                    enemyPrefabs = new string[] { "Landmine", "Charge", "Scatter" };
            }

            //set spawn delay
            if (scene.name.Contains("Final"))
            {
                minSpawn = 1;
                maxSpawn = 3;
            }
            else
            {
                minSpawn = spawnDelays[levelNum].x;
                maxSpawn = spawnDelays[levelNum].y;
            }

            //replace enemies with chosen type
            if (levelNum != 6 && levelNum != 12)
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

        int runNum = 1;
        if (SequenceManager.Instance != null)
            runNum = SequenceManager.Instance.runNum;

        //set up specific levels
        if (scene.name == "Level 1")
        {
            StartCoroutine(DialogueManager.Instance.IntroDialogue());
        }
        else if (scene.name == "Level 2")
        {
            Terminal terminal = GameObject.Find("Terminal A").GetComponent<Terminal>();
            if (runNum > 1)
            {
                terminal.complete = true;
                UnlockBarrier(terminal.barrier);
                FinishTerminalIcon();
                numTerminals--;
                foreach (MeshRenderer m in terminal.bars)
                    m.material = terminalGreen;
            }
        }

        //check if spawning enemies
        if (scene.name != "End Screen")
        {
            if (minSpawn > 0 && (levelNum > 3 || (levelNum == 3 && runNum > 1) || scene.name.Contains("Final")) && !noSpawn)
            {
                enemyTimer.gameObject.SetActive(true);
                player.GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
                spawningEnemies = true;
                spawnTimer = Random.Range(minSpawn / 2f, maxSpawn / 2f) + spawnDelays[levelNum].z;
                totalSpawn = spawnTimer;
            }
            else
            {
                spawningEnemies = false;
                enemyTimer.gameObject.SetActive(false);
                if (noSpawn)
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
            StartCoroutine(LoadNextLevel(GameObject.Find("Elevator_End").GetComponent<Elevator>().nextArea, true));
        }
        for (int i = 1; i <= 6; i++)
        {
            if (Input.GetKeyDown(i.ToString()) && Input.GetKey(KeyCode.LeftShift))
            {
                SceneManager.LoadScene("Level " + i);
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            int killed = enemyParent.childCount;
            foreach (Transform child in enemyParent)
                Destroy(child.gameObject);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(SpawnEnemies(1, Vector3.zero, true));
        }


        if (spawningEnemies && !pauseGame && !loadingLevel)
        {
            spawnTimer -= Time.deltaTime;
            enemyTimer.GetChild(2).GetComponent<TextMeshProUGUI>().text = Mathf.Round(spawnTimer * 10) / 10f + "s";
            enemyTimer.GetChild(4).GetComponent<Image>().fillAmount = 1 - spawnTimer / totalSpawn;
            if (spawnTimer < 0)
            {
                StartCoroutine(SpawnEnemies(1));
                spawnTimer = Random.Range(minSpawn, maxSpawn);
                totalSpawn = spawnTimer;
            }
        }
    }



    public IEnumerator SpawnEnemies(int n, Vector3 setPos = default, bool debug = false)
    {
        if (loadingLevel || enemyParent.childCount >= 25)
            yield break;

        if (debug)
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            //foreach (Transform child in enemyParent)
            //    Destroy(child.gameObject);
        }

        for (int i = 0; i < n; i++)
        {
            string name = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)] + "_" + enemyType;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Enemies/" + name);
            if (prefab != null)
            {
                int repeats = (name.Contains("Swarm") || name.Contains("Landmine")) ? 2 : 1;
                for (int j = 0; j < repeats; j++)
                {
                    if (loadingLevel)
                        yield break;
                    if (setPos != Vector3.zero) //if we're given a position, just spawn there
                    {
                        GameObject enemy = Instantiate(prefab, setPos + new Vector3(0, 15, 0), Quaternion.identity, enemyParent);
                        enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, -100, 0);
                    }
                    else
                    {
                        float minDist = 3;
                        float maxDist = 10;
                        Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * Random.Range(minDist, maxDist) + new Vector3(0, 1, 0);
                        int attempts = 0;

                        //while pos overlaps something or doesn't touch the ground, regenerate
                        float checkSize = (prefab.name.Contains("Tank")) ? 2f : 1f;
                        //TODO: more accurate check size? (based on prefab scale or smth)
                        //TODO: don't spawn if on other side of barrier? (check walkable)
                        while (Physics.OverlapSphere(player.position + offset, checkSize).Length > 0 || Physics.OverlapSphere(player.position + offset + new Vector3(0, -1.5f, 0), 1f, LayerMask.GetMask("Ground")).Length == 0)
                        {
                            if (debug)
                                Instantiate(testSphereRed, player.position + offset, Quaternion.identity, transform);
                            offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * Random.Range(minDist, maxDist) + new Vector3(0, 1, 0);
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
                            if (debug)
                                yield return new WaitForSeconds(0.2f);
                        }
                        if (maxDist < 20)
                        {
                            if (debug)
                                Instantiate(testSphereGreen, player.position + offset, Quaternion.identity, transform);
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
        bar = Instantiate(terminalBar, player.position + new Vector3(0, 1.3f, 0), Quaternion.identity).transform.GetChild(1).GetComponent<Image>();
        AudioManager.Instance.Play("Terminal Charge");
        float elapsed = 0;
        while (elapsed < 4)
        {
            if (bar == null)
                yield break;
            bar.fillAmount = elapsed / 4f;
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
        if (currentTerminal.ID != "")
            DialogueManager.Instance.PlayByID(currentTerminal.ID);
        foreach (GameObject g in currentTerminal.toggleOnComplete)
            g.SetActive(!g.activeSelf);
        FinishTerminalIcon();
        numTerminals--;

        if (currentTerminal.barrier != null)
            UnlockBarrier(currentTerminal.barrier);
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
        GameObject.Find("Pathfinding").GetComponent<Pathfinding>().UpdateGrids();
    }



    public IEnumerator LoadNextLevel(string nextArea, bool skip = false)
    {
        loadingLevel = true;
        int levelNum = 0;
        int.TryParse(SceneManager.GetActiveScene().name.Substring(6), out levelNum);
        levelNum++;

        if (!skip)
        {
            //compute change to enemy pcts
            if (levelNum-1 >= 3 && levelNum-1 != 6 && levelNum-1 != 12)
            {
                float time = SequenceManager.Instance.levelTime;
                int parTime = clearPars[levelNum - 1];
                if (time <= parTime * 0.5f)
                    instinctLvl += 5;
                else if (time <= parTime * 0.75)
                    instinctLvl += 4;
                else if (time <= parTime)
                    instinctLvl += 3;
                else if (time <= parTime * 1.5f)
                    instinctLvl += 2;
                else if (time <= parTime * 2f)
                    instinctLvl += 1;
                Debug.Log(instinctLvl + " (instinct)");

                int kills = SequenceManager.Instance.levelKills;
                int parKills = killPars[levelNum - 1];
                if (kills >= parKills)
                    logicLvl += 5;
                else if (kills >= parKills * 0.8f)
                    logicLvl += 4;
                else if (kills >= parKills * 0.6f)
                    logicLvl += 3;
                else if (kills >= parKills * 0.4f)
                    logicLvl += 2;
                else if (kills >= parKills * 0.2f)
                    logicLvl += 1;
                Debug.Log(logicLvl + " (memory)");

                int dmg = SequenceManager.Instance.levelDmg;
                if (dmg == 0)
                    memoryLvl += 3;
                else if (dmg == 1)
                    memoryLvl += 2;
                else if (dmg <= 3)
                    memoryLvl += 1;
                Debug.Log(memoryLvl + " (logic)");
            }

            //transition to next level
            AudioManager.Instance.Play("Elevator Down");
            foreach (Transform child in enemyParent)
                Destroy(child.gameObject);
            if (levelNum == 7)
                StartCoroutine(AudioManager.Instance.Area2());
            else if (levelNum == 13)
                StartCoroutine(AudioManager.Instance.Area3());

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
        }
        else
            yield return null;

        areaText.text = nextArea;
        SceneManager.LoadScene("Level " + levelNum);
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
        Fader.Instance.FadeInOut(0.3f, 0.3f);
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(scene);
    }



    public IEnumerator GameOver()
    {
        DialogueManager.Instance.StopCoroutines();
        pauseGame = true;

        if (SceneManager.GetActiveScene().name.Contains("Final"))
        {
            StartCoroutine(FinalNoDeath());
            yield break;
        }

        StartCoroutine(AudioManager.Instance.FadeOutAll(0));
        AudioManager.Instance.Play("Static");
        AudioManager.Instance.Play("Game Over");
        Camera.main.GetComponent<GlitchManager>().ShowGlitch(2, 1);
        DialogueManager.Instance.PlayByID("OnDeath", true, true);

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

        if (SceneManager.GetActiveScene().name == "Level 16")
        {
            StartCoroutine(StartFinal());
            yield break;
        }

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

    private IEnumerator StartFinal()
    {
        SceneManager.LoadScene("Final 1");

        TMPro.TextMeshProUGUI txt = gameOver.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        string message = "Progr";
        foreach (char c in message)
        {
            txt.text += c;
            if (c == ' ')
                yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < 3; i++)
        {
            txt.text = "Progr-";
            yield return new WaitForSeconds(0.5f);
            txt.text = "Progr";
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < 5; i++)
        {
            gameOver.SetActive(false);
            yield return new WaitForSeconds(Random.Range(0f, 0.05f));
            gameOver.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0f, 0.4f));
        }
        yield return new WaitForSeconds(1f);
        txt.text = "";
        yield return new WaitForSeconds(2f);
        message = "Reboot Successful";
        foreach (char c in message)
        {
            txt.text += c;
            if (c == ' ')
                yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
        }

        areaText.text = "????";
        enemyTimer.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);
        AudioManager.Instance.Play("Area Final");
        StartCoroutine(AudioManager.Instance.StartFade("Area Final", 1, 0.25f));
        player.GetComponent<PlayerMovement>().TakeDamage(-20);
        player.GetComponent<PlayerPrograms>().ResetCds();

        float elapsed = 0;
        while (elapsed < 5f)
        {
            gameOver.GetComponent<CanvasGroup>().alpha = 1 - elapsed / 5f;
            yield return null;
            elapsed += Time.deltaTime;
        }
        DialogueManager.Instance.PlayByID("Final_Intro");
        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);

        enemyPrefabs = new string[] { "Swarm", "Tank", "Artillery", "Aggro", "Evasive", "Landmine", "Charge", "Scatter" };
        enemyTimer.gameObject.SetActive(true);
        pauseGame = false;
    }

    private IEnumerator FinalNoDeath()
    {
        DialogueManager.Instance.PlayByID("OnDeath", true);
        Camera.main.GetComponent<GlitchManager>().ShowGlitch(2, 1);
        AudioManager.Instance.Play("Static");
        AudioManager.Instance.Play("Game Over");
        //StartCoroutine(AudioManager.Instance.StartFade("Area Final", 0.5f, 0.05f));

        //TODO: computer anim providing shield & rebooting Reya
        yield return new WaitForSeconds(0.5f);
        player.GetComponent<PlayerMovement>().shield.SetActive(true);

        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);
        player.GetComponent<PlayerMovement>().TakeDamage(-20);
        pauseGame = false;
        GameObject vfx = Instantiate(finalVFX, player.position + new Vector3(0, -2.5f, 0), Quaternion.identity);
        vfx.transform.GetChild(0).GetComponent<Animator>().Play("MemoryBurst");
        player.GetComponent<PlayerMovement>().shield.SetActive(false);

        //TODO: play SFX?

        //knock back nearby enemies
        Collider[] enemies = Physics.OverlapSphere(new Vector3(player.position.x, 1f, player.position.z), 10f, LayerMask.GetMask("Enemy"));
        foreach (Collider enemy in enemies)
        {
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (enemy.transform.position - player.position).normalized;
                direction.y = 0.2f;
                float distance = Vector3.Distance(new Vector3(player.position.x, 0, player.position.z), new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z));
                float forceMagnitude = Mathf.Lerp(3000f, 500f, distance / 10f); // Stronger force if closer
                rb.GetComponent<Enemy>().stunTimer = 1f;
                rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);
            }
        }

        AudioManager.Instance.Stop("Game Over");
        //StartCoroutine(AudioManager.Instance.StartFade("Area Final", 0.05f, 0.25f));
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
            SequenceManager.Instance.lastLevelReached = int.Parse(SceneManager.GetActiveScene().name.Substring(6));
        else
            SequenceManager.Instance.lastLevelReached = 7;

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



    public IEnumerator Ending()
    {
        pauseGame = true;
        PlayerMovement p = player.GetComponent<PlayerMovement>();
        Transform accessPt = GameObject.Find("Access Point").transform;

        DialogueManager.Instance.PlayByID("M_Approach");

        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);
        pauseGame = false;
    }

    public IEnumerator LastAccessPt()
    {
        pauseGame = true;
        spawningEnemies = false;
        foreach (Transform child in enemyParent)
            Destroy(child.gameObject);

        RewardManager.Instance.Reward(3); //TODO: make sure compile button doesn't work
        yield return new WaitForSeconds(3f);

        //show program creation UI glitching in and out
        GameObject canvas = GameObject.Find("Canvas");
        for (int i = 0; i < 5; i++)
        {
            canvas.SetActive(false);
            yield return new WaitForSeconds(Random.Range(0f, 0.05f));
            canvas.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0f, 0.4f));
        }

        DialogueManager.Instance.PlayByID("Final_Ending"); //TODO: troubleshoot only 1st line of dialogue playing
        yield return new WaitForSeconds(2f);

        //more glitches
        /*for (int i = 0; i < 10; i++)
        {
            canvas.SetActive(false);
            yield return new WaitForSeconds(Random.Range(0f, 0.05f));
            canvas.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0f, 0.4f));
        }*/
        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);

        //ending sequence -> credits
    }
}
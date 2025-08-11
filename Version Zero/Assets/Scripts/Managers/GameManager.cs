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
    [SerializeField] private GameObject finalVFX;
    private GameObject canvas;
    private Transform player;

    [Header("Debug")]
    [SerializeField] private GameObject debugSphereGreen;
    [SerializeField] private GameObject debugSphereRed;



    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int sceneNum = 0;
        int.TryParse(SceneManager.GetActiveScene().name.Substring(6), out sceneNum);

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

            //set enemies available by level
            enemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
            if (sceneNum == 4)
                enemyPrefabs = new string[] { "Swarm", "Tank", "Artillery" };
            else if (sceneNum == 7)
                enemyPrefabs = new string[] { "Aggro" };
            else if (sceneNum == 8)
                enemyPrefabs = new string[] { "Evasive" };
            else if (sceneNum == 9)
                enemyPrefabs = new string[] { "Aggro", "Evasive" };
            else if (sceneNum == 13)
                enemyPrefabs = new string[] { "Landmine", "Charge" };
            else if (sceneNum == 14)
                enemyPrefabs = new string[] { "Landmine", "Charge", "Scatter" };

            //set spawn delay
            if (scene.name.Contains("Final"))
            {
                minSpawn = 1;
                maxSpawn = 3;
            }
            else
            {
                minSpawn = spawnDelays[sceneNum].x;
                maxSpawn = spawnDelays[sceneNum].y;
            }

            //replace enemies with chosen type
            if (sceneNum != 6 && sceneNum != 12)
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
            if (minSpawn > 0 && (sceneNum > 3 || (sceneNum == 3 && runNum > 1) || scene.name.Contains("Final")) && !noSpawn)
            {
                enemyTimer.gameObject.SetActive(true);
                player.GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
                spawningEnemies = true;
                spawnTimer = Random.Range(minSpawn / 2f, maxSpawn / 2f) + spawnDelays[sceneNum].z;
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
                                Instantiate(debugSphereRed, player.position + offset, Quaternion.identity, transform);
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
                                Instantiate(debugSphereGreen, player.position + offset, Quaternion.identity, transform);
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
        int levelNum = int.Parse(SceneManager.GetActiveScene().name.Substring(6)) + 1;

        if (!skip)
        {
            AudioManager.Instance.Play("Elevator Down");
            foreach (Transform child in enemyParent)
                Destroy(child.gameObject);
            if (levelNum == 7)
                StartCoroutine(AudioManager.Instance.Area2());
            else if (levelNum == 10)
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
    
        /*SequenceManager.Instance.health = player.GetComponent<PlayerMovement>().health;
        Destroy(player.gameObject);
        SceneManager.LoadScene("End Screen");*/
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
        //TODO: computer saves from death during Final area
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

        yield return new WaitForSeconds(1f);
        AudioManager.Instance.Play("Area Final");
        StartCoroutine(AudioManager.Instance.StartFade("Area Final", 1, 0.25f));
        player.GetComponent<PlayerMovement>().TakeDamage(-20);
        //TODO: reset player program cds

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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using Newtonsoft.Json;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Dialogue")]
    public GameObject dialogue;
    [SerializeField] private GameObject[] portraits;
    [SerializeField] private float typeSpeed;

    private int terminalNum;
    private int lvlNum;
    private List<Dictionary<string, Dictionary<string, string>>> dialogueBank = new List<Dictionary<string, Dictionary<string, string>>>();

    [Header("First Access Pt")]
    [SerializeField] private Transform buildSelect;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI completeTxt;

    [Header("Coroutines")]
    public IEnumerator playMultipleCor;
    public IEnumerator playCor;

    [Header("Misc")]
    private bool skip;
    [SerializeField] private TextMeshProUGUI areaIntroText;


    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadFromJson();
        terminalNum = 0;
        if (scene.name.Contains("Final"))
            lvlNum = 17;
        else
            int.TryParse(scene.name.Substring(6), out lvlNum);
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && (GameManager.Instance.pauseGame || GameManager.Instance.loadingLevel))
        {
            skip = true;
        }
    }


    //////////////////////////////////
    ///////// PLAY DIALOGUE //////////
    //////////////////////////////////

    public void PlayMultiple(string[] lines)
    {
        StopCoroutines();
        playMultipleCor = PlayMultipleDialogues(lines);
        StartCoroutine(playMultipleCor);
    }

    public IEnumerator PlayMultipleDialogues(string[] lines)
    {
        foreach (string s in lines)
        {
            if (playCor != null)
                StopCoroutine(playCor);
            playCor = PlayDialogue(s, 2f);
            yield return playCor;
        }
    }

    public IEnumerator PlayDialogue(string line, float waitTime)
    {
        line = ShowPortraits(line);
        skip = false;

        //type out dialogue
        TextMeshProUGUI txt = dialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        txt.text = "";
        txt.fontSize = (line.Length > 100) ? 32 : 35;
        
        dialogue.SetActive(true);
        bool addingHTML = false;
        string html = "";
        foreach (char c in line)
        {
            if (c == '<')
            {
                addingHTML = true;
                html = "<";
            }
            else if (c == '>')
            {
                addingHTML = false;
                txt.text += html + ">";
            }
            else if (addingHTML)
                html += c;
            else if (c == '`')
            {
                if (!skip)
                    yield return new WaitForSeconds(0.15f * typeSpeed);
            }
            else if (c != '~')
            {
                txt.text += c;
                if (!skip)
                {
                    if (c == '.' || c == ',')
                        yield return new WaitForSeconds(0.15f * typeSpeed);
                    else if (c == ' ')
                        yield return new WaitForSeconds(0.08f * typeSpeed);
                    else
                        yield return new WaitForSeconds(0.04f * typeSpeed);
                }
            }
        }
        if (line[line.Length - 1] == '—')
            waitTime *= 0.5f;

        skip = false;
        while (!skip && waitTime > 0)
        {
            waitTime -= Time.deltaTime;
            yield return null;
        }
        dialogue.SetActive(false);
        txt.text = "";
    }

    private string ShowPortraits(string line)
    {
        bool final = SceneManager.GetActiveScene().name.Contains("Final");
        portraits[0].SetActive(line[0] != '~' && line[0] != '!' && line[0] != '@'); //reya

        portraits[1].SetActive(line[0] == '~' && !final); //computer
        portraits[2].SetActive((line[0] == '~' && final) || line[0] == '@'); //computer_final

        portraits[3].SetActive(line[0] == '!' && lvlNum == 6 && GameManager.Instance.enemyType == "Logic"); //gardener_logic
        portraits[4].SetActive(line[0] == '!' && lvlNum == 6 && GameManager.Instance.enemyType == "Instinct"); //gardener_instinct
        portraits[5].SetActive(line[0] == '!' && lvlNum == 6 && GameManager.Instance.enemyType == "Memory"); //gardener_memory

        portraits[6].SetActive(line[0] == '!' && lvlNum == 12 && GameManager.Instance.enemyType == "Logic"); //hunter_logic
        portraits[7].SetActive(line[0] == '!' && lvlNum == 12 && GameManager.Instance.enemyType == "Instinct"); //hunter_instinct
        portraits[8].SetActive(line[0] == '!' && lvlNum == 12 && GameManager.Instance.enemyType == "Memory"); //hunter_memory

        if (line[0] == '~' || line[0] == '!' || line[0] == '@')
            line = line.Substring(1);
        return line;
    }

    public void StopCoroutines()
    {
        if (playCor != null)
            StopCoroutine(playCor);
        if (playMultipleCor != null)
            StopCoroutine(playMultipleCor);
        //StopAllCoroutines();
        dialogue.SetActive(false);
    }



    //////////////////////////////////
    ///////// LOAD FROM BANK /////////
    //////////////////////////////////

    public string[] PlayByID(string ID, bool play = true, bool persistent = false)
    {
        if (persistent)
            lvlNum = 0;

        string runNum = "1";
        if (ID == "ordered")
        {
            terminalNum++;
            ID = "Pt " + terminalNum;
            if (ID == "Pt 1")
                SequenceManager.Instance.timesPlayed[lvlNum][ID]++;
            runNum = "" + SequenceManager.Instance.timesPlayed[lvlNum]["Pt 1"];
        }
        else
        {
            runNum = "" + (++SequenceManager.Instance.timesPlayed[lvlNum][ID]);
        }

        if (dialogueBank.Count > lvlNum && dialogueBank[lvlNum].ContainsKey(ID))
        {
            int greatestInt = -1;
            foreach (var value in dialogueBank[lvlNum][ID].Values)
            {
                if (int.TryParse(value, out int parsedInt))
                {
                    int runInt = int.Parse(runNum);
                    if (parsedInt <= runInt && parsedInt > greatestInt)
                    {
                        greatestInt = parsedInt;
                    }
                }
            }

            List<string> lines = new List<string>();
            bool correctPart = false;
            foreach (var kvp in dialogueBank[lvlNum][ID])
            {
                if (kvp.Value == "" && correctPart)
                    break;

                if (correctPart)
                    lines.Add(kvp.Value);

                if (kvp.Value == "" + greatestInt)
                    correctPart = true;
            }
            if (play)
                PlayMultiple(lines.ToArray());
            return lines.ToArray();
        }
        else
        {
            Debug.LogWarning($"No dialogue found for level {lvlNum} and run {runNum}");
            return null;
        }
    }

    private void LoadFromJson()
    {
        TextAsset[] files = Resources.LoadAll<TextAsset>("Dialogue");
        foreach (TextAsset file in files)
        {
            var dict = ParseJsonToDictionary(file.text);
            foreach (var outerKey in dict.Keys)
            {
                var innerDict = dict[outerKey];
                var keysToUpdate = new List<string>(innerDict.Keys);
                foreach (var innerKey in keysToUpdate)
                {
                    if (innerDict[innerKey] != null && innerDict[innerKey].Contains("--"))
                    {
                        innerDict[innerKey] = innerDict[innerKey].Replace("--", "—");
                    }
                }
            }
            dialogueBank.Add(dict);

            var counts = new Dictionary<string, int>();
            foreach (var key in dict.Keys)
            {
                counts[key] = 0;
            }
            SequenceManager.Instance.timesPlayed.Add(counts);
        }
    }

    private Dictionary<string, Dictionary<string, string>> ParseJsonToDictionary(string jsonString)
    {
        try
        {
            var parsedData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonString);
            return parsedData;
        }
        catch (JsonException ex)
        {
            Debug.LogError("Error parsing JSON: " + ex.Message);
            return null;
        }
    }



    //////////////////////////////////
    //////// SPECIFIC CUTSCENES //////
    //////////////////////////////////

    public IEnumerator IntroDialogue()
    {
        GameManager.Instance.pauseGame = true;
        dialogue.SetActive(true);
        TextMeshProUGUI txt = dialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        string[] dialogueToPlay = PlayByID("Intro", false);
        if (!SequenceManager.Instance.skipIntro && dialogueToPlay.Length > 0)
        {
            int fadeInIndex = (SequenceManager.Instance.timesPlayed[lvlNum]["Intro"] == 1) ? 13 : 6;
            StartCoroutine(AudioManager.Instance.StartFade("Startup UI", 4f, 0f));
            for (int i = 0; i < dialogueToPlay.Length; i++)
            {
                skip = false;
                dialogueToPlay[i] = ShowPortraits(dialogueToPlay[i]);
                float slowDown = (i < 1) ? 1.5f : 1f;
                txt.text = "";
                foreach (char c in dialogueToPlay[i])
                {
                    if (c == '`')
                    {
                        if (!skip)
                            yield return new WaitForSeconds(0.15f);
                    }
                    else if (c != '~')
                    {
                        txt.text += c;
                        if (!skip)
                        {
                            if (c == '.' || c == ',')
                                yield return new WaitForSeconds(0.10f * typeSpeed);
                            else if (c == ' ')
                                yield return new WaitForSeconds(0.10f * typeSpeed);
                            else
                                yield return new WaitForSeconds(0.05f * typeSpeed);
                        }
                    }
                }
                if (i == fadeInIndex)
                {
                    float fadeTime = (skip) ? 6 : 10;
                    Fader.Instance.FadeOut(fadeTime);
                    AudioManager.Instance.Play("Area 1");
                    StartCoroutine(AudioManager.Instance.StartFade("Area 1", 5f, 0.25f));
                }
                else if (i == dialogueToPlay.Length - 1)
                {
                    GameManager.Instance.pauseGame = false;
                }

                skip = false;
                float waitTimer = (dialogueToPlay[i][dialogueToPlay[i].Length - 1] == '—') ? 1 : 2;
                while (!skip && waitTimer > 0)
                {
                    waitTimer -= Time.deltaTime;
                    yield return null;
                }
            }
            dialogue.SetActive(false);
        }
        else
        {
            dialogue.SetActive(false);
            StartCoroutine(AudioManager.Instance.StartFade("Startup UI", 1f, 0f));
            yield return new WaitForSeconds(1f);
            AudioManager.Instance.Play("Area 1");
            StartCoroutine(AudioManager.Instance.StartFade("Area 1", 2f, 0.2f));
            Fader.Instance.FadeOut(0.5f);
            yield return new WaitForSeconds(0.5f);
            GameManager.Instance.pauseGame = false;
        }

        //show area intro text
        float waitTime = 0.1f;
        foreach (char ch in "Abandoned Rooftop, Hightower District\n(Digital Space)")
        {
            if (ch == '(')
                yield return new WaitForSeconds(1);
            areaIntroText.text += ch;
            yield return new WaitForSeconds(waitTime);
            if (ch == 'p')
            {
                yield return new WaitForSeconds(0.3f);
                waitTime = 0.05f;
            }
        }
        yield return new WaitForSeconds(1);
        Color col = areaIntroText.color;
        for (float i = 1; i > 0; i -= 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            areaIntroText.color = new Color(col.r, col.g, col.b, i);
        }
        Destroy(areaIntroText.gameObject);
    }


    public IEnumerator FirstAccessPt()
    {
        GameManager.Instance.playerPaused = true;
        if (SequenceManager.Instance.runNum == 1)
        {
            GameObject.Find("Terminal A").GetComponent<Terminal>().directionsText.gameObject.SetActive(false);
            buildSelect.GetChild(2).gameObject.SetActive(true);
            ProgramManager.Instance.programUI.gameObject.SetActive(true);
            StopCoroutines();
            yield return new WaitForSeconds(4f);
            string[] firstAccessPt = PlayByID("Access_Intro", false);
            for (int i = 0; i < firstAccessPt.Length; i++)
            {
                yield return PlayDialogue(firstAccessPt[i], 1f);
                if (i == 1)
                {
                    buildSelect.GetChild(2).gameObject.SetActive(false);
                    buildSelect.GetChild(1).gameObject.SetActive(true);
                    StartCoroutine(ProgressBar());
                    yield return new WaitForSeconds(3.5f);
                }
            }
            yield return new WaitForSeconds(3);
            PlayByID("Access_A");
        }
        else
        {
            buildSelect.GetChild(0).gameObject.SetActive(true);
            ProgramManager.Instance.programUI.gameObject.SetActive(true);
            //yield return new WaitForSeconds(1);
            //StartCoroutine(PlayMultipleDialogues(firstAccessPt));
        }

        yield return new WaitUntil(() => GameObject.Find("Player").transform.position.z < 0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(GameManager.Instance.SpawnEnemies(1, new Vector3(40, 0, -5)));

        yield return new WaitForSeconds(1.2f);
        PlayByID("First_Enemy");
        GameObject.Find("Player").GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
    }

    private IEnumerator ProgressBar()
    {
        completeTxt.text = "Restarting... please wait";
        progressBar.fillAmount = 0;
        float elapsed = 0;
        while (elapsed < 15)
        {
            progressBar.fillAmount = Mathf.Min(elapsed / 25, progressBar.fillAmount + (Random.Range(0.01f, 0.2f) / 25));
            float randomWait = Random.Range(0.01f, 0.2f);
            elapsed += randomWait;
            yield return new WaitForSeconds(randomWait);
        }
        progressBar.fillAmount = 1;
        completeTxt.text = "Restart complete!";
        AudioManager.Instance.Play("Terminal Activate");
        yield return new WaitForSeconds(2);
        buildSelect.GetChild(0).gameObject.SetActive(true);
        for (float i = 2; i > 0; i -= 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            buildSelect.GetChild(1).GetComponent<CanvasGroup>().alpha = i / 2f;
        }
        buildSelect.GetChild(1).gameObject.SetActive(false);
    }
}
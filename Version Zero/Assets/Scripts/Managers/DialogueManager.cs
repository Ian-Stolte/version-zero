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
    [TextArea(3, 5)] [SerializeField] private string[] introDialogue;
    [TextArea(3, 5)] [SerializeField] private string[] introDialogue2;
    [SerializeField] private GameObject dialogue;
    [SerializeField] private GameObject[] portraits;
    [SerializeField] private float typeSpeed;

    [Header("Terminals")]
    private int terminalNum;
    private int lvlNum;
    private List<Dictionary<string, Dictionary<string, string>>> terminalDialogue = new List<Dictionary<string, Dictionary<string, string>>>();
    private List<Dictionary<string, int>> terminalCounts = new List<Dictionary<string, int>>();

    [Header("First Access Pt")]
    [TextArea(3, 5)] [SerializeField] private string[] firstAccessPt;
    [SerializeField] private string[] firstEnemy;
    [SerializeField] private Transform buildSelect;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI completeTxt;

    [Header("Coroutines")]
    public IEnumerator playMultipleCor;
    public IEnumerator playCor;

    [Header("Misc")]
    private bool skip;
    [SerializeField] private TextMeshProUGUI areaIntroText;
    [TextArea(3, 5)] [SerializeField] private string[] gardenerDialogue;

    
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        terminalNum = 0;
        lvlNum = int.Parse(scene.name.Substring(6));
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            skip = true;
        }
    }


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

    public IEnumerator PlayDialogue(string line, float waitTime, bool canSkip=false)
    {
        //set up portraits
        line = ShowPortraits(line);

        //type out dialogue
        TextMeshProUGUI txt = dialogue.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        txt.text = "";
        dialogue.SetActive(true);
        bool addingHTML = false;
        string html = "";
        foreach (char c in line)
        {
            if (c=='<')
            {
                addingHTML = true;
                html = "<";
            }
            else if (c=='>')
            {
                addingHTML = false;
                txt.text += html+">";
            }
            else if (addingHTML)
                html += c;
            else if (c=='*')
                yield return new WaitForSeconds(0.15f * typeSpeed);
            else if (c != '~')
            {
                txt.text += c;
                if (c=='.' || c==',')
                    yield return new WaitForSeconds(0.15f * typeSpeed);
                else if (c==' ')
                    yield return new WaitForSeconds(0.08f * typeSpeed);
                else
                    yield return new WaitForSeconds(0.04f * typeSpeed);
            }
        }
        if (line[line.Length-1] == '—')
            waitTime *= 0.5f;
        yield return new WaitForSeconds(waitTime);
        dialogue.SetActive(false);
        txt.text = "";
    }

    private string ShowPortraits(string line)
    {
        portraits[0].SetActive(line[0] != '~' && line[0] != '!'); //reya
        portraits[1].SetActive(line[0] == '~'); //computer
        portraits[2].SetActive(line[0] == '!'); //gardener
        if (line[0] == '~' || line[0] == '!')
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
    /////////// TERMINALS ////////////
    //////////////////////////////////


    public void PlayTerminal(string ID)
    {   
        string runNum = "";
        if (ID == "ordered")
        {
            terminalNum++;
            ID = "Pt " + terminalNum;
            if (ID == "Pt 1")
                terminalCounts[lvlNum - 1][ID]++;
            runNum = "" + terminalCounts[lvlNum - 1]["Pt 1"];
        }
        else
        {
            runNum = "" + (++terminalCounts[lvlNum - 1][ID]);
        }

        if (terminalDialogue.Count > lvlNum - 1 && terminalDialogue[lvlNum - 1].ContainsKey(ID))
        {
            int greatestInt = -1;
            foreach (var value in terminalDialogue[lvlNum - 1][ID].Values)
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
            foreach (var kvp in terminalDialogue[lvlNum - 1][ID])
            {
                if (kvp.Value == "" && correctPart)
                    break;

                if (correctPart)
                    lines.Add(kvp.Value);

                if (kvp.Value == ""+greatestInt)
                    correctPart = true;
            }
            PlayMultiple(lines.ToArray());
        }
        else
        {
            Debug.LogWarning($"No dialogue found for level {lvlNum} and run {runNum}");
        }
    }

    private void Start()
    {
        LoadFromJson();
    }

    private void LoadFromJson()
    {
        string path = "Assets/Resources/Dialogue";
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                if (!file.Contains(".meta"))
                {
                    string relativePath = Path.GetRelativePath(Application.dataPath, file).Substring("Resources".Length+1);
                    relativePath = Path.ChangeExtension(relativePath, null);
                    var res = Resources.Load<TextAsset>(relativePath).text;
                    var dict = ParseJsonToDictionary(res);
                    terminalDialogue.Add(dict);
                    var counts = new Dictionary<string, int>();
                    foreach (var key in dict.Keys)
                    {
                        counts[key] = 0;
                    }
                    terminalCounts.Add(counts);
                }
            }
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
        string[] dialogueToPlay = (SequenceManager.Instance.runNum == 1) ? introDialogue : introDialogue2;
        if (!GameManager.Instance.skipDialogue)
        {
            for (int i = 0; i < dialogueToPlay.Length; i++)
            {
                skip = false;
                dialogueToPlay[i] = ShowPortraits(dialogueToPlay[i]);
                float slowDown = (i < 1) ? 1.5f : 1f;
                txt.text = "";
                foreach (char c in dialogueToPlay[i])
                {
                    if (c == '*')
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
                if (i == dialogueToPlay.Length - 2)
                {
                    float fadeTime = (skip) ? 6 : 10;
                    Fader.Instance.FadeOut(fadeTime);
                    AudioManager.Instance.Play("Area 1");
                    StartCoroutine(AudioManager.Instance.StartFade("Area 1", 0.5f, 0.25f));
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
            AudioManager.Instance.Play("Area 1");
            StartCoroutine(AudioManager.Instance.StartFade("Area 1", 0.5f, 0.2f));
            Fader.Instance.FadeOut(0.5f);
            yield return new WaitForSeconds(0.5f);
            GameManager.Instance.pauseGame = false;
        }
        
        //show area intro text
        float waitTime = 0.1f;
        foreach (char ch in "Abandoned Rooftop, Hightower District\n(Virtual Reality)")
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


    public IEnumerator FirstAccessPt(string[] dialogue)
    {
        GameManager.Instance.playerPaused = true;
        if (SequenceManager.Instance.runNum == 1)
        {
            buildSelect.GetChild(2).gameObject.SetActive(true);
            ProgramManager.Instance.programUI.gameObject.SetActive(true);
            StopCoroutines();
            yield return new WaitForSeconds(1.5f);
            for (int i = 0; i < dialogue.Length; i++)
            {
                yield return PlayDialogue(dialogue[i], 1f);
                if (i == 1)
                {
                    buildSelect.GetChild(2).gameObject.SetActive(false);
                    buildSelect.GetChild(1).gameObject.SetActive(true);
                    StartCoroutine(ProgressBar());
                    yield return new WaitForSeconds(3);
                }
                if (i == 5)
                    yield return new WaitForSeconds(1);
            }
            yield return new WaitForSeconds(3);
            PlayMultiple(firstAccessPt);
        }
        else
        {
            buildSelect.GetChild(0).gameObject.SetActive(true);
            ProgramManager.Instance.programUI.gameObject.SetActive(true);
            //yield return new WaitForSeconds(1);
            //StartCoroutine(PlayMultipleDialogues(firstAccessPt));
        }
        yield return new WaitUntil(() => !GameManager.Instance.playerPaused);
        GameManager.Instance.UnlockBarrier(GameObject.Find("Barrier").transform);
        GameManager.Instance.FinishTerminalIcon();

        yield return new WaitUntil(() => GameObject.Find("Player").transform.position.z < 0);
        yield return new WaitForSeconds(1);
        StartCoroutine(GameManager.Instance.WaveEnemies(1, new Vector3(40, 0, -5)));

        yield return new WaitForSeconds(1.2f);
        StartCoroutine(PlayMultipleDialogues(firstEnemy));
        GameObject.Find("Player").GetComponent<PlayerMovement>().hpBar.gameObject.SetActive(true);
    }

    private IEnumerator ProgressBar()
    {
        completeTxt.text = "Restarting... please wait";
        progressBar.fillAmount = 0;
        float elapsed = 0;
        while (elapsed < 15)
        {
            progressBar.fillAmount = Mathf.Min(elapsed/25, progressBar.fillAmount + (Random.Range(0.01f, 0.2f)/25));
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
            buildSelect.GetChild(1).GetComponent<CanvasGroup>().alpha = i/2f;
        }
        buildSelect.GetChild(1).gameObject.SetActive(false);
    }


    public IEnumerator GardenerDialogue()
    {
        StopCoroutines();
        GameManager.Instance.pauseGame = true;
        dialogue.SetActive(true);
        for (int i = 0; i < gardenerDialogue.Length-1; i++)
        {
            yield return PlayDialogue(gardenerDialogue[i], 2f);
        }
        StartCoroutine(PlayDialogue(gardenerDialogue[gardenerDialogue.Length-1], 2f));
        yield return new WaitForSeconds(3);
        dialogue.SetActive(false);
        GameManager.Instance.pauseGame = false;
    }
}
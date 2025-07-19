using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgramManager : MonoBehaviour
{
    public static ProgramManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Bools")]
    public bool SKIP_CRAFTING;
    [SerializeField] private bool showTutorial;
    [HideInInspector] public bool spellsLocked;
    private bool musicOn;
    private bool moreInfo;

    [Header("Parents")]
    public Transform programUI;
    [SerializeField] private Transform blockParent;
    [SerializeField] private Transform keybindUI;
    [SerializeField] private Transform keybindIcons;
    [SerializeField] private Transform keybindSlots;
    [SerializeField] private Transform programList;
    [SerializeField] private Transform cdParent;

    [Header("Buttons")]
    public GameObject compileButton;
    [SerializeField] private TextMeshProUGUI infoButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject confirmButton;
    [SerializeField] private GameObject randomButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject emptyImage;
    [SerializeField] private GameObject spellListItem;
    [SerializeField] private GameObject cdIconPrefab;

    [Header("Colors")]
    [SerializeField] private Color fullSymbolColor;
    [SerializeField] private Color invalidColor;
    [SerializeField] private Color[] typeColors;

    [Header("Misc")]
    [SerializeField] private PlayerPrograms player;
    [SerializeField] private GameObject[] tutorials;
    public GameObject buildSelect;

    [Header("Upgrade")]
    private bool upgradeShown;
    [SerializeField] private GameObject upgradeTutorial;

    [Header("Keybinds")]
    public KeyCode[] defaultBinds;
    public string[] bindTxt;
    public List<KeyStringPair> keyStringPairs;
    [HideInInspector] public Dictionary<KeyCode, string> keybindStrMap = new Dictionary<KeyCode, string>();

    [Header("Program Data")]
    public string buildpath;
    //TODO: read directly from prefab folders?
    public List<GameObject> shapeBlocks;
    public List<GameObject> effectBlocks;
    public List<GameObject> modBlocks;
    private List<GameObject> blocks = new List<GameObject>();
    public List<Program> programs = new List<Program>();


    public void Start()
    {
        foreach (var pair in keyStringPairs)
        {
            keybindStrMap[pair.key] = pair.value;
        }
        if (GameObject.Find("Options Manager") == null)
            StartingHand();
    }

    public void StartingHand()
    {
        buildpath = "logic";
        foreach (GameObject g in shapeBlocks)
            blocks.Add(g);
        foreach (GameObject g in effectBlocks)
            blocks.Add(g);
        foreach (GameObject g in modBlocks)
            blocks.Add(g);
        player = GameObject.Find("Player").GetComponent<PlayerPrograms>();
        if (SKIP_CRAFTING)
        {
            programUI.gameObject.SetActive(true);

            string[] startingBlocks = new string[] { "Line", "Damage", "Circle", "Displace", "Pulse", "Pause", "Damage" };
            foreach (string s in startingBlocks)
            {
                GameObject prefab = blocks.Find(b => b.name == s);
                if (prefab == null)
                    Debug.LogError("Starting block prefab not found!");
                else
                    CreateBlock(prefab);
            }
            List<Block> lineStun = new List<Block>();
            lineStun.Add(GameObject.Find("Line").GetComponent<Block>());
            lineStun.Add(GameObject.Find("Damage").GetComponent<Block>());
            Program lineStunSpell = new Program(lineStun, KeyCode.Mouse0);
            programs.Add(lineStunSpell);
            List<Block> circleDisplace = new List<Block>();
            circleDisplace.Add(GameObject.Find("Circle").GetComponent<Block>());
            circleDisplace.Add(GameObject.Find("Displace").GetComponent<Block>());
            Program circleDisplaceSpell = new Program(circleDisplace, KeyCode.Mouse1);
            programs.Add(circleDisplaceSpell);
            List<Block> meleeUlt = new List<Block>();
            meleeUlt.Add(GameObject.Find("Pulse").GetComponent<Block>());
            meleeUlt.Add(GameObject.Find("Pause").GetComponent<Block>());
            meleeUlt.Add(GameObject.Find("Damage").GetComponent<Block>());
            Program meleeUltSpell = new Program(meleeUlt, KeyCode.Mouse2);
            programs.Add(meleeUltSpell);
            ConfirmSpells();
            EnterGame();
        }
        tutorials[0].SetActive(true);
        tutorials[0].transform.GetChild(0).gameObject.SetActive(showTutorial);
    }


    public void CreateBlock(string blockName)
    {
        CreateBlock(blocks.Find(b => b.name == blockName));
    }

    public void CreateBlock(GameObject prefab)
    {
        GameObject block = Instantiate(prefab, Vector2.zero, Quaternion.identity, blockParent);
        RectTransform r = block.GetComponent<RectTransform>();
        for (int j = 0; j < 20; j++)
        {
            r.anchoredPosition = new Vector2(Random.Range(-(540 - r.sizeDelta.x), 820 - r.sizeDelta.x), Random.Range(-360, 330));
            bool noOverlap = true;
            foreach (Transform child in blockParent)
            {
                if (child.gameObject != block && child.gameObject.activeSelf && Vector2.Distance(block.GetComponent<RectTransform>().anchoredPosition, child.GetComponent<RectTransform>().anchoredPosition) < 150)
                {
                    noOverlap = false;
                }
            }
            if (noOverlap)
                break;
        }
        block.name = block.name.Substring(0, block.name.Length - 7);
    }



    public void Reforge()
    {
        GameManager.Instance.pauseGame = true;
        player.enabled = false;
        cdParent.gameObject.SetActive(false);
        programUI.gameObject.SetActive(true);
        compileButton.SetActive(true);
        infoButton.transform.parent.gameObject.SetActive(true);
        confirmButton.SetActive(false);
        randomButton.SetActive(false);
        spellsLocked = false;

        HashSet<string> blockNames = new HashSet<string>();
        foreach (Transform child in blockParent)
        {
            Block b = child.GetComponent<Block>();
            if (child.gameObject.activeSelf)
            {
                if (!blockNames.Contains(b.name))
                    blockNames.Add(b.name);
                else if (!upgradeShown) //TODO: also check that block is not at max lvl (i.e can actually be upgraded)
                {
                    upgradeShown = true;
                    upgradeTutorial.SetActive(true);
                }

                Color c = child.GetComponent<Image>().color;
                child.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 1);
                b.symbol.GetComponent<Image>().enabled = false;
                b.highlight.gameObject.SetActive(false);
                b.nameTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.gameObject.SetActive(true);
            }
        }
    }



    public void CompileSpells()
    {
        //create programs from blocks attached to keybinds
        programs.Clear();
        if (moreInfo)
            Info();
        foreach (Transform child in keybindSlots)
        {
            KeybindSlot script = child.GetComponent<KeybindSlot>();
            if (script.right != null)
            {
                GetBlockList(script.right, script.keybind);
            }
        }
        GameObject auto = GameObject.Find("Auto");
        GameObject aura = GameObject.Find("Aura");
        if (auto != null)
        {
            if (auto.GetComponent<Block>().right != null)
            {
                GetBlockList(auto.GetComponent<Block>());
            }
        }
        if (aura != null)
        {
            if (aura.GetComponent<Block>().right != null)
            {
                GetBlockList(aura.GetComponent<Block>());
            }
        }

        //hide all other blocks
        foreach (Transform child in blockParent)
        {
            Block b = child.GetComponent<Block>();
            if (!b.attached)
            {
                b.symbol.canMove = false;
                Color c = child.GetComponent<Image>().color;
                child.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 0.1f);
                b.nameTxt.GetComponent<CanvasGroup>().alpha = 0.5f;
                b.cdTxt.GetComponent<CanvasGroup>().alpha = 0.1f;
            }
        }

        if (showTutorial)
        {
            tutorials[0].SetActive(false);
            tutorials[1].SetActive(true);
        }
        infoButton.transform.parent.gameObject.SetActive(false);
        compileButton.SetActive(false);
        confirmButton.SetActive(true);
        randomButton.SetActive(true);
        backButton.SetActive(true);
        spellsLocked = true;

        foreach (Program p in programs)
        {
            foreach (Block b in p.blocks)
            {
                b.transform.GetChild(0).GetComponent<Image>().enabled = true;
                Symbol sym = b.symbol;
                sym.min = new Vector2(-80 * p.blocks.IndexOf(b) - 40, sym.min.y);
                sym.max = new Vector2(80 * (p.blocks.Count - p.blocks.IndexOf(b)) - 40, sym.max.y);
                sym.canMove = true;
            }
        }
    }

    //find all blocks attached to a keybind slot
    private void GetBlockList(Block b, KeyCode keybind = KeyCode.None)
    {
        List<Block> blockList = new List<Block>();
        while (b != null)
        {
            blockList.Add(b);

            //show symbol UI
            Color c = b.GetComponent<Image>().color;
            b.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 0.3f);
            b.cdTxt.SetActive(false);

            b = b.right;
        }
        programs.Add(new Program(blockList, keybind));
    }


    public void UndoSpells()
    {
        foreach (Transform child in blockParent)
        {
            if (child.gameObject.activeSelf)
            {
                Block b = child.GetComponent<Block>();
                Color c = child.GetComponent<Image>().color;
                child.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 1);
                b.nameTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.SetActive(true);

                child.GetChild(0).GetComponent<Image>().enabled = false;
                b.highlight.gameObject.SetActive(false);
                b.cdTxt.gameObject.SetActive(true);
            }
        }

        if (showTutorial)
        {
            tutorials[0].SetActive(true);
            tutorials[1].SetActive(false);
        }
        compileButton.SetActive(true);
        confirmButton.SetActive(false);
        randomButton.SetActive(false);
        backButton.SetActive(false);
        spellsLocked = false;
    }



    private void Update()
    {
        if (!spellsLocked) //check valid blocks
        {
            int valid = 0;
            foreach (Transform child in keybindSlots)
            {
                KeybindSlot script = child.GetComponent<KeybindSlot>();
                valid = CheckValidBlocks(valid, script.right, child.GetComponent<Image>(), (script.keybind == KeyCode.None));
            }
            GameObject aura = GameObject.Find("Aura");
            if (aura != null)
                valid = CheckValidBlocks(valid, aura.GetComponent<Block>().right, aura.GetComponent<Image>(), false, true);
            GameObject auto = GameObject.Find("Auto");
            if (auto != null)
                valid = CheckValidBlocks(valid, auto.GetComponent<Block>().right, auto.GetComponent<Image>(), false);
            compileButton.GetComponent<Button>().interactable = (valid > 0);
        }

        else //check valid symbols
        {
            if (programs.Count == 0)
            {
                confirmButton.GetComponent<Button>().interactable = false;
            }
            else
            {
                bool readyToConfirm = true;
                foreach (Program p in programs)
                {
                    bool finished = true;
                    foreach (Block b in p.blocks)
                    {
                        if (b.symbol.adjSymbols < p.blocks.Count)
                        {
                            finished = false;
                            readyToConfirm = false;
                            break;
                        }
                    }
                    foreach (Block b in p.blocks)
                    {
                        b.highlight.gameObject.SetActive(finished);
                    }
                }
                confirmButton.GetComponent<Button>().interactable = readyToConfirm;
            }
        }

        //click to disable upgrade tutorial
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && upgradeTutorial.activeSelf)
            upgradeTutorial.SetActive(false);
    }

    private int CheckValidBlocks(int valid, Block b, Image img, bool noKeybind, bool noShape = false)
    {
        if (b == null)
        {
            img.color = new Color(1, 1, 1, 1);
            return valid;
        }

        bool shape = noShape;
        bool effect = false;
        while (b != null)
        {
            if (b.tag == "shape")
                shape = true;
            else if (b.tag == "effect")
                effect = true;
            b = b.right;
        }
        if (shape && effect && !noKeybind)
        {
            img.color = new Color(1, 1, 1, 1);
            return valid + 1;
        }
        else
        {
            img.color = invalidColor;
            return -99;
        }
    }



    public void RandomSymbols()
    {
        foreach (Transform child in blockParent)
        {
            Block b = child.GetComponent<Block>();
            Vector2 offset = new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 10f));
            b.symbol.GetComponent<RectTransform>().anchoredPosition = new Vector2((b.symbol.min.x + b.symbol.max.x) / 2f * 1.35f, (b.symbol.min.y + b.symbol.max.y) / 2f) + offset;

        }
        ConfirmSpells();
    }

    public void ConfirmSpells()
    {
        showTutorial = false;
        foreach (GameObject g in tutorials)
            g.SetActive(false);
        confirmButton.SetActive(false);
        randomButton.SetActive(false);
        backButton.SetActive(false);

        //filter out aura and auto programs
        player.auraProgram.name = "";
        player.autoProgram.name = "";
        foreach (Program p in programs)
        {
            float cd = 0;
            bool addedAuto = false;
            bool addedAura = false;
            foreach (Block b in p.blocks)
            {
                cd += b.cd;
                if (b.name == "Aura")
                {
                    player.auraProgram = p;
                    addedAura = true;
                }
                else if (b.name == "Auto")
                {
                    player.autoProgram = p;
                    addedAuto = true;
                }
            }
            if (addedAuto)
                player.autoTick = cd / 2f;
        }

        //TODO: do this better? (don't need results UI any more)
        foreach (Program p in programs)
        {
            //spawn symbol
            p.symbol = Instantiate(emptyImage, Vector2.zero, Quaternion.identity, programList);
            p.symbol.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);
            p.symbol.name = p.name + " Symbol";
            Vector2 totalPos = Vector2.zero;
            string programName = "";
            float cd = 0;
            foreach (Block b in p.blocks)
            {
                programName += b.nameTxt.text + " + ";
                cd += b.cd;
                Vector3 scale = new Vector3(b.symbol.transform.localScale.x * b.transform.localScale.x, b.symbol.transform.localScale.y * b.transform.localScale.y, 1);
                GameObject sym = Instantiate(b.symbol.gameObject, b.symbol.transform.position, Quaternion.identity, p.symbol.transform);
                sym.transform.localScale = scale;
                sym.GetComponent<Image>().color = fullSymbolColor;
                totalPos += sym.GetComponent<RectTransform>().anchoredPosition;
            }
            foreach (Transform child in p.symbol.transform)
            {
                child.GetComponent<RectTransform>().anchoredPosition -= totalPos / p.symbol.transform.childCount;
                Destroy(child.GetComponent<Symbol>());
                Destroy(child.GetComponent<BoxCollider2D>());
            }

            //set program values
            p.name = programName.Substring(0, programName.Length - 3);
            p.cdMax = cd;
        }
        if (player.auraProgram.name != "")
            programs.Remove(player.auraProgram);
        if (player.autoProgram.name != "")
            programs.Remove(player.autoProgram);
        StartCoroutine(EnterGame());
    }

    public IEnumerator EnterGame()
    {
        AudioManager.Instance.Play("Enter Game");
        foreach (Transform child in cdParent)
        {
            Destroy(child.gameObject);
        }
        cdParent.gameObject.SetActive(true);

        Fader.Instance.FadeIn(0.5f);
        yield return new WaitForSeconds(0.5f);

        //create program icons
        int index = 0;
        foreach (Program p in programs)
        {
            Block shape = p.blocks.Find(b => b.tag == "shape");
            CreateProgramIcon(p, new Vector2(-800 + (170 * index), -450), p.keybindStr, shape.name);
            index++;
        }

        index = 0;
        if (player.auraProgram.name != "")
        {
            CreateProgramIcon(player.auraProgram, new Vector2(800, -450), "AURA", "");
            index++;
        }
        if (player.autoProgram.name != "")
        {
            Block shape = player.autoProgram.blocks.Find(b => b.tag == "shape");
            if (shape != null)
                CreateProgramIcon(player.autoProgram, new Vector2(800 - (170 * index), -450), "AUTO", shape.name);
        }

        programUI.gameObject.SetActive(false);
        Fader.Instance.FadeOut(0.5f);

        GameManager.Instance.pauseGame = false;
        GameManager.Instance.playerPaused = false;
        player.GetComponent<PlayerMovement>().enabled = true;
        player.enabled = true;
        player.InitializeAura();
    }

    private void CreateProgramIcon(Program p, Vector2 pos, string type, string shape)
    {
        Transform cdIcon = Instantiate(cdIconPrefab, Vector2.zero, Quaternion.identity, cdParent).transform;
        cdIcon.GetComponent<RectTransform>().anchoredPosition = pos;
        cdIcon.GetChild(0).GetComponent<TextMeshProUGUI>().text = type;
        Transform symbol = Instantiate(p.symbol, Vector2.zero, Quaternion.identity, cdIcon.GetChild(3)).transform;
        symbol.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        symbol.localScale *= 1.1f;
        symbol.SetSiblingIndex(cdIcon.childCount - 2);
        p.fillTimer = cdIcon.GetChild(cdIcon.childCount - 1).gameObject;
        if (type == "AURA")
            p.fillTimer.GetComponent<Image>().fillAmount = 1;
        cdIcon.GetChild(1).GetComponent<TextMeshProUGUI>().text = shape;
    }



    public List<Block> ChooseRandom(int n, string[] forbidden = null, string type = "none", string[] category = null)
    {
        //TODO: add diff percents? --- keep in 3 separate lists, but decrement pct of given list when chosen (e.g 40-40-20, then choose effect -> 50-25-25)
        if (forbidden == null)
            forbidden = new string[0];

        bool skipAura = false;
        bool skipAuto = false;
        foreach (Transform child in blockParent)
        {
            if (child.name == "Aura")
                skipAura = true;
            else if (child.name == "Auto")
                skipAuto = true;
        }
        List<Block> starting = new List<Block>();
        List<Block> chosen = new List<Block>();
        foreach (GameObject g in blocks)
        {
            if (!((skipAura && g.name == "Aura") || (skipAuto && g.name == "Auto")) && (type == "none" || type == g.GetComponent<Block>().type) && (category == null || category.Contains(g.GetComponent<Block>().tag)) && !forbidden.Contains(g.name))
                starting.Add(g.GetComponent<Block>());
        }

        for (int i = 0; i < n; i++)
        {
            if (starting.Count > 0)
            {
                Block b = starting[Random.Range(0, starting.Count)];
                chosen.Add(b);
                starting.Remove(b);
            }
            else
            {
                Debug.LogError("No valid reward blocks!");
                break;
            }
        }

        return chosen;
    }



    public Color ColorFromType(string type)
    {
        if (type == "logic")
            return typeColors[0];
        else if (type == "memory")
            return typeColors[1];
        else if (type == "instinct")
            return typeColors[2];
        else if (type == "perception")
            return typeColors[3];
        else
            return new Color(1, 1, 1, 1);
    }


    //
    //Button functions
    //

    public void ButtonClick()
    {
        AudioManager.Instance.Play("Button Click");
    }

    public void Info()
    {
        moreInfo = !moreInfo;
        foreach (Transform child in blockParent)
        {
            child.GetComponent<Block>().infoTxt.gameObject.SetActive(moreInfo);
            child.GetComponent<Block>().cdTxt.gameObject.SetActive(!moreInfo);
        }
        string buttonTxt = (moreInfo) ? "Less Info" : "Explain";
        infoButton.text = buttonTxt;
    }

    public void ChangeKeybind(KeybindSlot k)
    {
        StartCoroutine(ChangeKeybindCor(k));
    }

    private IEnumerator ChangeKeybindCor(KeybindSlot k)
    {
        TextMeshProUGUI txt = k.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        txt.text = "[press a key]";
        txt.fontSize = 18;

        while (true)
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(code))
                    {
                        k.keybind = code;
                        txt.text = (keybindStrMap.ContainsKey(code)) ? keybindStrMap[code] : code.ToString();
                        txt.fontSize = 26;
                        foreach (Transform child in k.transform.parent)
                        {
                            KeybindSlot script = child.GetComponent<KeybindSlot>();
                            if (script != k && script.keybind == code)
                            {
                                script.keybind = KeyCode.None;
                                script.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = "__";
                            }
                        }
                        yield break;
                    }
                }
            }
            yield return null;
        }
    }
}



[System.Serializable]
public class Program
{
    public Program(List<Block> blocks_, KeyCode bind)
    {
        blocks = blocks_;
        name = blocks_[0].name;
        keybind = bind;
        if (ProgramManager.Instance.keybindStrMap.ContainsKey(bind))
            keybindStr = ProgramManager.Instance.keybindStrMap[bind];
        else
            keybindStr = bind.ToString();
    }

    public string name;
    public List<Block> blocks;
    public float cdMax;
    public float cdTimer;
    [HideInInspector] public GameObject fillTimer;
    public KeyCode keybind;
    public string keybindStr;
    public GameObject symbol;
    public GameObject programList;
}

[System.Serializable]
public class KeyStringPair
{
    public KeyCode key;
    public string value;
}
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
    [HideInInspector] public bool spellsLocked;
    private bool musicOn;
    private bool moreInfo;

    [Header("Parents")]
    public Transform programUI;
    [SerializeField] private Transform blockParent;
    [SerializeField] private Transform keybindSlots;
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
    [SerializeField] private Color[] sectorColors;
    [SerializeField] private Color[] symbolIndicatorColors;

    [Header("Misc")]
    public GameObject buildSelect;
    [SerializeField] private GameObject symbolsTutorial;
    private bool showTutorial = true;
    private PlayerPrograms player;

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
    public List<GameObject> baseBlocks;
    public List<GameObject> effectBlocks;
    public List<GameObject> modBlocks;
    [HideInInspector] public List<GameObject> allBlocks = new List<GameObject>();
    public List<Block> blocks = new List<Block>();
    public List<Program> programs = new List<Program>();


    public void Start()
    {
        foreach (var pair in keyStringPairs)
        {
            keybindStrMap[pair.key] = pair.value;
        }
        StartingHand();
    }

    public void StartingHand()
    {
        buildpath = "logic";
        foreach (GameObject g in baseBlocks)
            allBlocks.Add(g);
        foreach (GameObject g in effectBlocks)
            allBlocks.Add(g);
        foreach (GameObject g in modBlocks)
            allBlocks.Add(g);
        player = GameObject.Find("Player").GetComponent<PlayerPrograms>();
        if (SKIP_CRAFTING)
        {
            programUI.gameObject.SetActive(true);

            string[] startingBlocks = new string[] { "Line", "Damage", "Circle", "Displace", "Pulse", "Pause", "Damage" };
            foreach (string s in startingBlocks)
            {
                GameObject prefab = allBlocks.Find(b => b.name == s);
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
            ConfirmSpells(true);
            EnterGame();
        }
    }


    public void CreateBlock(string blockName)
    {
        CreateBlock(allBlocks.Find(b => b.name == blockName));
    }

    public void CreateBlock(GameObject prefab)
    {
        GameObject block = Instantiate(prefab, Vector2.zero, Quaternion.identity, blockParent);
        RectTransform r = block.GetComponent<RectTransform>();
        for (int j = 0; j < 20; j++)
        {
            r.anchoredPosition = new Vector2(Random.Range(-(100 - r.sizeDelta.x), 820 - r.sizeDelta.x), Random.Range(-360, 330));
            bool noOverlap = true;
            foreach (Block b in blocks)
            {
                if (b.gameObject != block && b.gameObject.activeSelf && Vector2.Distance(block.GetComponent<RectTransform>().anchoredPosition, b.GetComponent<RectTransform>().anchoredPosition) < 180)
                {
                    noOverlap = false;
                }
            }
            if (noOverlap)
                break;
        }
        block.name = block.name.Substring(0, block.name.Length - 7);
        blocks.Add(block.GetComponent<Block>());

        //check if should show upgrade tutorial
        HashSet<string> blockNames = new HashSet<string>();
        foreach (Block b in blocks)
        {
            if (b.gameObject.activeSelf)
            {
                if (!blockNames.Contains(b.name))
                    blockNames.Add(b.name);
                else if (!upgradeShown) //TODO: also check that block is not at max lvl (i.e can actually be upgraded)
                {
                    upgradeShown = true;
                    upgradeTutorial.SetActive(true);
                }
            }
        }
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

        foreach (Block b in blocks)
        {
            if (b.gameObject.activeSelf)
            {
                b.nameTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.gameObject.SetActive(true);
                b.GetComponent<CanvasGroup>().alpha = 1;
                b.upgradeCircles.SetActive(true);

                b.symbol.GetComponent<Image>().enabled = false;
                b.symbolBG.SetActive(false);
                b.symbol.transform.SetParent(b.transform);
            }
        }
        foreach (Transform child in keybindSlots)
        {
            child.GetChild(4).gameObject.SetActive(false);
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
            if (script.shapeBase.target != null)
            {
                GetBlockList(script, script.keybind);
                child.GetChild(4).gameObject.SetActive(true);
            }
        }
        //TODO: check auto and aura

        //hide all other blocks
        foreach (Block b in blocks)
        {
            if (b.slot == null)
            {
                b.symbol.canMove = false;
                b.GetComponent<CanvasGroup>().alpha = 0.3f;
            }
            else
            {
                b.symbol.transform.SetParent(blockParent);
            }
            b.nameTxt.GetComponent<CanvasGroup>().alpha = 0.5f;
            b.cdTxt.GetComponent<CanvasGroup>().alpha = 0.5f;
            b.upgradeCircles.SetActive(false);
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
                b.symbol.GetComponent<Image>().enabled = true;
                Symbol s = b.symbol;
                s.min = new Vector2(-500, b.GetComponent<RectTransform>().anchoredPosition.y - 100);
                s.max = new Vector2(-500 + 210 * (p.blocks.Count), b.GetComponent<RectTransform>().anchoredPosition.y + 100);
                s.canMove = true;
                b.symbolBG.SetActive(true);
            }
        }

        if (SequenceManager.Instance.runNum == 1 && showTutorial)
            symbolsTutorial.SetActive(true);
    }

    //find all blocks attached to a keybind slot
    private void GetBlockList(KeybindSlot k, KeyCode keybind = KeyCode.None)
    {
        FunctionSlot shape = k.shapeBase;
        List<Block> blockList = new List<Block>();
        blockList.Add(shape.target);
        foreach (Transform child in shape.transform)
        {
            FunctionSlot slot = child.GetComponent<FunctionSlot>();
            if (slot != null)
            {
                if (slot.target == null)
                    break;
                else
                    blockList.Add(slot.target);
            }
        }
        programs.Add(new Program(blockList, keybind, k.transform.GetChild(4).gameObject));
    }


    public void UndoSpells()
    {
        foreach (Block b in blocks)
        {
            if (b.gameObject.activeSelf)
            {
                b.nameTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.GetComponent<CanvasGroup>().alpha = 1;
                b.cdTxt.SetActive(true);
                b.GetComponent<CanvasGroup>().alpha = 1;
                b.upgradeCircles.SetActive(true);

                b.symbol.GetComponent<Image>().enabled = false;
                b.symbolBG.SetActive(false);
                b.symbol.transform.SetParent(b.transform);
            }
        }
        foreach (Transform child in keybindSlots)
        {
            child.GetChild(4).gameObject.SetActive(false);
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
                valid = CheckValidBlocks(valid, script.shapeBase, child.GetComponent<Image>(), (script.keybind == KeyCode.None));
            }
            /*GameObject aura = GameObject.Find("Aura");
            if (aura != null)
                valid = CheckValidBlocks(valid, aura.GetComponent<Block>().right, aura.GetComponent<Image>(), false, true);
            GameObject auto = GameObject.Find("Auto");
            if (auto != null)
                valid = CheckValidBlocks(valid, auto.GetComponent<Block>().right, auto.GetComponent<Image>(), false);*/
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
                    bool valid = true;
                    foreach (Block b in p.blocks)
                    {
                        if (b.symbol.adjSymbols < p.blocks.Count)
                        {
                            readyToConfirm = false;
                            valid = false;
                            break;
                        }
                    }
                    //p.symbolIndicator.GetComponent<Image>().color = (valid) ? symbolIndicatorColors[0] : symbolIndicatorColors[1];
                    p.symbolIndicator.SetActive(valid);
                }
                confirmButton.GetComponent<Button>().interactable = readyToConfirm;
            }
        }

        //click to disable upgrade tutorial
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && upgradeTutorial.activeSelf)
            upgradeTutorial.SetActive(false);
    }

    private int CheckValidBlocks(int valid, FunctionSlot slot, Image img, bool noKeybind, bool noShape = false)
    {
        if (slot.target == null) //unused slot
            return valid;

        FunctionSlot firstChild = slot.transform.GetChild(1).GetComponent<FunctionSlot>();
        if (firstChild != null && firstChild.target != null) //at least 1 effect
        {
            return valid + 1;
        }
        else //invalid: shape w/ no effects
        {
            return -99;
        }
    }



    public void RandomSymbols()
    {
        foreach (Block b in blocks)
        {
            Vector2 offset = new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 10f));
            b.symbol.GetComponent<RectTransform>().anchoredPosition = new Vector2(-450, b.GetComponent<RectTransform>().anchoredPosition.y+10) + offset;
        }
        ConfirmSpells(true);
    }

    public void ConfirmSpells(bool random=false)
    {
        foreach (Program p in programs)
        {
            p.blocks.RemoveAll(b => b == null);
        }

        confirmButton.SetActive(false);
        randomButton.SetActive(false);
        backButton.SetActive(false);
        symbolsTutorial.SetActive(false);
        if (!random)
            showTutorial = false;

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
            Block shape = p.blocks.Find(b => b.tag == "base");
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
            Block shape = player.autoProgram.blocks.Find(b => b.tag == "base");
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

        //spawn symbol
        Transform symbol = Instantiate(emptyImage, Vector2.zero, Quaternion.identity, cdIcon.GetChild(3)).transform;
        symbol.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        symbol.SetSiblingIndex(cdIcon.childCount - 2);

        Vector2 totalPos = Vector2.zero;
        string programName = "";
        float cd = 0;
        foreach (Block b in p.blocks)
        {
            programName += b.nameTxt.text + " + ";
            cd += b.cd;
            GameObject s = Instantiate(b.symbol.gameObject, b.symbol.transform.position, Quaternion.identity, symbol);
            s.GetComponent<Image>().color = fullSymbolColor;
            totalPos += s.GetComponent<RectTransform>().anchoredPosition;
        }
        foreach (Transform child in symbol)
        {
            child.GetComponent<RectTransform>().anchoredPosition -= totalPos / symbol.transform.childCount;
            Destroy(child.GetComponent<Symbol>());
            Destroy(child.GetComponent<BoxCollider2D>());
        }

        //set program values
        p.name = programName.Substring(0, programName.Length - 3);
        p.cdMax = cd;

        p.fillTimer = cdIcon.GetChild(cdIcon.childCount - 1).gameObject;
        if (type == "AURA")
            p.fillTimer.GetComponent<Image>().fillAmount = 1;
        cdIcon.GetChild(1).GetComponent<TextMeshProUGUI>().text = shape;
    }



    public List<Block> ChooseRandom(int n, string[] forbidden = null, string sector = "none", string[] category = null)
    {
        //TODO: add diff percents? --- keep in 3 separate lists, but decrement pct of given list when chosen (e.g 40-40-20, then choose effect -> 50-25-25)
        if (forbidden == null)
            forbidden = new string[0];

        bool skipAura = false;
        bool skipAuto = false;
        foreach (Block b in blocks)
        {
            if (b.name == "Aura")
                skipAura = true;
            else if (b.name == "Auto")
                skipAuto = true;
        }
        List<Block> starting = new List<Block>();
        List<Block> chosen = new List<Block>();
        foreach (GameObject g in allBlocks)
        {
            if (!((skipAura && g.name == "Aura") || (skipAuto && g.name == "Auto")) && (sector == "none" || sector == g.GetComponent<Block>().sector) && (category == null || category.Contains(g.GetComponent<Block>().tag)) && !forbidden.Contains(g.name))
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



    public Color ColorFromSector(string sector)
    {
        if (sector == "logic")
            return sectorColors[0];
        else if (sector == "memory")
            return sectorColors[1];
        else if (sector == "instinct")
            return sectorColors[2];
        else if (sector == "perception")
            return sectorColors[3];
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
        foreach (Block b in blocks)
        {
            b.infoTxt.gameObject.SetActive(moreInfo);
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
        TextMeshProUGUI txt = k.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
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
                                script.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "__";
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
    public Program(List<Block> blocks_, KeyCode bind_, GameObject symbolIndicator_=null)
    {
        blocks = blocks_;
        name = blocks_[0].name;
        keybind = bind_;
        symbolIndicator = symbolIndicator_;
        if (ProgramManager.Instance.keybindStrMap.ContainsKey(bind_))
            keybindStr = ProgramManager.Instance.keybindStrMap[bind_];
        else
            keybindStr = bind_.ToString();
    }

    public string name;
    public List<Block> blocks;
    public float cdMax;
    public float cdTimer;
    [HideInInspector] public GameObject fillTimer;
    public KeyCode keybind;
    public string keybindStr;
    public GameObject symbolIndicator;
}

[System.Serializable]
public class KeyStringPair
{
    public KeyCode key;
    public string value;
}
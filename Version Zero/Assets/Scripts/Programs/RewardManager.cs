using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public int num;
    public int width;

    [Header("Block UI")]
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private GameObject modPrefab;
    [SerializeField] private GameObject keybindPrefab;
    [SerializeField] private Color[] sectorColors;

    [Header("Objects")]
    [SerializeField] private GameObject showPrograms;
    [SerializeField] private GameObject hidePrograms;
    [SerializeField] private GameObject blockBG;

    [Header("Transforms")]
    [SerializeField] private Transform rewardParent;
    [SerializeField] private Transform blockParent;

    [Header("Rerolls")]
    public int rerolls;
    [SerializeField] private TextMeshProUGUI rerollTxt;
    [SerializeField] private TextMeshProUGUI rerollTxtSmall;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Reward(num);
        }
    }

    public void Reward(int n)
    {
        foreach (Transform child in rewardParent)
        {
            if (child.name.Contains("Reward"))
                Destroy(child.gameObject);
        }

        bool smallIcons = !rewardParent.GetComponent<Image>().enabled;
        if (!smallIcons)
        {
            rewardParent.GetComponent<Image>().enabled = true;
            hidePrograms.SetActive(false);
            showPrograms.SetActive(true);
            blockBG.SetActive(false);
        }

        List<Block> chosenBlocks = new List<Block>();
        for (int i = 0; i < n; i++)
        {
            string[] forbidden = new string[chosenBlocks.Count];
            for (int j = 0; j < chosenBlocks.Count; j++)
            {
                forbidden[j] = chosenBlocks[j].name;
            }

            float rand = Random.Range(0f, 1f);
            if (rand < 0.4f)
                chosenBlocks.Add(ProgramManager.Instance.ChooseRandom(1, forbidden, ProgramManager.Instance.buildpath)[0]);
            else
                chosenBlocks.Add(ProgramManager.Instance.ChooseRandom(1, forbidden)[0]);
        }
        ShowRewards(chosenBlocks);
        if (smallIcons)
            ShowPrograms();
        GameManager.Instance.pauseGame = true;
    }


    public void ShowPrograms()
    {
        ProgramManager.Instance.Reforge();
        rewardParent.GetComponent<Image>().enabled = false;
        showPrograms.SetActive(false);
        hidePrograms.SetActive(true);
        ProgramManager.Instance.compileButton.SetActive(false);
        blockBG.SetActive(true);
        foreach (Transform child in rewardParent)
        {
            if (child.name.Contains("Reward"))
            {
                child.GetComponent<RectTransform>().anchoredPosition = new Vector2(child.GetComponent<RectTransform>().anchoredPosition.x * 0.55f, -440);
                child.localScale *= 0.7f;
            }
        }
    }

    public void HidePrograms()
    {
        ProgramManager.Instance.programUI.gameObject.SetActive(false);
        rewardParent.GetComponent<Image>().enabled = true;
        hidePrograms.SetActive(false);
        showPrograms.SetActive(true);
        blockBG.SetActive(false);
        foreach (Transform child in rewardParent)
        {
            if (child.name.Contains("Reward"))
            {
                child.GetComponent<RectTransform>().anchoredPosition = new Vector2(child.GetComponent<RectTransform>().anchoredPosition.x / 0.55f, 0);
                child.localScale /= 0.7f;
            }
        }
    }

    public void ShowRewards(List<Block> blocks)
    {
        int n = blocks.Count;
        rewardParent.gameObject.SetActive(true);
        if (n <= width) //just one row
        {
            List<Block> currRow = new List<Block>();
            for (int i = 0; i < n; i++)
            {
                currRow.Add(blocks[i]);
            }
            MakeRow(currRow, 0, 0);
        }
        else if (n % width == 1)
        {
            //first row of width-1
            List<Block> currRow = new List<Block>();
            for (int i = 0; i < width - 1; i++)
            {
                currRow.Add(blocks[i]);
            }
            MakeRow(currRow, 0, n / width);
            currRow.Clear();

            //middle rows
            int rowCount = 0;
            int rowNum = 1;
            for (int i = width - 1; i < n - 2; i++)
            {
                rowCount++;
                currRow.Add(blocks[i]);
                if (rowCount == width)
                {
                    MakeRow(currRow, rowNum, n / width);
                    rowCount = 0;
                    currRow.Clear();
                    rowNum++;
                }
            }

            //last row of 2
            for (int i = n - 2; i < n; i++)
            {
                currRow.Add(blocks[i]);
            }
            MakeRow(currRow, rowNum, n / width);
        }
        else
        {
            int rowNum = 0;
            int rowCount = 0;
            int totalRows = (n % width == 0) ? n / width - 1 : n / width;
            List<Block> currRow = new List<Block>();
            for (int i = 0; i < n; i++)
            {
                rowCount++;
                currRow.Add(blocks[i]);
                if (rowCount == width)
                {
                    MakeRow(currRow, rowNum, totalRows);
                    rowCount = 0;
                    currRow.Clear();
                    rowNum++;
                }
            }
            MakeRow(currRow, rowNum, totalRows);
        }
    }

    private void MakeRow(List<Block> row, int rowNum, int totalRows)
    {
        float rowY = (totalRows == 0) ? 50 : Mathf.Lerp(200 * totalRows, -200 * totalRows, rowNum / (totalRows * 1f));
        for (int i = 0; i < row.Count; i++)
        {
            //2: -400, 400
            float rowX = 600 * (i - 0.5f);

            //3: -500, 0, 500
            if (row.Count == 3)
                rowX = 500 * (i - 1);

            //4: -600, -200, 200, 600
            else if (row.Count == 4)
                rowX = 400 * (i - 1.5f);

            Transform reward;
            bool keybind = (row[i].name == "Auto" || row[i].name == "Aura");
            if (keybind)
                reward = Instantiate(keybindPrefab, Vector2.zero, Quaternion.identity, rewardParent).transform;
            else if (row[i].tag == "mod")
                reward = Instantiate(modPrefab, Vector2.zero, Quaternion.identity, rewardParent).transform;
            else if (row[i].tag == "base")
                reward = Instantiate(basePrefab, Vector2.zero, Quaternion.identity, rewardParent).transform;
            else
                reward = Instantiate(effectPrefab, Vector2.zero, Quaternion.identity, rewardParent).transform;

            //name
            if (!keybind)
            {
                reward.GetChild(3).GetComponent<TextMeshProUGUI>().text = row[i].nameTxt.text;
                while (reward.GetChild(3).GetComponent<TextMeshProUGUI>().preferredWidth > reward.GetChild(1).GetComponent<RectTransform>().sizeDelta.x + 80 && reward.GetChild(3).GetComponent<TextMeshProUGUI>().fontSize > 24)
                {
                    reward.GetChild(3).GetComponent<TextMeshProUGUI>().fontSize -= 1;
                }

                //upgrades
                int maxLvls = Mathf.CeilToInt(row[i].cd - row[i].minCd);
                for (int j = reward.GetChild(0).childCount - 1; j >= 0; j--)
                {
                    if (j >= maxLvls)
                        Destroy(reward.GetChild(0).GetChild(j).gameObject);
                }

                //other properties
                string cdText = ((row[i].cd + "").Length > 1) ? row[i].cd + "s" : row[i].cd + ".0s";
                reward.GetChild(4).GetComponent<TextMeshProUGUI>().text = cdText;
                reward.GetChild(5).GetComponent<TextMeshProUGUI>().text = row[i].description;
            }
            else
            {
                reward.GetChild(1).GetComponent<TextMeshProUGUI>().text = row[i].nameTxt.text;
                reward.GetChild(3).GetComponent<TextMeshProUGUI>().text = row[i].description;
            }

            //set type color
            if (row[i].sector == "instinct")
                reward.GetChild(2).GetComponent<Image>().color = sectorColors[0];
            else if (row[i].sector == "logic")
                reward.GetChild(2).GetComponent<Image>().color = sectorColors[1];
            else if (row[i].sector == "memory")
                reward.GetChild(2).GetComponent<Image>().color = sectorColors[2];
            else if (row[i].name == "Damage")
                reward.GetChild(2).GetComponent<Image>().color = new Color(0, 0, 0, 1);

            //Set position & references
            reward.GetComponent<RectTransform>().anchoredPosition = new Vector2(rowX, rowY);
            reward.GetComponent<RewardClick>().blockParent = blockParent;
            reward.GetComponent<RewardClick>().blockToAdd = row[i].gameObject;
        }
    }


    public void AddReroll()
    {
        rerolls++;
        rerollTxt.text = "(" + rerolls + ")";
        rerollTxtSmall.text = "Rerolls: <b>" + rerolls + "</b>";
        rerollTxt.transform.parent.GetComponent<Button>().interactable = (rerolls > 0);
    }

    public void Reroll()
    {
        rerolls--;
        rerollTxt.text = "(" + rerolls + ")";
        rerollTxtSmall.text = "Rerolls: <b>" + rerolls + "</b>";
        rerollTxt.transform.parent.GetComponent<Button>().interactable = (rerolls > 0);
        Reward(num);
    }
}
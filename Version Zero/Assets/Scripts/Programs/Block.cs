using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Block : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Dragging")]
    private Vector2 lastPos;
    [HideInInspector] public RectTransform rectTransform;
    private Canvas canvas;
    private bool dragging;
    [HideInInspector] public bool attached;
    [SerializeField] private bool noKeybind;

    [Header("Movement Children")]
    private List<Block> blocks = new List<Block>();
    public GameObject leftSpace;
    public GameObject rightSpace;

    private GameObject targetSpace;
    private Block upgrade;
    [HideInInspector] public Symbol symbol;
    public Block left;
    public Block right;
    public KeybindSlot keybind;

    [Header("Info Children")]
    public GameObject cdTxt;
    public GameObject typeTxt;
    public TextMeshProUGUI nameTxt;
    public TextMeshProUGUI levelTxt;
    public TextMeshProUGUI infoTxt;
    public GameObject highlight;
    public GameObject levelUp;

    [Header("Spell Effects")]
    public string scifiName;
    public int lvls = 1;
    public string type;
    public string tag;
    [SerializeField] private List<string> blockedTags;
    public float minCd;
    public float cd;
    [TextArea(4, 8)] public string description;

    [Header("Saves")]
    private Vector3 posSAVE;
    private Vector3 symbolPosSAVE;
    private Block leftSAVE;
    private Block rightSAVE;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        symbol = transform.GetChild(0).GetComponent<Symbol>();
        symbol.GetComponent<Image>().enabled = false;
        
        string[] modTags = new string[]{"passive"};
        if (!Array.Exists(modTags, t => t == tag))
        {
            string formattedCD = ((cd+"").Length == 1) ? cd + ".0s" : cd + "s";
            cdTxt.GetComponent<TextMeshProUGUI>().text = formattedCD;
        }
    }


    void Start()
    {
        if (GameManager.Instance.scifiNames)
            nameTxt.text = scifiName;
        //else
        //    nameTxt.text = name.Substring(0, name.Length-7);
        infoTxt.text = description;
    }

    private void Update()
    {
        if (dragging)
        {
            //hide indicators by default
            foreach (Transform child in transform.parent)
            {
                Block bl = child.GetComponent<Block>();
                bl.leftSpace.SetActive(false);
                bl.rightSpace.SetActive(false);
                bl.levelUp.SetActive(false);
            }
            foreach (Transform child in GameObject.Find("Keybinds").transform)
            {
                child.GetComponent<KeybindSlot>().rightSpace.SetActive(false);
            }

            Bounds b = GetComponent<BoxCollider2D>().bounds;
            Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, b.extents*3, 0, LayerMask.GetMask("Block"));
            foreach(Collider2D c in hits)
            {
                if (c.gameObject != gameObject)
                {
                    //if overlap another block...
                    Block bScript = c.GetComponent<Block>();
                    KeybindSlot kScript = c.GetComponent<KeybindSlot>();
                    if (bScript != null)
                    {
                        if (rectTransform.anchoredPosition.x > bScript.rectTransform.anchoredPosition.x && bScript.right == null && bScript.ValidTag(this, false) && (bScript.attached || bScript.noKeybind) && !noKeybind)
                        {
                            targetSpace = bScript.rightSpace;
                            targetSpace.SetActive(true);
                            break;
                        }
                        else if (noKeybind && rectTransform.anchoredPosition.x < bScript.rectTransform.anchoredPosition.x && bScript.left == null && bScript.ValidTag(this, true) && !bScript.attached)
                        {
                            targetSpace = bScript.leftSpace;
                            targetSpace.SetActive(true);
                            break;
                        }
                    }
                    else if (kScript != null)
                    {
                        if (rectTransform.anchoredPosition.x > kScript.GetComponent<RectTransform>().anchoredPosition.x && kScript.right == null && !noKeybind)
                        {
                            targetSpace = kScript.rightSpace;
                            targetSpace.SetActive(true);
                            break;
                        }
                    }
                }
            }


            //check for same-type blocks to upgrade
            bool upgradeFound = false;
            Collider2D[] tightHits = Physics2D.OverlapBoxAll(b.center, b.extents*1.5f, 0, LayerMask.GetMask("Block"));
            foreach (Collider2D c in tightHits)
            {
                if (c.name == name && c.gameObject != gameObject)
                {
                    Block bl = c.GetComponent<Block>();
                    if (bl.cd > bl.minCd && bl.tag != "passive")
                    {
                        if (upgrade == null)
                            AudioManager.Instance.Play("Upgrade Hover");
                        bl.levelUp.SetActive(true);
                        bl.leftSpace.SetActive(false);
                        bl.rightSpace.SetActive(false);
                        upgrade = bl;
                        upgradeFound = true;
                    }
                }
            }
            if (!upgradeFound)
                upgrade = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked)
        {
            dragging = true;
            AudioManager.Instance.Play("Grab Block");
            // Convert the mouse position to local space relative to the RectTransform
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                canvas.worldCamera,
                out lastPos
            );
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked)
        {
            // Bring the dragged window to the front
            transform.SetSiblingIndex(transform.parent.childCount - 1);

            Vector2 localMousePos;
            // Convert the mouse position to local space relative to the canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localMousePos
            );
            rectTransform.anchoredPosition = localMousePos - lastPos;
            
            // Bound to the border of the UI
            float newX = Mathf.Clamp(rectTransform.anchoredPosition.x, -(860 - rectTransform.sizeDelta.x), 850 - rectTransform.sizeDelta.x);
            float newY = Mathf.Clamp(rectTransform.anchoredPosition.y, -415, 415);
            rectTransform.anchoredPosition = new Vector2(newX, newY);

            targetSpace = null;
            //ResetSymbol(false);
            //ResetSymbol(true);
            if (left != null)
            {
                left.right = null;
                left = null;
            }
            if (right != null)
            {
                right.left = null;
                right = null;
            }
            if (keybind != null)
            {
                keybind.right = null;
                keybind = null;
            }
            attached = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked)
        {
            dragging = false;
            //upgrade if released on same type
            if (upgrade != null)
            {
                upgrade.levelUp.SetActive(false);
                for (int i = 0; i < lvls; i++)
                {
                    upgrade.cd = Mathf.Max(upgrade.minCd, upgrade.cd - 1f);
                    if (upgrade.cd > upgrade.minCd)
                        upgrade.lvls++;
                }
                string lvlTxt = (upgrade.cd == upgrade.minCd) ? "Max" : "Lv. " + (int.Parse(upgrade.levelTxt.text.Substring(4))+1);
                upgrade.levelTxt.text = lvlTxt;
                string cdTxt = ((upgrade.cd+"").Length == 1) ? upgrade.cd + ".0s" : upgrade.cd + "s";
                upgrade.cdTxt.GetComponent<TextMeshProUGUI>().text = cdTxt;
                Destroy(gameObject);
                AudioManager.Instance.Play("Upgrade");
            }

            //snap if released next to valid block
            else if (targetSpace != null)
            {
                AudioManager.Instance.Play("Snap Block");
                Vector3 offset = new Vector3(2 + (rectTransform.sizeDelta.x - 100)/2, 0, 0);
                if (targetSpace.name == "Left Space") //if we're an auto/aura block
                {
                    rectTransform.position = targetSpace.GetComponent<RectTransform>().position - offset;
                    right = targetSpace.transform.parent.GetComponent<Block>();
                    right.left = this;
                    right.attached = true;
                }
                else
                {
                    rectTransform.position = targetSpace.GetComponent<RectTransform>().position + offset;
                    if (targetSpace.transform.parent.name.Contains("Slot"))
                    {
                        keybind = targetSpace.transform.parent.GetComponent<KeybindSlot>();
                        keybind.right = this;
                    }
                    else
                    {
                        left = targetSpace.transform.parent.GetComponent<Block>();
                        left.right = this;
                        left.attached = true;
                    }
                }
                targetSpace.SetActive(false);
                attached = true;
            }
        }
    }


    /*public void ResetSymbol(bool toRight)
    {
        Color c = GetComponent<Image>().color;
        GetComponent<Image>().color = new Color(c.r, c.g, c.b, 1);
        symbol.ResetPos();
        highlight.SetActive(false); // highlight
        nameTxt.GetComponent<CanvasGroup>().alpha = 1;
        cdTxt.SetActive(true);

        if (toRight && right != null)
        {
            right.ResetSymbol(true);
        }
        else if (!toRight && left != null)
        {
            left.ResetSymbol(false);
        }
    }*/


    public bool ValidTag(Block script, bool toRight)
    {
        if (blockedTags.Contains(script.tag) || script.blockedTags.Contains(tag))
        {
            return false;
        }
        else if (toRight)
        {
            if (right == null)
                return true;
            else
                return right.ValidTag(script, true);
        }
        else
        {
            if (left == null)
                return true;
            else
                return left.ValidTag(script, false);
        }
    }


    public void SaveState()
    {
        posSAVE = GetComponent<RectTransform>().anchoredPosition;
        symbolPosSAVE = symbol.GetComponent<RectTransform>().anchoredPosition;
        leftSAVE = left;
        rightSAVE = right;
    }

    public void ReadState()
    {
        GetComponent<RectTransform>().anchoredPosition = posSAVE;
        symbol.transform.GetComponent<RectTransform>().anchoredPosition = symbolPosSAVE;
        left = leftSAVE;
        right = rightSAVE;
    }
}
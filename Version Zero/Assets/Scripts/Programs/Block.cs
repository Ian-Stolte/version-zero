using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Block : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Dragging")]
    private Vector2 lastPos;
    [HideInInspector] public RectTransform rectTransform;
    private Canvas canvas;
    private bool dragging;
    [HideInInspector] public FunctionSlot slot;
    private float hoverTimer;
    private bool hovering;

    [Header("Movement")]
    private GameObject targetSpace;
    private Block upgrade;

    [Header("Children")]
    public Symbol symbol;
    public GameObject symbolBG;
    public GameObject cdTxt;
    public TextMeshProUGUI nameTxt;
    public TextMeshProUGUI infoTxt;
    public GameObject upgradeCircles;
    public GameObject hoverGlow;
    public GameObject levelUp;

    [Header("Spell Effects")]
    public string sector;
    new public string tag;
    [SerializeField] private LayerMask targetLayer;
    public float minCd;
    public float cd;
    [HideInInspector] public int lvls;
    [TextArea(4, 8)] public string description;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        string[] noCD = new string[] { "mod", "keybind" };
        if (!Array.Exists(noCD, t => t == tag))
        {
            string formattedCD = ((cd + "").Length == 1) ? cd + ".0s" : cd + "s";
            cdTxt.GetComponent<TextMeshProUGUI>().text = formattedCD;
        }
        infoTxt.text = description;
        //upgradeCircles.SetActive(false);

        int maxLvls = Mathf.CeilToInt(cd - minCd);
        for (int i = upgradeCircles.transform.childCount-1; i >= 0; i--)
        {
            if (i >= maxLvls)
                Destroy(upgradeCircles.transform.GetChild(i).gameObject);
        }
    }


    private void Update()
    {
        if (dragging)
        {
            //hide indicators by default
            /*foreach (Transform child in transform.parent)
            {
                Block bl = child.GetComponent<Block>();
                if (bl != null)
                    bl.levelUp.SetActive(false);
            }*/
            foreach (Transform child in GameObject.Find("Function Slots").transform)
            {
                child.GetChild(0).gameObject.SetActive(false);
                foreach (Transform innerChild in child)
                {
                    if (innerChild.childCount > 0)
                        innerChild.GetChild(0).gameObject.SetActive(false);
                }
            }

            //check for slots to snap to
            Bounds b = GetComponent<CircleCollider2D>().bounds;
            Collider2D[] hits = Physics2D.OverlapCircleAll(b.center, b.extents.x * 0.8f, targetLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.transform.childCount > 0)
                {
                    if (hit.GetComponent<FunctionSlot>().target == null)
                    {
                        if (!(hit.GetComponent<FunctionSlot>().onlyEffects && tag == "mod")) //can't attach charge to aura
                        {
                            targetSpace = hit.transform.GetChild(0).gameObject;
                            targetSpace.SetActive(true);
                        }
                    }
                }
            }

            //check for same-type blocks to upgrade
            bool upgradeFound = false;
            Collider2D[] tightHits = Physics2D.OverlapCircleAll(b.center, b.extents.x * 0.5f, LayerMask.GetMask("Block"));
            foreach (Collider2D c in tightHits)
            {
                if (c.name == name && c.gameObject != gameObject)
                {
                    Block bl = c.GetComponent<Block>();
                    if (bl.cd > bl.minCd && (bl.tag == "base" || bl.tag == "effect"))
                    {
                        if (upgrade == null)
                            AudioManager.Instance.Play("Upgrade Hover");
                        bl.levelUp.SetActive(true);
                        bl.upgradeCircles.SetActive(true);
                        upgrade = bl;
                        upgradeFound = true;
                    }
                }
            }

            /*if (!upgradeFound)
            {
                upgrade = null;
                foreach (Transform child in transform.parent)
                {
                    Block bl = child.GetComponent<Block>();
                    if (bl != null)
                        bl.upgradeCircles.SetActive(false);
                }
            }*/
        }

        if (hovering && !dragging)
            hoverTimer += Time.deltaTime;
        if (!ProgramManager.Instance.moreInfo)
        {
            infoTxt.gameObject.SetActive(hoverTimer > 0.5f && hovering);
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

        if (hoverGlow != null && !ProgramManager.Instance.spellsLocked)
            hoverGlow.SetActive(true);
        if (upgradeCircles != null)
            upgradeCircles.SetActive(false);
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
            float newX = Mathf.Clamp(rectTransform.anchoredPosition.x, -(860 - rectTransform.sizeDelta.x), 930 - rectTransform.sizeDelta.x);
            float newY = Mathf.Clamp(rectTransform.anchoredPosition.y, -415, 415);
            rectTransform.anchoredPosition = new Vector2(newX, newY);

            if (targetSpace != null)
            {
                FunctionSlot fs = targetSpace.transform.parent.GetComponent<FunctionSlot>();
                if (fs != null && fs.target == this)
                {
                    fs.Detach();
                    slot = null;
                }
            }
            targetSpace = null;

            symbol.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            hoverTimer = 0f;
            /*if (rectTransform.anchoredPosition.x > 850 - rectTransform.sizeDelta.x && -200 < rectTransform.anchoredPosition.y && 200 > rectTransform.anchoredPosition.y)
                levelUp.SetActive(true);
            else
                levelUp.SetActive(false);*/
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
                    if (upgrade.cd > upgrade.minCd)
                    {
                        upgrade.lvls++;
                        int index = upgrade.lvls - 2;
                        if (index >= 0 && index < upgrade.upgradeCircles.transform.childCount)
                            upgrade.upgradeCircles.transform.GetChild(index).GetChild(0).gameObject.SetActive(true);
                    }
                    upgrade.cd = Mathf.Max(upgrade.minCd, upgrade.cd - 1f);
                }
                string cdTxt = ((upgrade.cd + "").Length == 1) ? upgrade.cd + ".0s" : upgrade.cd + "s";
                upgrade.cdTxt.GetComponent<TextMeshProUGUI>().text = cdTxt;
                ProgramManager.Instance.blocks.Remove(this);
                Destroy(gameObject);
                AudioManager.Instance.Play("Upgrade");
                if (upgrade.slot != null)
                    upgrade.slot.ShowElectricity();
            }

            //snap if released next to valid slot
            else if (targetSpace != null)
            {
                AudioManager.Instance.Play("Snap Block");
                rectTransform.position = targetSpace.GetComponent<RectTransform>().position;
                targetSpace.SetActive(false);
                slot = targetSpace.transform.parent.GetComponent<FunctionSlot>();
                slot.Attach(this);
            }
            
            //discard for reroll if released on right edge
            /*else if (rectTransform.anchoredPosition.x > 850 - rectTransform.sizeDelta.x && -180 < rectTransform.anchoredPosition.y && 180 > rectTransform.anchoredPosition.y)
            {
                RewardManager.Instance.AddReroll();
                Destroy(gameObject);
            }*/


            if (hoverGlow != null)
                hoverGlow.SetActive(false);
            if (upgradeCircles != null)
                upgradeCircles.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked)
        {
            hoverTimer = 0f;
            hovering = true;
            //upgradeCircles.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked)
        {
            hovering = false;
            if (!ProgramManager.Instance.moreInfo)
                infoTxt.gameObject.SetActive(false);
            //upgradeCircles.SetActive(false);
        }
    }
}
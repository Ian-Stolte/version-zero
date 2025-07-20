using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BlockNew : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Dragging")]
    private Vector2 lastPos;
    [HideInInspector] public RectTransform rectTransform;
    private Canvas canvas;
    private bool dragging;

    [Header("Movement")]
    private List<Block> blocks = new List<Block>();
    private GameObject targetSpace;
    private Block upgrade;

    [Header("Children")]
    public Symbol symbol;
    public GameObject cdTxt;
    public GameObject sectorIndicator;
    public TextMeshProUGUI nameTxt;
    public TextMeshProUGUI infoTxt;
    public GameObject upgradeCircles;
    public GameObject hoverGlow;
    public GameObject levelUp;

    [Header("Spell Effects")]
    public int lvls = 1;
    public string sector;
    public string tag;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private List<string> blockedTags;
    public float minCd;
    public float cd;
    [TextArea(4, 8)] public string description;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        symbol.GetComponent<Image>().enabled = false;

        string[] modTags = new string[] { "passive" };
        if (!Array.Exists(modTags, t => t == tag))
        {
            string formattedCD = ((cd + "").Length == 1) ? cd + ".0s" : cd + "s";
            cdTxt.GetComponent<TextMeshProUGUI>().text = formattedCD;
        }
        infoTxt.text = description;
    }


    private void Update()
    {
        if (dragging)
        {
            //hide indicators by default
            foreach (Transform child in transform.parent)
            {
                BlockNew bl = child.GetComponent<BlockNew>();
                bl.levelUp.SetActive(false);
            }
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
                        targetSpace = hit.transform.GetChild(0).gameObject;
                        targetSpace.SetActive(true);
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

        if (hoverGlow != null)
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
            float newX = Mathf.Clamp(rectTransform.anchoredPosition.x, -(860 - rectTransform.sizeDelta.x), 850 - rectTransform.sizeDelta.x);
            float newY = Mathf.Clamp(rectTransform.anchoredPosition.y, -415, 415);
            rectTransform.anchoredPosition = new Vector2(newX, newY);

            if (targetSpace != null)
            {
                FunctionSlot slot = targetSpace.transform.parent.GetComponent<FunctionSlot>();
                if (slot != null && slot.target == this)
                    slot.Detach();
            }
            targetSpace = null;
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
                //TODO: fill in upgrade circles
                string cdTxt = ((upgrade.cd + "").Length == 1) ? upgrade.cd + ".0s" : upgrade.cd + "s";
                upgrade.cdTxt.GetComponent<TextMeshProUGUI>().text = cdTxt;
                Destroy(gameObject);
                AudioManager.Instance.Play("Upgrade");
            }

            //snap if released next to valid slot
            else if (targetSpace != null)
            {
                AudioManager.Instance.Play("Snap Block");
                rectTransform.position = targetSpace.GetComponent<RectTransform>().position;
                targetSpace.SetActive(false);
                targetSpace.transform.parent.GetComponent<FunctionSlot>().Attach(this);
            }

            if (hoverGlow != null)
                hoverGlow.SetActive(false);
            if (upgradeCircles != null)
                upgradeCircles.SetActive(true);
        }
    }
    

    /*public bool ValidTag(Block script, bool toRight)
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
    }*/
}
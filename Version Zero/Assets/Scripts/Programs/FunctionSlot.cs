using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionSlot : MonoBehaviour
{
    public Block target;

    public bool rootSlot;
    public bool onlyEffects;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TMPro.TextMeshProUGUI cdTxt;

    private void Start()
    {
        target = null;
    }

    public void Attach(Block block)
    {
        target = block;
        Transform parent = (rootSlot) ? transform : transform.parent;
        int missing = (parent.GetComponent<FunctionSlot>().target == null) ? 1 : 0;
        foreach (Transform child in parent)
        {
            if (child.GetComponent<FunctionSlot>() != null)
                missing += (child.GetComponent<FunctionSlot>().target == null) ? 1 : 0;
        }
        if (missing == 0)
        {
            GameObject newSlot = Instantiate(slotPrefab, Vector2.zero, Quaternion.identity, parent);
            float yPos = (rootSlot) ? 0 : GetComponent<RectTransform>().anchoredPosition.y;
            if (!parent.GetComponent<FunctionSlot>().onlyEffects)
            {
                newSlot.GetComponent<RectTransform>().anchoredPosition = new Vector2((parent.childCount - 1) * 150, yPos);
            }
            else
            {
                newSlot.GetComponent<RectTransform>().anchoredPosition = new Vector2((parent.childCount - 1) * 134, yPos);
                newSlot.transform.localScale = new Vector3(1, 1, 1);
            }
            newSlot.GetComponent<FunctionSlot>().target = null;
            newSlot.GetComponent<FunctionSlot>().cdTxt = cdTxt;
        }
        ShowElectricity();
    }

    public void Detach()
    {
        target = null;
        if (!rootSlot)
        {
            Transform parent = transform.parent;
            int lastIndex = parent.childCount - 1;
            // Count empty slots
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                FunctionSlot slot = parent.GetChild(i).GetComponent<FunctionSlot>();
                if (slot != null && slot.target == null)
                    lastIndex = i;
                else
                    break;
            }

            // Destroy all but one empty slot at the end
            if (lastIndex == 1 && parent.GetComponent<FunctionSlot>().target == null && parent.GetComponent<FunctionSlot>().onlyEffects)
                lastIndex = -1;
            for (int i = parent.childCount - 1; i > lastIndex; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
        else if (onlyEffects && transform.childCount <= 2) //if only effects, go from 2 -> 1 on root detach
        {
            if (transform.GetChild(1).GetComponent<FunctionSlot>().target == null)
                Destroy(transform.GetChild(1).gameObject);
        }
        ShowElectricity();
    }


    public void ShowElectricity()
    {
        float cooldown = 0f;
        Transform parent = (rootSlot) ? transform : transform.parent;
        if (parent.GetComponent<FunctionSlot>().target == null || parent.name == "Aura Slot") //if no parent, hide electricity
        {
            foreach (Transform child in parent)
            {
                if (child.childCount > 2)
                    child.GetChild(2).gameObject.SetActive(false);
            }
            cdTxt.gameObject.SetActive(false);
        }
        else
        {
            bool show = true;
            foreach (Transform child in parent)
            {
                FunctionSlot slot = child.GetComponent<FunctionSlot>();
                if (slot != null)
                {
                    if (slot.target == null)
                        show = false;
                    child.GetChild(2).gameObject.SetActive(show);
                    if (show)
                    {
                        cooldown += slot.target.cd;
                    }
                }
            }
            cdTxt.gameObject.SetActive(cooldown > 0);
            cooldown += parent.GetComponent<FunctionSlot>().target.cd;
            if (parent.name == "Auto Slot")
                cdTxt.text = cooldown/2f + "s";
            else
                cdTxt.text = cooldown + "s";
        }
    }
}

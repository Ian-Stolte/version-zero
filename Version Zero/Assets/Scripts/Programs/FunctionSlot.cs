using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionSlot : MonoBehaviour
{
    public Block target;

    public bool shape;
    [SerializeField] private GameObject slotPrefab;

    private void Start()
    {
        target = null;
    }

    public void Attach(Block block)
    {
        target = block;
        Transform parent = (shape) ? transform : transform.parent;
        int missing = (parent.GetComponent<FunctionSlot>().target == null) ? 1 : 0;
        foreach (Transform child in parent)
        {
            if (child.GetComponent<FunctionSlot>() != null)
                missing += (child.GetComponent<FunctionSlot>().target == null) ? 1 : 0;
        }
        if (missing == 0)
        {
            GameObject newSlot = Instantiate(slotPrefab, Vector2.zero, Quaternion.identity, parent);
            float yPos = (shape) ? 0 : GetComponent<RectTransform>().anchoredPosition.y;
            newSlot.GetComponent<RectTransform>().anchoredPosition = new Vector2((parent.childCount - 1) * 150, yPos);
            newSlot.GetComponent<FunctionSlot>().target = null;
        }
        ShowElectricity();
    }

    public void Detach()
    {
        target = null;
        if (!shape)
        {
            Transform parent = transform.parent;
            int myIndex = transform.GetSiblingIndex();

            //if all slots to the right are empty, destroy them
            List<GameObject> toDestroy = new List<GameObject>();
            bool destroy = true;
            for (int i = parent.childCount - 1; i > myIndex; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent<FunctionSlot>() != null)
                {
                    if (child.GetComponent<FunctionSlot>().target == null)
                    {
                        toDestroy.Add(child.gameObject);
                    }
                    else
                    {
                        destroy = false;
                        break;
                    }
                }
            }
            if (destroy)
            {
                foreach (GameObject g in toDestroy)
                    Destroy(g);
            }

            // if we are the last child, destroy empty slots to our left but leave 1 free
            myIndex = transform.GetSiblingIndex();
            if (myIndex == parent.childCount - 1)
            {
                int lastFilled = 0;
                for (int i = 0; i < myIndex; i++)
                {
                    var slot = parent.GetChild(i).GetComponent<FunctionSlot>();
                    if (slot != null && slot.target != null)
                        lastFilled = i;
                }
                if (lastFilled != myIndex - 1)
                {
                    for (int i = lastFilled + 2; i <= myIndex; i++)
                    {
                        var slot = parent.GetChild(i).GetComponent<FunctionSlot>();
                        if (slot != null && slot.target == null)
                        {
                            Destroy(parent.GetChild(i).gameObject);
                        }
                    }
                }
            }
        }
        ShowElectricity();
    }


    private void ShowElectricity()
    {
        Transform parent = (shape) ? transform : transform.parent;
        if (parent.GetComponent<FunctionSlot>().target == null)
        {
            foreach (Transform child in parent)
            {
                if (child.childCount > 2)
                    child.GetChild(2).gameObject.SetActive(false);
            }
        }
        else
        {
            bool show = true;
            foreach (Transform child in parent)
            {
                if (child.GetComponent<FunctionSlot>() != null)
                {
                    if (child.GetComponent<FunctionSlot>().target == null)
                        show = false;
                    child.GetChild(2).gameObject.SetActive(show);
                }
            }
        }
    }
}

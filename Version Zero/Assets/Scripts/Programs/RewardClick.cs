using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardClick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool keybind;
    public GameObject blockToAdd;
    public Transform blockParent;

    public void AddSpell()
    {
        StartCoroutine(AddSpellCor());
    }

    public IEnumerator AddSpellCor()
    {
        AudioManager.Instance.Play("Reward Click");
        if (ProgramManager.Instance.programUI.gameObject.activeSelf)
        {
            yield return null;
            ProgramManager.Instance.compileButton.SetActive(true);
        }
        else
        {
            GameObject.Find("Fader").GetComponent<Fader>().FadeInOut(0.5f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            ProgramManager.Instance.Reforge();
        }
        GameObject.Find("Rewards").SetActive(false);
        if (!keybind)
        {
            ProgramManager.Instance.CreateBlock(blockToAdd);
        }
        else if (transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text == "Auto")
        {
            GameObject.Find("Keybinds").transform.GetChild(0).gameObject.SetActive(true);
            GameObject.Find("Function Slots").transform.GetChild(0).gameObject.SetActive(true);
            ProgramManager.Instance.modBlocks.Remove(ProgramManager.Instance.modBlocks.Find(b => b.name == "Auto"));
            ProgramManager.Instance.allBlocks.Remove(ProgramManager.Instance.allBlocks.Find(b => b.name == "Auto"));
        }
        else if (transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text == "Aura")
        {
            GameObject.Find("Keybinds").transform.GetChild(4).gameObject.SetActive(true);
            GameObject.Find("Function Slots").transform.GetChild(4).gameObject.SetActive(true);
            ProgramManager.Instance.modBlocks.Remove(ProgramManager.Instance.modBlocks.Find(b => b.name == "Aura"));
            ProgramManager.Instance.allBlocks.Remove(ProgramManager.Instance.allBlocks.Find(b => b.name == "Aura"));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.Play("Button Hover");
        transform.localScale *= 1.2f;
        int index = (keybind) ? 3 : 5;
        transform.GetChild(index).localScale /= 1.2f;
        RectTransform rect = transform.GetChild(index).GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y/1.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale /= 1.2f;
        int index = (keybind) ? 3 : 5;
        transform.GetChild(index).localScale *= 1.2f;
        RectTransform rect = transform.GetChild(index).GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y*1.2f);
    }
}

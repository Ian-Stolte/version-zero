using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class KeybindSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public FunctionSlot shapeBase;

    public KeyCode keybind;
    public bool atuomatic;

    public GameObject infoTxt;


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked && infoTxt != null)
        {
            infoTxt.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!ProgramManager.Instance.spellsLocked && infoTxt != null)
        {
            if (!ProgramManager.Instance.moreInfo)
                infoTxt.SetActive(false);
        }
    }    
}

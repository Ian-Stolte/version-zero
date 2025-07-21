using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Symbol : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector2 lastPos;
    private Vector2 startingPos;
    [HideInInspector] public RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas canvas;

    public bool canMove;
    public Vector2 min;
    public Vector2 max;

    public int adjSymbols;


    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent.parent as RectTransform;
        startingPos = rectTransform.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();
    }


    private void Update()
    {
        if (ProgramManager.Instance.spellsLocked && canMove)
        {
            //TODO: replace w/ better logic (e.g check a bounding box around all symbols)
            Bounds b = GetComponent<BoxCollider2D>().bounds;
            adjSymbols = Physics2D.OverlapBoxAll(b.center, b.extents*3f, 0, LayerMask.GetMask("Symbol")).Length;
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (ProgramManager.Instance.spellsLocked && canMove)
        {
            // Convert the mouse position to local space relative to the RectTransform
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.worldCamera,
                out lastPos
            );
            lastPos -= rectTransform.anchoredPosition;
        }
        GetComponent<Image>().color = new Color32(255, 197, 92, 255);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ProgramManager.Instance.spellsLocked && canMove)
        {
            // Bring the dragged window to the front
            transform.parent.SetSiblingIndex(transform.parent.childCount - 1);

            Vector2 localMousePos;
            // Convert the mouse position to local space relative to the canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.worldCamera,
                out localMousePos
            );
            rectTransform.anchoredPosition = localMousePos - lastPos;
            
            // Bound the window to the border of the UI
            float newX = Mathf.Clamp(rectTransform.anchoredPosition.x, min.x, max.x);
            float newY = Mathf.Clamp(rectTransform.anchoredPosition.y, min.y, max.y);
            rectTransform.anchoredPosition = new Vector2(newX, newY);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GetComponent<Image>().color = new Color32(134, 235, 255, 255);
    }

    public void ResetPos()
    {
        rectTransform.anchoredPosition = startingPos;
        GetComponent<Image>().enabled = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AccessPoint : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey;
    [SerializeField] private float interactDist;
    [SerializeField] private bool reward;
    private bool used;

    [Header("On Approach")]
    [SerializeField] private bool dialogueOnApproach;
    private bool approached;

    [Header("On Complete")]
    public Transform directionsText;
    public GameObject[] showOnComplete;
    [SerializeField] private Material usedMat;

    [Header("Dialogue")]
    public string ID;

    private Transform player;
    private Transform cam;


    void Start()
    {
        player = GameObject.Find("Player").transform;
        cam = GameObject.Find("Main Camera").transform;
    }

    void Update()
    {
        if (dialogueOnApproach && !approached)
        {
            if (Vector3.Distance(player.position, transform.position) < 13)
            {
                approached = true;
                if (SceneManager.GetActiveScene().name == "Final 10")
                    StartCoroutine(GameManager.Instance.Ending());
                else
                    DialogueManager.Instance.PlayByID("Access_Approach");
            }
        }

        bool playerClose = Vector3.Distance(player.position, transform.position) < interactDist;
        transform.GetChild(0).gameObject.SetActive(playerClose);
        transform.GetChild(0).transform.forward = cam.forward;
        if (playerClose && Input.GetKeyDown(interactKey))
        {
            if (!used)
            {
                used = true;
                if (directionsText != null)
                {
                    foreach (Transform child in directionsText)
                    {
                        TextMeshProUGUI txt = child.GetComponent<TextMeshProUGUI>();
                        txt.fontStyle = FontStyles.Bold | FontStyles.Strikethrough;
                        Color c = txt.color;
                        txt.color = new Color(c.r, c.g, c.b, 0.3f);
                    }
                }
                if (SceneManager.GetActiveScene().name == "Level 2")
                    StartCoroutine(DialogueManager.Instance.FirstAccessPt());
                else if (SceneManager.GetActiveScene().name == "Final 10")
                    StartCoroutine(GameManager.Instance.LastAccessPt());

                else
                {
                    if (reward)
                    {
                        RewardManager.Instance.Reward(3);
                        GetComponent<MeshRenderer>().material = usedMat;
                    }
                    else
                        ProgramManager.Instance.Reforge();

                    if (ID != "")
                        StartCoroutine(DelayedDialogue());
                }

                if (showOnComplete != null)
                {
                    foreach (GameObject g in showOnComplete)
                        g.SetActive(!g.activeSelf);
                }
            }
            else
            {
                ProgramManager.Instance.Reforge();
            }
        }
    }

    private IEnumerator DelayedDialogue()
    {
        DialogueManager.Instance.StopCoroutines();
        yield return new WaitForSeconds(1);
        DialogueManager.Instance.PlayByID(ID);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terminal : MonoBehaviour
{
    [SerializeField] private float activateDist;
    [SerializeField] private GameObject activateTxt;
    [HideInInspector] public bool complete;

    [Header("On Complete")]
    public MeshRenderer screen;
    public MeshRenderer[] bars;
    public GameObject[] toggleOnComplete;
    public Transform barrier;
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
        if (Vector3.Distance(player.position, transform.position) < activateDist && !complete && !GameManager.Instance.playerPaused)
        {
            activateTxt.SetActive(true);
            if (Input.GetKeyDown(GameManager.Instance.terminalBind))
            {
                GameManager.Instance.currentTerminal = this;
                StartCoroutine(GameManager.Instance.UseTerminal());
            }
        }
        else
        {
            activateTxt.SetActive(false);
        }

        activateTxt.transform.forward = cam.forward;
    }
}

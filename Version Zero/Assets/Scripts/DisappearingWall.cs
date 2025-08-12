using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearingWall : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material transMat;
    [SerializeField] private Material solidMat;

    [Header("Timer")]
    private float timer;
    [SerializeField] private Vector2 solidTime;
    [SerializeField] private Vector2 invisTime;
    [SerializeField] private float startingTime;

    private bool solid = true;


    void Start()
    {
        timer = startingTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            solid = !solid;
            if (transform.childCount == 0)
            {
                FlipStates(transform);
            }
            else
            {
                foreach (Transform child in transform)
                {
                    FlipStates(child);
                }
                if (GetComponent<MeshRenderer>() != null)
                    FlipStates(transform);
            }
            if (solid)
                timer = Random.Range(solidTime.x, solidTime.y);
            else
                timer = Random.Range(invisTime.x, invisTime.y);
        }
    }

    private void FlipStates(Transform obj)
    {
        if (obj.GetComponent<BoxCollider>() != null)
        {
            obj.GetComponent<BoxCollider>().enabled = solid;
            obj.GetComponent<MeshRenderer>().material = (solid) ? solidMat : transMat;
            obj.gameObject.layer = (solid) ? LayerMask.NameToLayer("Barrier") : LayerMask.NameToLayer("Default");
        }
        else
        {
            obj.gameObject.SetActive(solid);
        }
    }
}

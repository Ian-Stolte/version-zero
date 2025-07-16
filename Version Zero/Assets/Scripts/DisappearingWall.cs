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
    [SerializeField] private float minTime;
    [SerializeField] private float maxTime;

    [SerializeField] private bool solid;


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
            timer = Random.Range(minTime, maxTime);
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

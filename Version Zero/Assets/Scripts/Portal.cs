using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private Vector3 offset;
    private bool teleporting;

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Player" && !teleporting)
        {
            teleporting = true;
            StartCoroutine(Teleport());
        }
    }

    private IEnumerator Teleport()
    {
        Fader.Instance.FadeInOut(0.2f, 0.2f);
        //do some VFX maybe
        yield return new WaitForSeconds(0.2f);
        GameObject.Find("Player").transform.position = destination.position + offset;
        destination.GetChild(0).GetComponent<UnityEngine.VFX.VisualEffect>().Play();
        yield return new WaitForSeconds(0.5f);
        teleporting = false;
    }
}

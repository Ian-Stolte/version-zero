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
            StartCoroutine(Teleport(col));
        }
    }

    private IEnumerator Teleport(Collider player)
    {
        Fader.Instance.FadeInOut(0.2f, 0.2f);
        //do some VFX maybe
        yield return new WaitForSeconds(0.2f);
        player.transform.position = destination.position + offset;
        player.GetComponent<PlayerMovement>().lastPos.Clear();
        GameObject.Find("Computer").transform.position = destination.position + offset / 2f;
        for (int i = 0; i < 40; i++)
            player.GetComponent<PlayerMovement>().lastPos.Add(destination.position + offset / 2f);
    
        destination.GetChild(0).GetComponent<UnityEngine.VFX.VisualEffect>().Play();
        yield return new WaitForSeconds(0.5f);
        teleporting = false;
    }
}

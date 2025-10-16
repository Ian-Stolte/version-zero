using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    public string nextArea;
    public bool final;


    void OnTriggerEnter(Collider hit)
    {
        if (hit.gameObject.name == "Player")
        {
            if (!final)
            {
                StartCoroutine(LowerElevator(hit.transform));
                StartCoroutine(hit.GetComponent<PlayerMovement>().CutsceneMove(transform.position, 0.5f));
            }
            else
            {
                StartCoroutine(GameManager.Instance.FinalNextLevel(nextArea));
            }
        }
    }

    private IEnumerator LowerElevator(Transform player)
    {
        // Create positions without y component for distance calculation
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        
        yield return new WaitUntil(() => Vector3.Distance(flatPos, new Vector3(player.transform.position.x, 0, player.transform.position.z)) < 0.5f);
        StartCoroutine(GameManager.Instance.LoadNextLevel(nextArea));
        for (float i = 0; i < 2.5f; i += 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            transform.position -= new Vector3(0, 0.02f * Mathf.Pow(i, 2), 0);
            player.transform.position -= new Vector3(0, 0.02f * Mathf.Pow(i, 2), 0);
        }
    }
}

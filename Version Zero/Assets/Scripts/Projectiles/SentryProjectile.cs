using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryProjectile : MonoBehaviour
{
    public Transform target;
    public float speed;

    public Program program;

    private bool hitTarget;


    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 adjustedPos = target.position + new Vector3(0, 0.5f, 0);

        if (!hitTarget)
        {
            Vector3 dir = (adjustedPos - transform.position).normalized;
            transform.position += Time.deltaTime * dir * speed;

            Quaternion lookRotation = Quaternion.LookRotation(adjustedPos - transform.position);
            transform.rotation = lookRotation * Quaternion.Euler(0, 90, 0);

            if (Vector3.Distance(transform.position, adjustedPos) < 0.5f)
            {
                hitTarget = true;
                GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(new Collider[] { target.GetComponent<Collider>() }, program, transform.position, true);
                transform.GetChild(3).gameObject.SetActive(true);
                AudioManager.Instance.Play("Projectile Hit");
                StartCoroutine(DelayedDestroy());
            }
        }
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}

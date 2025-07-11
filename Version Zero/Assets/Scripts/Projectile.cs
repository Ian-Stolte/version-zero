using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 dir;
    public float speed;
    public int dmg;

    public float despawnDist;
    private float distance;

    private bool hitPlayer;
    private bool destroying;

    private Transform player;


    private void Start()
    {
        player = GameObject.Find("Player").transform;
    }

    private void Update()
    {
        if (!hitPlayer)
        {
            transform.position += Time.deltaTime * dir * speed;
            distance += Time.deltaTime * dir.magnitude * speed;
            if (distance > despawnDist && !destroying)
            {
                destroying = true;
                //StartCoroutine(DelayedDestroy());
                Destroy(gameObject);
            }

            if (!destroying)
                CheckCollisions();
        }
    }

    private void CheckCollisions()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Player") && !player.GetComponent<PlayerPrograms>().dashing)
            {
                hitPlayer = true;
                player.GetComponent<PlayerMovement>().TakeDamage(dmg);
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

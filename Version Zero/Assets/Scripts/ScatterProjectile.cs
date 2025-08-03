using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScatterProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed;
    private Vector3 target;

    [Header("Attack")]
    public float atkRange;
    public int dmg;
    public float tickTime;
    private float tickTimer;

    public float lifeTimer;

    [Header("Bools")]
    [SerializeField] private bool stationary;
    private bool shooting;
    private bool reachedTarget;
    private bool destroying;

    private Transform player;


    private void Start()
    {
        player = GameObject.Find("Player").transform;
        tickTimer = tickTime;
        if (stationary)
        {
            shooting = true;
            reachedTarget = true;
        }
    }

    private void Update()
    {
        if (shooting)
        {
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                //play anim/VFX to show electricity field
                reachedTarget = true;
                transform.GetChild(0).gameObject.SetActive(true);
            }

            if (!reachedTarget) //move
            {
                Vector3 dir = (target - transform.position).normalized;
                transform.position += Time.deltaTime * dir * speed;
            }
            else if (!destroying) //attack
            {
                if (lifeTimer < 1000)
                    lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0)
                    StartCoroutine(DelayedDestroy());

                float dist = Vector3.Distance(player.position, transform.position);
                tickTimer = (dist < atkRange) ? tickTimer - Time.deltaTime : tickTime / 2f;
                transform.GetChild(0).GetChild(0).gameObject.SetActive(dist < atkRange);

                if (tickTimer <= 0)
                {
                    tickTimer = tickTime;
                    player.GetComponent<PlayerMovement>().TakeDamage(dmg);
                }
            }
        }
    }

    private IEnumerator DelayedDestroy()
    {
        destroying = true;

        Vector3 scale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            transform.localScale = scale * (1 - elapsed * 2);
            yield return null;
            elapsed += Time.deltaTime;
        }
        Destroy(gameObject);
    }


    public void Shoot(Vector3 newTarget)
    {
        shooting = true;
        target = newTarget;
        transform.SetParent(null);
    }
}

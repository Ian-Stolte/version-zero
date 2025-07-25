using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LandmineEnemy : Enemy
{
    [Header("Values")]
    [SerializeField] private float defSpeed;

    [Header("Movement")]
    private bool inTransition;
    private bool buried;
    [SerializeField] private float buryMin;
    [SerializeField] private float buryMax;

    [Header("Attack")]
    [SerializeField] private float attackRange;
    [SerializeField] private float explosionForce;
    [SerializeField] private int dmg;
    [SerializeField] private float attackRadius;
    [SerializeField] private GameObject explosionVFX;
    private bool destroying;


    void Update()
    {
        base.Update();

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange)
            aggro = true;

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0 && !inTransition && !destroying)
        {
            if (!buried && dist < buryMin)
                StartCoroutine(Bury());
            else if (buried && dist > buryMax)
                StartCoroutine(Unbury());

            //move
            float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
            if (!buried)
            {
                MoveTo(player.transform.position, speed);
            }

            //attack
            if (buried && dist < attackRange)
            {
                StartCoroutine(Attack());
            }
        }
    }


    private IEnumerator Bury()
    {
        inTransition = true;
        //play bury anim
        Vector3 scale = transform.localScale;
        for (float i = 1; i > 0; i -= 0.01f)
        {
            transform.localScale = new Vector3(scale.x, scale.y / 2f + (scale.y * i) / 2f, scale.z);
            yield return new WaitForSeconds(0.01f);
        }

        buried = true;
        inTransition = false;
    }

    private IEnumerator Unbury()
    {
        inTransition = true;
        //play bury anim
        Vector3 scale = transform.localScale;
        for (float i = 0; i < 1; i += 0.02f)
        {
            transform.localScale = new Vector3(scale.x, scale.y + scale.y * i, scale.z);
            yield return new WaitForSeconds(0.01f);
        }
        buried = false;
        inTransition = false;
    }


    private IEnumerator Attack()
    {
        destroying = true;
        anim.Play("Landmine_Attack");
        transform.GetChild(1).gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }


    public override void TakeDamage(int dmg)
    {
        if (!destroying)
        {
            base.TakeDamage(dmg);
            if (health <= 0)
                StartCoroutine(Attack());
        }
    }

    private void OnDestroy()
    {
        Collider[] enemyHits = Physics.OverlapSphere(transform.position, attackRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider hit in enemyHits)
        {
            Vector3 dir = (hit.transform.position - transform.position).normalized;
            hit.GetComponent<Rigidbody>().AddForce(dir * explosionForce);
            hit.GetComponent<Enemy>().stunTimer = 0.5f;
            hit.GetComponent<Enemy>().TakeDamage(dmg);
        }

        if (Vector3.Distance(player.transform.position, transform.position) < attackRadius)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;
            player.GetComponent<Rigidbody>().AddForce(dir * explosionForce);
            player.GetComponent<PlayerMovement>().TakeDamage(dmg);
        }
        Instantiate(explosionVFX, transform.position + new Vector3(0, 1f, 0), Quaternion.identity);
    }
}
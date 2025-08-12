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
    private bool sitting;
    [SerializeField] private float sitMin;
    [SerializeField] private float sitMax;

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
        if (dist < aggroRange && transform.position.y < 3)
            aggro = true;

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0 && !inTransition && !destroying)
        {
            if (!sitting && dist < sitMin)
                StartCoroutine(Sit());
            else if (sitting && dist > sitMax)
                StartCoroutine(Stand());

            //move
            float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
            if (!sitting)
            {
                anim.SetBool("Moving", true);
                MoveTo(player.transform.position, speed);
            }
            else
            {
                anim.SetBool("Moving", false);
            }

            //attack
            if (sitting && dist < attackRange)
            {
                StartCoroutine(Attack());
            }
        }
        else
        {
            anim.SetBool("Moving", false);
        }
    }


    private IEnumerator Sit()
    {
        inTransition = true;
        anim.Play("Landmine_Sit");
        yield return new WaitForSeconds(0.8f);
        sitting = true;
        inTransition = false;
    }

    private IEnumerator Stand()
    {
        inTransition = true;
        anim.Play("Landmine_Stand");
        yield return new WaitForSeconds(0.3f);
        sitting = false;
        inTransition = false;
    }


    private IEnumerator Attack()
    {
        destroying = true;
        anim.Play("Landmine_Explode");
        transform.GetChild(1).gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        health = 0;
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
        if (health <= 0)
        {
            Collider[] enemyHits = Physics.OverlapSphere(transform.position, attackRadius, LayerMask.GetMask("Enemy"));
            foreach (Collider hit in enemyHits)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                hit.GetComponent<Rigidbody>().AddForce(dir * explosionForce);
                hit.GetComponent<Enemy>().stunTimer = 0.5f;
                hit.GetComponent<Enemy>().TakeDamage(dmg*2);
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
}
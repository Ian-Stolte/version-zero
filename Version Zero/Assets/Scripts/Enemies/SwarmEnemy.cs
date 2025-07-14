using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SwarmEnemy : Enemy
{   
    [Header("Values")]
    [SerializeField] private float defSpeed;

    [Header("Attack")]
    [SerializeField] private float atkDelay;
    private float atkTimer;
    [SerializeField] private float meleeRange;
    [SerializeField] private int dmg;


    void Start()
    {
        atkTimer = atkDelay;
        base.Start();
    }

    void Update()
    {
        base.Update();
        
        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange)
            aggro = true;

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0)
        {
            atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);

            //move
            float speed = (slowTimer > 0) ? defSpeed*0.3f : defSpeed;
            if (dist > meleeRange)
            {
                MoveTo(player.transform.position, speed);
            }

            //attack
            if (atkTimer <= 0 && dist < meleeRange && !player.GetComponent<PlayerPrograms>().dashing)
            {
                player.GetComponent<PlayerMovement>().TakeDamage(dmg);
                anim.Play("Swarm_Attack");
                Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
                rb.AddForce(dir * -400, ForceMode.Impulse);
                atkTimer = atkDelay;
            }
        }
    }
}

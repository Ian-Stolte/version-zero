using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArtilleryEnemy : Enemy
{   
    [Header("Values")]
    [SerializeField] private float defSpeed;

    [Header("Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float atkDelay;
    private float atkTimer;
    [SerializeField] private int dmg;

    [Header("Missiles")]
    [SerializeField] private int numProj;
    [SerializeField] private float spread;
    [SerializeField] private GameObject projPrefab;

    [Header("States")]
    [SerializeField] private float retreatRange;
    [SerializeField] private float retreatThreshold;
    public GameObject shield;

    
    void Start()
    {
        atkTimer = atkDelay * 0.5f;
        base.Start();
        if (shield != null)
            shielded = true;
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
            //look at player
            Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
            transform.rotation = Quaternion.LookRotation(dir);
            
            float speed = (slowTimer > 0) ? defSpeed*0.3f : defSpeed;
            
            if ((health/(1f*maxHealth) <= retreatThreshold) && dist < retreatRange) //retreat mode
            {
                atkRange = retreatRange + 0.5f;
                rb.MovePosition(rb.position + transform.forward * -speed * Time.deltaTime);
            }
            else if (dist > atkRange) //move closer to attack
            {
                MoveTo(player.transform.position, speed);
            }
            //ranged attack
            if (atkTimer <= 0 && dist < atkRange)
            {
                StartCoroutine(FireProjectiles(dir));
            }   
        }
    }

    private IEnumerator FireProjectiles(Vector3 dir)
    {
        if (shield != null)
        {
            shielded = false;
            shield.SetActive(false);
        }
        atkTimer = atkDelay;
        anim.Play("Artillerist_Open");
        yield return new WaitForSeconds(0.3f);
        AudioManager.Instance.Play("Artillerist Fire");
        for (int i = 0; i < numProj; i++)
        {
            GameObject proj = Instantiate(projPrefab, transform.position, Quaternion.LookRotation(dir));
            proj.GetComponent<Missile>().dmg = dmg;
            proj.GetComponent<Missile>().dir = dir * 0.5f + new Vector3(0, 2.5f+(0.1f*i), 0);
            proj.GetComponent<Missile>().target = new Vector3(player.transform.position.x, 0, player.transform.position.z) + player.GetComponent<PlayerMovement>().moveDir*5 + Quaternion.Euler(0, Random.Range(0, 360), 0) * new Vector3(Random.Range(0f, spread), 0, 0);
        }
        if (shield != null)
        {
            yield return new WaitForSeconds(2f);
            shield.SetActive(true);
            shielded = true;
        }
    }
}

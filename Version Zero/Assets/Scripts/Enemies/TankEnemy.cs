using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TankEnemy : Enemy
{   
    [Header("Values")]
    [SerializeField] private float defSpeed;

    [Header("Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float atkDelay;
    private float atkTimer;
    [SerializeField] private int dmg;
    [SerializeField] private float stompForce;
    [SerializeField] private GameObject stompIndicator;
    private bool stomping;
    [SerializeField] private float stompTime;


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
            if (atkDelay == 0 && dist > atkRange)
                atkTimer = Mathf.Max(0.5f, atkTimer);
            
            float speed = (slowTimer > 0) ? defSpeed*0.3f : defSpeed;
            if (dist > atkRange/2f && !stomping)
            {
                MoveTo(player.transform.position, speed);
            }
            if (atkTimer <= 0 && dist < atkRange*0.75f && atkDelay > 0)
            {
                anim.Play("Tank_Stomp");
                atkTimer = atkDelay;
                StartCoroutine(Stomp());
            }
            else if (atkTimer <= 0 && dist < atkRange && atkDelay == 0)
            {
                player.GetComponent<PlayerMovement>().TakeDamage(dmg);
                atkTimer = 1f;
            }
        
            if (atkDelay == 0)
                stompIndicator.SetActive(dist < atkRange);
        }
    }


    private IEnumerator Stomp()
    {
        stomping = true;
        stompIndicator.SetActive(true);
        float elapsed = 0f;
        while (elapsed < stompTime)
        {
            if (!GameManager.Instance.pauseGame)
            {
                stompIndicator.transform.GetChild(0).localScale = new Vector3(Mathf.Lerp(0.2f, 1, elapsed/stompTime), 1, Mathf.Lerp(0.2f, 1, elapsed/stompTime));
                elapsed += Time.deltaTime;
            }
            yield return null;
            if (stunTimer > 0)
            {
                stompIndicator.SetActive(false);
                stomping = false;
                yield break;
            }
        }
        AudioManager.Instance.Play("Stomp Impact");
        if (Vector3.Distance(player.transform.position, transform.position) < atkRange)
        {
            player.GetComponent<PlayerMovement>().TakeDamage(dmg);
        
            Vector3 dir = (player.transform.position - transform.position).normalized + new Vector3(0, 0.5f, 0);
            player.GetComponent<Rigidbody>().AddForce(dir * stompForce, ForceMode.Impulse);
        }

        stompIndicator.SetActive(false);
        stomping = false;
    }
}

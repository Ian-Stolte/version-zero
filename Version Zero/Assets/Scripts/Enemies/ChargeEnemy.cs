using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChargeEnemy : Enemy
{
    [Header("Values")]
    [SerializeField] private float defSpeed;
    private bool lineOfSight;

    [Header("Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float atkDelay;
    [SerializeField] private float chargeTime;
    public float atkTimer;

    [SerializeField] private int dmg;
    [SerializeField] private int numProjectiles;
    [SerializeField] private int numWaves;
    [SerializeField] private GameObject projPrefab;
    [SerializeField] private float projSpeed;

    private bool attacking;


    void Start()
    {
        atkTimer = atkDelay;
        base.Start();
    }

    void Update()
    {
        base.Update();

        lineOfSight = !Physics.Raycast(transform.position, (player.transform.position - transform.position).normalized, Vector3.Distance(transform.position, player.transform.position), terrainLayer);
        if (Physics.OverlapSphere(transform.position, collisionRadius, terrainLayer).Length > 0 && Vector3.Distance(transform.position, player.transform.position) > 3)
            lineOfSight = false;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange)
            aggro = true;

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0)
        {
            //move
            float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
            if ((dist > atkRange || !lineOfSight) && !attacking)
            {
                MoveTo(player.transform.position, speed);
            }
            else
            {
                //rotate in place
                float targetYRotation = Mathf.Repeat(transform.eulerAngles.y + 90f, 360f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, targetYRotation, 0), Time.deltaTime);
            }

            //attack
                atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);
            if (atkTimer <= 0 && dist < atkRange && lineOfSight)
            {
                StartCoroutine(Attack());
                atkTimer = atkDelay + chargeTime;
            }
        }
    }


    private IEnumerator Attack()
    {
        attacking = true;
        //anim.Play("Charge_Attack");

        //wait for charge time
        float elapsed = 0f;
        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
            if (stunTimer > 0)
            {
                attacking = false;
                yield break;
            }
        }

        int baseAngle = 0;
        for (int i = 0; i < numWaves; i++)
        {
            Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
            for (int j = 0; j < numProjectiles; j++)
            {
                float angle = 0f;
                if (numProjectiles > 1)
                    angle = Mathf.Lerp(0, 360, (float)j/(numProjectiles)) + baseAngle;
                Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * dir;

                GameObject proj = Instantiate(projPrefab, transform.position + rotatedDir * 0.5f + new Vector3(0, 0.5f, 0), Quaternion.LookRotation(rotatedDir));
                var projectile = proj.GetComponent<Projectile>();
                projectile.dmg = dmg;
                projectile.dir = rotatedDir;
                projectile.speed = projSpeed;
                projectile.despawnDist = atkRange + 5f;
            }
            yield return new WaitForSeconds(0.3f);
            baseAngle += 15;
        }
        attacking = false;
        anim.Play("Idle");
    }
}
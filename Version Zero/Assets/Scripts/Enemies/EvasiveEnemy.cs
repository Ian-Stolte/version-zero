using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvasiveEnemy : Enemy
{   
    [Header("Values")]
    [SerializeField] private float defSpeed;

    [Header("Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float atkDelay;
    private float atkTimer;
    [SerializeField] private int dmg;
    [SerializeField] private int numProjectiles;
    [SerializeField] private GameObject projPrefab;
    [SerializeField] private float projSpeed;
    private bool attacking;

    [Header("Materials")]
    [SerializeField] private Material attackMat;
    private Material normalMat;

    [Header("Dash")]
    [SerializeField] private float dashDist;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashDelay;
    [SerializeField] private LayerMask terrainLayer;
    private float dashCD;
    private bool dashing;
    [SerializeField] private float tooClose;


    void Start()
    {
        atkTimer = atkDelay;
        base.Start();
        normalMat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        base.Update();
        
        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange && transform.position.y < 3)
            aggro = true;

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0)
        {
            //look at player
            Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
            transform.rotation = Quaternion.LookRotation(dir);

            //move closer if far away
            float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
            if (dist > atkRange && !dashing && !attacking)
                rb.MovePosition(rb.position + transform.forward * speed * Time.deltaTime);

            //attack
            atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);
            if (atkTimer <= 0 && dist < atkRange)
            {
                StartCoroutine(Attack());
                atkTimer = atkDelay;
            }


            //dash
            if (!attacking)
            {
                dashCD = Mathf.Max(0, dashCD - Time.deltaTime);
                if (dist < tooClose && dashCD <= 0)
                    StartCoroutine(Dash(Random.Range(-30f, 30f), -1)); //dash away

                if (dist > atkRange + 3f && dashCD <= 0)
                    StartCoroutine(Dash(Random.Range(-30f, 30f), 1, 0.5f)); //dash closer

                if (Physics.OverlapSphere(transform.position, 2, LayerMask.GetMask("Projectile")).Length > 0 && dashCD <= dashDelay-0.4f) //dodge projectiles
                {
                    int sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                    StartCoroutine(Dash(Random.Range(70, 110) * sign, 1, 0.5f));
                }
            }
        }
    }


    private IEnumerator Attack()
    {
        //GetComponent<MeshRenderer>().material = attackMat;
        attacking = true;
        anim.Play("Evasive_Attack");
        float elapsed = 0f;
        while (elapsed < 1)
        {
            elapsed += Time.deltaTime;
            yield return null;
            if (stunTimer > 0)
            {
                GetComponent<MeshRenderer>().material = normalMat;
                attacking = false;
                yield break;
            }
        }

        int sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
        StartCoroutine(Dash(Random.Range(70, 110) * sign));
        yield return new WaitForSeconds(0.2f);

        Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
        rb.AddForce(dir * -200, ForceMode.Impulse);
        for (int i = 0; i < numProjectiles; i++)
        {
            float angle = 0f;
            if (numProjectiles > 1)
                angle = Mathf.Lerp(-30f, 30f, (float)i / (numProjectiles - 1));
            Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * dir;

            GameObject proj = Instantiate(projPrefab, transform.position + rotatedDir * 0.5f + new Vector3(0, 0.5f, 0), Quaternion.LookRotation(rotatedDir));
            var projectile = proj.GetComponent<Projectile>();
            projectile.dmg = dmg;
            projectile.dir = rotatedDir;
            projectile.speed = projSpeed;
            projectile.despawnDist = atkRange + 2f;
        }
        
        //anim horizontal -> upright
        yield return new WaitForSeconds(0.5f);
        attacking = false;
        //GetComponent<MeshRenderer>().material = normalMat;
    }



    private IEnumerator Dash(float angle, int sign = 1, float slowFactor = 1f, int numTimes = 1)
    {
        dashing = true;
        dashCD = dashDelay;
     
        float distMod = 0;
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist > 15 && sign == -1)
            distMod = 2f;
        else if (dist < 3 && Mathf.Abs(angle) <= 30)
            distMod = 2f;
        else if (dist < 5 && Mathf.Abs(angle) <= 30)
            distMod = 1f;
        Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward * sign;
        Vector3 targetPoint = transform.position + direction.normalized * dashDist;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, dashDist, terrainLayer))
        {
            targetPoint = hit.point - direction.normalized * 0.5f;
            //regenerate if would hit a wall immediately
            if (Vector3.Distance(targetPoint, transform.position) < 2f)
            {
                if (Mathf.Abs(angle) >= 70) //if side dash invert direction
                {
                    StartCoroutine(Dash(-angle, sign));
                    yield break;
                }
                else //if dash away or toward, try small side dash
                {
                    int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                    StartCoroutine(Dash(Random.Range(70, 110) * newSign, 1, 0.5f));
                    yield break;
                }
            }
        }

        //apply slows
        float spd = (slowTimer > 0) ? dashSpeed*0.3f : dashSpeed;
        float totalDist = (dashDist + distMod) * slowFactor;

        transform.GetChild(1).GetComponent<TrailRenderer>().emitting = true;

        float dashTime = totalDist / spd;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < dashTime && Vector3.Distance(transform.position, targetPoint) > 0.1f)
        {
            float t = elapsed / dashTime;
            rb.velocity = (targetPoint - startPos).normalized * (spd - t * t);

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPoint;
        rb.velocity = Vector3.zero;

        //instinct multi-dash
        if (dashDelay < 1 && Random.Range(0f, 1f) > 0.5f && numTimes <= 3)
        {
            yield return new WaitForSeconds(0.1f);
            if (attacking && dist < atkRange)
            {
                StartCoroutine(Dash(Random.Range(-30f, 30f), -1, 0.5f, numTimes + 1));
            }
            else if (attacking && dist > atkRange)
            {
                StartCoroutine(Dash(Random.Range(-30f, 30f), 1, 0.5f, numTimes + 1));
            }
            else
            {
                int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                StartCoroutine(Dash(Random.Range(70, 110) * newSign, 1, 0.5f, numTimes + 1));
            }
            yield break;
        }
        
        transform.GetChild(1).GetComponent<TrailRenderer>().emitting = false;
        dashing = false;
    }
}

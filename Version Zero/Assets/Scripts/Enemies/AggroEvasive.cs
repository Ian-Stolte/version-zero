using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AggroEvasive : Enemy
{   
    [Header("Values")]
    [SerializeField] private float defSpeed;
    private bool lineOfSight;

    [Header("Mode Switch")]
    private bool evasive;
    private float modeSwitchPct;

    [Header("Boss")]
    [SerializeField] private GameObject startBarrier;
    [SerializeField] private GameObject endBarrier;
    [SerializeField] private GameObject memoryReward;

    [Header("Aggro Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float aggroAtkDelay;
    private float atkTimer;
    [SerializeField] private float atkDuration;
    [SerializeField] private int aggroDmg;
    [SerializeField] private GameObject atkPrefab;
    private Transform atkWarning;
    private IEnumerator atkCor;

    [Header("Evasive Attack")]
    [SerializeField] private float evasiveAtkDelay;
    [SerializeField] private int evasiveDmg;
    [SerializeField] private int numProjectiles;
    [SerializeField] private GameObject projPrefab;
    [SerializeField] private float projSpeed;

    [Header("Dash")]
    [SerializeField] private float dashDist;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashDelay;
    private float dashCD;
    [SerializeField] private float tooClose;

    [Header("Bools")]
    private bool dashing;
    private bool attacking;
    private bool canHitPlayer;
    private bool hitboxOn;


    void Update()
    {
        base.Update();

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange && !aggro)
        {
            aggro = true;
            StartCoroutine(StartAggro());
        }
    
        if (!GameManager.Instance.pauseGame)
            stunTimer -= Time.deltaTime; //un-stuns twice as fast
        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0)
        {
            if (evasive) //evasive mode
            {
                lineOfSight = !Physics.Raycast(transform.position, (player.transform.position - transform.position).normalized, Vector3.Distance(transform.position, player.transform.position), terrainLayer);
                if (Physics.OverlapSphere(transform.position, collisionRadius, terrainLayer).Length > 0 && Vector3.Distance(transform.position, player.transform.position) > 3)
                    lineOfSight = false;

                //move closer if far away
                float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
                if ((dist > atkRange || !lineOfSight) && !dashing && !attacking)
                    MoveTo(player.transform.position, speed, 0.1f);
                else if (lineOfSight)
                {
                    Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
                    transform.rotation = Quaternion.LookRotation(dir);
                }

                //attack
                atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);
                if (atkTimer <= 0 && dist < atkRange && lineOfSight)
                {
                    StartCoroutine(EvasiveAttack());
                    atkTimer = evasiveAtkDelay;
                }

                //dash
                if (!attacking)
                {
                    dashCD = Mathf.Max(0, dashCD - Time.deltaTime);
                    if (dist < tooClose && dashCD <= 0 && lineOfSight)
                        StartCoroutine(Dash(Random.Range(-30f, 30f), -1)); //dash away

                    if (dist > atkRange + 3f && dashCD <= 0)
                        StartCoroutine(Dash(Random.Range(-30f, 30f), 1, 0.5f)); //dash closer

                    if (Physics.OverlapSphere(transform.position, 2, LayerMask.GetMask("Projectile")).Length > 0 && dashCD <= dashDelay - 0.4f) //dodge projectiles
                    {
                        int sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                        StartCoroutine(Dash(Random.Range(70, 110) * sign, 1, 0.5f));
                    }
                }
            }
            else //aggro mode
            {
                if (!attacking)
                    atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);

                //move if too far or no LOS
                bool lineOfSight = !Physics.Raycast(transform.position, (player.transform.position - transform.position).normalized, Vector3.Distance(transform.position, player.transform.position), terrainLayer);
                if (Physics.OverlapSphere(transform.position, collisionRadius, terrainLayer).Length > 0 && Vector3.Distance(transform.position, player.transform.position) > 3)
                    lineOfSight = false;
                float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
                if ((dist > atkRange || !lineOfSight) && !attacking)
                {
                    MoveTo(player.transform.position, speed);
                }

                //attack
                if (atkTimer <= 0 && dist < atkRange && lineOfSight && !player.GetComponent<PlayerPrograms>().dashing)
                {
                    atkTimer = aggroAtkDelay;
                    atkCor = AggroAttack();
                    StartCoroutine(atkCor);
                }
            }
        }
        else if (atkCor != null) //stop attack if stunned/game paused
        {
            StopCoroutine(atkCor);
            if (atkWarning != null)
                Destroy(atkWarning.parent.gameObject);
            atkCor = null;
            attacking = false;
            canHitPlayer = false;
            hitboxOn = false;
        }

        if (canHitPlayer && hitboxOn)
        {
            if (Physics.OverlapSphere(transform.position, 0.5f, LayerMask.GetMask("Player")).Length > 0)
            {
                player.GetComponent<PlayerMovement>().TakeDamage(aggroDmg);
                canHitPlayer = false;
            }
        }
    }


    private IEnumerator StartAggro()
    {
        atkTimer = aggroAtkDelay;

        startBarrier.SetActive(true);
        AudioManager a = AudioManager.Instance;
        foreach (Sound s in a.currentSongs)
            StartCoroutine(a.StartFade(s.name, 1, 0));
        a.Play("Boss 2");
        StartCoroutine(a.StartFade("Boss 2", 1, 0.2f));

        //play intro dialogue
        GameManager.Instance.pauseGame = true;
        DialogueManager.Instance.PlayByID("Boss_Intro");
        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);
        GameManager.Instance.pauseGame = false;

        //set up health bar
        GameManager.Instance.bossUI.SetActive(true);
        healthBar = GameObject.Find("Boss Fill").GetComponent<Image>();
        //TODO: set up any indicators for phases
        /*for (float i = spawnInterval; i < 1; i+=spawnInterval)
        {
            GameObject indicator = Instantiate(spawnIndicator, Vector2.zero, Quaternion.identity, healthBar.transform.parent.parent);
            indicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(-348, 348, i), 0);
            indicators.Add(indicator);
        }*/
    }



    private void SwitchMode()
    {
        //Debug.Log("Switch mode: " + modeSwitchPct + "%");
        if (Random.Range(0f, 1f) < modeSwitchPct)
        {
            modeSwitchPct = 0f;
            evasive = !evasive;
            atkTimer = 1.5f;
        }
        else
        {
            modeSwitchPct += 0.1f;
        }
    }



    private IEnumerator AggroAttack()
    {
        SwitchMode();
        attacking = true;
        GetComponent<Animator>().Play("Aggro_Attack");

        Vector3 target = player.transform.position + player.GetComponent<PlayerMovement>().moveDir * 2 + (player.transform.position - transform.position).normalized * 2f;
        Vector3 dir = Vector3.Scale(target - transform.position, new Vector3(1, 0, 1)).normalized;
        transform.rotation = Quaternion.LookRotation(dir);

        //shorten target if would hit a wall
        RaycastHit hit;
        float dist = Vector3.Distance(target, transform.position);
        if (Physics.Raycast(transform.position, dir, out hit, dist, terrainLayer))
        {
            target = hit.point - dir.normalized * 0.5f;
        }

        float spd = (slowTimer > 0) ? dashSpeed * 0.5f : dashSpeed;
        float duration = (slowTimer > 0) ? atkDuration * 0.5f : atkDuration;

        atkWarning = Instantiate(atkPrefab, new Vector3(target.x, 0, target.z), transform.rotation).transform.GetChild(0);
        StartCoroutine(AttackIndicator(atkWarning, duration));

        bool preDash = Random.Range(0f, 1f) > 0.5f;
        if (preDash && Vector3.Distance(target, transform.position) > 7)
        {
            yield return new WaitForSeconds(duration - dist / spd - 0.3f);
            int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
            yield return Dash(Random.Range(70, 110) * newSign, 1, 0.5f);
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            yield return new WaitForSeconds(duration - dist / spd);
        }

        if (!hitboxOn)
        {
            canHitPlayer = true;
            hitboxOn = true;
        }
        dir = Vector3.Scale(target - transform.position, new Vector3(1, 0, 1)).normalized;
        while (Vector2.Distance(new Vector2(target.x, atkWarning.position.z), new Vector2(target.x, transform.position.z)) > 0.5f && attacking)
        {
            //TODO: fix something else setting rb.velocity & causing aggro to occasionally get stuck
            rb.velocity = dir * spd;
            yield return null;
        }
        rb.velocity = Vector3.zero;
        canHitPlayer = false;
        hitboxOn = false;

        Destroy(atkWarning.parent.gameObject);
        yield return new WaitForSeconds(0.2f);
        attacking = false;
        SwitchMode();
    }

    private IEnumerator AttackIndicator(Transform atkWarning, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (atkWarning == null)
                yield break;
            atkWarning.localScale = new Vector3(1, 1, 1) * elapsed / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        attacking = false;
    }


    
    private IEnumerator Dash(float angle, int sign = 1, float slowFactor = 1f, int failedAttempts = 0, int numTimes = 1)
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
                if (failedAttempts >= 5)
                    yield break;

                if (Mathf.Abs(angle) >= 70) //if side dash invert direction
                {
                    StartCoroutine(Dash(-angle, sign, slowFactor, failedAttempts+1));
                    yield break;
                }
                else //if dash away or toward, try small side dash
                {
                    int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                    StartCoroutine(Dash(Random.Range(70, 110) * newSign, 1, 0.5f, failedAttempts+1));
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
                StartCoroutine(Dash(Random.Range(-30f, 30f), -1, 0.5f, failedAttempts, numTimes + 1));
            }
            else if (attacking && dist > atkRange)
            {
                StartCoroutine(Dash(Random.Range(-30f, 30f), 1, 0.5f, failedAttempts, numTimes + 1));
            }
            else
            {
                int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                StartCoroutine(Dash(Random.Range(70, 110) * newSign, 1, 0.5f, failedAttempts, numTimes + 1));
            }
            yield break;
        }
        
        transform.GetChild(1).GetComponent<TrailRenderer>().emitting = false;
        dashing = false;
    }



    private IEnumerator EvasiveAttack()
    {
        SwitchMode();
        attacking = true;
        anim.Play("Evasive_Attack");

        //wait 1 sec to charge
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
            if (stunTimer > 0)
            {
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
            projectile.dmg = evasiveDmg;
            projectile.dir = rotatedDir;
            projectile.speed = projSpeed;
            projectile.despawnDist = atkRange + 2f;
        }

        yield return new WaitForSeconds(0.5f);
        attacking = false;
    }



    public override void TakeDamage(int dmg)
    {
        base.TakeDamage(dmg);
        RectTransform rightTri = healthBar.transform.parent.GetChild(1).GetComponent<RectTransform>();
        rightTri.anchoredPosition = new Vector2(Mathf.Lerp(-150, 140, health/(maxHealth * 1.0f)), rightTri.anchoredPosition.y);
    }



    private void OnDestroy()
    {
        if (atkWarning != null)
            Destroy(atkWarning.parent.gameObject);

        GameObject reward = Instantiate(memoryReward, transform.position + new Vector3(0, 0, 0), Quaternion.identity);
        //TODO: set reward program randomly
        healthBar.transform.parent.parent.gameObject.SetActive(false);
        foreach (Transform child in transform.parent)
            Destroy(child.gameObject);

        endBarrier.SetActive(false);

        if (!GameManager.Instance.pauseGame)
            AudioManager.Instance.KillBoss2();
    }
}

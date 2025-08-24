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
    public GameObject startBarrier;
    [SerializeField] private GameObject[] rewardPrograms;
    [SerializeField] private GameObject memoryReward;
    private int arenaNum = 1;
    [SerializeField] private Vector3[] arenaStarts;
    [SerializeField] private Vector3[] playerStarts;

    [Header("Invis")]
    private float invisTimer = 15f;

    [Header("Aggro Attack")]
    [SerializeField] private float atkRange;
    [SerializeField] private float aggroAtkDelay;
    private float atkTimer;
    [SerializeField] private float atkDuration;
    [SerializeField] private int aggroDmg;
    [SerializeField] private GameObject atkPrefab;
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private float slashForce;
    private GameObject atkWarning;
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
            invisTimer = Mathf.Max(0, invisTimer - Time.deltaTime);
            if (invisTimer <= 0 && !attacking)
            {
                StartCoroutine(GoInvis());
                invisTimer = Random.Range(12, 22);
            }
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
                    if (Random.Range(0f, 1f) < 0.6f)
                        atkCor = AggroAttack(false);
                    else
                        atkCor = AggroAttack(true);
                    StartCoroutine(atkCor);
                }
            }
        }
        else if (atkCor != null) //stop attack if stunned/game paused
        {
            StopCoroutine(atkCor);
            if (atkWarning != null)
                Destroy(atkWarning);
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
        GameManager.Instance.bossUI.transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = "The Hunter";
        healthBar = GameObject.Find("Boss Fill").GetComponent<Image>();
    }



    private IEnumerator SwitchMode()
    {
        yield return new WaitUntil(() => !attacking);
        if (Random.Range(0f, 1f) < modeSwitchPct)
        {
            modeSwitchPct = 0f;
            evasive = !evasive;
            atkTimer = 1.5f;
            if (evasive)
            {
                anim.Play("Hunter_GoEvasive");
            }
            else
            {
                anim.Play("Hunter_GoAggro");
            }
        }
        else
        {
            modeSwitchPct += 0.1f;
        }
    }



    private IEnumerator GoInvis()
    {
        atkTimer = Random.Range(2f, 6f);

        /*foreach (Transform child in transform.GetChild(2))
        {
            child.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
        }*/
        transform.GetChild(2).gameObject.SetActive(false);

        int sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
        StartCoroutine(Dash(Random.Range(70, 110) * sign, 1, 0.5f));

        yield return new WaitForSeconds(7f);
        /*foreach (Transform child in transform.GetChild(2))
        {
            child.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }*/
        transform.GetChild(2).gameObject.SetActive(true);
    }



    private IEnumerator AggroAttack(bool slash)
    {
        attacking = true;
        StartCoroutine(SwitchMode());
        GetComponent<Animator>().Play("Hunter_Dash");
        transform.GetChild(2).gameObject.SetActive(true);

        float offsetDir = (slash) ? -2f : 3;
        Vector3 target = player.transform.position + player.GetComponent<PlayerMovement>().moveDir*2 + (player.transform.position - transform.position).normalized * offsetDir;
        Vector3 dir = Vector3.Scale(target - transform.position, new Vector3(1, 0, 1)).normalized;
        transform.rotation = Quaternion.LookRotation(dir);
        Vector3 followThrough = (slash) ? Vector3.zero : (target - transform.position).normalized * 3f;

        //shorten target if would hit a wall
        RaycastHit hit;
        float dist = Vector3.Distance(target, transform.position);
        if (Physics.Raycast(transform.position, dir, out hit, dist, terrainLayer))
        {
            target = hit.point - dir.normalized * 0.5f;
            followThrough = Vector3.zero;
        }

        float spd = (slowTimer > 0) ? dashSpeed * 0.5f : dashSpeed;
        float duration = (slowTimer > 0) ? atkDuration * 0.5f : atkDuration;

        atkWarning = Instantiate(atkPrefab, new Vector3(target.x, 0, target.z), transform.rotation);
        StartCoroutine(AttackIndicator(atkWarning.transform.GetChild(0), duration));

        //chance to dash before attacking
        bool preDash = Random.Range(0f, 1f) > 0.5f && !slash;
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
        transform.GetChild(1).GetComponent<TrailRenderer>().emitting = true;

        dir = Vector3.Scale(target - transform.position, new Vector3(1, 0, 1)).normalized;
        while (Vector3.Distance(target+followThrough, transform.position) > 0.5f && attacking)
        {
            //TODO: fix something else setting rb.velocity & causing aggro to occasionally get stuck
            rb.velocity = dir * spd;
            yield return null;
        }
        rb.velocity = Vector3.zero;
        canHitPlayer = false;
        hitboxOn = false;

        Destroy(atkWarning);

        if (slash)
        {
            anim.Play("Hunter_Circle");
            //circle slash
            atkWarning = Instantiate(slashPrefab, new Vector3(transform.position.x, 0, transform.position.z), Quaternion.identity);
            yield return StartCoroutine(AttackIndicator(atkWarning.transform.GetChild(0), 0.4f));
            if (Vector3.Distance(player.transform.position, transform.position) < 5)
            {
                player.GetComponent<PlayerMovement>().TakeDamage(aggroDmg);
                Vector3 kbDir = (player.transform.position - transform.position).normalized + new Vector3(0, 0.3f, 0);
                player.GetComponent<Rigidbody>().AddForce(kbDir * slashForce, ForceMode.Impulse);
            }
            Destroy(atkWarning);
        }

        yield return new WaitForSeconds(0.2f);
        transform.GetChild(1).GetComponent<TrailRenderer>().emitting = true;
        attacking = false;
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
                    StartCoroutine(Dash(-angle, sign, slowFactor, failedAttempts + 1));
                    yield break;
                }
                else //if dash away or toward, try small side dash
                {
                    int newSign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                    StartCoroutine(Dash(Random.Range(70, 110) * newSign, 1, 0.5f, failedAttempts + 1));
                    yield break;
                }
            }
        }

        //apply slows
        float spd = (slowTimer > 0) ? dashSpeed * 0.3f : dashSpeed;
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
        attacking = true;
        StartCoroutine(SwitchMode());
        anim.Play("Hunter_GoHorizontal");

        //wait 0.5 sec to charge
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
        transform.GetChild(2).gameObject.SetActive(true);

        int sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
        StartCoroutine(Dash(Random.Range(70, 110) * sign));
        yield return new WaitForSeconds(0.2f);

        //randomly choose between 4 different attack variations
        float attackType = Random.Range(0f, 1f);
        if (attackType < 0.5f)
        {
            //basic attack
            FireProjectiles(numProjectiles);
        }
        else if (attackType < 0.75f)
        {
            //fire three waves
            FireProjectiles(numProjectiles);
            yield return new WaitForSeconds(0.3f);
            FireProjectiles(numProjectiles + 1);
            yield return new WaitForSeconds(0.3f);
            FireProjectiles(numProjectiles);
        }
        else
        {
            //attack then dash 4 times in a row
            for (int i = 0; i < 4; i++)
            {
                FireProjectiles(1, 5);
                yield return new WaitForSeconds(0.2f);
                sign = (Random.Range(0f, 1f) > 0.5f) ? 1 : -1;
                StartCoroutine(Dash(Random.Range(70, 110) * sign));
            }
        }

        yield return new WaitForSeconds(0.2f);
        anim.Play("Hunter_GoUpright");
        attacking = false;
    }

    private void FireProjectiles(int numProj, int speedBoost=0)
    {
        anim.Play("Hunter_Shoot");
        Vector3 dir = Vector3.Scale(player.transform.position - transform.position, new Vector3(1, 0, 1)).normalized;
        rb.AddForce(dir * -200, ForceMode.Impulse);
        for (int i = 0; i < numProj; i++)
        {
            float angle = 0f;
            if (numProj > 1)
                angle = Mathf.Lerp(-30f, 30f, (float)i / (numProj - 1));
            Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * dir;

            GameObject proj = Instantiate(projPrefab, transform.position + rotatedDir * 0.5f, Quaternion.LookRotation(rotatedDir));
            var projectile = proj.GetComponent<Projectile>();
            projectile.dmg = evasiveDmg;
            projectile.dir = rotatedDir;
            projectile.speed = projSpeed+speedBoost;
            projectile.despawnDist = atkRange + 3f + speedBoost;
        }
    }


    public override void TakeDamage(int dmg)
    {
        base.TakeDamage(dmg);
        RectTransform rightTri = healthBar.transform.parent.GetChild(1).GetComponent<RectTransform>();
        rightTri.anchoredPosition = new Vector2(Mathf.Lerp(-150, 140, health / (maxHealth * 1.0f)), rightTri.anchoredPosition.y);
        if ((health <= 100 && arenaNum == 1) || (health <= 60 && arenaNum == 2) || (health <= 20 && arenaNum == 3))
        {
            StartCoroutine(NextArena());
        }
        else if (health <= 0)
            StartCoroutine(CustomDestroy());
    }

    private IEnumerator NextArena()
    {
        arenaNum++;
        gridIndex++;
        GameManager.Instance.pauseGame = true;
        Fader.Instance.FadeInOut(0.5f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        //move boss and player
        transform.position = arenaStarts[arenaNum - 1];
        Vector3 origPos = player.transform.position;
        player.transform.position = playerStarts[arenaNum - 1];
        //move computer
        player.GetComponent<PlayerMovement>().lastPos.Clear();
        GameObject.Find("Computer").transform.position = player.transform.position + new Vector3(-1, 0, -1);
        for (int i = 0; i < 40; i++)
            player.GetComponent<PlayerMovement>().lastPos.Add(player.transform.position + new Vector3(-1, 0, -1));
        //move camera
        Vector3 offset = player.transform.position - origPos;
        Camera.main.transform.position += offset;

        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.pauseGame = false;
    }


    private IEnumerator CustomDestroy()
    {
        if (atkWarning != null)
            Destroy(atkWarning);
        AudioManager.Instance.KillBoss2();
        healthBar.transform.parent.parent.gameObject.SetActive(false);

        StartCoroutine(NextArena());
        yield return new WaitForSeconds(0.5f);
        yield return null;
        GameManager.Instance.pauseGame = false;

        //drop program reward
        int index;
        if (GameManager.Instance.enemyType == "Instinct")
        {
            index = SequenceManager.Instance.boss1Kills[0];
            SequenceManager.Instance.boss1Kills[0] += 1;
        }
        else if (GameManager.Instance.enemyType == "Logic")
        {
            index = SequenceManager.Instance.boss1Kills[1];
            SequenceManager.Instance.boss1Kills[1] += 1;
        }
        else // "Memory"
        {
            index = SequenceManager.Instance.boss1Kills[2];
            SequenceManager.Instance.boss1Kills[2] += 1;
        }
        if (index < rewardPrograms.Length)
        {
            GameObject reward = Instantiate(memoryReward, transform.position + new Vector3(0, 0, 0), Quaternion.identity);
            reward.GetComponent<Memory>().program = rewardPrograms[index];
        }
        else
        {
            Debug.Log("Unlocked all " + GameManager.Instance.enemyType + " programs.");
        }
        
        Destroy(gameObject);
    }
}

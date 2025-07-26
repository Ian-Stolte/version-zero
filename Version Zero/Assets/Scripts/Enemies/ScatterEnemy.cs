using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScatterEnemy : Enemy
{
    [Header("Movement")]
    [SerializeField] private float defSpeed;
    [SerializeField] private float targetMin;
    [SerializeField] private float targetMax;
    private Vector3 movementTarget;
    private bool lineOfSight;
    private float stuckTimer;

    [Header("Attack")]
    [SerializeField] private float atkDelay;
    private float atkTimer;
    [SerializeField] private float atkRange;
    [SerializeField] private float attackDistMin;
    [SerializeField] private float attackDistMax;

    [SerializeField] private List<GameObject> projectiles;
    [SerializeField] private GameObject projPrefab;


    void Start()
    {
        atkTimer = atkDelay;
        base.Start();
    }

    void Update()
    {
        base.Update();

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist < aggroRange && !aggro)
        {
            aggro = true;
            movementTarget = ChooseTarget(targetMin, targetMax, new Vector3(transform.position.x, 0.7f, transform.position.z));
        }

        if (!GameManager.Instance.pauseGame && aggro && stunTimer <= 0)
        {
            atkTimer = Mathf.Max(0, atkTimer - Time.deltaTime);

            //move
            float speed = (slowTimer > 0) ? defSpeed * 0.3f : defSpeed;
            if (Vector3.Distance(rb.position, movementTarget) < 3f)
                stuckTimer += Time.deltaTime;
            if (Vector3.Distance(rb.position, movementTarget) < 0.5f || stuckTimer > 2f)
                movementTarget = ChooseTarget(targetMin, targetMax, new Vector3(transform.position.x, 0.7f, transform.position.z));
            else
                MoveTo(movementTarget, speed);

            //attack
            if (atkTimer <= 0 && dist < atkRange && !player.GetComponent<PlayerPrograms>().dashing)
            {
                StartCoroutine(Attack());
                atkTimer = atkDelay;
            }
        }
    }


    private Vector3 ChooseTarget(float minDist, float maxDist, Vector3 center)
    {
        stuckTimer = 0f;
        Vector3 target;
        do
        {
            target = center + Quaternion.Euler(0, Random.Range(0, 360), 0) * new Vector3(1, 0, 1) * Random.Range(minDist, maxDist);
            //lineOfSight = !Physics.Raycast(transform.position, target-transform.position, Vector3.Distance(target, transform.position), terrainLayer);
        } while (!pathfinding.IsWalkable(target, gridIndex+1));
        return target;
    }


    private IEnumerator Attack()
    {
        //TODO: pick better target
        Vector3 atkTarget = ChooseTarget(attackDistMin, attackDistMax, player.transform.position) - new Vector3(0, 0.5f, 0);

        anim.Play("Scatter_Attack");
        //TODO: manually animate attack?

        yield return new WaitForSeconds(0.5f);
        projectiles[0].GetComponent<ScatterProjectile>().Shoot(new Vector3(atkTarget.x, 0, atkTarget.z));
        projectiles.RemoveAt(0);
        foreach (GameObject g in projectiles)
            StartCoroutine(MoveProjUp(g));

        GameObject newProj = Instantiate(projPrefab, transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity, transform);
        projectiles.Add(newProj);
        Vector3 scale = newProj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            newProj.transform.localScale = scale * elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator MoveProjUp(GameObject g)
    {
        float startingY = g.transform.position.y;
        float elapsed = 0;
        while (elapsed < 0.5f)
        {
            g.transform.position = new Vector3(g.transform.position.x, startingY + elapsed, g.transform.position.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
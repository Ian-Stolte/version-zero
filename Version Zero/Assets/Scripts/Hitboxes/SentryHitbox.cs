using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryHitbox : Hitbox
{
    public float shootInterval;
    private float shootTimer;
    [SerializeField] private GameObject projPrefab;
    private Transform closestEnemy;

    public float lifetime = 8f;


    private void Start()
    {
        shootTimer = shootInterval/2f;
    }

    private void Update()
    {
        if (!GameManager.Instance.pauseGame)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 8f, LayerMask.GetMask("Enemy"));
            closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider col in nearbyEnemies)
            {
                float dist = Vector3.Distance(col.transform.position, transform.position);
                //TODO: check line of sight
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestEnemy = col.transform;
                }
            }

            //rotate toward closest enemy
            if (closestEnemy)
            {
                Vector3 direction = closestEnemy.position - transform.position;
                if (direction != Vector3.zero)
                {
                    float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    Quaternion targetRotation = Quaternion.Euler(0, yAngle, 0);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
                }
            }

            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0 && closestEnemy != null)
            {
                shootTimer = shootInterval;
                FireProjectile();
            }


            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
            {
                //TODO: destroy vfx/sfx
                Destroy(gameObject);
            }
        }
    }


    private void FireProjectile()
    {
        GameObject proj = Instantiate(projPrefab, transform.position + transform.forward*0.5f + new Vector3(0, 0.5f, 0), Quaternion.identity);
        proj.GetComponent<SentryProjectile>().target = closestEnemy;
        proj.GetComponent<SentryProjectile>().program = program;
    }

    public override void CheckCollisions() { return; }
}

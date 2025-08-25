using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryHitbox : Hitbox
{
    public float shootInterval;
    private float shootTimer;

    private float lifetime = 8f;


    private void Update()
    {
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 8f, LayerMask.GetMask("Enemy"));
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in nearbyEnemies)
        {
            float dist = Vector3.Distance(col.transform.position, transform.position);
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
        if (shootTimer <= 0)
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


    private void FireProjectile()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 10f);
        List<Collider> targets = new List<Collider>();
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Enemy"))
            {
                targets.Add(col);
            }
        }
    }

    public override void CheckCollisions() { return; }
}

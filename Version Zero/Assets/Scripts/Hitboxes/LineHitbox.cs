using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineHitbox : Hitbox
{
    public Vector3 dir;
    public float speed;
    public float despawnDist;
    private float distance;

    private bool destroying;
    private bool hitEnemy;


    void Update()
    {
        if (!hitEnemy)
        {
            transform.position += Time.deltaTime * dir * speed;
            distance += Time.deltaTime * dir.magnitude * speed;
            if (distance > despawnDist && !destroying)
            {
                destroying = true;
                StartCoroutine(DelayedDestroy());
            }

            if (!destroying)
                CheckCollisions();
        }
    }

    public override void CheckCollisions()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f, LayerMask.GetMask("Enemy"));
        if (hits.Length > 0 && !hitEnemy)
        {
            hitEnemy = true;
            GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(new Collider[] { hits[0] }, program, transform.position);
            transform.GetChild(3).gameObject.SetActive(true);
            AudioManager.Instance.Play("Projectile Hit");
            StartCoroutine(DelayedDestroy());
        }
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }
}

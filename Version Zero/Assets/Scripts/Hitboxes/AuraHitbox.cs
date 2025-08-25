using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuraHitbox : Hitbox
{
    public float tickRate;


    private void Start()
    {
        StartCoroutine(Aura());
    }

    private IEnumerator Aura()
    {
        while(true)
        {
            yield return new WaitForSeconds(tickRate);
            CheckCollisions();
        }
    }

    public override void CheckCollisions()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, transform.localScale.x/2, LayerMask.GetMask("Enemy"));
        if (cols.Length > 0)
            GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(cols, program, transform.position, true);
    }
}

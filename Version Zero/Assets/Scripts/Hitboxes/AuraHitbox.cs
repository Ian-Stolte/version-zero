using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class AuraHitbox : Hitbox
{
    public float tickRate;
    private float timer;
    public VisualEffect vfx;


    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            timer = 0;
            CheckCollisions();
        }
        if (timer <= 0.1f)
            vfx.SetFloat("Strength", 1 - timer*8);  //1 -> 0.2f
        else
            vfx.SetFloat("Strength", timer*2);  //0.2f -> 1
    }

    public override void CheckCollisions()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, transform.localScale.x/2, LayerMask.GetMask("Enemy"));
        if (cols.Length > 0)
            GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(cols, program, transform.position, true);
    }
}

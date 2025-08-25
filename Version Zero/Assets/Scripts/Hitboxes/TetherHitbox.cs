using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetherHitbox : Hitbox
{
    public Transform enemy;
    [HideInInspector] public Transform player;
    private float timer = 2f;

    private Color baseColor;
    [SerializeField] private GameObject hitVFX;


    private void Start()
    {
        baseColor = GetComponent<Renderer>().material.color;
        GetComponent<Renderer>().material.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f*baseColor.a);
    }

    private void Update()
    {
        if (enemy != null && player != null)
        {
            // Move to midpoint
            transform.position = (enemy.position + player.position) / 2f;

            // Point from player to enemy
            Vector3 direction = enemy.position - player.position;
            if (direction != Vector3.zero)
            {
                float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, yAngle + 90, 90);
            }

            // Scale to distance
            float distance = Vector3.Distance(enemy.position, player.position);
            transform.localScale = new Vector3(0.2f, distance * 0.45f, 0.2f);

            GetComponent<Renderer>().material.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Lerp(0.1f, 1.5f, 1 - timer / 2f));

            if (Vector3.Distance(enemy.position, player.position) > 9f)
            {
                Destroy(gameObject);
                //TODO: vfx/sfx of chain breaking
            }

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                CheckCollisions();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void CheckCollisions()
    {
        GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(new Collider[] { enemy.GetComponent<BoxCollider>() }, program, transform.position);
        Instantiate(hitVFX, enemy.position + new Vector3(0, 0.5f, 0), Quaternion.identity);
        Destroy(gameObject);
    }
}

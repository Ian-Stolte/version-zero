using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayHitbox : Hitbox
{
    void Start()
    {
        StartCoroutine(Delay());
    }

    private IEnumerator Delay()
    {
        //show warning indicator
        float elapsed = 0;
        while (elapsed < 1.5f)
        {
            if (!GameManager.Instance.pauseGame)
            {
                transform.GetChild(0).localScale = new Vector3(Mathf.Lerp(0, 1, elapsed / 1.5f), transform.GetChild(0).localScale.y, Mathf.Lerp(0, 1, elapsed / 1.5f));
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        //trigger program
        AudioManager.Instance.Play("Impact");
        CheckCollisions();
        transform.GetChild(1).gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        CheckCollisions();
        transform.GetChild(2).gameObject.SetActive(true);

        //fade out
        Material mat = GetComponent<MeshRenderer>().material;
        Material mat2 = transform.GetChild(0).GetComponent<MeshRenderer>().material;
        Color startColor = mat.color;
        Color startColor2 = mat2.color;
        elapsed = 0;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed/0.5f);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            mat2.color = new Color(startColor2.r, startColor2.g, startColor2.b, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    public override void CheckCollisions()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, transform.localScale.x/2, LayerMask.GetMask("Enemy"));
        if (cols.Length > 0)
            GameObject.Find("Player").GetComponent<PlayerPrograms>().ProgramEffects(cols, program, transform.position);
    }
}

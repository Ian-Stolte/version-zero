using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{   
    [HideInInspector] public Vector3 dir;
    [HideInInspector] public Vector3 target;
    public float speed;
    public int dmg;

    private bool waiting;

    [SerializeField] private GameObject warningPrefab;
    private GameObject warning;

    private Transform player;


    private void Start()
    {
        player = GameObject.Find("Player").transform;
        warning = Instantiate(warningPrefab, target, Quaternion.identity);
        StartCoroutine(Warn(warning.transform.GetChild(0), (20/dir.y + 19/5f)/speed - 0.15f));
    }

    private void Update()
    {
        if (transform.position.y > 20 && !waiting)
        {
            waiting = true;
            Fall();
        }
        else if (transform.position.y < 0)
        {
            Destroy(gameObject);
            AudioManager.Instance.Play("Missile Land");
        }

        if (!waiting && !GameManager.Instance.pauseGame)
            transform.position += Time.deltaTime * dir * speed;
        
        if (dir.y < 0)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (Collider c in hits)
            {
                if (c.CompareTag("Player") && !player.GetComponent<PlayerPrograms>().dashing)
                {
                    player.GetComponent<PlayerMovement>().TakeDamage(dmg);
                    Destroy(gameObject);
                }
            }
        }
    }

    private void Fall()
    {
        transform.position = new Vector3(target.x, 19, target.z);
        dir = new Vector3(0, -5, 0);
        waiting = false;
    }

    private IEnumerator Warn(Transform warning, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (!GameManager.Instance.pauseGame)
            {
                warning.localScale = new Vector3(1, 1, 1) * elapsed/duration;
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        Destroy(warning.parent.gameObject);
    }

    private void OnDestroy()
    {
        Destroy(warning);
    }
}

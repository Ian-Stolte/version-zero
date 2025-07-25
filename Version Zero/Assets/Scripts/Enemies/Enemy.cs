using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Values")]
    public int maxHealth;
    public int health;
    [SerializeField] private bool customDestroy;

    [Header("States")]
    public int aggroRange;
    [HideInInspector] public bool aggro;
    [HideInInspector] public float stunTimer;
    [HideInInspector] public float slowTimer;

    [Header("Pathfinding")]
    public int gridIndex;
    public float collisionRadius;
    public LayerMask terrainLayer;
    [HideInInspector] public bool pathReady;
    [HideInInspector] public Pathfinding pathfinding;
    private Vector3 moveTarget;
    private Vector3[] path;
    private int waypointIndex;

    [Header("Canvas")]
    public Image healthBar;
    [SerializeField] private TextMeshProUGUI statusTxt;
    [SerializeField] private GameObject damageNumber;
    [SerializeField] private GameObject mark;
    private int markDmg;
    private float markTimer;

    [Header("Materials")]
    [SerializeField] private Material damageMat;
    private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    private List<Material> originalMaterials = new List<Material>();

    [Header("Misc")]
    [HideInInspector] public bool shielded;
    [HideInInspector] public IEnumerator auraBurn;

    [Header("References")]
    public Animator anim;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public GameObject player;


    public void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody>();
        player = GameObject.Find("Player");
        pathfinding = GameObject.Find("Pathfinding").GetComponent<Pathfinding>();
        //terrainLayer = LayerMask.GetMask("Obstacle", "Terminal");

        //cache material refs
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null && child.name != "Shield" && !child.name.Contains("Warning"))
            {
                meshRenderers.Add(mr);
                originalMaterials.Add(mr.material);
            }
        }
    }

    public void Update()
    {
        if (transform.position.y < 2)
            Destroy(GetComponent<TrailRenderer>());
        if (!GameManager.Instance.pauseGame)
        {
            stunTimer -= Time.deltaTime;
            slowTimer -= Time.deltaTime;
            anim.SetBool("Stunned", stunTimer > 0);
            if (stunTimer > 0)
                statusTxt.text = "stunned_";
            else if (slowTimer > 0)
                statusTxt.text = "slowed_";
            else
                statusTxt.text = "";

            transform.GetChild(0).transform.forward = Camera.main.transform.forward;
        }

        markTimer -= Time.deltaTime;
        if (markTimer <= 0)
        {
            mark.SetActive(false);
            markDmg = 0;
        }
    }



    //
    //TAKING DAMAGE
    //

    public IEnumerator ApplyBurn(int burn, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            TakeDamage(burn);
            yield return new WaitForSeconds(0.33f);
        }
    }

    public void MarkDamage(int dmg)
    {
        if (markDmg > 0)
        {
            int tempDmg = markDmg;
            markDmg = 0;
            TakeDamage(tempDmg);
        }

        markDmg = dmg;
        mark.SetActive(true);
        markTimer = 2;
    }

    public virtual void TakeDamage(int dmg)
    {
        if (markDmg > 0)
        {
            dmg += markDmg;
            markDmg = 0;
        }
        if (mark != null)
            mark.SetActive(false);

        //warn other enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies)
        {
            Enemy script = e.GetComponent<Enemy>();
            Vector3 dir = e.transform.position - transform.position;
            float dist = Vector3.Distance(e.transform.position, transform.position);
            if (dist < script.aggroRange / 2 && !Physics.Raycast(transform.position, dir, dist, LayerMask.GetMask("Ground")))
            {
                script.TakeDamage(0);
            }
        }

        if (dmg > 0 && !shielded)
        {
            health -= dmg;
            if (health > 0)
            {
                StartCoroutine(TakeDamageFlash());
                //show damage number
                GameObject dmgNumber = Instantiate(damageNumber, transform.position, Quaternion.identity, transform.GetChild(0));
                dmgNumber.transform.forward = transform.GetChild(0).forward;
                dmgNumber.GetComponent<TextMeshProUGUI>().text = "" + dmg;
                Vector2 randomPos = new Vector2(Random.Range(-100, 100), Random.Range(0, 100));
                dmgNumber.GetComponent<RectTransform>().anchoredPosition = randomPos;
                StartCoroutine(FadeText(dmgNumber, 0.5f, randomPos.normalized * 100));
            }
        }
        aggro = true;
        healthBar.fillAmount = health / (maxHealth * 1.0f);
        if (health <= 0 && !customDestroy)
        {
            //play death anim
            Destroy(gameObject);
        }
    }

    public IEnumerator TakeDamageFlash(bool clear = false)
    {
        if (!clear)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr != null && child.name != "Shield" && !child.name.Contains("Warning") && !child.name.Contains("Projectile"))
                {
                    mr.material = damageMat;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
        else
            yield return null;

        // Restore original materials
        for (int i = 0; i < meshRenderers.Count; i++)
        {
            if (meshRenderers[i] != null)
                meshRenderers[i].material = originalMaterials[i];
        }
    }

    private IEnumerator FadeText(GameObject txt, float duration, Vector2 dir)
    {
        Vector2 origScale = txt.transform.localScale;
        float elapsed = 0;
        while (elapsed < 0.3f)
        {
            txt.GetComponent<RectTransform>().anchoredPosition += Time.deltaTime * dir;
            txt.GetComponent<CanvasGroup>().alpha = elapsed / 0.3f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0;
        while (elapsed < duration)
        {
            txt.GetComponent<RectTransform>().anchoredPosition += Time.deltaTime * dir;
            txt.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(1, 0.5f, elapsed / duration);
            txt.transform.localScale = origScale * Mathf.Lerp(1, 0.75f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(txt);
    }



    //
    //PATHFINDING
    //

    public void OnPathFound(Vector3[] newPath, bool successful)
    {
        if (successful)
        {
            path = newPath;
            pathReady = true;
            waypointIndex = 0;
        }
    }

    public void MoveTo(Vector3 pos, float speed, float pathRecheck=0.5f)
    {
        bool lineOfSight = !Physics.Raycast(transform.position, (pos - transform.position).normalized, Vector3.Distance(transform.position, pos), terrainLayer);
        if (Physics.OverlapSphere(transform.position, collisionRadius, terrainLayer).Length > 0)
            lineOfSight = false;

        if (lineOfSight) //direct movement w/ line of sight
        {
            Vector3 dir = Vector3.Scale(pos - transform.position, new Vector3(1, 0, 1)).normalized;
            transform.rotation = Quaternion.LookRotation(dir);
            rb.MovePosition(rb.position + (pos - transform.position).normalized * speed * Time.deltaTime);
        }
        else //pathfinding if blocked
        {
            if (Vector3.Distance(pos, moveTarget) > pathRecheck)
            {
                moveTarget = pos;
                pathfinding.FindPath(transform.position, pos, gridIndex, OnPathFound);
                //RequestManager.RequestPath(transform.position, player.position, false, OnPathFound);
            }

            if (pathReady)
                FollowPath(speed);
        }
    }

    private void FollowPath(float speed)
    {
        if (path.Length > 0)
        {
            if (Vector3.Distance(transform.position, path[waypointIndex]) < 0.5f)
            {
                waypointIndex++;
            }
            if (waypointIndex >= path.Length)
            {
                pathReady = false;
            }
            else
            {
                Vector3 dir = Vector3.Scale(path[waypointIndex] - transform.position, new Vector3(1, 0, 1)).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                rb.MovePosition(rb.position + (path[waypointIndex]-transform.position).normalized * speed * Time.deltaTime);
            }
        }
    }
}

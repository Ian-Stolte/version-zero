using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [HideInInspector] public Vector3 moveDir;
    private Rigidbody rb;
    [HideInInspector] public bool cutsceneMovement;
    private bool running;

    [Header("Health")]
    public int health;
    [SerializeField] private int maxHealth;
    public Transform hpBar;
    [SerializeField] private float maxBurstDmg;
    private float immunityTimer;
    private int currentBurst;
    public bool canDie;

    [Header("Computer")]
    public Transform computer;
    [SerializeField] private float maxCompDist;
    [SerializeField] private float compDist;
    [SerializeField] private float dampTime;
    private Vector3 dampVel = Vector3.zero;
    private Vector3 diff;
    [HideInInspector] public List<Vector3> lastPos = new List<Vector3>();
    [SerializeField] private int maxPosData;
    //y-movement
    [SerializeField] private float compYFreq;
    [SerializeField] private float compYAmp;
    private float compPhase;

    [Header("Shield")]
    public GameObject shield;
    [HideInInspector] public float shieldTimer;

    [Header("Tutorial")]
    public GameObject tutorialWASD;
    public GameObject tutorialDialogue;

    [Header("Misc")]
    public Animator anim;
    [SerializeField] private Animator damageFlash;
    //Game Over
    private bool endingGame;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health = maxHealth;
        for (int i = 0; i < 30; i++)
            lastPos.Add(transform.position + new Vector3(-2, 0, 0));
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        computer.position = transform.position + new Vector3(-2, 1, 0);
        lastPos.Clear();
        for (int i = 0; i < 30; i++)
            lastPos.Add(transform.position + new Vector3(-2, 0, 0));
    }


    void Update()
    {
        //Glitch based on HP
        if (!Camera.main.GetComponent<GlitchManager>().showingGlitch)
            Camera.main.GetComponent<Glitch>().glitch = Mathf.Lerp(0, 0.3f, Mathf.Pow((maxHealth - health) / (1f * maxHealth), 3));

        if (!GameManager.Instance.pauseGame)
        {
            immunityTimer = Mathf.Max(0, immunityTimer - Time.deltaTime);
            shieldTimer = Mathf.Max(0, shieldTimer - Time.deltaTime);
            shield.SetActive(shieldTimer > 0);
        }

        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y - 10 * Time.deltaTime, rb.velocity.z); //apply gravity

        running = Input.GetKey(KeyCode.LeftShift);

        // Set animator speed
        anim.speed = (running) ? 1.5f : 1f;
    
        if (transform.position.y < -10)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }


    void FixedUpdate()
    {
        //Movement
        int lateral = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            lateral++;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            lateral--;
        int forward = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            forward++;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            forward--;

        //compute input direction
        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
        if (!cutsceneMovement)
            moveDir = (lateral*camRight + forward*camForward).normalized;

        //move or not move based on game state
        if ((moveDir != Vector3.zero && !GameManager.Instance.pauseGame && !GameManager.Instance.playerPaused && !GetComponent<PlayerPrograms>().dashing) || (cutsceneMovement))
        {
            float spd = (running) ? speed * 1.5f : speed;
            if (cutsceneMovement)
                spd = speed * 0.7f;
            float rotSpd = rotationSpeed;
            rb.MovePosition(rb.position + moveDir * spd * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotSpd * Time.deltaTime);
            anim.SetBool("Moving", true);
            if (tutorialWASD != null)
                tutorialWASD.SetActive(false);
            if (tutorialDialogue != null)
                tutorialDialogue.SetActive(false);
        }
        else
        {
            anim.SetBool("Moving", false);
        }


        //COMPUTER FOLLOW
        //get average direction of movement
        Vector3 total = Vector3.zero;
        foreach (Vector3 pos in lastPos)
            total += pos;
        diff = transform.position - total/lastPos.Count;

        float distPct = Vector3.Distance(computer.position, transform.position)/maxCompDist - 0.5f;
        //x-z position
        float adjustedDampTime = Mathf.Lerp(dampTime*100, dampTime, distPct);
        computer.position = Vector3.SmoothDamp(computer.position, transform.position - diff*compDist, ref dampVel, dampTime);
        computer.rotation = Quaternion.Slerp(computer.rotation, Quaternion.LookRotation(new Vector3(diff.x, 0, diff.z)*compDist), rotationSpeed * Time.deltaTime);
        //y position
        float freq = Mathf.Lerp(compYFreq*0.5f, compYFreq, distPct);
        float amp = Mathf.Lerp(compYAmp, compYAmp*2, distPct);
        compPhase += freq * Time.deltaTime * 2f * Mathf.PI;
        computer.position += new Vector3(0, Mathf.Sin(compPhase) * amp, 0);
        //update lastPos array
        Vector3 dist = lastPos[lastPos.Count-Mathf.Min(5, lastPos.Count)] - transform.position;
        dist = new Vector3(dist.x, 0, dist.z);
        if (dist.magnitude > 0.01f)
        {
            lastPos.Add(transform.position);
            if (lastPos.Count > maxPosData)
                lastPos.Remove(lastPos[0]);
        }
    }


    public void TakeDamage(int dmg)
    {
        if (immunityTimer == 0 && shieldTimer == 0)
        {
            //keep track of damage taken in last 0.5s & give immunity if past burst threshold
            currentBurst += dmg;
            StartCoroutine(UndoBurst(dmg));
            if (currentBurst > maxBurstDmg)
            {
                immunityTimer = 0.5f;
            }

            //cancel terminal progress
            StopCoroutine(GameManager.Instance.UseTerminal());
            AudioManager.Instance.Stop("Terminal Charge");
            if (GameManager.Instance.bar != null)
                Destroy(GameManager.Instance.bar.transform.parent.gameObject);
            GameManager.Instance.playerPaused = false;

            //take damage
            health = Mathf.Max(0, health-dmg);
            if (health <= 0)
            {
                if (canDie)
                {
                    if (!GameManager.Instance.pauseGame)
                        StartCoroutine(GameManager.Instance.GameOver());
                }
                else
                    health = maxHealth;
            }
            else if (dmg > 0)
            {
                AudioManager.Instance.Play("Take Damage");
                damageFlash.Play("DamageFlash");
                Camera.main.GetComponent<GlitchManager>().ShowGlitch(0.5f, 0.5f);
                SequenceManager.Instance.levelDmg += dmg;
            }
            
            //set HP bar fill
            if (hpBar != null)
            {
                hpBar.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = 0.05f + 0.95f * health/(maxHealth * 1.0f);
                RectTransform rightTri = hpBar.GetChild(0).GetChild(1).GetComponent<RectTransform>();
                rightTri.anchoredPosition = new Vector2(Mathf.Lerp(-137, 120, health/(maxHealth * 1.0f)), rightTri.anchoredPosition.y);
                hpBar.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = health + "/" + maxHealth;
            }
        }
    }

    private IEnumerator UndoBurst(int dmg)
    {
        yield return new WaitForSeconds(0.5f);
        currentBurst -= dmg;
    }

    public IEnumerator CutsceneMove(Vector3 target, float dist)
    {
        cutsceneMovement = true;
        yield return new WaitUntil(() => !GetComponent<PlayerPrograms>().dashing);
        Vector3 flatTarget = new Vector3(target.x, 0, target.z);
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        moveDir = (flatTarget-flatPos).normalized;
        yield return new WaitUntil(() => Vector3.Distance(flatTarget, new Vector3(transform.position.x, 0, transform.position.z)) < dist);
        cutsceneMovement = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using TMPro;

public class PlayerPrograms : MonoBehaviour
{
    [Header("Aura")]
    public float auraTick;
    public float auraDampen;
    [SerializeField] GameObject auraHitbox;
    private GameObject auraObj;
    [HideInInspector] public Program auraProgram;

    [Header("Auto")]
    public float autoTick;
    [SerializeField] private float autoTimer;
    [HideInInspector] public Program autoProgram;

    [Header("Charge")]
    [SerializeField] private GameObject chargeVFX;
    private Program chargeProgram;
    private float chargeTimer;

    [Header("Hitboxes")]
    [SerializeField] private GameObject[] hitboxes;

    [Header("Misc")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TextMeshProUGUI noTargetText;

    [Header("Dash")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;
    [HideInInspector] public bool dashing;
    [SerializeField] private Material transparentMat;


    public void InitializeAura()
    {
        Destroy(auraObj);
        if (auraProgram.name != "")
        {
            auraObj = Instantiate(auraHitbox, transform.position + new Vector3(0, -1, 0), Quaternion.identity, transform);
            auraObj.GetComponent<Hitbox>().program = auraProgram;
            auraObj.GetComponent<AuraHitbox>().tickRate = auraTick;
        }
    }

    void Update()
    {
        if (!GameManager.Instance.pauseGame)
        {
            foreach (Program p in ProgramManager.Instance.programs)
            {
                p.cdTimer = Mathf.Max(0, p.cdTimer - Time.deltaTime);
                p.fillTimer.GetComponent<Image>().fillAmount = p.cdTimer / p.cdMax;
                if (!GameManager.Instance.loadingLevel)
                {
                    if (Input.GetKeyDown(p.keybind) && p.cdTimer <= 0)
                    {
                        if (p.blocks.Exists(b => b.name == "Charge"))
                        {
                            chargeProgram = p;
                            chargeTimer = 0f;
                            GameManager.Instance.playerPaused = true;
                            chargeVFX.GetComponent<VisualEffect>().SetFloat("Strength", 0);
                            chargeVFX.SetActive(true);
                        }
                        else
                            UseProgram(p);
                    }
                    else if (Input.GetKeyDown(p.keybind) && p.cdTimer <= 0.5f)
                    {
                        StartCoroutine(DelayedCast(p));
                    }
                }
            }

            if (autoProgram.name != "" && !GameManager.Instance.loadingLevel)
            {
                autoTimer = Mathf.Max(0, autoTimer - Time.deltaTime);
                if (autoTimer <= 0)
                {
                    UseProgram(autoProgram);
                    autoTimer = autoTick;
                }
                autoProgram.fillTimer.GetComponent<Image>().fillAmount = autoTimer / autoTick;
            }

            //charge timer
            if (chargeProgram != null)
            {
                chargeTimer += Time.deltaTime;
                chargeVFX.GetComponent<VisualEffect>().SetFloat("Strength", Mathf.Min(1, chargeTimer / 2f));
                if (!Input.GetKey(chargeProgram.keybind))
                {
                    UseProgram(chargeProgram, true);
                    chargeProgram = null;
                    GameManager.Instance.playerPaused = false;
                    chargeVFX.SetActive(false);
                }
            }
        }
        else
        {
            chargeProgram = null;
            GameManager.Instance.playerPaused = false;
        }
    }


    private IEnumerator DelayedCast(Program p)
    {
        yield return new WaitUntil(() => p.cdTimer <= 0);
        if (p.blocks.Exists(b => b.name == "Charge"))
        {
            if (Input.GetKey(p.keybind))
            {
                chargeProgram = p;
                chargeTimer = 0f;
                GameManager.Instance.playerPaused = true;
                chargeVFX.GetComponent<VisualEffect>().SetFloat("Strength", 0);
                chargeVFX.SetActive(true);
            }
        }
        else
            UseProgram(p);
    }

    private void UseProgram(Program p, bool charge=false)
    {
        if (charge)
        {
            p.chargeTime = Mathf.Min(2f, chargeTimer);
        }

        Block dash = p.blocks.Find(b => b.name == "Phase");
        if (dash != null && !dashing)
        {
            StartCoroutine(Dash(p));
        }
        else
        {
            p.cdTimer = p.cdMax;
            Quaternion rot = Quaternion.Euler(0, Quaternion.LookRotation(transform.position - MousePos()).eulerAngles.y, 0);
            foreach (Block b in p.blocks)
            {
                if (b.name == "Circle")
                {
                    GameObject hitbox = Instantiate(hitboxes[0], MousePos(), rot);
                    hitbox.GetComponent<Hitbox>().program = p;
                    break;
                }
                else if (b.name == "Line")
                {
                    AudioManager.Instance.Play("Shoot Projectile");
                    Vector3 dir = (MousePos() - transform.position);
                    dir = new Vector3(dir.x, 0, dir.z).normalized;
                    GameObject hitbox = Instantiate(hitboxes[1], transform.position, rot);
                    hitbox.GetComponent<LineHitbox>().dir = dir;
                    hitbox.GetComponent<LineHitbox>().speed = 12 + 5 * p.chargeTime;
                    hitbox.GetComponent<LineHitbox>().despawnDist = 10 + 3 * p.chargeTime;
                    hitbox.GetComponent<Hitbox>().program = p;
                    break;
                }
                else if (b.name == "Pulse" || b.name == "Shield")
                {
                    GameObject hitbox = Instantiate(hitboxes[2], transform.position + new Vector3(0, -0.8f, 0), Quaternion.identity);
                    hitbox.GetComponent<Hitbox>().program = p;
                    if (b.name == "Shield")
                        GetComponent<PlayerMovement>().shieldTimer += 1f;
                    break;
                }
                else if (b.name == "Load")
                {
                    AudioManager.Instance.Play("Place Trap");
                    GameObject hitbox = Instantiate(hitboxes[3], MousePos(), rot);
                    hitbox.GetComponent<Hitbox>().program = p;
                    if (p.chargeTime > 0)
                    {
                        GameObject sparks = Instantiate(chargeVFX, hitbox.transform.position, Quaternion.identity, hitbox.transform);
                        sparks.GetComponent<VisualEffect>().SetFloat("Strength", chargeVFX.GetComponent<VisualEffect>().GetFloat("Strength") / 2f);
                    }
                    break;
                }
                else if (b.name == "Double")
                {
                    GameObject hitbox = Instantiate(hitboxes[4], MousePos(), rot);
                    hitbox.GetComponent<Hitbox>().program = p;
                    break;
                }
                else if (b.name == "Tether")
                {
                    Collider target = GetTargetFromCursor();
                    if (target != null)
                    {
                        if (Vector3.Distance(target.transform.position, transform.position) > 8f)
                        {
                            //fail SFX & visual
                            AudioManager.Instance.Play("Error");
                            StopCoroutine("NoTarget");
                            StartCoroutine("NoTarget");
                            p.cdTimer = 0;
                        }
                        else
                        {
                            //calculate rotation
                            Vector3 direction = target.transform.position - transform.position;
                            float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                            TetherHitbox tether = Instantiate(hitboxes[5], (target.transform.position + transform.position) / 2f, Quaternion.Euler(0, yAngle + 90, 90)).GetComponent<TetherHitbox>();
                            tether.program = p;
                            tether.enemy = target.transform;
                            tether.player = transform;

                            //TODO: vfx/size increase if charge > 0
                        }
                    }
                    else
                    {
                        p.cdTimer = 0;
                    }
                }
                else if (b.name == "Sentry")
                {
                    GameObject hitbox = Instantiate(hitboxes[6], MousePos() + new Vector3(0, 0.5f, 0), rot);
                    hitbox.GetComponent<Hitbox>().program = p;
                    hitbox.GetComponent<SentryHitbox>().shootInterval = p.cdMax/8f;
                    hitbox.GetComponent<SentryHitbox>().lifetime = 4f + p.cdMax/2f;
                    if (p.chargeTime > 0)
                    {
                        GameObject sparks = Instantiate(chargeVFX, hitbox.transform.position, Quaternion.identity, hitbox.transform);
                        sparks.GetComponent<VisualEffect>().SetFloat("Strength", chargeVFX.GetComponent<VisualEffect>().GetFloat("Strength")/2f);
                    }
                    break;
                }
            }
        }
    }

    public void ProgramEffects(Collider[] cols, Program p, Vector3 pos, bool aura = false)
    {
        if (AudioManager.Instance.playEffects)
        {
            foreach (Block b in p.blocks)
            {
                if (b.tag == "effect")
                    AudioManager.Instance.Play(b.name);
            }
        }

        float chargeMult = 1 + 0.5f * p.chargeTime;
        foreach (Collider c in cols)
        {
            Enemy script = c.GetComponent<Enemy>();
            if (script != null)
            {
                //TODO: better way to do this??
                int dmg = 0;
                int burn = 0;
                int injectDmg = 0;
                int markDmg = 0;
                float stun = 0;
                float bait = 0;
                float slow = 0;
                foreach (Block b in p.blocks)
                {
                    if (b.name == "Pause")
                        stun += (aura) ? 0.3f : 1.5f;
                    else if (b.name == "Bait")
                        bait += (aura) ? 0.3f : 1.5f;
                    else if (b.name == "Inject")
                        injectDmg += (aura) ? 1 : 4;
                    else if (b.name == "Slow")
                        slow += (aura) ? 0.5f : 2f;
                    else if (b.name == "Damage")
                        dmg += (aura) ? 1 : 4;
                    else if (b.name == "Burn")
                        burn += (aura) ? 1 : 1;
                    else if (b.name == "Mark")
                        markDmg += (aura) ? 1 : 8;
                    else if (b.name == "Displace")
                    {
                        Vector3 dir = (c.transform.position - pos);
                        dir = (new Vector3(dir.x, 0.2f, dir.z)).normalized;
                        int kbStrength = (aura) ? 600 : 1000;
                        c.GetComponent<Rigidbody>().AddForce(dir * kbStrength * chargeMult, ForceMode.Impulse);
                        script.stunTimer = 0.5f;
                    }
                }
                if (dmg > 0)
                    script.TakeDamage((int)Mathf.Round(dmg*chargeMult));
                if (burn > 0)
                {
                    if (aura)
                    {
                        if (script.auraBurn != null)
                            StopCoroutine(script.auraBurn);
                        script.auraBurn = script.ApplyBurn(burn, 4);
                        StartCoroutine(script.auraBurn);
                    }
                    else
                    {
                        if (chargeMult > 1.5f)
                            StartCoroutine(script.ApplyBurn(burn * 2, 4));
                        else if (chargeMult > 1)
                            StartCoroutine(script.ApplyBurn(burn, (int)Mathf.Round(4 * chargeMult)));
                        else
                            StartCoroutine(script.ApplyBurn(burn, 4));
                    }
                }
                if (injectDmg > 0)
                    script.InjectDamage((int)Mathf.Round(injectDmg*chargeMult));
                if (markDmg > 0)
                    script.MarkDamage((int)Mathf.Round(markDmg*chargeMult));
                if (stun > 0)
                    script.stunTimer = stun * chargeMult;
                if (bait > 0)
                    script.baitTimer = bait * chargeMult;
                if (slow > 0)
                    script.slowTimer = slow * chargeMult;
            }
        }
    }


    private IEnumerator Dash(Program p)
    {
        dashing = true;
        AudioManager.Instance.Play("Dash");
        Physics.IgnoreLayerCollision(6, 12, true);
        GetComponent<TrailRenderer>().emitting = true;

        Vector3 dir = (MousePos() - transform.position);
        dir = new Vector3(dir.x, 0, dir.z).normalized;
        StartCoroutine(GoTransparent(0.3f));

        float elapsed = 0;
        while (elapsed < dashDur)
        {
            GetComponent<Rigidbody>().velocity = dir * dashForce * (-Mathf.Pow((elapsed / dashDur), 2) + 1);
            elapsed += Time.deltaTime;
            yield return null;
        }
        //use program
        p.cdTimer = p.cdMax;
        GameObject hitbox = Instantiate(hitboxes[2], transform.position + new Vector3(0, -0.8f, 0), Quaternion.identity);
        hitbox.GetComponent<Hitbox>().program = p;

        dashing = false;
        GetComponent<TrailRenderer>().emitting = false;
        Physics.IgnoreLayerCollision(6, 12, false);
    }


    private IEnumerator GoTransparent(float duration, float a = 0.5f)
    {
        List<Material> origMats = new List<Material>();
        foreach (Transform child in transform.GetChild(1))
        {
            SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                origMats.Add(renderer.material);
                renderer.material = transparentMat;
                //apply custom a
            }
            else
            {
                origMats.Add(transparentMat);
            }
        }
        yield return new WaitForSeconds(duration);
        for (int i = 0; i < origMats.Count; i++)
        {
            SkinnedMeshRenderer renderer = transform.GetChild(1).GetChild(i).GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.material = origMats[i];
                //apply custom a
            }
        }
    }


    private Collider GetTargetFromCursor()
    {
        Collider[] hits = Physics.OverlapSphere(MousePos(), 2, LayerMask.GetMask("Enemy"));

        if (hits.Length == 0)
        {
            //fail SFX & visual
            AudioManager.Instance.Play("Error");
            StopCoroutine("NoTarget");
            StartCoroutine("NoTarget");
            return null;
        }
        else
        {
            Collider closest = hits[0];
            foreach (Collider hit in hits)
            {
                if (Vector3.Distance(hit.transform.position, MousePos()) < Vector3.Distance(closest.transform.position, MousePos()))
                    closest = hit;
            }
            return closest;
        }
    }

    private IEnumerator NoTarget()
    {
        noTargetText.gameObject.SetActive(true);
        Color c = noTargetText.color;
        noTargetText.color = new Color(c.r, c.g, c.b, 1);
        yield return new WaitForSeconds(0.5f);
        for (float i = 1; i > 0; i -= 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            noTargetText.color = new Color(c.r, c.g, c.b, i);
        }
        noTargetText.gameObject.SetActive(false);
    }


    private Vector3 MousePos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            return hit.point;
        }
        return Vector3.zero;
    }


    public void ResetCds()
    {
        foreach (Program p in ProgramManager.Instance.programs)
        {
            p.cdTimer = 0;
            p.fillTimer.GetComponent<Image>().fillAmount = 0;
        }
    }
}
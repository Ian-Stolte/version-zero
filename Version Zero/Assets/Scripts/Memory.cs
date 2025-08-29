using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memory : MonoBehaviour
{
    [SerializeField] private GameObject newProgramUI;
    public GameObject program;
    [SerializeField] private string dialogueName;
    private bool active;
    [HideInInspector] public GameObject barrier;


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") && !active)
        {
            active = true;
            StartCoroutine(TriggerMemory());
        }
    }

    private IEnumerator TriggerMemory()
    {
        GameManager.Instance.pauseGame = true;
        GetComponent<Animator>().Play("MemoryRise");
        yield return new WaitForSeconds(1f);
        if (dialogueName != "")
            DialogueManager.Instance.PlayByID(dialogueName);
        yield return new WaitUntil(() => !DialogueManager.Instance.dialogue.activeSelf);

        //show new program
        GameObject newProgram = Instantiate(newProgramUI, Vector3.zero, Quaternion.identity, GameObject.Find("Canvas").transform);
        newProgram.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 300);
        GameObject programVisual = Instantiate(program, Vector3.zero, Quaternion.identity, newProgram.transform);
        programVisual.GetComponent<RectTransform>().anchoredPosition = new Vector2(270, 80);
        ProgramManager.Instance.CreateBlock(program);
        if (program.GetComponent<Block>().tag == "effect")
            ProgramManager.Instance.effectBlocks.Add(program);
        else if (program.GetComponent<Block>().tag == "base")
            ProgramManager.Instance.baseBlocks.Add(program);
        else if (program.GetComponent<Block>().tag == "mod")
            ProgramManager.Instance.modBlocks.Add(program);
        ProgramManager.Instance.allBlocks.Add(program);

        newProgram.transform.GetChild(5).GetComponent<TMPro.TextMeshProUGUI>().text = program.GetComponent<Block>().description + ".";

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            newProgram.GetComponent<CanvasGroup>().alpha = elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        } 

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));
        GetComponent<Animator>().Play("MemoryBurst");
        GameManager.Instance.pauseGame = false;
        //knock back nearby enemies
        Collider[] enemies = Physics.OverlapSphere(new Vector3(transform.position.x, 0, transform.position.z), 10f, LayerMask.GetMask("Enemy"));
        foreach (Collider enemy in enemies)
        {
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (enemy.transform.position - transform.position).normalized;
                direction.y = 0.2f;
                float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z));
                float forceMagnitude = Mathf.Lerp(3000f, 500f, distance / 10f); // Stronger force if closer
                rb.GetComponent<Enemy>().stunTimer = 1f;
                rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);
            }
        }
        barrier.SetActive(false);

        //add program to blocks list
        Destroy(newProgram);
    }
}

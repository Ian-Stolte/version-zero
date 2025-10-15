using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public KeyCode panLeft;
    public KeyCode panRight;
    public bool rotSnap;

    private Transform target;
    public Vector3 baseOffset;
    private Vector3 offset;
    public float rotationSpeed;

    public bool cutscene;


    void Start()
    {
        target = GameObject.Find("Player").transform;
        transform.position = target.position - offset;
        //offset = target.position - transform.position;
    }

    void Update()
    {
        if (!GameManager.Instance.pauseGame || cutscene)
        {
            transform.position = target.position - offset;
            /*if (rotSnap)
            {
                if (Input.GetKeyDown(panLeft))
                {
                    transform.RotateAround(target.transform.position, Vector3.up, -45);
                    offset = target.position-transform.position;
                }
                else if (Input.GetKeyDown(panRight))
                {
                    transform.RotateAround(target.transform.position, Vector3.up, 45);
                    offset = target.position-transform.position;
                }
            }
            else
            {
                int mouseX = 0;
                if (Input.GetKey(panLeft))
                    mouseX--;
                if (Input.GetKey(panRight))
                    mouseX++;
                if (mouseX != 0)
                {
                    transform.RotateAround(target.transform.position, Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
                    offset = target.position-transform.position;
                }
            }*/

            //automatic follow
            /*else
            {
                float currY = transform.rotation.eulerAngles.y;
                float targetY = target.transform.rotation.eulerAngles.y;
                float yRot = Mathf.LerpAngle(currY, targetY, Time.deltaTime * rotationSpeed);
                transform.RotateAround(target.transform.position, Vector3.up, yRot - currY);
            }*/
        }
    }

    //move to target offset and back
    public IEnumerator SetOffset(Vector3 diff, float duration)
    {
        cutscene = true;
        Vector3 startOffset = offset;
        Vector3 targetOffset = baseOffset - diff;
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            offset = Vector3.Lerp(startOffset, targetOffset, timer / duration);
            yield return null;
        }
        offset = targetOffset;
        /*yield return new WaitForSeconds(0.5f);
        timer = 0;
        while (timer < duration/2f)
        {
            timer += Time.deltaTime;
            offset = Vector3.Lerp(targetOffset, startOffset, timer / (duration/2f));
            yield return null;
        }
        offset = startOffset;*/
        cutscene = false;
    }
}

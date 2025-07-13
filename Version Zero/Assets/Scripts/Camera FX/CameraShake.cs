using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0;
        while (elapsed < duration)
        {
            transform.localPosition = originalPos + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0)*magnitude*(1-(elapsed/duration));
            elapsed += 0.03f;
            yield return new WaitForSeconds(0.03f);
        }
        transform.localPosition = originalPos;
    }
}

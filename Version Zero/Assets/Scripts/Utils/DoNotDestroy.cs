using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoNotDestroy : MonoBehaviour
{
    [SerializeField] string objTag;

    void Awake()
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag(objTag);
        if (obj.Length > 1)
        {
            if (name == "Player") //bring player to starting pos
            {
                foreach (GameObject g in obj)
                {
                    if (g != gameObject)
                    {
                        g.transform.position = transform.position;
                        g.transform.rotation = transform.rotation;
                    }
                }
            }
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }
}
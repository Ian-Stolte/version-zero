using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Hitbox : MonoBehaviour
{
    public Program program;
    public Collider[] ignoreCols;

    public abstract void CheckCollisions();
}

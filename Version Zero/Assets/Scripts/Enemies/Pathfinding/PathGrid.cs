using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGrid : MonoBehaviour
{
    public LayerMask unwalkable;
    public Vector2 rawGridSize;
    public float nodeRadius;
    public float walkableCheckRadius;
    public Node[,] grid;

    public bool drawGizmos;

    private int gridSizeX, gridSizeZ;


    void Start()
    {
        gridSizeX = Mathf.RoundToInt(rawGridSize.x / (nodeRadius*2));
        gridSizeZ = Mathf.RoundToInt(rawGridSize.y / (nodeRadius*2));
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeZ];
        Vector3 bottomLeft = transform.position - new Vector3(rawGridSize.x/2, 0, rawGridSize.y/2);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Vector3 pos = bottomLeft + new Vector3((2*x + 1)*nodeRadius, 0, (2*z + 1)*nodeRadius);
                bool walkable = Physics.OverlapSphere(pos, walkableCheckRadius, unwalkable).Length == 0;
                grid[x, z] = new Node(walkable, pos, x, z);
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkZ = node.gridZ + z;
                
                if (checkX >= 0 && checkX < gridSizeX && checkZ >= 0 && checkZ < gridSizeZ)
                    neighbors.Add(grid[checkX, checkZ]);
            }
        }
        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 pos)
    {
        pos -= transform.position;
        float percentX = (pos.x + rawGridSize.x/2) / rawGridSize.x;
        float percentZ = (pos.z + rawGridSize.y/2) / rawGridSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);

        int x = Mathf.RoundToInt((gridSizeX-1)*percentX);
        int z = Mathf.RoundToInt((gridSizeZ-1)*percentZ);

        return grid[x, z];
    }

    void OnDrawGizmos()
    {
        if (grid != null && drawGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = n.walkable ? Color.white : Color.red;
                Gizmos.DrawWireCube(n.position, Vector3.one * (2 * nodeRadius - 0.1f));
            }
        }
    }
}


public class Node
{
    public bool walkable;
    public Vector3 position;
    public int gridX;
    public int gridZ;

    public int gCost;
    public int hCost;

    public Node parent;

    public Node(bool _walkable, Vector3 _pos, int _gridX, int _gridZ)
    {
        walkable = _walkable;
        position = _pos;
        gridX = _gridX;
        gridZ = _gridZ;
    }

    public int fCost
    {
        get {
            return gCost + hCost;
        }
    }
}
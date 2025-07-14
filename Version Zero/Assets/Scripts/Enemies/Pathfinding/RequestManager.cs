using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestManager : MonoBehaviour
{
    static RequestManager instance;
    void Awake()
    {
        instance = this;
    }

    Queue<PathRequest> requestQueue = new Queue<PathRequest>();
    PathRequest currentRequest;
    bool isProcessing;


    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, int gridIndex, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, gridIndex, callback);
        instance.requestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    private void TryProcessNext()
    {
        if (!isProcessing && requestQueue.Count > 0)
        {
            currentRequest = requestQueue.Dequeue();
            isProcessing = true;
            StartCoroutine(GetComponent<Pathfinding>().FindPath(currentRequest.pathStart, currentRequest.pathEnd, currentRequest.gridIndex, FinishedProcessing, true));
        }
    }

    public void FinishedProcessing(Vector3[] path, bool success)
    {
        currentRequest.callback(path, success);
        isProcessing = false;
        TryProcessNext();
    }


    struct PathRequest
    {
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public int gridIndex;
        public Action<Vector3[], bool> callback;

        public PathRequest(Vector3 _start, Vector3 _end, int _gridIndex, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            gridIndex = _gridIndex;
            callback = _callback;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ClickToWorld : MonoBehaviour
{
    NavMeshAgent Agent;
    LineRenderer lr;
    Vector3 previousLocation = new Vector3(), newLocation = new Vector3();

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        if (NavMesh.SamplePosition(transform.position, out var hit, 150, 1))
        {
            gameObject.AddComponent<NavMeshAgent>();
            Agent = GetComponent<NavMeshAgent>();
            Agent.speed = 10;
        }

        StartCoroutine(MoveAgent());
        StartCoroutine(DisplayPath());
    }

    bool WithinRangeOf(float distance, Vector3 source, Vector3 dest)
    {
        if (dest.x - distance <= source.x && dest.x + distance >= source.x)
            if (dest.y - distance <= source.y && dest.y + distance >= source.y)
                if (dest.z - distance <= source.z && dest.z + distance >= source.z)
                    return true;
        return false;
    }

    IEnumerator DisplayPath()
    {
        while (true)
        {
            lr.positionCount = 1;
            lr.SetPosition(0, transform.position); ;
            if (Agent.path.corners.Length > 0 && WithinRangeOf(50f, Agent.pathEndPosition, newLocation))
            {
                lr.positionCount += Agent.path.corners.Length;
                var counter = 1;
                foreach (var point in Agent.path.corners)
                {
                    lr.SetPosition(counter, point);
                    counter++;
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator MoveAgent()
    {
        while (true)
        {
            if (previousLocation != newLocation)
            {
                Agent.destination = newLocation;
                previousLocation = newLocation;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
                newLocation = hit.point;
        }
    }
}

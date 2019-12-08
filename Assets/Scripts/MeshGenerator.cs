using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    [SerializeField]
    int xGridSize, zGridSize;

    [SerializeField]
    float minYGen, maxYGen, scaleFactor;

    [SerializeField]
    bool continousGeneration = false;

    System.Random rand = new System.Random();
    List<List<Vector3>> vertices = new List<List<Vector3>>();
    List<int> indices = new List<int>();
    List<Color32> colours = new List<Color32>();
    Mesh _mesh;

    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        GetComponent<MeshCollider>().sharedMesh = _mesh;
        CreateShape();
        UpdateMesh();
        if (continousGeneration)
            StartCoroutine(ContinousGeneration());
        else GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    float GetYPointFromPosition(int x, int z)
    {
        try
        {
            return vertices.ElementAt(z).ElementAt(x).y;
        }
        catch (Exception e)
        {
            return 0f;
        }
    }

    float GetAverageYPoint(int x, int z)
    {
        var counter = 0;
        var yAverage = 0f;
        var temp = 0f;

        temp = GetYPointFromPosition(x - 1, z); //left from point
        if (temp != 0)
        {
            yAverage += temp;
            counter++;
        }

        temp = GetYPointFromPosition(x - 1, z - 1); //left and back from point
        if (temp != 0)
        {
            yAverage += temp;
            counter++;
        }

        temp = GetYPointFromPosition(x, z - 1); //back from point
        if (temp != 0)
        {
            yAverage += temp;
            counter++;
        }

        temp = GetYPointFromPosition(x + 1, z - 1); //right and back from point
        if (temp != 0)
        {
            yAverage += temp;
            counter++;
        }

        temp = GetYPointFromPosition(x + 1, z); //right from point
        if (temp != 0)
        {
            yAverage += temp;
            counter++;
        }

        if (counter == 0)
            return 0;

        return yAverage / counter;
    }

    Vector3 CreateVertex(int x, int z)
    {
        var average = GetAverageYPoint(x, z);
        var y = average + (float)(rand.NextDouble() * Math.Abs(maxYGen - minYGen)) + minYGen; //range between two input values from editor
        y *= scaleFactor;
        y = Mathf.Clamp(y, minYGen, maxYGen);

        return new Vector3(x, y, z);
    }

    void CreateVertices()
    {
        for (int zCount = 0; zCount <= zGridSize; zCount++)
        {
            var row = new List<Vector3>();
            for (int xCount = 0; xCount <= xGridSize; xCount++)
            {
                row.Add(CreateVertex(xCount, zCount));
            }
            vertices.Add(row);
        }
    }

    float FindClosestValue(float value, float min, float max)
    {
        var minDist = Mathf.Abs(min - value);
        var maxDist = Mathf.Abs(max - value);
        var result = minDist < maxDist ? min : max;
        return result;
    }

    Color FindColourRange(float value, bool max)
    {
        var minValue = 0f;
        var maxValue = 0f;
        var dist = Mathf.Abs(minYGen - maxYGen);

        minValue = minYGen;
        maxValue = minYGen + Mathf.Abs(dist * 0.25f);
        if (value >= minValue && value <= maxValue)
        {
            return Color.blue;
        }

        minValue = minYGen + Mathf.Abs(dist * 0.25f);
        maxValue = minYGen + Mathf.Abs(dist * 0.5f);
        if (value >= minValue && value <= maxValue)
        {
            return Color.yellow;
        }

        minValue = minYGen + Mathf.Abs(dist * 0.5f);
        maxValue = minYGen + Mathf.Abs(dist * 0.75f);
        if (value >= minValue && value <= maxValue)
        {
            return Color.green;
        }

        minValue = minYGen + Mathf.Abs(dist * 0.75f);
        maxValue = minYGen + Mathf.Abs(dist * 0.9f);
        if (value >= minValue && value <= maxValue)
        {
            return Color.grey;
        }

        if (value > maxValue)
            return Color.white;

        return Color.black;
    }

    void AssignColours()
    {
        colours.Clear();
        foreach (var list in vertices)
        {
            foreach (var vertex in list)
            {
                colours.Add(Color.Lerp(FindColourRange(vertex.y, false), FindColourRange(vertex.y, true), vertex.y));
            }
        }
    }

    void CreateIndices()
    {
        indices.Clear();
        var vert = 0;
        for (int zCount = 0; zCount < zGridSize; zCount++)
        {
            for (int xCount = 0; xCount < zGridSize; xCount++)
            {
                indices.Add(vert + 0);
                indices.Add(vert + xGridSize + 1);
                indices.Add(vert + 1);
                indices.Add(indices.Last());
                indices.Add(vert + xGridSize + 1);
                indices.Add(indices.Last() + 1);

                vert++;
            }
            vert++;
        }
    }

    void CreateShape()
    {
        CreateVertices();
        AssignColours();
        CreateIndices();
    }

    void MoveMesh()
    {
        vertices.RemoveAt(0);
        colours.RemoveRange(0, xGridSize + 1);

        for (int iCount = 0; iCount < vertices.Count; iCount++)
        {
            for (int iCount2 = 0; iCount2 < vertices.ElementAt(iCount).Count; iCount2++)
            {
                var temp = vertices.ElementAt(iCount).ElementAt(iCount2);
                temp.z -= 1;
                vertices[iCount][iCount2] = temp;
            }
        }

        var tempList = new List<Vector3>();
        for (int iCount = 0; iCount <= xGridSize; iCount++)
        {
            tempList.Add(CreateVertex(iCount, zGridSize));
        }
        vertices.Add(tempList);
        AssignColours();
        CreateIndices();
    }

    IEnumerator ContinousGeneration()
    {
        while (true)
        {
            MoveMesh();
            UpdateMesh();
            yield return new WaitForSeconds(0.01f);
        }
    }

    List<Vector3> MakeOneDimensionalList(List<List<Vector3>> listToChange)
    {
        List<Vector3> result = new List<Vector3>();

        foreach (var list in listToChange)
            foreach (var vertex in list)
            {
                result.Add(vertex);
            }

        return result;
    }

    void UpdateMesh()
    {
        _mesh.Clear();
        _mesh.vertices = MakeOneDimensionalList(vertices).ToArray();
        _mesh.triangles = indices.ToArray();
        _mesh.colors32 = colours.ToArray();
        _mesh.RecalculateNormals();
    }
}

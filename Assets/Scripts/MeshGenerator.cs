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
    float scaleFactor, heightFactor;

    [SerializeField]
    bool continousGeneration = false;

    System.Random rand = new System.Random();
    List<List<Vector3>> vertices = new List<List<Vector3>>();
    List<int> indices = new List<int>();
    List<Color32> colours = new List<Color32>();
    Mesh _mesh;
    int zCurrentCounter = 0;

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

    Vector3 CreateVertex(int x, int z, int zScalar)
    {
        float xCoord = (float)x / xGridSize * scaleFactor;
        float zCoord = (float)zScalar / zGridSize * scaleFactor;

        var y = Mathf.PerlinNoise(xCoord, zCoord);
        //    y *= scaleFactor;
        //y = Mathf.Clamp(y, minYGen * scaleFactor, maxYGen * scaleFactor);

        return new Vector3(x, y, z);
    }

    void CreateVertices()
    {
        for (int zCount = 0; zCount <= zGridSize; zCount++)
        {
            var row = new List<Vector3>();
            for (int xCount = 0; xCount <= xGridSize; xCount++)
            {
                row.Add(CreateVertex(xCount, zCount, zCount));
            }
            vertices.Add(row);
            zCurrentCounter++;
        }
        zCurrentCounter--; //minus because you need +1 vertex for the grid size.
    }

    Color FindColourRange(float value, bool max)
    {
        var minValue = 0f;
        var maxValue = 0f;

        minValue = 0;
        maxValue = .25f;
        if (value >= minValue && value <= maxValue)
        {
            if (!max)
                return Color.blue;
            return Color.yellow;
        }

        minValue = .25f;
        maxValue = .5f;
        if (value >= minValue && value <= maxValue)
        {
            if (!max)
                return Color.yellow;
            return Color.green;
        }

        minValue = .5f;
        maxValue = .75f;
        if (value >= minValue && value <= maxValue)
        {
            if (!max)
                return Color.green;
            return Color.grey;
        }

        minValue = .75f;
        maxValue = .9f;
        if (value >= minValue && value <= maxValue)
        {
            if (!max)
                return Color.grey;
            return Color.white;
        }

        if (value > .9f)
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
                indices.Add(vert);
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

    void ApplyHeightMap()
    {
        for (int x = 0; x < vertices.Count; x++)
            for (int z = 0; z < vertices.ElementAt(x).Count; z++)
            {
                var temp = vertices[x][z];
                vertices[x][z] = new Vector3(temp.x, temp.y * heightFactor, temp.z);
            }
    }

    void CreateShape()
    {
        CreateVertices();
        AssignColours();
        ApplyHeightMap();
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
            tempList.Add(CreateVertex(iCount, zGridSize, zCurrentCounter));
        }
        zCurrentCounter++;

        vertices.Add(tempList);

        AssignColours();
        ApplyHeightMap();
        CreateIndices();
    }

    IEnumerator ContinousGeneration()
    {
        while (true)
        {
            if (continousGeneration)
            {
                MoveMesh();
                UpdateMesh();
            }
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

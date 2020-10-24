using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();
    
    private MeshFilter _filter;
    private Mesh _mesh;
    
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();
        
        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();
        
        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }
    private static Vector2[] edgeToVertecies = {
        new Vector2(0, 1),
        new Vector2(1, 2),
        new Vector2(2, 3),
        new Vector2(0, 3),
        new Vector2(4, 5),
        new Vector2(5, 6),
        new Vector2(6, 7),
        new Vector2(4, 7),
        new Vector2(0, 4),
        new Vector2(1, 5),
        new Vector2(2, 6),
        new Vector2(3, 7)
    };

    private List<Vector3> getCubeVertices(Vector3 zeroVertex, float D) {
        return new List<Vector3> {
            zeroVertex,
            zeroVertex + new Vector3(0, D, 0),
            zeroVertex + new Vector3(D, D, 0),
            zeroVertex + new Vector3(D, 0, 0),
            zeroVertex + new Vector3(0, 0, D),
            zeroVertex + new Vector3(0, D, D),
            zeroVertex + new Vector3(D, D, D),
            zeroVertex + new Vector3(D, 0, D)
        };
    }

    private Vector3 getPointByEdge(Vector2 edge, Vector3[] cubeVertices) {
        Vector3 a = cubeVertices[(int) edge.x];
        Vector3 b = cubeVertices[(int) edge.y];
        float Fa = Mathf.Abs(Field.F(a));
        float Fb = Mathf.Abs(Field.F(b)); 
        float c = Fa / (Fb + Fa);
        Vector3 result = a + (b - a) * c;
        return result;
    }

    private Vector3 getNormal(Vector3 vertex) {
        Vector3 dx = new Vector3(0.0001F, 0, 0);
        Vector3 dy = new Vector3(0, 0.0001F, 0);
        Vector3 dz = new Vector3(0, 0, 0.0001F);
        Vector3 result = new Vector3(
            Field.F(vertex - dx) - Field.F(vertex + dx),
            Field.F(vertex - dy) - Field.F(vertex + dy),
            Field.F(vertex - dz) - Field.F(vertex + dz)
            );
        result.Normalize();
        return result;
    }


    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    private void Update()
    {
        float L = 5F;
        float D = 0.1F;

        vertices.Clear();
        indices.Clear();
        normals.Clear();
        
        Field.Update();

        List<Vector3> resultVertecies = new List<Vector3>();
        List<int> resultTriangles = new List<int>();
        
        
        for (float x = -L; x < L; x += D) {
            for (float y = -L; y < L; y += D) {
                for (float z = -L; z < L; z += D) {
                    Vector3 zeroVertex = new Vector3(x, y, z);
                    Vector3[] cubeVertices = getCubeVertices(zeroVertex, D).ToArray();
                    byte case1 = 0;
                    for (int i = 0; i < cubeVertices.Length; i++) {
                        if (Field.F(cubeVertices[i]) > 0) case1 |= (byte) (1 << i);
                    }
            
                    int cnt = MarchingCubes.Tables.CaseToTrianglesCount[case1];
                    for (int i = 0; i < cnt; i++) {
                        int3 triangleEdges = MarchingCubes.Tables.CaseToVertices[case1][i]; 
                        int[] triangle = {triangleEdges.x, triangleEdges.y, triangleEdges.z};
                        int index = resultVertecies.Count;
                        for (int j = 0; j < triangle.Length; j++) {
                            resultTriangles.Add(index + j);
                            Vector3 point = getPointByEdge(edgeToVertecies[triangle[j]], cubeVertices);
                            resultVertecies.Add(point);
                        }
                    }
                }
            }
        }

        // ----------------------------------------------------------------
        // Generate mesh here. Below is a sample code of a cube generation.
        // ----------------------------------------------------------------

        // What is going to happen if we don't split the vertices? Check it out by yourself by passing
        // sourceVertices and sourceTriangles to the mesh.
        for (int i = 0; i < resultTriangles.Count; i++)
        {
            indices.Add(vertices.Count);
            Vector3 vertexPos = resultVertecies[resultTriangles[i]];
            
            vertices.Add(vertexPos);
            normals.Add(getNormal(vertexPos));
        }

        // Here unity automatically assumes that vertices are points and hence (x, y, z) will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        _mesh.SetNormals(normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }
}
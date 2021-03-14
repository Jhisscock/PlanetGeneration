using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class MeshGeneration : MonoBehaviour
{
    public List<Vector3> vertices;
    public List<Triangles> triangles;
    private Mesh mesh;
    public int lod;
    private float gr = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
    public float noiseX, noiseY, noiseScale;
    public float minHeight, maxHeight, heightScale;
    public float sinValue;
    public float RotationSpeed;
    private bool sineMode = false;
    public Gradient gradient;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Init();
    }

    void Init(){
        CreateIcosahedron(CreateQuad());
        Subdivide();
        UpdateMesh();
    }

    void InitSine(){
        CreateIcosahedron(CreateQuad());
        Subdivide();
        UpdateMeshSine();
    }

    Vector3[] CreateQuad(){
        Vector3[] verts = new Vector3[]{
            new Vector3(-gr, 0, -1),
            new Vector3(-gr, 0, 1),
            new Vector3(gr, 0, -1),
            new Vector3(gr, 0, 1)
        };
        
        return verts;
    }

    void CreateIcosahedron(Vector3[] verts){
        vertices = new List<Vector3>();

        Quaternion rotX = Quaternion.Euler(90f, 90f, 0f);
        Matrix4x4 mX = Matrix4x4.Rotate(rotX);
        Quaternion rotZ = Quaternion.Euler(0f, 90f, 90f);
        Matrix4x4 mZ = Matrix4x4.Rotate(rotZ);

        vertices.Add(verts[0].normalized);
        vertices.Add(verts[1].normalized);
        vertices.Add(verts[2].normalized);
        vertices.Add(verts[3].normalized);
        vertices.Add(mX.MultiplyPoint3x4(verts[0]).normalized);
        vertices.Add(mX.MultiplyPoint3x4(verts[1]).normalized);
        vertices.Add(mX.MultiplyPoint3x4(verts[2]).normalized);
        vertices.Add(mX.MultiplyPoint3x4(verts[3]).normalized);
        vertices.Add(mZ.MultiplyPoint3x4(verts[0]).normalized);
        vertices.Add(mZ.MultiplyPoint3x4(verts[1]).normalized);
        vertices.Add(mZ.MultiplyPoint3x4(verts[2]).normalized);
        vertices.Add(mZ.MultiplyPoint3x4(verts[3]).normalized);

        triangles = new List<Triangles>();

        triangles.Add(new Triangles(10, 1, 4));
        triangles.Add(new Triangles(10, 4, 11));
        triangles.Add(new Triangles(10, 11, 6));
        triangles.Add(new Triangles(10, 6, 0));
        triangles.Add(new Triangles(10, 0, 1));
        triangles.Add(new Triangles(11, 4, 3));
        triangles.Add(new Triangles(4, 1, 5));
        triangles.Add(new Triangles(1, 0, 8));
        triangles.Add(new Triangles(0, 6, 7));
        triangles.Add(new Triangles(6, 11, 2));
        triangles.Add(new Triangles(9, 3, 5));
        triangles.Add(new Triangles(9, 5, 8));
        triangles.Add(new Triangles(9, 8, 7));
        triangles.Add(new Triangles(9, 7, 2));
        triangles.Add(new Triangles(9, 2, 3));
        triangles.Add(new Triangles(5, 3, 4));
        triangles.Add(new Triangles(8, 5, 1));
        triangles.Add(new Triangles(7, 8, 0));
        triangles.Add(new Triangles(2, 7, 6));
        triangles.Add(new Triangles(3, 2, 11));
    }

    void Subdivide(){
        Dictionary<int, int> midPointCache = new Dictionary<int, int>();

        for(int i = 0; i < lod; i++){
            List<Triangles> newTris = new List<Triangles>();
            foreach(Triangles tri in triangles){
                int x = tri.mVertices[0];
                int y = tri.mVertices[1];
                int z = tri.mVertices[2];

                int xy = GetMidPointIndex(midPointCache, x, y);
                int yz = GetMidPointIndex(midPointCache, y, z);
                int zx = GetMidPointIndex(midPointCache, z, x);

                newTris.Add(new Triangles(x, xy, zx));
                newTris.Add(new Triangles(y, yz, xy));
                newTris.Add(new Triangles(z, zx, yz));
                newTris.Add(new Triangles(xy, yz, zx));
            }

            triangles = newTris;
        }
    }

    public int GetMidPointIndex (Dictionary<int, int> cache, int indexA, int indexB)
    {

        int smallerIndex = Mathf.Min (indexA, indexB);
        int greaterIndex = Mathf.Max (indexA, indexB);
        int key = (smallerIndex << 16) + greaterIndex;

        int ret;
        if (cache.TryGetValue (key, out ret)){
            return ret;
        }
        Vector3 p1 = vertices[indexA];
        Vector3 p2 = vertices[indexB];
        Vector3 middle = Vector3.Lerp(p1, p2, 0.5f).normalized;

        ret = vertices.Count;
        vertices.Add(middle);
        cache.Add(key, ret);
        return ret;
    }

    void UpdateMesh(){
        mesh.Clear();

        int[] tris = new int[triangles.Count * 3];

        Color[] colors = new Color[vertices.Count];
        int count = 0;

        for(int i = 0; i < triangles.Count * 3; i+=3){
            tris[i] = triangles[count].mVertices[0];
            tris[i + 1] = triangles[count].mVertices[1];
            tris[i + 2] = triangles[count].mVertices[2];
            count++;
        }

        for (int i = 0; i < vertices.Count; i++){
            float perlin = Mathf.PerlinNoise(vertices[i].x + noiseX * noiseScale + vertices[i].z, vertices[i].y + noiseY * noiseScale + vertices[i].z);
            Color noiseColor;
            Vector3 dir = transform.TransformPoint(vertices[i]) - transform.position;
            vertices[i] = dir * Mathf.Lerp(minHeight, maxHeight, perlin * heightScale);
            noiseColor = gradient.Evaluate(perlin * vertices[i].magnitude);
            colors[i] = noiseColor;
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = tris;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    void UpdateMeshSine(){
        mesh.Clear();

        int[] tris = new int[triangles.Count * 3];

        Color[] colors = new Color[vertices.Count];
        int count = 0;

        for(int i = 0; i < triangles.Count * 3; i+=3){
            tris[i] = triangles[count].mVertices[0];
            tris[i + 1] = triangles[count].mVertices[1];
            tris[i + 2] = triangles[count].mVertices[2];
            count++;
        }
        
        for (int i = 0; i < vertices.Count; i++){
            Vector3 dir = transform.TransformPoint(vertices[i]) - transform.position;
            vertices[i] = dir * (1 + Mathf.Sin(vertices[i].y * sinValue * Time.timeSinceLevelLoad) * 0.05f);
            colors[i] = Color.red;
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = tris;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    private void SaveMesh(){
        string path = EditorUtility.SaveFilePanel("Save Mesh Asset", "Assets/", name, "asset");
        // Path is empty if the user exits out of the window
        if(string.IsNullOrEmpty(path)) {
            return;
        }

        // Transforms the path to a full system path, to help minimize bugs
        path = FileUtil.GetProjectRelativePath(path);

        // Check if this path already contains a mesh
        // If yes, we want to replace that mesh with the baked mesh while keeping the same GUID,
        // so any other object using it will automatically update
        var oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(oldMesh != null) {
            // Clear all mesh data on the old mesh, readying it to receive new data
            oldMesh.Clear();
            // Copy mesh data from the new mesh to the old mesh
            EditorUtility.CopySerialized(mesh, oldMesh);
        } else {
            // Nothing is at this path (or it wasn't a mesh), so create a new asset
            AssetDatabase.CreateAsset(mesh, path);
        }

        // Tell Unity to save all assets
        AssetDatabase.SaveAssets();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.RightBracket)){
            lod++;
            Init();
        }
        if(Input.GetKeyDown(KeyCode.LeftBracket)){
            lod--;
            Init();
        }
        if(Input.GetKeyDown(KeyCode.R)){
            noiseX = Random.Range(0, 1000);
            noiseY = Random.Range(0, 1000);
            Init();
            sineMode = false;
        }
        if(Input.GetKeyDown(KeyCode.Q)){
            Init();
            sineMode = false;
        }
        if(Input.GetKeyDown(KeyCode.S)){
            sineMode = true;
        }
        if(Input.GetKeyDown(KeyCode.Return)){
            SaveMesh();
        }
        if(sineMode){
            lod = 4;
            InitSine();
        }

        transform.Rotate(Vector3.up * (RotationSpeed * Time.deltaTime));
    }
}

public class Triangles{
    public List<int> mVertices;
    public Triangles(int x, int y, int z){
        mVertices = new List<int>() {x,y,z};
    }
}

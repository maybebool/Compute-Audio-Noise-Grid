using UnityEngine;


public class CubeGenerator : MonoBehaviour {
    
    public ComputeShader computeShader;
    public int width = 10;
    public int length = 10;
    public float gap = 0.5f;
    public float seed;
    public float amplitude;
    public float seedChangeSpeed = 1.0f;
    public MeshFilter outMeshFilter;
    public AudioData audioData;

    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _indexBuffer;
    private ComputeBuffer _argsBuffer;
    private int _kernelHandle;
    private Mesh _mesh;
    
    private static readonly int Width = Shader.PropertyToID("width");
    private static readonly int Length = Shader.PropertyToID("length");
    private static readonly int Gap = Shader.PropertyToID("gap");
    private static readonly int Seed = Shader.PropertyToID("seed");
    private static readonly int Amplitude = Shader.PropertyToID("amplitude");
    private static readonly int AmplitudeBuffer = Shader.PropertyToID("amplitudeBuffer");
    private static readonly int VertexBuffer = Shader.PropertyToID("vertexBuffer");
    private static readonly int IndexBuffer = Shader.PropertyToID("indexBuffer");
    private static readonly int ArgsBuffer = Shader.PropertyToID("argsBuffer");


    private void Start() {
        _kernelHandle = computeShader.FindKernel("CSMain");

        var cubeCount = width * length;
        var vertexCount = cubeCount * 8;
        var indexCount = cubeCount * 36;

        // Create buffers
        _vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 6);
        _indexBuffer = new ComputeBuffer(indexCount, sizeof(uint));
        _argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

        // Set buffers and parameters
        computeShader.SetBuffer(_kernelHandle, VertexBuffer, _vertexBuffer);
        computeShader.SetBuffer(_kernelHandle, IndexBuffer, _indexBuffer);
        computeShader.SetBuffer(_kernelHandle, ArgsBuffer, _argsBuffer);
        computeShader.SetInt(Width, width);
        computeShader.SetInt(Length, length);
        computeShader.SetFloat(Gap, gap);

        // Create the mesh
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

    private void Update() {
        seed += Time.deltaTime * seedChangeSpeed;
        computeShader.SetFloat(Seed, seed);
        computeShader.SetFloat(AmplitudeBuffer, audioData.amplitudeBuffer);
        computeShader.SetFloat(Amplitude, amplitude);
        computeShader.Dispatch(_kernelHandle, width / 8 , length / 8, 1);
        UpdateMesh();
    }


    /// <summary>
    /// Updates the MeshFilter's mesh with the data from the ComputeBuffers.
    /// </summary>
    /// <remarks>
    /// This method retrieves the data from the ComputeBuffers and updates the MeshFilter's mesh.
    /// It first retrieves the data from the ComputeBuffers and converts it into arrays of vertices and indices.
    /// Then, it updates the vertices, normals, and triangles of the Mesh object.
    /// Finally, it assigns the updated mesh to the MeshFilter component of the GameObject.
    /// </remarks>
    private void UpdateMesh() {
        var cubeCount = width * length;
        var vertexCount = cubeCount * 8;
        var indexCount = cubeCount * 36;

        var vertices = new Vertex[vertexCount];
        _vertexBuffer.GetData(vertices);

        var indices = new uint[indexCount];
        _indexBuffer.GetData(indices);

        var meshVertices = new Vector3[vertexCount];
        var meshNormals = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++) {
            meshVertices[i] = vertices[i].Position;
            meshNormals[i] = vertices[i].Normal;
        }

        _mesh.vertices = meshVertices;
        _mesh.normals = meshNormals;
        _mesh.triangles = System.Array.ConvertAll(indices, i => (int)i);

        outMeshFilter.mesh = _mesh;
    }

    /// <summary>
    /// Called when the GameObject is being destroyed.
    /// Releases the ComputeBuffers used by the CubeGenerator.
    /// </summary>
    /// <remarks>
    /// This method releases the ComputeBuffers used by the CubeGenerator to avoid memory leaks.
    /// It is automatically called by Unity when the GameObject is being destroyed.
    /// </remarks>
    private void OnDestroy() {
        _vertexBuffer.Release();
        _indexBuffer.Release();
        _argsBuffer.Release();
    }

    struct Vertex {
        public Vector3 Position;
        public Vector3 Normal;
    }
}
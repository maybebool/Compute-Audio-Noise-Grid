using UnityEngine;

struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
}
public class CubeGenerator : MonoBehaviour
{
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
    
    
    private static readonly int VertexBuffer = Shader.PropertyToID("vertexBuffer");
    private static readonly int IndexBuffer = Shader.PropertyToID("indexBuffer");
    private static readonly int ArgsBuffer = Shader.PropertyToID("argsBuffer");
    private static readonly int Width = Shader.PropertyToID("width");
    private static readonly int Length = Shader.PropertyToID("length");
    private static readonly int Gap = Shader.PropertyToID("gap");
    private static readonly int Seed = Shader.PropertyToID("seed");
    private static readonly int AmplitudeBuffer = Shader.PropertyToID("amplitudeBuffer");
    private static readonly int Amplitude = Shader.PropertyToID("amplitude");


    private void Start()
    {
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

    private void Update()
    {
        seed += Time.deltaTime * seedChangeSpeed;
        computeShader.SetFloat(Seed, seed);
        computeShader.SetFloat(AmplitudeBuffer, audioData.amplitudeBuffer);
        computeShader.SetFloat(Amplitude, amplitude);
        computeShader.Dispatch(_kernelHandle, width, length, 1);
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        var cubeCount = width * length;
        var vertexCount = cubeCount * 8;
        var indexCount = cubeCount * 36;

        var vertices = new Vertex[vertexCount];
        _vertexBuffer.GetData(vertices);

        var indices = new uint[indexCount];
        _indexBuffer.GetData(indices);

        var meshVertices = new Vector3[vertexCount];
        var meshNormals = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            meshVertices[i] = vertices[i].Position;
            meshNormals[i] = vertices[i].Normal;
        }

        _mesh.vertices = meshVertices;
        _mesh.normals = meshNormals;
        _mesh.triangles = System.Array.ConvertAll(indices, i => (int)i);

        outMeshFilter.mesh = _mesh;
    }

    private void OnDestroy()
    {
        _vertexBuffer.Release();
        _indexBuffer.Release();
        _argsBuffer.Release();
    }
}
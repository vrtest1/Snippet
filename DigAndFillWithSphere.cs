using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DigAndFillWithSphere : MonoBehaviour
{
    [Header("掘るオブジェクト（Sphere Collider付き）")]
    public Transform diggerObject;
    public float digRadius = 0.1f;
    public float digStrength = 0.02f;

    [Header("盛るオブジェクト（Sphere Collider付き）")]
    public Transform fillerObject;
    public float fillRadius = 0.1f;
    public float fillStrength = 0.02f;
    public float maxHeight = 1.0f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] workingVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        workingVertices = (Vector3[])mesh.vertices.Clone();
    }

    void Update()
    {
        if (diggerObject != null)
        {
            ApplyDig(diggerObject.position);
        }

        if (fillerObject != null)
        {
            ApplyFill(fillerObject.position);
        }

        mesh.vertices = workingVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void ApplyDig(Vector3 worldPos)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldPos);

        for (int i = 0; i < workingVertices.Length; i++)
        {
            float dist = Vector3.Distance(workingVertices[i], localCenter);
            if (dist < digRadius)
            {
                float falloff = 1f - dist / digRadius;
                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                workingVertices[i].y -= digStrength * falloff;
            }
        }
    }

    void ApplyFill(Vector3 worldPos)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldPos);

        for (int i = 0; i < workingVertices.Length; i++)
        {
            float dist = Vector3.Distance(workingVertices[i], localCenter);
            if (dist < fillRadius)
            {
                float falloff = 1f - dist / fillRadius;
                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                float newY = workingVertices[i].y + fillStrength * falloff;

                if (newY <= maxHeight)
                {
                    workingVertices[i].y = newY;
                }
            }
        }
    }

    public void ResetMesh()
    {
        workingVertices = (Vector3[])originalVertices.Clone();
        mesh.vertices = workingVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

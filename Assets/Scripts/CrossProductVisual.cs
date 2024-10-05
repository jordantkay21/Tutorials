using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossProductVisual : MonoBehaviour
{
    public static CrossProductVisual Instance;

    public bool DisplayCrossProductViz;

    [Header("Debug Visual Cross Product")]
    public Transform CharacterForwardViz;
    public Transform CameraForwardViz;
    public Transform CrossProductViz;
    public Transform CharacterUpViz;
    public Transform NegCharacterUpViz;
    public Transform OriginViz;
    public GameObject PlayerCam_ParallelogramViz;
    public GameObject CrossPlayer_ParallelogramViz;
    public GameObject CrossCam_ParallelogramViz;

    public Color PositiveCrossColor;
    public Color NegativeCrossColor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        DebugManager.Instance.Log(LogSeverity.Debug, DebugCategories.ExecutionOrder, $"{this.GetType().Name} initialized");
    }

    private void Update()
    {
        if (DebugManager.Instance.DisplayCrossProductVisual)
        {

            CharacterForwardViz.gameObject.SetActive(true);
            CameraForwardViz.gameObject.SetActive(true);
            CrossProductViz.gameObject.SetActive(true);
            CharacterUpViz.gameObject.SetActive(true);
            NegCharacterUpViz.gameObject.SetActive(true);
            OriginViz.gameObject.SetActive(true);
            PlayerCam_ParallelogramViz.SetActive(true);
            CrossPlayer_ParallelogramViz.SetActive(true);
            CrossCam_ParallelogramViz.SetActive(true);
        }
        else
        {
            CharacterForwardViz.gameObject.SetActive(false);
            CameraForwardViz.gameObject.SetActive(false);
            CrossProductViz.gameObject.SetActive(false);
            CharacterUpViz.gameObject.SetActive(false);
            NegCharacterUpViz.gameObject.SetActive(false);
            OriginViz.gameObject.SetActive(false);
            PlayerCam_ParallelogramViz.SetActive(false);
            CrossPlayer_ParallelogramViz.SetActive(false);
            CrossCam_ParallelogramViz.SetActive(false);
        }
    }

    public void DisplayCrossProductVisual(float sign, Vector3 camForwardProjectedXZ, Vector3 crossProduct, Vector3 playerForward, Vector3 playerUp)
    {
        if (DebugManager.Instance.DisplayCrossProductVisual)
        {
            Color parallelogramColor = (sign >= 0) ? PositiveCrossColor : NegativeCrossColor;

            CameraForwardViz.position = camForwardProjectedXZ;
            CharacterForwardViz.position = playerForward;
            CrossProductViz.position = crossProduct;
            CharacterUpViz.position = playerUp;
            NegCharacterUpViz.position = -playerUp;

            VisualizeVector(CameraForwardViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, camForwardProjectedXZ, Color.blue);
            VisualizeVector(CharacterForwardViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, playerForward, Color.green);
            VisualizeVector(CrossProductViz.gameObject.GetComponent<LineRenderer>(), OriginViz.position, crossProduct, Color.red);

            // Visualize parallelogram
            DrawParallelogram(OriginViz.position, playerForward, camForwardProjectedXZ, parallelogramColor, sign, PlayerCam_ParallelogramViz: PlayerCam_ParallelogramViz);
            DrawParallelogram(OriginViz.position, crossProduct, playerForward, parallelogramColor, sign, CrossPlayer_ParallelogramViz: CrossPlayer_ParallelogramViz);
            DrawParallelogram(OriginViz.position, crossProduct, camForwardProjectedXZ, parallelogramColor, sign, CrossCam_ParallelogramViz: CrossCam_ParallelogramViz);
        }

    }

    #region Visual Helpers
    void VisualizeVector(LineRenderer lr, Vector3 origin, Vector3 direction, Color color)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + direction);
        lr.startWidth = .03f;
        lr.endWidth = .03f;
    }
    void DrawParallelogram(Vector3 origin, Vector3 vecA, Vector3 vecB, Color parallelogramColor, float sign, GameObject PlayerCam_ParallelogramViz = null, GameObject CrossPlayer_ParallelogramViz = null, GameObject CrossCam_ParallelogramViz = null)
    {
        // Define the four corners of the parallelogram
        Vector3[] vertices = new Vector3[4];
        vertices[0] = origin;
        vertices[1] = origin + vecA;
        vertices[2] = origin + vecA + vecB;
        vertices[3] = origin + vecB;

        // Create the parallelogram mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;

        int[] triangles;
        if (sign >= 0)
        {
            // Original winding order
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
        }
        else
        {
            //Reverse the winding order
            triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
        }
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        GameObject targetParallelogramViz = null;

        if (CrossCam_ParallelogramViz != null)
            targetParallelogramViz = CrossCam_ParallelogramViz;
        else if (CrossPlayer_ParallelogramViz != null)
            targetParallelogramViz = CrossPlayer_ParallelogramViz;
        else
            targetParallelogramViz = PlayerCam_ParallelogramViz;

        if (targetParallelogramViz == null)
        {
            Debug.LogError("No ParallelogramViz GameObject assigned.");
            return;
        }

        // Update or add MeshFilter and MeshRenderer
        MeshFilter mf = targetParallelogramViz.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = targetParallelogramViz.AddComponent<MeshFilter>();
        }

        mf.mesh = mesh;

        MeshRenderer mr = targetParallelogramViz.GetComponent<MeshRenderer>();
        if (mr == null)
            mr = targetParallelogramViz.AddComponent<MeshRenderer>();

        // Set material and color
        if (mr.material == null)
        {
            mr.material = new Material(Shader.Find("Standard"));
        }

        mr.material.color = parallelogramColor;
    }
    #endregion
}

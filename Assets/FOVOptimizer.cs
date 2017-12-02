using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FOVOptimizer : MonoBehaviour
{

    // Use this for initialization
    public GraphicsEngine m_engine;
    public Texture2D m_Img;
    public Canvas m_canvas;

    [HideInInspector]
    public bool IsUpdatedMesh;
    [HideInInspector]
    public float m_fov_best;
    [HideInInspector]
    public float m_iou_best;
    [HideInInspector]
    public FOVCaptorType fovCaptorType;


    private Camera m_mainCamera;
    private Camera m_meshCaptor;

    private int iter;
    private List<float> m_fov_list, m_iou_list;

    private bool IsOptimalFOV;

    private Rect canvasPlane2D;

    [HideInInspector]
    public enum FOVCaptorType { waiteFor, isPrepared, isDone, isFinished };
    // isDone: this fov optimal is done
    // isFinish: all fov optimal is done

    void Start()
    {
        m_mainCamera = m_engine.m_projector.m_mainCamera;
        m_meshCaptor = m_engine.m_meshCaptor;
        //m_fov_list = new List<float>() { 1, 30};
        m_fov_list = new List<float>() {20};
        m_iou_list = new List<float>();
        canvasPlane2D = m_canvas.pixelRect;
        IsOptimalFOV = true;

        InitOptimizer();
    }
    // Update is called once per frame
    void Update()
    {
        if (!IsOptimalFOV) return;
        if (fovCaptorType == FOVCaptorType.isDone) return;
        if (fovCaptorType == FOVCaptorType.isFinished) return;

        switch (fovCaptorType)
        {
            case FOVCaptorType.waiteFor:
                break;
            case FOVCaptorType.isPrepared:
                Texture2D meshMask = CaptureMeshMask();
                EvaluateMesh(meshMask, m_Img);
                RecoverFromEvaluation();
                break;
            case FOVCaptorType.isFinished:
                break;
            default:
                Debug.Log("Error Happend in FOV!");
                break;
        }
    }

    public void InitOptimizer()
    {
        fovCaptorType = FOVCaptorType.waiteFor;
        IsUpdatedMesh = false;
        m_fov_list[0] = m_mainCamera.fieldOfView;
        m_iou_best = 0;
        m_fov_best = m_mainCamera.fieldOfView;
        iter = 0;

        m_iou_list.Clear();
    }
    public void SetFOV(float fov)
    {
        m_mainCamera.fieldOfView = fov;
        m_meshCaptor.fieldOfView = fov;
    }
    public void EvaluateMesh(Texture2D meshMask, Texture2D netMask)
    {
        // Fix first fov
        if (iter == 0) m_fov_list[iter] = m_mainCamera.fieldOfView;

        // Check Img Size corresponding
        if (meshMask.texelSize != netMask.texelSize)
        {
            Debug.Log("Error: these two texture in FOVOptimizer EvaluationMesh not correspond!");
            return;
        }

        // Get IoU
        int intersection = 0;
        int union = 0;
        float MeshDefault = 21f / 255f;
        for (int r = 0; r < meshMask.height; r++)
        {
            for (int c = 0; c < meshMask.width; c++)
            {
                float netDefault = 0f;
                if (meshMask.GetPixel(c, r) == new Color(MeshDefault, MeshDefault, MeshDefault)
                    && netMask.GetPixel(c, r) == new Color(netDefault, netDefault, netDefault))
                { }
                else if (meshMask.GetPixel(c, r) != new Color(MeshDefault, MeshDefault, MeshDefault)
                    && netMask.GetPixel(c, r) != new Color(netDefault, netDefault, netDefault))
                {
                    intersection++;
                    union++;
                }
                else
                    union++;
            }
        }
        float IOU = intersection / (float)union;
        m_iou_list.Add(IOU);

        // Compute IsUpdatedMesh and fov
        if (IOU < 0.95)
        {
            IsUpdatedMesh = m_iou_list[iter] >= m_iou_list[Mathf.Max(iter - 1, 0)] ? true : false;
            UpdateBestFOV(iter);
        }
        else
            IsUpdatedMesh = true;

        if(!m_engine.m_is_quiet||
            !(m_engine.exper_isCubeExper && m_engine.exper_isCubeExper)) 
            Debug.Log("FOV: " + m_fov_list[iter] + "   IOU: " + IOU + "   UpdateMesh: " + IsUpdatedMesh
            + "   Best FOV: " + m_fov_best);

        // Make Reconstruction Decision
           // Least requirement  &&  iter right
        if (IOU < 0.95 && iter < m_fov_list.Count - 1)
        {
            UpdateFOV();
            //Canvas.ForceUpdateCanvases();
            //SceneView.RepaintAll();
            //m_engine.m_projector.ForceUpdateCanvasFromFOV();
            m_engine.ReconstructAllGeometries();
            //m_engine.Invoke_ReconstructAllGeometries();
        }
        else
        {
            UpdateBestFOV(iter);
            fovCaptorType = FOVCaptorType.isFinished;
        }
    }
    public void PrepareForEvaluation()
    {
        if (!IsOptimalFOV) return;

        // Prepare for capture
        m_engine.m_renderEngine.isRenderingGizmos = false;

        // Prepare for fov engine
        fovCaptorType = FOVCaptorType.isPrepared;
    }
    private void RecoverFromEvaluation()
    {
        // Recover for capture
        m_engine.m_renderEngine.isRenderingGizmos = true;

        // Recover for fov engine
        if (fovCaptorType!= FOVCaptorType.isFinished)
            fovCaptorType = FOVCaptorType.isDone;
    }
    public Texture2D CaptureMeshMask(string path = null)
    {
        int w = m_Img.width;
        int h = m_Img.height;
        Texture2D meshMask = new Texture2D(w, h, TextureFormat.RGB24, false);
        RenderTexture rt = new RenderTexture((int)canvasPlane2D.width, (int)canvasPlane2D.height, 1);

        m_meshCaptor.pixelRect = canvasPlane2D;
        m_meshCaptor.targetTexture = rt;
        m_meshCaptor.Render();
        RenderTexture.active = rt;
        meshMask.ReadPixels(new Rect((canvasPlane2D.width - w) / 2, 0, w, h), 0, 0);
        //meshMask.Apply();

        m_meshCaptor.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(rt);

        //SaveImg(meshMask, path);
        return meshMask;
    }
    public void UpdateFOV()
    {
        iter++;
        SetFOV(m_fov_list[iter]);
    }
    public void UpdateBestFOV(int iter)
    {
        if (m_iou_list[iter] >= m_iou_best)
        {
            m_fov_best = m_fov_list[iter];
            m_iou_best = m_iou_list[iter];
        }
    }
    public void SaveImg(Texture2D img, string path = null)
    {
        byte[] bytes = img.EncodeToPNG();
        // Save In "Assets/Imgs" Path
        string filename = path == null? 
            Application.dataPath + "/Imgs/Img"+ System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png"
            :path;
        while(!File.Exists(filename))
        {
            File.WriteAllBytes(filename, bytes);
        }

    }
    #region Backup Save Img
    ////把摄像头视野 打印出png图片  
    //private Rect CutRect = new Rect(0, 0, 1, 1);
    //private void MakeCameraImg(Camera mCam, int width, int height)
    //{
    //    Image mImage;
    //    RenderTexture rt = new RenderTexture(width, height, 2);
    //    mCam.pixelRect = new Rect(0, 0, Screen.width, Screen.height);
    //    mCam.targetTexture = rt;
    //    Texture2D screenShot = new Texture2D((int)(width * CutRect.width), (int)(height * CutRect.height),
    //                                             TextureFormat.RGB24, false);
    //    mCam.Render();
    //    RenderTexture.active = rt;
    //    screenShot.ReadPixels(new Rect(width * CutRect.x, width * CutRect.y, width * CutRect.width, height * CutRect.height), 0, 0);
    //    mCam.targetTexture = null;
    //    RenderTexture.active = null;
    //    UnityEngine.Object.Destroy(rt);
    //    byte[] bytes = screenShot.EncodeToPNG();
    //    string filename = Application.dataPath + "/Imgs/Img"
    //                      + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
    //    System.IO.File.WriteAllBytes(filename, bytes);

    //    //mImage.;
    //    //return mImage;
    //}
    #endregion
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using NumericalRecipes;
using MyGeometry;
using UnityEditor;
using UnityExtension;
using System;

public class GraphicsEngine : MonoBehaviour
{
    public Projector m_projector;
    public RenderEngine m_renderEngine;
    public FOVOptimizer m_fovOptimizer;
    public GameObject m_meshViewer;
    public Camera m_meshCaptor;
    public bool m_is_quiet;

    [HideInInspector]
    public List<FaceEngine> m_faceEngine_list = new List<FaceEngine>();
    [HideInInspector]
    public List<BodyEngine> m_bodyEngine_list = new List<BodyEngine>();
    [HideInInspector]
    public List<List<BodyEngine>> m_handleEngine_lists = new List<List<BodyEngine>>();
    [HideInInspector]
    public List<List<GameObject>> m_handleViewer_lists = new List<List<GameObject>>();
    [HideInInspector]
    public List<GameObject> m_Inst_list = new List<GameObject>();
    [HideInInspector]
    public List<GameObject> m_meshViewer_list = new List<GameObject>();
    [HideInInspector]
    public List<GameObject> m_meshViewer_best_list = new List<GameObject>();
    [HideInInspector]
    public FaceEngine m_faceEngine_temp;
    [HideInInspector]
    public BodyEngine m_bodyEngine_temp;
    //Image<Gray, byte> faceImg = null;
    //Image<Gray, byte> bodyImg = null;

    [HideInInspector]
    public Image<Rgb, byte> m_OrigImg;
    public Texture2D m_Img, m_MaskImg;
    [HideInInspector]
    public Image<Gray, byte> m_LineImg;
    [HideInInspector]
    public List<List<Vector2>> m_LineData;
    [HideInInspector]
    public string m_ImgFolder,
        m_ImgName,
        m_ImgPath,
        m_prefab_path,
        m_obj_path;

    [HideInInspector]
    public bool be_isSymmetry;
    [HideInInspector]
    public bool be_isSnap;
    [HideInInspector]
    public bool be_isStop;
    [HideInInspector]
    public float be_bottomCover;
    [HideInInspector]
    public float be_offsetScale;

    // Experiment Params
    [HideInInspector]
    public bool exper_isCubeExper;
    [HideInInspector]
    public bool exper_isMultiExper;
    [HideInInspector]
    public FileStream m_fs = null;
    [HideInInspector]
    public bool is_experiment_finished = false;

    Camera m_mainCamera = null;
    List<Geometry> geometries = new List<Geometry>();

    private void Awake()
    {
        m_is_quiet = true;
        m_prefab_path = "Assets/Output_prefab";
        //m_obj_path = "ExportedObj";
        m_mainCamera = m_projector.m_mainCamera;

        //m_faceEngine_temp = new FaceEngine();
        //m_faceEngine_temp.CreateFaceEngine(this, m_projector, m_renderEngine);
        //m_bodyEngine_temp = new BodyEngine();
        //m_bodyEngine_temp.CreateBodyEngine(this, m_projector, m_renderEngine, m_faceEngine_temp);

        m_fovOptimizer.m_engine = this;
        exper_isCubeExper = false;
        exper_isMultiExper = false;
        m_meshCaptor.fieldOfView = m_projector.m_mainCamera.fieldOfView;
    }
    private void Start()
    {
    }
    // This is for new input
    public void InitEngine()
    {
        UpdateEngine();

        // Delete Final Result
        ClearGameObjectList(m_meshViewer_best_list);

        // Clear FOV Optimizer
        this.m_fovOptimizer.InitOptimizer();
    }
    private void UpdateEngine()
    {
        //Update MeshList
        UpdateFinalMesh();

        // Recover Stop flag
        be_isStop = false;

        // Clear renderdata
        m_renderEngine.ClearAll();

        // Clear gameobject and 3 lists

        foreach (Transform child in this.gameObject.transform)
            Destroy(child.gameObject);
        ClearGameObjectList(m_meshViewer_list);
        ClearGameObjectList(m_handleViewer_lists);

        m_faceEngine_list.Clear();
        m_bodyEngine_list.Clear();
        m_handleEngine_lists.Clear();
    }
    private void UpdateFinalMesh()
    {
        if (!this.m_fovOptimizer.IsUpdatedMesh) return;
        if (m_meshViewer_list.Count == 0) return;

        // Clear Orig Data
        ClearGameObjectList(m_meshViewer_best_list);

        foreach (var go in m_meshViewer_list)
        {
            // Creat MeshViwer
            GameObject new_go = GameObject.Instantiate(go);
            new_go.name = new_go.name.Substring(0, new_go.name.Length - 7) + "_best";
            new_go.SetActive(false);
            this.m_meshViewer_best_list.Add(new_go);
        }

        foreach (var gol in m_handleViewer_lists)
        {
            foreach (var go in gol)
            {
                // Creat MeshViwer
                GameObject new_go = GameObject.Instantiate(go);
                new_go.name = new_go.name.Substring(0, new_go.name.Length - 7) + "_best";
                new_go.SetActive(false);
                this.m_meshViewer_best_list.Add(new_go);
            }
        }
    }
    private void ClearGameObjectList(List<GameObject> gol)
    {
        foreach (var go in gol)
        {
            Destroy(go);
        }
        gol.Clear();
    }
    private void ClearGameObjectList(List<List<GameObject>> gols)
    {
        foreach (var gol in gols)
        {
            ClearGameObjectList(gol);
        }
        gols.Clear();
    }
    private void Update()
    {
        if (CheckBodyEngineEnd())
        {
            bool existHandle = false;
            foreach (var he_list in m_handleEngine_lists)
            {
                foreach (var he in he_list)
                {
                    FitHandle(he);
                    existHandle = true;
                }
            }
            if (existHandle)
            {
                foreach (var be in m_bodyEngine_list)
                {
                    be.isDetectingCenter = BodyEngine.DetectStatus.NotDetect;
                }
            }
        }

        if (CheckAllEngineEnd())
        {
            m_fovOptimizer.PrepareForEvaluation();
        }

        if (m_fovOptimizer.fovCaptorType == FOVOptimizer.FOVCaptorType.isFinished)
        {
            // Reset fovCaptorType
            m_fovOptimizer.fovCaptorType = FOVOptimizer.FOVCaptorType.waiteFor;
            
            // Update Engine
            UpdateFinalMesh();

            // Show Result
            foreach (var go in m_meshViewer_best_list) go.SetActive(true);
            ClearGameObjectList(m_meshViewer_list);
            //m_fovOptimizer.SetFOV(m_fovOptimizer.m_fov_best);
            ClearGameObjectList(m_handleViewer_lists);

            SaveAllMesh();
            if (exper_isCubeExper) SaveInstanceSeg();
            is_experiment_finished = true;
        }
    }
    public bool CheckAllEngineEnd()
    {
        if (m_bodyEngine_list.Count == 0) return false;

        bool existHandle = false;
        foreach (var ge_list in m_handleEngine_lists)
        {
            foreach (var ge in ge_list)
            {
                if (ge.isDetectingCenter != BodyEngine.DetectStatus.Isdone) return false;
                existHandle = true;
            }
        }

        if (!existHandle)
        {
            foreach (var be in m_bodyEngine_list)
            {
                if (be.isDetectingCenter != BodyEngine.DetectStatus.Isdone)
                {
                    return false;
                }
            }
        }


        foreach (var ge_list in m_handleEngine_lists)
        {
            foreach (var ge in ge_list)
            {
                ge.isDetectingCenter = BodyEngine.DetectStatus.NotDetect;
            }
        }
        foreach (var be in m_bodyEngine_list)
        {
            be.isDetectingCenter = BodyEngine.DetectStatus.NotDetect;
        }
        return true;
    }
    public bool CheckAllEngineNoWorking()
    {
        if (m_bodyEngine_list.Count == 0) return true;

        bool existHandle = false;
        foreach (var ge_list in m_handleEngine_lists)
        {
            foreach (var ge in ge_list)
            {
                if (ge.isDetectingCenter != BodyEngine.DetectStatus.NotDetect) return false;
                existHandle = true;
            }
        }

        if (!existHandle)
        {
            foreach (var be in m_bodyEngine_list)
            {
                if (be.isDetectingCenter != BodyEngine.DetectStatus.NotDetect)
                {
                    return false;
                }
            }
        }
        return true;
    }
    public bool CheckBodyEngineEnd()
    {
        if (m_bodyEngine_list.Count == 0) return false;

        foreach (var be in m_bodyEngine_list)
        {
            if (be.isDetectingCenter != BodyEngine.DetectStatus.Isdone)
            {
                return false;
            }
        }

        return true;
    }
    public void FitTopCircle(FaceEngine faceEngine, Image<Gray, byte> img)
    {
        faceEngine.SolveCircle(img);
    }
    public void FitTopRect(FaceEngine faceEngine, Image<Gray, byte> img)
    {
        faceEngine.SolveRect(img);
    }
    public void FitBody(BodyEngine bodyEngine)
    {
        bodyEngine.SolveBody();
    }
    public void FitHandle(BodyEngine handleEngine)
    {
        handleEngine.SolveBody();
    }
    public void ChangeCameraFOV()
    {
        m_mainCamera.fieldOfView = 10;
    }
    public void UpdataFileName(string FolderPath = "", string ImgName = "")
    {
        m_ImgFolder = FolderPath;
        m_ImgName = ImgName;

        int Str_index = m_ImgFolder.IndexOf("ImageData");
        int Str_len = m_ImgFolder.Length - m_ImgFolder.IndexOf("ImageData");
        m_ImgPath = Path.Combine(m_ImgFolder.Substring(Str_index, Str_len), ImgName);
    }
    public void SaveAllMesh()
    {
        int i_inst = 0;
        foreach (var inst in m_meshViewer_best_list)
        {
            i_inst++;
            SavePrefab(inst, i_inst);
        }
        Selection.objects = m_meshViewer_best_list.ToArray();
        EditorObjExporter.ExportWholeSelectionToSingle();
        //EditorObjExporter.ExportEachSelectionToSingle(m_obj_path);
    }
    public void SavePrefab(GameObject g, int i_inst)
    {
        // Save Mesh .asset
        Mesh mesh = g.GetComponent<MeshFilter>().mesh;
        string ObjectPath = m_prefab_path + "/" + g.name + ".asset";
        AssetDatabase.CreateAsset(mesh, ObjectPath);
        AssetDatabase.SaveAssets();

        // Save Material .mat
        string MaterialPath = m_prefab_path + "/" + g.name + ".mat";
        AssetDatabase.CreateAsset(g.GetComponent<MeshRenderer>().material, MaterialPath);
        AssetDatabase.SaveAssets();

        // Save Prefab .prefab
        string PrefabPath = m_prefab_path + "/" + g.name + ".prefab";
        UnityEngine.Object prefab = PrefabUtility.CreatePrefab(PrefabPath, g);
    }
    public void SaveOBJ(GameObject g, int i_inst)
    {
        Selection.activeGameObject = g;
        EditorObjExporter.ExportEachSelectionToSingle();
    }
    public void SaveCubeCorner(List<Quad> rects)
    {
        m_renderEngine.ClearAll();
        if (rects == null) { Debug.Log("Error: The cube data is not right!"); return; }

        StreamWriter sw = new StreamWriter(m_fs);

        Vector2[] corners = rects.Count > 1 ? new Vector2[8] : new Vector2[4];
        Array.Copy(m_projector.Proj3dToImage(rects[0].CornerPoints3d).ToArray(), 0, corners, 0, 4);
        if(rects.Count> 1) Array.Copy(m_projector.Proj3dToImage(rects[rects.Count-1].CornerPoints3d).ToArray(), 0, corners, 4, 4);
        // Re-scale
        for (int i = 0; i < corners.Length; i++)
        {
            m_renderEngine.DrawPoint(corners[i], UnityEngine.Color.red);
            corners[i].x = corners[i].x / m_Img.width * m_OrigImg.Width;
            corners[i].y = corners[i].y / m_Img.height* m_OrigImg.Height;
            sw.WriteLine(corners[i].x + " " + corners[i].y);
        }

        sw.Flush();
    }
    public void SaveInstanceSeg()
    {
        string curr_path = System.Environment.CurrentDirectory;
        string folderPath = curr_path + "/Assets/Resources/ImageData/Experiments_Cube/Result";
        string filePath = folderPath + "/" + m_ImgName + "_seg.png";
        Texture2D[]  masks = new Texture2D[m_meshViewer_best_list.Count];
        foreach (var go in m_meshViewer_best_list)  go.SetActive(false);
        m_renderEngine.isRenderingGizmos = false;

        for (int i = 0; i < m_meshViewer_best_list.Count; i++)
        {
            m_meshViewer_best_list[i].SetActive(true);
            masks[i] = m_fovOptimizer.CaptureMeshMask();
            m_meshViewer_best_list[i].SetActive(false);
        }
        foreach (var go in m_meshViewer_best_list) go.SetActive(true);
        m_renderEngine.isRenderingGizmos = true;

        // Merge Mask
        Texture2D mask = new Texture2D(m_Img.width, m_Img.height);
        for (int w = 0; w < mask.width; w++)
            for (int h = 0; h < mask.height; h++)
                 mask.SetPixel(w, h, new UnityEngine.Color(0,0,0));
        float MeshDefault = 21f / 255f;

        for (int i = 0; i < masks.Length; i++)
        {
            for (int w = 0; w < mask.width; w++)
            {
                for (int h = 0; h < mask.height; h++)
                {
                    if (masks[i].GetPixel(w, h) != new UnityEngine.Color(MeshDefault, MeshDefault, MeshDefault))
                        mask.SetPixel(w, h, new UnityEngine.Color((i + 1) * 10f / 255f, 0, 1f));
                }
            }
        }
        Image<Rgb, byte> img_mask = UnityExtension.IExtension.Texture2DToImage(mask);
        img_mask.Save(filePath);
        //m_fovOptimizer.SaveImg(mask, filePath);
    }
    public void ParseRawMask(Image<Rgb, byte> img)
    {
        MaskParser mp = new MaskParser(img);
        this.geometries = mp.Parse();
    }
    public void Invoke_ReconstructAllGeometries()
    {
        Invoke("ReconstructAllGeometries", 0.05f);
    }
    public void ReconstructAllGeometries()
    {
        UpdateEngine();

        int i_inst = 0;
        
        // Fix null detection result
        if (geometries.Count == 0 && exper_isCubeExper) SaveInstanceSeg();
        foreach (var geo in geometries)
        {
            i_inst++;
            // Creat GameObject
            GameObject inst = new GameObject("Instance" + i_inst);
            inst.transform.parent = this.gameObject.transform;
            this.m_Inst_list.Add(inst);

            // Creat FaceEngine
            FaceEngine faceEngine = AttchFaceEngineToObject(inst, m_projector, m_renderEngine);
            List<Image<Rgb, byte>> face_imgs = geo.GetFaceImage();
            List<Image<Rgb, byte>> handle_imgs = geo.GetHandleImage();
            this.m_faceEngine_list.Add(faceEngine);

            // Creat MeshViwer
            GameObject meshViewer = GameObject.Instantiate(m_meshViewer);
            meshViewer.name = m_ImgName + '_' + i_inst;
            this.m_meshViewer_list.Add(meshViewer);

            if (geo.label == Label.Cube)
            {
                Image<Rgb, byte> body_img = geo.GetBodyImage(true);

                // skeleton extractor
                SkeletonExtractor se = new SkeletonExtractor(geo.GetBodyImage(), face_imgs[0], i_inst);
                bool iscurve = false;
                List<Vector2> axis = se.ExtractCubeSkeleton(out iscurve);
                faceEngine.axis = axis;
                faceEngine.body_img = body_img.Convert<Gray, byte>();
                this.FitTopRect(faceEngine, face_imgs[0].Convert<Gray, byte>());
                BodyEngine bodyEngine = AttchBodyEngineToObject(inst, meshViewer, this, m_projector, m_renderEngine, faceEngine, axis, iscurve);
                this.m_bodyEngine_list.Add(bodyEngine);
                //---------------------------------------------------
                //this.FitBody(bodyEngine);
                //---------------------------------------------------
                
                // Handle
                List<BodyEngine> m_handleEngine_list = new List<BodyEngine>();
                List<GameObject> m_handleViewer_list = new List<GameObject>();
                int handle_i_inst = 0;
                foreach (Image<Rgb, byte> hi in handle_imgs)
                {
                    GameObject handleViewer = GameObject.Instantiate(m_meshViewer);
                    handleViewer.name = m_ImgName + '_' + i_inst + "_handle" + handle_i_inst;
                    m_handleViewer_list.Add(handleViewer);

                    SkeletonExtractor seh = new SkeletonExtractor(handle_i_inst);
                    seh.SkeletonExtractorHandle(geo.GetBodyImage(), hi);
                    bool ishcurve = false;
                    List<Vector2> axis_h = seh.ExtractSkeleton(out ishcurve);

                    BodyEngine handleEngine = AttchHandleEngineToObject(inst, handleViewer, this,
                        m_projector, m_renderEngine, faceEngine, bodyEngine,
                        hi.Convert<Gray, byte>(), axis_h, ishcurve);
                    m_handleEngine_list.Add(handleEngine);
                    handle_i_inst++;
                }
                this.m_handleViewer_lists.Add(m_handleViewer_list);
                this.m_handleEngine_lists.Add(m_handleEngine_list);
            }
            else if (geo.label == Label.Cylinder)
            {
                List<List<Vector2>> face_points = geo.Faces();
                double minerror = double.MaxValue;
                int best_face_idx = 0;
                for (int i = 0; i < face_points.Count; i++)
                {
                    double error = faceEngine.LikeAEllipse(face_points[i]);
                    if (error < minerror)
                    {
                        minerror = error;
                        best_face_idx = i;
                    }
                }
                Image<Rgb, byte> body_img = geo.GetBodyImage(true);
                //body_img = MaskParser.FillBodyWithHandle(body_img, handle_imgs);

                // skeleton extractor
                bool iscurve = false;
                SkeletonExtractor se = new SkeletonExtractor(geo.GetBodyImage(), face_imgs, best_face_idx, i_inst);
                List<Vector2> axis = se.ExtractSkeleton(out iscurve);
                faceEngine.axis = axis;
                faceEngine.body_img = body_img.Convert<Gray, byte>();

                this.FitTopCircle(faceEngine, face_imgs[best_face_idx].Convert<Gray, byte>());
                BodyEngine bodyEngine = AttchBodyEngineToObject(inst, meshViewer, this, m_projector, m_renderEngine, faceEngine, axis, iscurve);

                this.m_bodyEngine_list.Add(bodyEngine);
                //---------------------------------------------------
                //this.FitBody(bodyEngine);
                //---------------------------------------------------

                // Handle
                List<BodyEngine> m_handleEngine_list = new List<BodyEngine>();
                List<GameObject> m_handleViewer_list = new List<GameObject>();
                int handle_i_inst = 0;
                foreach (Image<Rgb, byte> hi in handle_imgs)
                {
                    GameObject handleViewer = GameObject.Instantiate(m_meshViewer);
                    handleViewer.name = m_ImgName + '_' + i_inst + "_handle" + handle_i_inst;
                    m_handleViewer_list.Add(handleViewer);

                    SkeletonExtractor seh = new SkeletonExtractor(handle_i_inst);
                    seh.SkeletonExtractorHandle(geo.GetBodyImage(), hi);
                    bool ishcurve = false;
                    List<Vector2> axis_h = seh.ExtractSkeleton(out ishcurve);

                    BodyEngine handleEngine = AttchHandleEngineToObject(inst, handleViewer, this,
                        m_projector, m_renderEngine, faceEngine, bodyEngine,
                        hi.Convert<Gray, byte>(), axis_h, ishcurve);
                    m_handleEngine_list.Add(handleEngine);
                    handle_i_inst++;
                }
                this.m_handleViewer_lists.Add(m_handleViewer_list);
                this.m_handleEngine_lists.Add(m_handleEngine_list);
            }
        }
////        if(m_bodyEngine_list.Count>1)
//        {
//            FaceEngineMulti m_FEM = new FaceEngineMulti(this, m_projector, m_renderEngine, m_faceEngine_list);
//            m_FEM.Optimize_allRects();
//        }

        foreach (var be in m_bodyEngine_list)
        {
            this.FitBody(be);
        }
    }
    public FaceEngine AttchFaceEngineToObject(GameObject gb, Projector proj, RenderEngine re)
    {
        FaceEngine fe = gb.AddComponent<FaceEngine>() as FaceEngine;
        fe.CreateFaceEngine(this, proj, re);
        return fe;
    }
    public BodyEngine AttchBodyEngineToObject(GameObject gb, GameObject viewer, GraphicsEngine ge, Projector proj, RenderEngine re, FaceEngine fe,
        List<Vector2> axis, bool iscurve)
    {
        BodyEngine be = gb.AddComponent<BodyEngine>() as BodyEngine;
        be.CreateBodyEngine(viewer, ge, proj, re, fe, axis, iscurve);
        return be;
    }
    public BodyEngine AttchHandleEngineToObject(GameObject gb, GameObject viewer, GraphicsEngine ge, Projector proj, RenderEngine re, FaceEngine fe, BodyEngine be,
        Image<Gray, byte> img, List<Vector2> axis, bool iscurve)
    {
        BodyEngine he = gb.AddComponent<BodyEngine>() as BodyEngine;
        bool isHandle = true;
        he.CreateHandleEngine(viewer, ge, proj, re, fe, be, axis, img, iscurve, isHandle);
        return he;
    }
}
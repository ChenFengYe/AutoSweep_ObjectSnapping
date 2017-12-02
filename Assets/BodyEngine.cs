using Emgu.CV;
using Emgu.CV.Structure;
using MyGeometry;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExtension;
using NumericalRecipes;
using UnityEditor;
using UnityEngine.SceneManagement;
using Accord.MachineLearning;

public class BodyEngine : MonoBehaviour
{
    GraphicsEngine m_engine;
    Projector m_projector;
    FaceEngine m_faceEngine;
    BodyEngine m_bodyEngine;
    RenderEngine m_renderEngine;

    GameObject thisObject;
    Image<Gray, byte> img = null;
    Circle3 topCircle = new Circle3();
    Quad topRect = new Quad();
    float init_radius;                                     // init radius for rect face

    List<Vector3> boundary3 = null;
    List<Vector2> boundary2_body = null;        // only Body
    List<List<Vector2>> boundary2_lines = null;
    List<Vector3> center3 = null;

    List<CenterFrame> frameList = new List<CenterFrame>();
    CenterFrame frameCur = null;
    Mesh GenernedMesh = null;

    // Optimal Params
    public DetectStatus isDetectingCenter;
    float precentOfCirclePoint_MaxCover_CoverChecker_Circle = 0.68f;   // 距离给定点的最远距离
    bool isStop;
    float offset_scale;
    bool IsHandle = false;
    bool isSymmetry = false;
    bool isFollowAxis = false;
    bool isCuboid = false;
    bool isSnap = true;
    int c_index_offset = 0;

    public Plane sectionPlane;
    float offset;
    int nearestPointNum_LocalTangential = 200;              // 值越小 对边界点的波动越敏感   // 值越大 取到的切线越平滑
    float precentOfRadius_maxDist_LocalTangential = 0.1f;     // 距离给定点的最远距离
    float precentOfRadius_minDist_MinBottomDist = 2f;       // Frame Center到
    //float precentOfCirclePoint_MaxCover_CoverChecker_Rect = 0.50f;   // 距离给定点的最远距离
    float precentOfBottomCover_threshold_rescale = 1.1f;    // 值约小 约不容易触底 值约大 约容易触底
    float angle_radian_MaxAngle_IgnoreErrorFrame = Mathf.PI / 23f;

    // Intermediate Result
    List<Vector3> m_c_list = new List<Vector3>();
    List<Vector3> m_dire_list = new List<Vector3>();
    List<Vector2> m_axis = new List<Vector2>();
    Line2 BestLine = new Line2(new Vector2(), new Vector2());
    List<Vector3> kNearPv3_left = new List<Vector3>();
    List<Vector3> kNearPv3_right = new List<Vector3>();
    List<Vector2> kNearPv2_left = new List<Vector2>();
    List<Vector2> kNearPv2_right = new List<Vector2>();

    // Optimal Radius Params
    int Inter_Num = 0;

    // Algorithm Visualization
    Ray setLine1;
    Ray setLine2;
    Ray setdirecLine;
    Vector3 HitLineStart1, HitLineEnd1 = new Vector3();
    Vector3 HitLineStart2, HitLineEnd2 = new Vector3();
    List<Vector2> boundary2_cover = new List<Vector2>();

    public void CreateBodyEngine(GameObject m_meshViewer, GraphicsEngine m_engine, Projector m_projector, RenderEngine m_renderEngine, FaceEngine m_faceEngine, List<Vector2> axis, bool isCurve)
    {
        this.m_engine = m_engine;
        this.m_projector = m_projector;
        this.m_faceEngine = m_faceEngine;
        this.m_renderEngine = m_renderEngine;
        this.m_axis = axis;
        this.img = m_faceEngine.body_img;
        this.thisObject = m_meshViewer;

        boundary2_lines = m_engine.m_LineData;
        boundary2_cover = new List<Vector2>();

        isSnap = m_engine.be_isSnap;
        isStop = false;
        isFollowAxis = true;
        isSymmetry = !isCurve;

        UpdateParams();
    }
    public void CreateHandleEngine(GameObject m_gameObject, GraphicsEngine m_engine, Projector m_projector, RenderEngine m_renderEngine, FaceEngine m_faceEngine, BodyEngine m_bodyEngine, List<Vector2> axis
        , Image<Gray, byte> img, bool isCurve, bool IsHandle)
    {
        CreateBodyEngine(m_gameObject, m_engine, m_projector, m_renderEngine, m_faceEngine, axis, isCurve);
        this.m_bodyEngine = m_bodyEngine;
        this.img = img;
        this.IsHandle = IsHandle;
    }
    private struct NearPoint3
    {
        public Vector3 v;
        public double dist;
        public int index;
    }
    private struct NearPoint2
    {
        public Vector2 v;
        public double dist;
        public int index;
    }
    public enum FaceType
    {
        Null, Circle, Rect
    }
    public enum TangentialSide
    {
        LeftSide, RightSide
    }
    private class CenterFrame
    {
        public int id;
        public Vector3 c = new Vector3();
        public Vector3 dire = new Vector3();
        public float r;
        public Circle3 circle;
        public Quad quad;

        //public Vector2 Insenction1_line = new Vector2();
        //public Vector2 Insenction2_line = new Vector2();

        public Vector3 Insection1 = new Vector3();
        public Vector3 Insection2 = new Vector3();
        public Vector3 tangential1 = new Vector3();
        public Vector3 tangential2 = new Vector3();
        public Vector3 hit1 = new Vector3();
        public Vector3 hit2 = new Vector3();

        public float weight;
        public float BottomDist = float.MaxValue;
        public float CoverPercent;

        public FaceType faceType = FaceType.Null;
    }
    public enum DetectStatus
    {
        NotDetect, Detecting, Detected, Isdone
    };
    void InitData()
    {
        thisObject.GetComponent<MeshFilter>().mesh = null;
        GenernedMesh = null;
        topCircle = m_faceEngine.topCircle;
        topRect = m_faceEngine.topRect;
        frameCur = new CenterFrame();
        frameList.Clear();
        isDetectingCenter = DetectStatus.NotDetect;
        isStop = false;
    }
    void OnRenderObject()
    {
        //m_renderEngine.DrawRay(ref setLine1, Color.green);
        //m_renderEngine.DrawRay(ref setLine2, Color.green);
        //m_renderEngine.DrawRay(ref setdirecLine, Color.blue);
        //m_renderEngine.DrawLine(ref HitLineStart1, ref HitLineEnd1, Color.red);
        //m_renderEngine.DrawLine(ref HitLineStart2, ref HitLineEnd2, Color.yellow);
        m_renderEngine.DrawPoints(ref boundary2_cover, Color.red);

        if (frameCur == null)
            return;
        else if (frameCur.faceType == FaceType.Circle)
        {
            Circle3 FrameCircle = new Circle3(frameCur.c, frameCur.r, frameCur.dire);
            m_renderEngine.DrawCircle(ref FrameCircle);
            //m_renderEngine.DrawPoints(ref kNearPv2_left, Color.red);
            //m_renderEngine.DrawPoints(ref kNearPv2_right, Color.blue);
            //m_renderEngine.DrawPoints(ref kNearPv3_left, Color.red);
            //m_renderEngine.DrawPoints(ref kNearPv3_right, Color.blue);
            //Vector2 xx = BestLine.start + 100f * BestLine.dir.normalized;
            //m_renderEngine.DrawLine(ref BestLine.start, ref xx, Color.black);
        }
        else
        {
            Quad FrameRect = new Quad(topRect, frameCur.c, frameCur.dire, frameCur.r / init_radius);
            m_renderEngine.DrawRect3(ref FrameRect);
        }
    }
    public void Update()
    {
        UpdateParams();

        if (isDetectingCenter == DetectStatus.Detecting) isDetectingCenter = DetectMidCenter();

        if (isDetectingCenter == DetectStatus.Detected) BuildDataFromFrame();

        //if (GUI.changed) EditorUtility.SetDirty(target);
        //SceneView.RepaintAll();
        //UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        //m_renderEngine.DrawMesh(GenernedMesh);
    }
    void UpdateParams()
    {
        isSnap = m_engine.be_isSnap;
        isSymmetry = m_engine.be_isSymmetry;
        precentOfCirclePoint_MaxCover_CoverChecker_Circle = m_engine.be_bottomCover;   // 距离给定点的最远距离
        if (!isStop) isStop = m_engine.be_isStop;
        offset_scale = m_engine.be_offsetScale;
    }
    public void SolveBody(GameObject gb = null, Image<Gray, byte> image = null)
    {
        //m_renderEngine.DrawPoints(m_engine.m_LineData, UnityEngine.Color.blue);

        // Set Image
        if (image != null) img = image;
        if (gb != null) thisObject = gb;

        InitData();

        if (IsHandle)
        {

            SnapHandle();
            return;
        }

        if (m_faceEngine.isTopCircle_Current)
            SnapCylinder();
        else
            SnapCuboid();
    }
    public void SnapCylinder()
    {
        UpdateParams();

        // Get Boundary3 body
        List<Vector2> boundary2_face = IExtension.GetBoundary(m_faceEngine.img);
        boundary2_body = ExtractOutline(img, boundary2_face);

        Vector3 normal = Vector3.Cross(
            Vector3.Cross(topCircle.Normal, m_projector.m_mainCamera.transform.forward),
            topCircle.Normal);
        sectionPlane = new Plane(normal.normalized, topCircle.Center);          // Get Section Plane

        boundary3 = m_projector.Proj2dToPlane(sectionPlane, boundary2_body);    // Project 2D edge points
        m_renderEngine.DrawPoints(boundary3);

        topCircle = FixTopFace(topCircle, boundary3);

        m_faceEngine.topCircle = topCircle;

        // Algorithm Init Params
        init_radius = topCircle.Radius;
        // Set center Line
        Compute_c_dire(m_axis, sectionPlane, topCircle.Center);

        frameCur.id = 0;
        frameCur.c = isFollowAxis ? m_c_list[frameCur.id] : ComputeOffsetC(topCircle.Center, topCircle.Normal);
        frameCur.r = topCircle.Radius;
        frameCur.dire = isFollowAxis ? m_dire_list[frameCur.id] : topCircle.Normal;
        frameCur.weight = 1;
        frameCur.faceType = FaceType.Circle;
        frameCur = BuildFirstFrame(frameCur);

        isDetectingCenter = DetectStatus.Detecting;
    }
    public void SnapCuboid()
    {
        UpdateParams();
        isCuboid = isSymmetry && true;

        // Get Boundary3 body
        List<Vector2> boundary2_face = IExtension.GetBoundary(m_faceEngine.img);
        boundary2_body = ExtractOutline(img, boundary2_face);

        int[] i_corner = SelectTwoCorners(m_projector.Proj3dToImage(topRect.CornerPoints3d), boundary2_body);
        Vector3 v_inSection = topRect.CornerPoints3d[i_corner[0]] - topRect.CornerPoints3d[i_corner[1]];
        Vector3 normal = Vector3.Cross(topRect.Normal, v_inSection);
        sectionPlane = new Plane(normal.normalized, topRect.Center);            // Get Section Plane

        boundary3 = m_projector.Proj2dToPlane(sectionPlane, boundary2_body);    // Project 2D edge points
        m_renderEngine.DrawPoints(boundary3);
        //m_renderEngine.DrawPoints(boundary2_lines, Color.blue);

        topRect = FixTopFace(topRect, boundary3);
        // Algorithm Init Params
        init_radius = Vector3.Distance(topRect.CornerPoints3d[i_corner[0]], topRect.CornerPoints3d[i_corner[1]]);
        // Set center Line
        Compute_c_dire(m_axis, sectionPlane, topRect.Center);

        // Check Cuboid FOV
        //isStop = CheckCuboidFOVNice(topRect.Normal, m_dire_list) ? false : true;

        isFollowAxis = false;
        frameCur.id = 0;
        frameCur.r = init_radius;
        frameCur.c = isFollowAxis ? m_c_list[frameCur.id] : ComputeOffsetC(topRect.Center, topRect.Normal);
        frameCur.dire = isFollowAxis ? m_dire_list[frameCur.id] : topRect.Normal;
        //frameCur.dire = isCuboid ? topRect.Normal : frameCur.dire;
        frameCur.weight = 1;
        frameCur.faceType = FaceType.Rect;
        frameCur = BuildFirstFrame(frameCur);

        isDetectingCenter = DetectStatus.Detecting;
    }

    #region Handle
    public void SnapHandle()
    {
        UpdateParams();

        // Get Boundary3 body
        boundary2_body = ExtractOutline(img);
        sectionPlane = m_bodyEngine.sectionPlane;
        boundary3 = m_projector.Proj2dToPlane(sectionPlane, boundary2_body);    // Project 2D edge points
        //m_renderEngine.DrawPoints(boundary3);

        // Algorithm Init Params
        Compute_c_dire(m_axis, sectionPlane);
        topCircle = GenerateHanldeTopFace(m_bodyEngine.GenernedMesh, sectionPlane);
        if (topCircle == null) isFollowAxis = true;

        //init_radius = topCircle.Radius;

        // chenxin
        frameCur.id = 0;
        frameCur.c = isFollowAxis ? m_c_list[frameCur.id] : ComputeOffsetC(topCircle.Center, topCircle.Normal);
        frameCur.r = topCircle != null ? topCircle.Radius : 0.001f;
        frameCur.dire = isFollowAxis ? m_dire_list[frameCur.id] : topCircle.Normal;
        frameCur.weight = 1;
        frameCur.faceType = FaceType.Circle;
        isFollowAxis = false;
        frameCur = BuildFirstFrame(frameCur);

        isDetectingCenter = DetectStatus.Detecting;
    }
    public Circle3 GenerateHanldeTopFace(Mesh mainBody, Plane setPlane)
    {
        // Ray hit
        //Ray axis_ray = new Ray(m_c_list.First(), -1.0f * m_dire_list.First());
        //List<Vector3> hitedTriangle = HitMesh(mainBody, axis_ray);
        //if (hitedTriangle == null) 
        //{
        //    Debug.Log("Error: Handle Ray hit nothing!");
        //    return null;
        //}
        //Vector3 triangleNormal = new Plane(hitedTriangle[0], hitedTriangle[1], hitedTriangle[2]).normal;
        //m_renderEngine.DrawRay(new Ray(hitedTriangle[0], triangleNormal));
        //Vector3 meanC = 1 / 3f * (hitedTriangle[0] + hitedTriangle[1] + hitedTriangle[2]);
        Ray axis_ray = new Ray(m_c_list.First(), -1.0f * m_dire_list.First());
        Vector3 hitedpoint = HitMesh(mainBody, axis_ray);
        Vector3 meanC =  Utility.IsNull(hitedpoint)?
            FindNearestOfMesh(mainBody, m_c_list.First()) :
            hitedpoint;
        meanC = m_c_list.First();

        // body
        //Vector3 meanC = FindNearestOfMesh(mainBody, m_c_list.First());
        if (Utility.IsNull(meanC)) Debug.Log("Error: Near point error!");
        Vector3 triangleNormal = (m_c_list.First() - meanC).normalized;

        // chenxin
        //meanC = m_c_list[0];
        Vector3 dire = triangleNormal + m_dire_list.First();
        Vector3 meanC_temp = m_c_list[0];
        // chennxin
        //dire = m_dire_list[0];
        Vector3 Insection1, Insection2;
        CenterFrame f = new CenterFrame();
        f.c = meanC_temp;
        f.dire = dire;
        float radius;
        if (RayTracein3DPlane_ortho(
            f,
            boundary2_body,
            setPlane
            ))
            radius = 0.5f * Vector3.Distance(f.Insection1, f.Insection2);
        else
            radius = 0.000005f;

        Circle3 handleFace = new Circle3(meanC, radius, dire);
        m_renderEngine.DrawCircle(handleFace);
        return handleFace;
    }

    #region Old Generate End Face
    //private void GenerateHanldeEndFaces(Mesh mainBody, Plane setPlane, CenterFrame frameLast)
    //{
    //    frameCur = frameLast;

    //    Ray axis_ray = new Ray(m_c_list.Last(), m_dire_list.Last());
    //    List<Vector3> hitedTriangle = HitMesh(mainBody, axis_ray);
    //    if (hitedTriangle == null)
    //    {
    //        Debug.Log("No Need for End Connected!");
    //        return;
    //    }

    //    //Vector3 v_near = FindNearestOfMesh(mainBody, frameCur.c);
    //    Vector3 v_near = (hitedTriangle[0] + hitedTriangle[1] + hitedTriangle[2]) / 3;
    //    m_renderEngine.DrawCircle(new Circle3(v_near, 0.02f, new Vector3(0, 1, 0)));
    //    if (Utility.IsNull(v_near)) Debug.Log("Error: Near point error!");

    //    // Sample Mid Center
    //    float dist = Vector3.Distance(v_near, frameCur.c);
    //    Vector3 dire_basic = (v_near - frameCur.c).normalized;

    //    // average radius of Last 10 frame
    //    int sub_frame = 10;
    //    float r_average = 0;
    //    foreach (var f in frameList.GetRange(
    //        Mathf.Max(0, frameList.Count - 1 - sub_frame),
    //        Mathf.Min(frameList.Count, sub_frame)))
    //        r_average += f.r;
    //    r_average /= Mathf.Min(frameList.Count, sub_frame);
    //    //r_average = 0.000005f;

    //    int sample_num = (int)(dist / r_average * 3f);
    //    for (int i = 0; i < sample_num; i++)
    //    {
    //        // 对称性
    //        CenterFrame frameNext = new CenterFrame();
    //        frameNext.id = frameCur.id + 1;
    //        frameNext.faceType = frameCur.faceType;

    //        // Step1 : Get Next Cur Direction and Cur Point
    //        // chenxin
    //        frameNext.c = frameLast.c + dist * (i + 1) / (float)sample_num * dire_basic;
    //        frameNext.dire = dire_basic;
    //        frameNext.weight = 0.5f;
    //        frameNext.r = r_average;

    //        frameList.Add(frameNext);
    //        VisualizeFrame(frameNext);
    //        frameCur = frameNext;
    //    }
    //}
    #endregion

    private List<Vector3> GenerateHanldeEndFaces(Mesh mainBody, Plane setPlane, List<CenterFrame> frameList)
    {
        if (frameList.Count < 2) return new List<Vector3>();

        List<Vector3> endFace = new List<Vector3>();
        CenterFrame lastFrame = frameList[frameList.Count - 2];
        CenterFrame prevFrame = frameList[frameList.Count - 3];
        //CenterFrame lastFrame = frameList.Last();
        //CenterFrame prevFrame = frameList[frameList.Count - 2];

        for (int i = 0; i < lastFrame.circle.CirclePoints.Count; i++)
        {
            Ray axis_ray = new Ray(
                lastFrame.circle.CirclePoints[i],
                (lastFrame.circle.CirclePoints[i]-prevFrame.circle.CirclePoints[i]).normalized);
            //m_renderEngine.DrawRay(axis_ray);
            Vector3 hitedpoint = HitMesh(mainBody, axis_ray);
            if (Utility.IsNull(hitedpoint)) return new List<Vector3>();

            endFace.Add(hitedpoint);
        }
        return endFace;
    }

    private Vector3 HitMesh(Mesh mainBody, Ray r)
    {
        RaycastHit hit;
        if (!Physics.Raycast(r, out hit)) return new Vector3();

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null) return new Vector3();
        // Hit hitpoint
        return hit.point;

        // Hit triangle
        //Vector3[] vertices = mainBody.vertices;
        //int[] triangles = mainBody.triangles;
        //Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
        //Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
        //Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
        //Transform hitTransform = hit.collider.transform;
        //p0 = hitTransform.TransformPoint(p0);
        //p1 = hitTransform.TransformPoint(p1);
        //p2 = hitTransform.TransformPoint(p2);
        //List<Vector3> hiTrangle = new List<Vector3> { hit.point, hit.point, hit.point };
        //return hiTrangle;
        //Debug.DrawLine(p0, p1);
        //Debug.DrawLine(p1, p2);
        //Debug.DrawLine(p2, p0);
    }

    private Vector3 FindNearestOfMesh(Mesh mainBody, Vector3 v)
    {
        Vector3[] vertices = mainBody.vertices;
        int[] triangles = mainBody.triangles;
        double dist = double.MaxValue;
        int index_close = 0;

        for (int i = 0; i < triangles.Length; i++)
        {
            double temp_dist = Vector3.Distance(vertices[triangles[i]], v);
            if (dist > temp_dist)
            {
                dist = temp_dist;
                index_close = triangles[i];
            }
        }

        return vertices[index_close];

    }

    #endregion

    CenterFrame BuildFirstFrame(CenterFrame frame)
    {
        // Step2: Get IntersectionPoitn
        RayTracein3DPlane_ortho(
            frame,
            boundary2_body,
            sectionPlane);

        // Step3 : Get Two Local Tangential
        frame.tangential1 = GetLocalTangential(frame.Insection1, boundary3, frame, TangentialSide.LeftSide);
        frame.tangential2 = GetLocalTangential(frame.Insection2, boundary3, frame, TangentialSide.RightSide);
        frame.r = ComputeRadius(frame.Insection1, frame.Insection2, frame.r, frame.faceType);
        frame.r = Optimize_Radius(frame);
        //frame.r = isCuboid ? frame.r : ComputeRadius(frame.Insection1, frame.Insection2, frame.r, frame.faceType);
        //frame.r = isCuboid ? frame.r : Optimize_Radius(frame);

        // Step4 : Get Bottom Distance
        RayTracein3DPlane_ortho(
            frame,
            boundary2_body,
            sectionPlane,
            false);
        ComputeBottomDist(frame);

        // Step5: Build Rect for cuboid
        if (isCuboid)
        {
            float scale = frame.r / init_radius;
            frame.quad = new Quad(topRect, frame.c, frame.dire, scale);
        }
        else
            frame.circle = new Circle3(frame.c, frame.r, frame.dire);

        return frame;
    }
    CenterFrame BuildNextFrame(CenterFrame frame)
    {

        // Update Alg Params
        //offset = isSymmetry ? init_radius * offset_scale : frame.r * offset_scale;

        // 对称性
        CenterFrame frameNext = new CenterFrame();
        frameNext.id = frame.id + 1;
        frameNext.faceType = frame.faceType;

        // Step1 : Get Next Cur Direction and Cur Point
        //         Fix tangential Null Problem
        if (IsHandle) isFollowAxis = CheckIsFollowAxis(frame);

        SetFrame_c_dire_r(frame, frameNext);

        // Step2: Get IntersectionPoitn
        RayTracein3DPlane_ortho(
            frameNext,
            boundary2_body,
            sectionPlane);

        if (!Utility.IsNull(frameNext.Insection1) && !Utility.IsNull(frameNext.Insection2))
        {
            Vector3 frameNext_temp = (frameNext.Insection1 + frameNext.Insection2) / 2;
            // 对称性
            frameNext.c = isSymmetry ? frameNext.c : frameNext_temp;
        }

        // Step3 : Get Two Local Tangential
        //frameNext.tangential1 = GetLocalTangential(frameNext.Insection1, boundary3, frame, TangentialSide.RightSide);
        //frameNext.tangential2 = GetLocalTangential(frameNext.Insection2, boundary3, frame, TangentialSide.LeftSide);
        frameNext.tangential1 = GetLocalTangential(frame.Insection1, boundary3, frame, TangentialSide.RightSide);
        frameNext.tangential2 = GetLocalTangential(frame.Insection2, boundary3, frame, TangentialSide.LeftSide);
        frameNext.r = ComputeRadius(frameNext.Insection1, frameNext.Insection2, frame.r, frame.faceType);
        frameNext.r = Optimize_Radius(frameNext);
        //frameNext.r = isCuboid ? frame.r : ComputeRadius(frameNext.Insection1, frameNext.Insection2, frame.r, frame.faceType);
        //frameNext.r = isCuboid ? frame.r : Optimize_Radius(frameNext);

        // Step4 : Get Bottom Distance
        RayTracein3DPlane_ortho(
            frameNext,
            boundary2_body,
            sectionPlane,
            false);
        ComputeBottomDist(frameNext);
        ComputeBottomCover(frameNext);

        // Step5: Build Rect for cuboid
        if (isCuboid)
        {
            float scale = frameNext.r / init_radius;
            frameNext.quad = new Quad(topRect, frameNext.c, frameNext.dire, scale);
        }
        else
            frameNext.circle = new Circle3(frameNext.c, frameNext.r, frameNext.dire);

        VisualizeFrame(frameNext);
        return frameNext;
    }
    void VisualizeFrame(CenterFrame frame)
    {
        // Visualization
        if (!m_engine.m_is_quiet) Debug.Log("ID: " + frame.id + "  BottomDist: " + frame.BottomDist + "  Cover: " + frame.CoverPercent + "  r" + frame.r);
        m_renderEngine.DrawPoint(frame.c);
        m_renderEngine.DrawPoint(frame.Insection1, Color.red);
        m_renderEngine.DrawPoint(frame.Insection2, Color.red);
        setdirecLine = new Ray(frame.c, frame.dire);
        setLine1 = new Ray(frame.Insection1, frame.tangential1);
        setLine2 = new Ray(frame.Insection2, frame.tangential2);
        HitLineStart1 = frame.c;
        HitLineStart2 = frame.c;
        HitLineEnd1 = frame.hit1;
        HitLineEnd2 = frame.hit2;
        if (frame.id % 10 == 0)
        {
            //m_renderEngine.DrawCircle(frame.circle);
        }
    }
    DetectStatus DetectMidCenter()
    {
        if (isStop)
        {
            if (!m_engine.m_is_quiet) Debug.Log("Warning: Let's Stop!");
            frameList.Add(frameCur);
            return DetectStatus.Detected;
        }
        if (!isCuboid && CheckMeshOutsideMask(frameCur))
        {
            //if (!m_engine.m_is_quiet) Debug.Log("Warning: Mesh Outside mask!");
            Debug.Log("Warning: Mesh Outside mask!");
            return DetectStatus.Detected;
        }
        if (isCuboid && CheckCuboidRadiusWave(frameCur) /*&& frameCur.BottomDist < frameCur.r * 0.05*/)
        {
            Debug.Log("Warning: Cuboid Radius wave!");
            return DetectStatus.Detected;
        }
        if (!IsHandle && CheckBottomCover(frameCur) /*&& frameCur.BottomDist < frameCur.r * 0.05*/)
        {
            Debug.Log("Warning: Circle Cover boundary!");
            frameList.Add(frameCur);
            return DetectStatus.Detected;
        }
        
        if (frameCur.BottomDist < ComputeBottomDist_Threshold(frameCur) * precentOfBottomCover_threshold_rescale)
        {
            //if (!m_engine.m_is_quiet) Debug.Log("Warning: Circle Too Close to boundary!");
            Debug.Log("Warning: Circle Too Close to boundary!");
            frameList.Add(frameCur);
            return DetectStatus.Detected;
        }
        if (frameCur.Insection1 == frameCur.Insection2) // 交点一直保持相同
        {
            int frame_begin_fix_num = isCuboid ? 50 : 20;
            if (frameCur.id < frame_begin_fix_num)
            {
                frameCur = BuildNextFrame(frameCur);
                if (!m_engine.m_is_quiet) Debug.Log("Fixing the beginning by enlarging offset! Step:" + frameCur.id);
                return DetectStatus.Detecting;
            }
            if (!m_engine.m_is_quiet) Debug.Log("Warning: Insection is same!");
            return DetectStatus.Detected;
        }
        if (frameCur.r < 0.0001)
        {
            if (!m_engine.m_is_quiet) Debug.Log("Warning: Radius is too small!");      // 半径过小
            return DetectStatus.Detected;
        }

        // Build next frame
        CenterFrame frameNext = BuildNextFrame(frameCur);
        frameList.Add(frameCur);
        frameCur = frameNext;
        //if (frameCur.id == 1) EditorApplication.isPaused = true ;

        return DetectStatus.Detecting;
    }
    bool CheckCuboidRadiusWave(CenterFrame frame)
    {
        if (frame == null) return false;
        if (frame.id - 1 < 4) return false;
        if (frame.id - 1 > frameList.Count - 1) return false;

        Vector3 p0 = frameList[Mathf.Max(0, frame.id - 2)].quad.CornerPoints3d[0];
        Vector3 p1 = frameList[frame.id - 1].quad.CornerPoints3d[0];
        Vector3 p2 = frame.quad.CornerPoints3d[0];

        Vector3 dire0 = frameList[Mathf.Max(0, frame.id - 2)].dire;
        Vector3 dire1 = frameList[frame.id - 1].dire;
        Vector3 dire2 = frame.dire;

        Vector3 corner_dire = (p1 - p0) + (p2 - p1); corner_dire.Normalize();
        Vector3 aver_dire = dire0 + dire1 + dire2; aver_dire.Normalize();

        if (Mathf.Abs(Vector3.Angle(corner_dire, aver_dire)) > 60)
            return true;
        float lastR = frameList[frame.id - 1].r;
        float currR = frame.r;
        return Mathf.Abs(lastR - currR) / lastR > 0.1 ? true : false;
    }
    bool CheckBottomCover(CenterFrame frame)
    {
        // Face Type Case
        //float MaxFlag = frame.faceType == FaceType.Circle ?
        //    precentOfCirclePoint_MaxCover_CoverChecker_Circle : precentOfCirclePoint_MaxCover_CoverChecker_Rect;

        // Symmetry Case
        //MaxFlag = isSymmetry ? 0.44f : MaxFlag;

        // Slider Control Case
        float MaxFlag = precentOfCirclePoint_MaxCover_CoverChecker_Circle;
        MaxFlag = isCuboid ? 0.35f : MaxFlag;
        //precentOfCirclePoint_MaxCover_CoverChecker_Circle = IsHandle ? 
        //    0.99f : 
        //    precentOfCirclePoint_MaxCover_CoverChecker_Circle;
        return frame.CoverPercent > MaxFlag ? true : false;
    }
    bool CheckMeshOutsideMask(CenterFrame frame)
    {
        // Step 1: Init Params
        if (frame.id < 3) return false;

        List<Vector2> boundary2_face = ComputeFaceBoundary2(frame);
        float vNotCover = 0;
        foreach (var p in boundary2_face)
        {
            int h = Mathf.RoundToInt(p.y);
            int w = Mathf.RoundToInt(p.x);
            if (h >= img.Height || h < 0 || w >= img.Width || w < 0)
            {
                vNotCover += 1; continue;
            }
            vNotCover += img[h, w].Intensity == 0 ? 1 : 0;
        }
        return vNotCover / boundary2_face.Count > 0.80 ? true : false;
    }
    bool CheckIsFollowAxis(CenterFrame f)
    {
        // no axis to follow
        if (c_index_offset != 0 && f.id + c_index_offset > m_c_list.Count - 2)
        {
            return false;
        }
        else if (isFollowAxis)
        {
            return true;
        }


        Vector2 c2 = m_projector.WorldToImage(f.c);

        // arrive end
        //if (Vector2.Distance(c2, m_axis.Last()) < 6)
        //{
        //    isStop = true;
        //    return true;
        //}

        // Check near Point not begin

        for (int i = 1; i < m_axis.Count - 1; i++)
        {
            if (Vector2.Distance(c2, m_axis[i]) < 6)
            {
                c_index_offset = i - f.id;
                return true;
            }
        }
        return false;
    }
    bool CheckCuboidFOVNice(Vector3 topRectNormal, List<Vector3> axisDires)
    {
        Vector3 dire_average = new Vector3();
        foreach (var dire in axisDires)
            dire_average += dire;
        dire_average.Normalize();
        topRectNormal.Normalize();
        Debug.Log(Vector3.Dot(dire_average, topRectNormal));
        return Vector3.Dot(dire_average, topRectNormal) > Mathf.Cos(1f / 18f * Mathf.PI) ?
            true : false;
    }
    void SetFrame_c_dire_r(CenterFrame f, CenterFrame next_f)
    {
        // Init
        next_f.r = f.r;

        // Step1 : Get Next Cur Direction and Cur Point
        //         Fix tangential Null Problem
        if (Utility.IsNull(f.tangential1) || Utility.IsNull(f.tangential2))
            next_f.dire = f.dire;
        else
        {
            next_f.dire = (f.tangential1 + f.tangential2) / 2;
            next_f.dire = 0.5f * (next_f.dire + f.dire);
            //next_f.dire = 0.2f * next_f.dire + 0.8f * f.dire;
            next_f.dire = isSymmetry ? f.dire : next_f.dire;
        }
        next_f.dire.Normalize();

        //          Fix Intersection Null Problem
        if (Utility.IsNull(f.Insection1) || Utility.IsNull(f.Insection2))
        {
            next_f.c = ComputeOffsetC(f.c, next_f.dire);
            //next_f.weight = 0.9f;
        }
        else
        {
            next_f.c = ComputeOffsetC((f.Insection1 + f.Insection2) / 2, next_f.dire);
            next_f.c = isSymmetry ? ComputeOffsetC(f.c, next_f.dire) : next_f.c;
            //next_f.weight = Mathf.Abs(Vector3.Dot(next_f.dire, f.dire));
        }

        // Follow Axis
        if (isFollowAxis)
        {
            if (next_f.id + c_index_offset > m_c_list.Count - 1)
            {
                isFollowAxis = false;
                return;
            }
            next_f.c = IsHandle ? m_c_list[next_f.id + c_index_offset] : next_f.c;
            next_f.dire = isCuboid ? f.dire : m_dire_list[next_f.id + c_index_offset]; next_f.dire.Normalize();
            //next_f.weight = Mathf.Abs(Vector3.Dot(next_f.dire, f.dire));
            return;
        }

    }
    void Compute_c_dire(List<Vector2> axis2, Plane setPlane, Vector3 c_topFace)
    {
        // Project 2d axis to 3d
        axis2 = IExtension.ResetPath(axis2, 5);

        // Compute corresponding center
        m_c_list = m_projector.Proj2dToPlane(setPlane, axis2);
        if (isSymmetry)
        {
            Vector3 diff_c = c_topFace - m_c_list.First();
            Vector3 dire = (m_c_list.Last() - m_c_list.First()).normalized;
            for (int i = 0; i < m_c_list.Count; i++)
            {
                m_c_list[i] = ComputeOffsetC(m_c_list[i] + diff_c, dire);
            }
        }

        // Compute corresponding direction
        m_dire_list = FitExtension.SetPointsTangential(m_c_list);
    }
    void Compute_c_dire(List<Vector2> axis2, Plane setPlane)
    {
        if (axis2.Count < 3)
        {
            Debug.Log("Error: No Axis!");
            return;
        }

        // Begin Point
        Vector2 begin_p2 = axis2.First();
        Vector3 begin_p3 = m_projector.Proj2dToPlane(setPlane, begin_p2);
        axis2.RemoveAt(0);

        // End Point 
        //Vector2 end_p = axis2.Last();
        //axis2.RemoveAt(axis2.Count-1);

        // Project 2d axis to 3d
        axis2 = IExtension.ResetPath(axis2, 5);

        // Compute corresponding center
        m_c_list = m_projector.Proj2dToPlane(setPlane, axis2);

        // Compute corresponding direction
        m_dire_list = FitExtension.SetPointsTangential(m_c_list);

        axis2.Insert(0, begin_p2);
        m_c_list.Insert(0, begin_p3);
        m_dire_list.Insert(0, (m_c_list[1] - m_c_list[0]).normalized);
        //m_dire_list.Insert(0, m_dire_list[1]);
    }
    void ComputeBottomCover(CenterFrame frame)
    {
        if (boundary2_body == null)
            boundary2_body = ExtractOutline(img, IExtension.GetBoundary(m_faceEngine.img));

        List<Vector2> boundary2_face = ComputeFaceBoundary2(frame);
        List<Vector2> boundary2_coincide = new List<Vector2>();
        // Check out the coincide points of two boundary
        double lineDistTheshold = Mathf.Max(ComputeLongestDist(boundary2_face) / 25, 2);
        foreach (var p_c in boundary2_face)
        {
            bool IsColosed = false;
            foreach (var p_body in boundary2_body)
            {
                if (Vector2.Distance(p_c, p_body) < lineDistTheshold)
                {
                    IsColosed = true;
                }
            }
            if (IsColosed)
            {
                boundary2_coincide.Add(p_c);
            }
        }
        boundary2_cover = boundary2_coincide;
        frame.CoverPercent = (float)boundary2_coincide.Count / (float)boundary2_face.Count;
    }
    void ComputeBottomDist(CenterFrame frame)
    {
        // Two hit points
        if (!Utility.IsNull(frame.hit1) && !Utility.IsNull(frame.hit2))
        {
            if (Vector3.Dot(frame.hit1 - frame.c, frame.dire)
                > Vector3.Dot(frame.hit2 - frame.c, frame.dire))
                frame.BottomDist = Vector2.Distance(m_projector.WorldToImage(frame.hit1), m_projector.WorldToImage(frame.c));
            else
                frame.BottomDist = Vector2.Distance(m_projector.WorldToImage(frame.hit2), m_projector.WorldToImage(frame.c));
        }
        // One hit point 1
        else if (!Utility.IsNull(frame.hit1))
        {
            frame.BottomDist = Vector2.Distance(m_projector.WorldToImage(frame.hit1), m_projector.WorldToImage(frame.c));
        }
        // One hit point 2
        else if (!Utility.IsNull(frame.hit2))
        {
            frame.BottomDist = Vector2.Distance(m_projector.WorldToImage(frame.hit2), m_projector.WorldToImage(frame.c));
        }
        // No hit point
        else
        {
            if (!m_engine.m_is_quiet) Debug.Log("Error: Bottom Check Hit Nothing!!!");
        }
    }
    float ComputeBottomDist_Threshold(CenterFrame frame)
    {
        Vector2 intersect;
        Vector2 c2 = m_projector.WorldToImage(frame.c);
        Vector2 dire2 = (m_projector.WorldToImage(frame.c + frame.dire) - m_projector.WorldToImage(frame.c)).normalized;
        bool isSucceeded = RayTracein2DPlane(ComputeFaceBoundary2(frame), c2, dire2, out intersect);
        float threshold = Vector2.Distance(c2, intersect);

        if (!isSucceeded)
            threshold = Vector2.Distance(m_projector.WorldToImage(frame.c + frame.r * frame.dire), m_projector.WorldToImage(frame.c));
        if (!m_engine.m_is_quiet) Debug.Log("MinDist: " + threshold);
        threshold = Mathf.Max(threshold, 4f);
        return threshold;
    }
    float ComputeRadius(Vector3 v1, Vector3 v2, float r_old, FaceType ft)
    {
        if (Utility.IsNull(v1) || Utility.IsNull(v2))
            return r_old;
        else
            return ft == FaceType.Circle ? 0.5f * Vector3.Distance(v1, v2) : Vector3.Distance(v1, v2);
    }
    Vector3 ComputeOffsetC(Vector3 c, Vector3 dire)
    {
        offset = 5;
        Vector2 c2_1 = m_projector.WorldToImage(c);
        Vector2 c2_2 = m_projector.WorldToImage(c + dire * 10);

        Vector2 dire2 = (c2_2 - c2_1).normalized;
        Vector2 b2 = c2_1 + offset * dire2;

        Vector3 b3 = m_projector.Proj2dToPlane(sectionPlane, b2);
        return b3;
    }
    float ComputeLongestDist(List<Vector2> ps)
    {
        float dist_max = 0;
        foreach (var pi in ps)
        {
            foreach (var pj in ps)
            {
                dist_max = Mathf.Max(dist_max, Vector2.Distance(pi, pj));
            }
        }
        return dist_max;
    }
    private List<Vector2> ComputeFaceBoundary2(CenterFrame frame)
    {
        List<Vector2> boundary2_face = new List<Vector2>();
        if (frame.faceType == FaceType.Circle)
            boundary2_face = m_projector.Proj3dToImage(new Circle3(frame.c, frame.r, frame.dire).CirclePoints);
        else if (frame.faceType == FaceType.Rect)
            boundary2_face = m_projector.Proj3dToImage(new Quad(topRect, frame.c, frame.dire, frame.r / init_radius).quadPoints3d);
        else
            Debug.Log("Error: This frame do not have face type!");
        return boundary2_face;
    }

    bool RansacSmooth(ref List<Vector2> points_2d)
    {
        // try line -- ransac
        RansacLine2d ransac = new RansacLine2d();
        Line2 best_fit_line = ransac.Estimate(points_2d);
        if (best_fit_line != null)      // fit line successfully
        {
            Debug.Log("Fit line successfully");
            Vector2 temp;
            for (int i = 0; i < points_2d.Count; i++)
            {
                if (i == 0) continue;
                temp = points_2d[i];
                float t = (temp.x - best_fit_line.start.x) / best_fit_line.dir.x;
                temp.y = best_fit_line.start.y + t * best_fit_line.dir.y;
                points_2d[i] = new Vector2(temp.x, temp.y);
            }
            return true;
        }
        else
        {
            return false;

        }
    }

    void BuildDataFromFrame()
    {
        if (frameList.Count == 0) return;
        if (GenernedMesh != null) return;// 已经构建过Mesh 就不Build Data了

        // Build EndFace
        List<Vector3> EndFace = new List<Vector3>();
        //if (IsHandle) EndFace = GenerateHanldeEndFaces(m_bodyEngine.GenernedMesh, sectionPlane, frameList);

        // Build TopFace Frame
        Vector3 c_init = isCuboid ? topRect.Center : topCircle.Center;
        Vector3 dire_init = isCuboid ? topRect.Normal : topCircle.Normal;
        float r_init = isCuboid ? init_radius : topCircle.Radius;

        // Center
        List<Vector3> GeneratedCenters = new List<Vector3>();
        GeneratedCenters.Add(c_init);
        foreach (var frame in frameList) GeneratedCenters.Add(frame.c);

        // Radius
        List<Vector2> radius = new List<Vector2>();
        float dist_bystep = 0;
        dist_bystep += Vector3.Distance(frameList[0].c, c_init);
        radius.Add(new Vector2(dist_bystep, r_init));
        for (int i = 0; i < frameList.Count; i++)
        {
            dist_bystep += Vector3.Distance(frameList[i].c, frameList[Mathf.Max(i - 1, 0)].c);
            radius.Add(new Vector2(dist_bystep, frameList[i].r));
        }

        // Weigts
        List<float> weights = GenerateWeights();

        // Directions
        List<Vector3> dires = new List<Vector3>();
        dires.Add(dire_init);
        foreach (var frame in frameList) dires.Add(frame.dire);

        ///************************************ smooth *******************************************
        // just. for test
        List<Vector3> t_GeneratedCenters = new List<Vector3>();
        t_GeneratedCenters.AddRange(GeneratedCenters);
        m_renderEngine.DrawPoints(t_GeneratedCenters, Color.yellow);

        // Fit centers and radius;
        GeneratedCenters = FitExtension.FitCenterCurve(GeneratedCenters, weights);
        //dires = FitExtension.FitCenterCurve(dires, weights);
        //radius = FitExtension.FitCurve_BilateralFilter(radius);
        //radius = FitExtension.FitCurve_BilateralFilter(radius);

        //// iterate 
        //// Weigts
        //weights = GenerateWeights();
        //GeneratedCenters = FitExtension.FitCenterCurve(GeneratedCenters, weights);
        //dires = FitExtension.FitCenterCurve(dires, weights);

        //if (!this.RansacSmooth(ref radius))
        //{
        //    Debug.Log("radius: FAIL to fit line");
        //    radius = FitExtension.FitCurve_BilateralFilter(radius);
        //}


        //for (int i = 0; i < t_GeneratedCenters.Count; i++)
        //{
        //    //m_renderEngine.Length_Point3 = radius[i].y;
        //    m_renderEngine.DrawPoint(t_GeneratedCenters[i], Color.yellow);
        //}

        //GeneratedCenters = FitExtension.FitCenterCurve(GeneratedCenters, weights);
        //Utility.ReplaceList(radius, 10, FitExtension.FitCurve_BilateralFilter(radius).GetRange(10, radius.Count - 10));
        //Utility.ReplaceList(radius, 0, FitExtension.FitCurve_RBFmodel(radius.GetRange(0, 10)));

        //radius = FitExtension.FitCurve_RBFmodel(radius);
        //radius = FitExtension.FitCurve_BilateralFilter(radius);
        //{
        //    // Fit Top error
        //    radius[2] = radius[1];
        //}

        //****************************************************************************************************

        // Build Circle and Quad Data
        switch (frameList[0].faceType)
        {
            case FaceType.Null:
                break;
            case FaceType.Circle:
                foreach (var f in frameList)
                    f.circle = new Circle3(f.c, f.r, f.dire);
                break;
            case FaceType.Rect:
                foreach (var f in frameList){
                    float scale = f.r / init_radius;
                    f.quad = new Quad(topRect, f.c, f.dire, scale);}
                break;
            default:
                break;
        }

        // Build Mesh
        List<Vector3> IndexList;
        switch (frameList[0].faceType)
        {
            case FaceType.Null:
                break;
            case FaceType.Circle:
                List<Circle3> CircleLists = new List<Circle3>();
                CircleLists.Clear();
                for (int i = 0; i < dires.Count; i++)
                {
                    if (i == 32) continue;
                    if (i == 48) continue;
                    if (i == 47) continue;
                    // Ignore Noise/Error Data
                    if (i >= 1 && Vector3.Dot(dires[i].normalized, dires[i - 1].normalized) < Mathf.Cos(Mathf.PI / angle_radian_MaxAngle_IgnoreErrorFrame))
                        continue;
                    CircleLists.Add(new Circle3(GeneratedCenters[i], radius[i].y, dires[i]));
                }
                GenernedMesh = Utility.MeshCreateFromCircleList(CircleLists, EndFace, out IndexList);
                GenerateMeshUVMaps_symmetry(GenernedMesh, sectionPlane, IndexList, CircleLists);
                break;
            case FaceType.Rect:
                List<Quad> RectLists = new List<Quad>();
                RectLists.Clear();
                for (int i = 0; i < dires.Count; i++)
                {
                    float scale = radius[i].y / init_radius;
                    if (i == 1) continue;if (i == 2) continue;if (i == 3) continue;if (i == 4) continue;if (i == 5) continue;
                    // Ignore Noise/Error Data
                    if (i >= 1 && Vector3.Dot(dires[i].normalized, dires[i - 1].normalized) < Mathf.Cos(Mathf.PI / angle_radian_MaxAngle_IgnoreErrorFrame))
                        continue;
                    if (i >= 1 && Vector3.Dot(dires[i].normalized, dires[i - 1].normalized) < 0)
                        //Debug.Log(Vector3.Dot(dires[i].normalized, dires[i - 1]));
                        continue;
                    RectLists.Add(new Quad(topRect, GeneratedCenters[i], dires[i], scale));
                }
                GenernedMesh = Utility.MeshCreateFromRectList(RectLists, out IndexList);
                GenerateMeshUVMaps_symmetry(GenernedMesh, sectionPlane, IndexList, RectLists);
                // Save Corner
                if (m_engine.exper_isCubeExper) m_engine.SaveCubeCorner(RectLists);
                break;
            default:
                break;
        }
        //GenerateMeshUVMaps(GenernedMesh);
        thisObject.GetComponent<MeshFilter>().mesh = GenernedMesh;
        thisObject.transform.position = new Vector3(0, 0, 0);

        // Create Texture
        thisObject.GetComponent<MeshRenderer>().material = thisObject.GetComponent<MeshRenderer>().material;
        thisObject.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load(m_engine.m_ImgPath, typeof(Texture2D)) as Texture2D;

        // Add Collider
        thisObject.AddComponent<MeshCollider>();

        isDetectingCenter = DetectStatus.Isdone;
    }
    List<float> GenerateWeights()
    {
        List<float> weights = new List<float>();
        List<CenterFrame> temp_frameList = new List<CenterFrame>();
        CenterFrame frame_topface = new CenterFrame();
        frame_topface.c = isCuboid ? topRect.Center : topCircle.Center;
        frame_topface.r = isCuboid ? init_radius : topCircle.Radius;
        frame_topface.dire = isCuboid ? topRect.Normal : topCircle.Normal;
        if (isCuboid) frame_topface.quad = topRect;
        else frame_topface.circle = topCircle;
        temp_frameList.Add(frame_topface);
        for (int i = 0; i < frameList.Count; i++)
        {
            if (frameList[i].circle == null)
                frameList[i].circle = new Circle3(frameList[i].c, frameList[i].r, frameList[i].dire);
            temp_frameList.Add(frameList[i]);
        }


        for (int i = 0; i < temp_frameList.Count; i++)
        {
            int pre_index = Mathf.Max(0, i - 1);
            int cur_index = i;
            int next_index = Mathf.Min(temp_frameList.Count - 1, i + 1);
            Vector3 p0 = isCuboid ? temp_frameList[pre_index].quad.CornerPoints3d[0] :
                temp_frameList[pre_index].circle.CirclePoints[0];
            Vector3 p1 = isCuboid ? temp_frameList[cur_index].quad.CornerPoints3d[0] :
                temp_frameList[cur_index].circle.CirclePoints[0];
            Vector3 p2 = isCuboid ? temp_frameList[next_index].quad.CornerPoints3d[0] :
                temp_frameList[next_index].circle.CirclePoints[0];

            Vector3 tangle0 = p1 - p0; tangle0.Normalize();
            Vector3 tangle1 = p2 - p1; tangle1.Normalize();
            weights.Add(Mathf.Abs(Vector3.Dot(tangle1, tangle0)));
            //weights.Add(1);
        }
        weights[0] = 1;
        weights[weights.Count - 1] = 1;
        // Fix 
        return weights;
    }
    private Circle3 FixTopFace(Circle3 circle, List<Vector3> boundary3)
    {
        Vector3 boundary_mean = new Vector3(0, 0, 0);
        foreach (var p in boundary3)
        {
            boundary_mean += p;
        }
        boundary_mean /= boundary3.Count;
        if (Vector3.Dot(circle.Normal, boundary_mean - circle.Center) > 0)
            return circle;
        else
            return new Circle3(circle.Center, circle.Radius, -1.0f * circle.Normal);
    }
    private Quad FixTopFace(Quad rect, List<Vector3> boundary3)
    {
        Vector3 boundary_mean = new Vector3(0, 0, 0);
        foreach (var p in boundary3)
        {
            boundary_mean += p;
        }
        boundary_mean /= boundary3.Count;
        if (Vector3.Dot(rect.Normal, boundary_mean - rect.Center) > 0)
            return rect;
        else
        {
            rect.FlipNormal();
            return rect;
        }
    }
    private List<Vector2> ExtractOutline(Image<Gray, byte> image, List<Vector2> topOutline = null)
    {
        // not cut face
        if (topOutline == null)
        {
            List<Vector2> allBoudnary2 = IExtension.GetBoundary(image);
            return allBoudnary2;
        }

        // cut face
        double lineDistTheshold = 5;
        List<Vector2> ObjectOutline = IExtension.GetBoundary(image);
        List<Vector2> Boundary2 = new List<Vector2>();
        foreach (var p_obj in ObjectOutline)
        {
            bool IsColosed = false;
            foreach (var p_top in topOutline)
            {
                if (Vector2.Distance(p_obj, p_top) < lineDistTheshold)
                {
                    IsColosed = true;
                }
            }
            if (!IsColosed)
            {
                Boundary2.Add(p_obj);
            }
        }
        return Boundary2;
    }
    private Vector2 GetLocalTangential(Vector2 p, List<Vector2> boundary2, CenterFrame frame, TangentialSide tanSide)
    {
        // Check Intersection
        if (Utility.IsNull(p))
        {
            if (!m_engine.m_is_quiet)
                Debug.Log("Error: Intersection in Local Tangential is NULL!");
            return new Vector2(0, 0);
        }

        // Get Nearnest Pointss
        List<NearPoint2> nearPs = new List<NearPoint2>();
        for (int i = 0; i < boundary2.Count; i++)
        {
            NearPoint2 p_i = new NearPoint2();
            p_i.v = boundary2[i];
            p_i.dist = (p - p_i.v).magnitude;
            p_i.index = i;
            nearPs.Add(p_i);
        }

        // Fit Line with k nearest points
        List<NearPoint2> kNearPs = (from a in nearPs orderby a.dist ascending select a).Take(nearestPointNum_LocalTangential).ToList();
        List<Vector2> kNearPv = null;
        kNearPv = tanSide == TangentialSide.LeftSide ? kNearPv2_left : kNearPv2_right;
        kNearPv.Clear();

        foreach (var kNearP in kNearPs)
        {
            if (kNearP.dist < 8)
            {
                kNearPv.Add(kNearP.v);
            }
        }
        var fitline = new RansacLine2d(0.05, 0.9);

        Line2 line;
        if (fitline.Estimate(kNearPv) != null)
        {
            line = fitline.bestline;
            // Check Local Tangential Direction
            Vector2 dire2 = m_projector.WorldToImage(frame.c + 10f * frame.dire)
                - m_projector.WorldToImage(frame.c);
            dire2.Normalize();
            if (Vector2.Dot(line.dir, dire2) < 0)
            {
                line.dir = -1 * line.dir;
            }
            if (tanSide == TangentialSide.RightSide)
            {
                BestLine = fitline.bestline;
            }
            return line.dir.normalized;
        }
        else
        {
            if (!m_engine.m_is_quiet) Debug.Log("Error: RANSAC Fit failure!");
            return new Vector2(0, 0);
        }
    }
    private Vector3 GetLocalTangential(Vector3 p, List<Vector3> boundary3, CenterFrame frame, TangentialSide tanSide)
    {
        // Check Intersection
        if (Utility.IsNull(p))
        {
            if (!m_engine.m_is_quiet) Debug.Log("Error: Intersection in Local Tangential is NULL!");
            return new Vector3(0, 0, 0);
        }

        // Get Nearnest Pointss
        List<NearPoint3> nearPs = new List<NearPoint3>();
        for (int i = 0; i < boundary3.Count; i++)
        {
            NearPoint3 p_i = new NearPoint3();
            p_i.v = boundary3[i];
            p_i.dist = (p - p_i.v).magnitude;
            p_i.index = i;
            nearPs.Add(p_i);
        }

        // Fit Line with k nearest points
        List<NearPoint3> kNearPs = (from a in nearPs orderby a.dist ascending select a).Take(nearestPointNum_LocalTangential).ToList();
        List<Vector3> kNearPv = null;
        kNearPv = tanSide == TangentialSide.LeftSide ? kNearPv3_left : kNearPv3_right;
        kNearPv.Clear();
        foreach (var kNearP in kNearPs)
        {
            if (kNearP.dist < frame.r * precentOfRadius_maxDist_LocalTangential)
                kNearPv.Add(kNearP.v);
        }
        var fitline = new RansacLine3d(0.05, 0.9);

        Ray Rayd;
        if (fitline.Estimate(kNearPv) != null)
        {
            Rayd = fitline.bestline.ToRay();
            // Check Local Tangential Direction
            if (Vector3.Dot(Rayd.direction, frame.dire) < 0)
            {
                Rayd.direction = -1 * Rayd.direction;
            }
            if (tanSide == TangentialSide.RightSide)
            {
                //BestLine = fitline.bestline;
            }
            return Rayd.direction.normalized;
        }
        else
        {
            if (!m_engine.m_is_quiet) Debug.Log("Error: RANSAC Fit failure!");
            return new Vector3(0, 0, 0);
        }
    }
    private bool RayTracein2DPlane(List<Vector2> points, Vector2 c, Vector2 dire, out Vector2 intersect)
    {
        intersect = new Vector2(0, 0);

        double minDist = double.MaxValue;
        dire = dire.normalized;
        Ray2D ray = new Ray2D(c, dire);

        List<Vector2> intersecs = new List<Vector2>();
        foreach (var p in points)
        {
            if (Utility.DistanceToRay(ray, p) < minDist
                && Vector2.Dot(p - c, dire) > 0)
            {
                minDist = Utility.DistanceToRay(ray, p);
                intersect = p;
            }
        }

        if (Utility.IsNull(intersect))
        {
            Debug.Log("Error: No Intersect!");
            return false;
        }
        return true;
    }
    private bool RayTracein2DPlane_nearest_twoside(List<List<Vector2>> lines,
        Vector2 curp2, Vector2 curdire2,
        Vector2 base1, Vector2 base2,
        Vector2 tangential1, Vector2 tangential2,
        out Vector2 intersec1, out Vector2 intersec2)
    {
        double insecPs_Dist_theshold = 2.5;
        double insec_base_Dist_theshold = 15;

        Vector2 cutNormal = Vector3.Cross(curdire2, new Vector3(0, 0, 1)).normalized;
        Ray ray = new Ray(curp2, cutNormal);

        List<Vector2> intersecs_all = new List<Vector2>();
        // Step 1: Mark the points and lines
        foreach (var line in lines)
        {
            // 1  标记和trace到的点 以及其对于的线
            List<Vector2> intersecs = new List<Vector2>();
            List<int> indexs = new List<int>();
            for (int i = 0; i < line.Count; i++)
                if (Utility.DistanceToRay(ray, line[i]) < insecPs_Dist_theshold)
                    intersecs.Add(line[i]);

            // 2  每条线内部 点分类  得到single交点
            List<List<Vector2>> classedIntersecs = IExtension.MyCluster(intersecs, (float)insecPs_Dist_theshold);    // Clustering

            // Average  Intersections of Each classes
            intersecs.Clear();
            for (int i = 0; i < classedIntersecs.Count; i++)
                intersecs.Add(Utility.Average(classedIntersecs[i]));
            foreach (var p in intersecs)
                indexs.Add(FindTheClosetPoint(line, p));

            // 3  去除不符合 切线防线的交点
            // Filter the intersec
            intersecs.Clear();
            foreach (var index in indexs)
            {
                Vector2 p_prev = line[Mathf.Max(0, index - 3)]
                    + line[Mathf.Max(0, index - 2)]
                    + line[Mathf.Max(0, index - 1)];
                p_prev /= 3f;
                Vector2 p_next = line[Mathf.Min(line.Count - 1, index + 1)]
                    + line[Mathf.Min(line.Count - 1, index + 2)]
                    + line[Mathf.Min(line.Count - 1, index + 3)];
                p_next /= 3f;
                Vector2 tangial = (p_next - p_prev).normalized;
                // 夹角小于30度   Normal side
                if (Vector2.Dot(line[index] - curp2, cutNormal) > 0
                    && Mathf.Abs(Vector2.Dot(tangential1, tangial)) > 0.98480775301221/*Mathf.Cos(Mathf.PI / 6f)*/)
                {
                    intersecs_all.Add(line[index]);
                    //Line2 ta = new Line2(line[index], line[index] + 100 * tangial);
                    //m_renderEngine.DrawLine(ta, Color.blue);
                }
                // 夹角小于30度   not Normal side
                if (Vector2.Dot(line[index] - curp2, cutNormal) < 0
                    && Mathf.Abs(Vector2.Dot(tangential1, tangial)) > 0.98480775301221 /*.Cos(Mathf.PI / 6f)*/)
                {
                    intersecs_all.Add(line[index]);
                    //Line2 ta = new Line2(line[index], line[index] + 100 * tangial);
                    //m_renderEngine.DrawLine(ta, Color.blue);
                }
                //Line2 ta = new Line2(line[index], line[index] + 100 * tangial);
                //m_renderEngine.DrawLine(ta, Color.blue);

            }
        }

        // 4  取剩下最近的交点
        // Find the closest point of two base1
        intersec1 = Utility.NewVector2(base1);
        intersec2 = Utility.NewVector2(base2);
        double dist_left = double.MaxValue;
        double dist_right = double.MaxValue;
        for (int i = 0; i < intersecs_all.Count; i++)
        {
            if (Vector2.Dot(intersecs_all[i] - curp2, cutNormal) > 0)
            {
                // Normal side
                double dist_temp = Vector2.Distance(intersecs_all[i], base1);
                if (dist_left > dist_temp && dist_temp < insec_base_Dist_theshold)
                {
                    dist_left = dist_temp;
                    intersec1 = Utility.ProjectToRay(ray, intersecs_all[i]);
                }
            }
            else
            {
                // Not Normal side
                double dist_temp = Vector2.Distance(intersecs_all[i], base2);
                if (dist_right > dist_temp && dist_temp < insec_base_Dist_theshold)
                {
                    dist_right = dist_temp;
                    intersec2 = Utility.ProjectToRay(ray, intersecs_all[i]);
                }
            }
        }
        // 计算剩下符合条件 与 基点最近的点
        //m_renderEngine.DrawPoints(intersecs_all, Color.red);
        //m_renderEngine.DrawPoint(intersec2, Color.red);
        if (base1 != intersec1 && base2 != intersec2)
            return true;
        else
            return false;

        return (base1 != intersec1 && base2 != intersec2) ? true : false;
    }
    private bool RayTracein2DPlane_ortho(List<Vector2> points, Vector2 curp2, Vector2 curdire2, out Vector2 intersec1, out Vector2 intersec2)
    {
        // This Function can be use in both 2D Image World and 3D world
        // 2D Image: set sectionPlaneNormal to new Vector(0,0,1)

        double insecPs_Dist_theshold = 2.5;

        Vector2 cutNormal = Vector3.Cross(curdire2, new Vector3(0, 0, 1)).normalized;
        Ray ray = new Ray(curp2, cutNormal);

        List<Vector2> intersecs = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            if (Utility.DistanceToRay(ray, points[i]) < insecPs_Dist_theshold)
            {
                intersecs.Add(points[i]);
            }
        }

        // Clustering
        //List<List<Vector3>> classedIntersecs = IExtension.ClusterPoints(intersecs);
        List<List<Vector2>> classedIntersecs = IExtension.MyCluster(intersecs, (float)insecPs_Dist_theshold);

        // Average  Intersections of Each classes
        intersecs.Clear();
        for (int i = 0; i < classedIntersecs.Count; i++)
        {
            intersecs.Add(Utility.Average(classedIntersecs[i]));
        }

        // Find the closest point of two side
        intersec1 = new Vector2();
        intersec2 = new Vector2();
        double dist_left = double.MaxValue;
        double dist_right = double.MaxValue;
        for (int i = 0; i < intersecs.Count; i++)
        {
            double dist_temp = Vector3.Distance(intersecs[i], curp2);
            if (Vector2.Dot(intersecs[i] - curp2, cutNormal) > 0)
            {
                // Normal side
                if (dist_left > dist_temp)
                {
                    dist_left = dist_temp;
                    intersec1 = Utility.ProjectToRay(ray, intersecs[i]);
                }
            }
            else
            {
                // Not Normal side
                if (dist_right > dist_temp)
                {
                    dist_right = dist_temp;
                    intersec2 = Utility.ProjectToRay(ray, intersecs[i]);
                }
            }
        }

        if (Utility.IsNull(intersec1)
            && Utility.IsNull(intersec2))
        {
            if (!m_engine.m_is_quiet) Debug.Log("Error: Ray Tracein2DPlane, no intersection points");
            return false;
        }
        else if (Utility.IsNull(intersec1))
        {
            //if (!m_engine.m_is_quiet) Debug.Log("Warining: intersec1 == null");
            return false;
        }
        else if (Utility.IsNull(intersec2))
        {
            //Debug.Log("Warining: intersec2 == null");
            return false;
        }
        if (Vector3.Distance(intersec1, intersec2) < insecPs_Dist_theshold)
        {
            // this two intersection is too close, so let them become same one.s
            Debug.Log("Warining: two intersection is too close");
            return false;
        }
        return true;
    }
    private bool RayTracein3DPlane_ortho(CenterFrame f, List<Vector2> points, Plane sectionPlane, bool isIntersec = true)
    {
        bool ishitted = false;

        Vector3 curp = f.c;
        Vector3 curdire = isIntersec ? f.dire : Vector3.Cross(f.dire, sectionPlane.normal);
        Vector2 curp2 = m_projector.WorldToImage(curp);
        Vector2 curdire2 = (m_projector.WorldToImage(curp + 10f * curdire) - curp2).normalized;

        Vector2 intersec1_2 = new Vector2();
        Vector2 intersec2_2 = new Vector2();
        Vector3 intersec1 = new Vector3();
        Vector3 intersec2 = new Vector3();

        if (RayTracein2DPlane_ortho(points, curp2, curdire2, out intersec1_2, out intersec2_2))
        {
            if (isIntersec && isSnap)
            {
                Vector2 tangential1 = GetLocalTangential(intersec1_2, points, f, TangentialSide.RightSide);
                Vector2 tangential2 = GetLocalTangential(intersec2_2, points, f, TangentialSide.LeftSide);
                Line2 ta1 = new Line2(intersec1_2, intersec1_2 + 100 * tangential1);
                Line2 ta2 = new Line2(intersec2_2, intersec2_2 + 100 * tangential2);
                m_renderEngine.DrawLine(ref ta1, Color.red);
                m_renderEngine.DrawLine(ref ta2, Color.red);

                RayTracein2DPlane_nearest_twoside(
                    boundary2_lines,
                    curp2, curdire2,
                    intersec1_2, intersec2_2,
                    tangential1, tangential2,
                    out intersec1_2, out intersec2_2);
            }
            intersec1 = m_projector.Proj2dToPlane(sectionPlane, intersec1_2);
            intersec2 = m_projector.Proj2dToPlane(sectionPlane, intersec2_2);
            ishitted = true;
        }
        else
        {
            if (!Utility.IsNull(intersec1_2))
                intersec1 = m_projector.Proj2dToPlane(sectionPlane, intersec1_2);
            if (!Utility.IsNull(intersec2_2))
                intersec2 = m_projector.Proj2dToPlane(sectionPlane, intersec2_2);
            ishitted = false;
        }

        // Output data
        if (isIntersec)
        {
            f.Insection1 = intersec1;
            f.Insection2 = intersec2;
        }
        else
        {
            f.hit1 = intersec1;
            f.hit2 = intersec2;
        }
        return ishitted;
    }
    private int[] SelectTwoCorners(List<Vector2> cornerPoints2, List<Vector2> boundary2)
    {
        List<float> shortestDist = new List<float>();
        for (int i = 0; i < cornerPoints2.Count; i++)
        {
            float dist = float.MaxValue;
            foreach (var p in boundary2)
            {
                dist = Mathf.Min(Vector2.Distance(cornerPoints2[i], p), dist);
            }
            shortestDist.Add(dist);
        }
        List<float> sortedList = (from a in shortestDist orderby a ascending select a).Take(2).ToList();
        int[] i_corner = new int[2];
        i_corner[0] = shortestDist.IndexOf(sortedList[0]);
        i_corner[1] = shortestDist.IndexOf(sortedList[1]);
        return i_corner;
    }
    private float Optimize_Radius(CenterFrame frame)
    {
        frame_forOptimal = frame;
        double r = frame_forOptimal.r;
        double[] bndl = new double[] { 0 };
        double[] x = new double[] { r / 20 };
        double[] bndu = new double[] { 20 };
        int funNum = 3;

        double diffstep = 0.0001;
        double epsg = 0.000000000001;
        double epsf = 0;
        double epsx = 0;
        int maxits = 0;
        alglib.minlmstate state;
        alglib.minlmreport rep;

        //set timer
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        Inter_Num = 0;
        // Do the optima
        alglib.minlmcreatev(funNum, x, diffstep, out state);
        alglib.minlmsetbc(state, bndl, bndu);
        alglib.minlmsetcond(state, epsg, epsf, epsx, maxits);
        alglib.minlmoptimize(state, function_project_radius, null, null);
        alglib.minlmresults(state, out x, out rep);
        stopwatch.Stop();

        //Debug.Log("End fitting , Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        return (float)x[0];
    }
    CenterFrame frame_forOptimal;
    private void function_project_radius(double[] x, double[] fi, object obj)
    {
        // Step 0: Init Params
        float[] weight = new float[] { 0.02f, 1.0f, 1.0f };
        Inter_Num++;
        frame_forOptimal.r = (float)x[0];

        // ignore if x[0] = 0
        if (x[0] == 0)
        {
            fi[0] = weight[0] * 1;
            fi[1] = weight[1] * 100;
            fi[2] = weight[2] * 100;
            //double cost = fi[0] + fi[1] + fi[2];
            //Debug.Log("R : " + x[0]);
            //Debug.Log("f1: " + fi[0]);
            //Debug.Log("f2: " + fi[1]);
            //Debug.Log("f3: " + fi[2]);
            //Debug.Log(Inter_Num + " cost:" + cost);
            return;
        }
        List<Vector2> boundary2_face = ComputeFaceBoundary2(frame_forOptimal);

        // Step 1: maximum radius f[0]

        // Step 2: Mask Cover f[1]
        double vNotCover = 0;
        foreach (var p in boundary2_face)
        {
            int h = Mathf.RoundToInt(p.y);
            int w = Mathf.RoundToInt(p.x);

            // out of Image
            if (h >= img.Height || h < 0 || w >= img.Width || w < 0)
            {
                vNotCover += 1;
                continue;
            }

            // Handle
            if (IsHandle)
                vNotCover += img[h, w].Intensity == 0 ? 1 : 0;
            // Body
            else
            {
                // 1 : handle area is not mask   0: handle area is mask
                if (img[h, w].Intensity != 0 && img[h, w].Intensity != 255)
                    vNotCover += 1 * x[0];
                //vNotCover += 0.0006 * x[0];
                // 1 for not cover
                if (img[h, w].Intensity == 0)
                    vNotCover += 1;
            }
        }
        # region backup
        //vNotCover /= (double)boundary2_face.Count;
        // equal side length         
        //fi[0] = weight[0] * 1.0 / (Mathf.PI * Mathf.Pow((float)x[0], 2));
        //fi[0] = 0;
        # endregion

        // Step 3: insection on circle boundary
        float sum_dist = 0;
        if (!Utility.IsNull(frame_forOptimal.Insection1) && !Utility.IsNull(frame_forOptimal.Insection2))
        {
            Vector2[] intersecs2 = new Vector2[2];
            intersecs2[0] = m_projector.WorldToImage(frame_forOptimal.Insection1);
            intersecs2[1] = m_projector.WorldToImage(frame_forOptimal.Insection2);
            foreach (var pi in intersecs2)
            {
                double dist_mini = double.MaxValue;
                foreach (var p in boundary2_face)
                {
                    double dist_temp = Vector2.Distance(pi, p);
                    dist_mini = dist_mini > dist_temp ? dist_temp : dist_mini;
                }
                sum_dist += (float)dist_mini;
            }

        }
        if (isCuboid) weight[1] = 15;
        fi[0] = weight[0] * 1.0 / x[0];
        fi[1] = weight[1] * vNotCover;
        fi[2] = weight[2] * sum_dist;

        double cost = fi[0] + fi[1] + fi[2];

        //Debug.Log(Inter_Num + " cost:" + cost +", R: " + x[0] + ", f1: " + fi[0]+", f2: " + fi[1] +", f3: " + fi[2]);
    }

    # region Generate Texture

    private void GenerateMeshUVMaps(Mesh mesh)
    {
        Vector2[] uvs = m_projector.WorldToImage(mesh.vertices.ToList()).ToArray();
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i].x /= img.Width;
            uvs[i].y = (img.Height - uvs[i].y) / img.Height;
        }
        mesh.uv = uvs;
    }

    private void GenerateMeshUVMaps_symmetry(Mesh mesh, Plane setPlane, List<Vector3> IndexList, List<Quad> RectList)
    {
        GenerateMeshUVMaps(mesh);

        mesh.RecalculateNormals();
        Vector3[] verties = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        //Vector2[] uv2 = new Vector2[mesh.vertices.Length];

        // Fix Normal
        bool isFlip = sectionPlane.GetSide(m_projector.m_mainCamera.transform.position);

        foreach (var index in IndexList)
        {
            int i = (int)index.x; // Rectlist Index
            int j = (int)index.y; // Corner Point Index
            int x = (int)index.z; // Mesh Index (vertices, UV)

            // Fix Top Face
            if (i == 0) continue;

            Vector3 rayDire = verties[x] - m_projector.m_mainCamera.transform.position;
            Vector3 normal = normals[x];

            // Can Hit!
            if (isFlip ? !sectionPlane.GetSide(verties[x]) : sectionPlane.GetSide(verties[x]))
            {
                //m_renderEngine.DrawPoint(verties[x], Color.red);
                Vector3 v_sym = Utility.PlaneSymmetryPoint(setPlane, verties[x]);
                //m_renderEngine.DrawPoint(v_sym, Color.blue);
                int[] vs = FindCloset2P(RectList[i].CornerPoints3d, v_sym);
                if (Mathf.Abs(vs[0] - j) == 1) vs[0] = (j + 2) % 4;
                // Get UV
                int i0 = FindTheIndex(IndexList, i, vs[0]);
                uv[x] = uv[i0];
            }
        }

        // End Top Face Texture
        for (int i = 0; i < RectList.Last().CornerPoints3d.Count; i++)
        {
            int i_end = FindTheIndex(IndexList, RectList.Count - 1, i);
            int i_top = FindTheIndex(IndexList, 0, i);
            uv[i_end] = uv[i_top];
        }

        mesh.uv = uv;
    }
    private void GenerateMeshUVMaps_symmetry(Mesh mesh, Plane setPlane, List<Vector3> IndexList, List<Circle3> CircleList)
    {
        GenerateMeshUVMaps(mesh);

        mesh.RecalculateNormals();
        Vector3[] verties = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;

        // Fix Normal
        bool isFlip = sectionPlane.GetSide(m_projector.m_mainCamera.transform.position);


        foreach (var index in IndexList)
        {
            int i = (int)index.x;
            int j = (int)index.y;
            int x = (int)index.z;

            // Fix Top Face
            if (i == 0) continue;
            // Fix Bottom Face
            if (i == CircleList.Count - 1)
                continue;

            Vector3 rayDire = verties[x] - m_projector.m_mainCamera.transform.position;
            Vector3 normal = normals[x];
            // Can Hit!
            if (isFlip ? !sectionPlane.GetSide(verties[x]) : sectionPlane.GetSide(verties[x]))
            {
                //m_renderEngine.DrawPoint(verties[x], Color.red);
                Vector3 v_sym = Utility.PlaneSymmetryPoint(setPlane, verties[x]);
                //m_renderEngine.DrawPoint(v_sym, Color.blue);
                int[] vs = FindCloset2P(CircleList[i].CirclePoints, v_sym);
                // Get UV
                int i0 = FindTheIndex(IndexList, i, vs[0]);
                int i1 = FindTheIndex(IndexList, i, vs[1]);
                float d0 = Vector3.Distance(v_sym, verties[i0]);
                float d1 = Vector3.Distance(v_sym, verties[i1]);
                float w0 = d1 / (d0 + d1);
                float w1 = d0 / (d0 + d1);
                uv[x] = w0 * uv[i0] + w1 * uv[i1];
            }
        }

        // End Top Face Texture
        mesh.uv = uv;
    }

    private int FindClosetP(List<Vector3> ps, Vector3 v)
    {
        double dist = double.MaxValue;
        int i_close = 0;
        for (int i = 0; i < ps.Count; i++)
        {
            double dist_temp = Vector3.Distance(ps[i], v);
            if (dist > dist_temp)
            {
                dist = dist_temp;
                i_close = i;
            }
        }
        return i_close;
    }

    private int[] FindCloset2P(List<Vector3> ps, Vector3 v)
    {
        int[] vs = new int[2];
        vs[0] = FindClosetP(ps, v);

        double dist = double.MaxValue;

        for (int i = 0; i < ps.Count; i++)
        {
            double dist_temp = Vector3.Distance(ps[i], v);
            if (dist > dist_temp && i != vs[0])
            {
                dist = dist_temp;
                vs[1] = i;
            }
        }
        return vs;
    }

    private int FindTheIndex(List<Vector3> indexs, int i, int j)
    {
        foreach (var v in indexs)
        {
            if ((int)v.x == i && (int)v.y == j) return (int)v.z;
        }
        return -1;  // error
    }
    # endregion
    private int FindTheClosetPoint(List<Vector2> ps, Vector2 p)
    {
        int index = 0;
        float dist_min = float.MaxValue;
        for (int i = 0; i < ps.Count; i++)
        {
            float dist = Vector2.Distance(ps[i], p);
            if (dist < dist_min)
            {
                index = i;
                dist_min = dist;
            }
        }
        return index;
    }
}

using System.Collections.Generic;
using System;
using System.Linq;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using NumericalRecipes;
using MyGeometry;

using UnityExtension;
using UnityEngine;

public class FaceEngine : MonoBehaviour
{
    public GraphicsEngine m_engine;
    [HideInInspector]
    public Image<Gray, byte> img;                                   // Mark of Image
    [HideInInspector]
    public Circle3 topCircle = new Circle3();                       // The Top Circle of the cylinder
    [HideInInspector]
    public Quad topRect = new Quad();
    [HideInInspector]
    public bool isTopCircle_Current = false;
    [HideInInspector]
    public float fov_face;

    // These for Multi Face Engine Optimal 
    [HideInInspector]
    public float x0, x1, x2, x3;

    List<Vector2> boundary2 = null;                                 // The boundary of the Mark of the Image
    List<Vector2> CirclePoints_2d = null;                           // The Circle Sample Points projected on the screen
    Projector m_projector;
    RenderEngine m_renderEngine;

    // the following points are all image pixels
    Vector2 mean = new Vector2();
    Vector2 majorstartp = new Vector2();
    Vector2 majorendp = new Vector2();
    Vector2 minorstartp = new Vector2();
    Vector2 minorendp = new Vector2();

    public string imgPath;

    int Inter_Num = -1;                                             // The interaction time of optima

    Vector3 majorstartp3;
    Vector3 majorendp3;
    Vector3 minorstartp3;

    public List<Vector2> axis = new List<Vector2>();
    public Image<Gray, byte> body_img;
    public Vector2 start_dire = new Vector2();
    float fov_optimal_scale = 10f;

    public void CreateFaceEngine(GraphicsEngine m_engine, Projector m_projector, RenderEngine m_renderEngine)
    {
        this.m_engine = m_engine;
        this.m_renderEngine = m_renderEngine;
        this.m_projector = m_projector;
    }

    public void SolveCircle(Image<Gray, byte> mask)
    {
        this.img = mask;

        //// Fit Circle
        FitEllipse();
        FitTopCircle();
        isTopCircle_Current = true;
    }
    public void SolveRect(Image<Gray, byte> mask)
    {
        this.img = mask;
        //// Fit Rectangle
        FitTopRectangle();
        isTopCircle_Current = false;
    }

    void OnRenderObject()
    {
        if (topCircle != null)
            m_renderEngine.DrawCircle(ref topCircle, Color.red);
        if (topRect != null)
            m_renderEngine.DrawRect3(ref topRect, Color.red);

        //m_renderEngine.DrawCircle(ref c1, Color.red);
        //m_renderEngine.DrawCircle(ref topCircle, Color.red);
    }

    #region Circle fit
    public double LikeAEllipse(List<Vector2> points)
    {
        foreach (Vector2 p in points)
            mean += p;

        mean /= points.Count;
        float W = points.Count;

        float[,] C = new float[2, 2];
        foreach (Vector2 point in points)
        {
            C[0, 0] += (point.x - mean.x) * (point.x - mean.x);
            C[0, 1] += (point.x - mean.x) * (point.y - mean.y);

            C[1, 0] += (point.y - mean.y) * (point.x - mean.x);
            C[1, 1] += (point.y - mean.y) * (point.y - mean.y);
        }

        C[0, 0] /= W;
        C[0, 1] /= W;
        C[1, 0] /= W;
        C[1, 1] /= W;

        Matrix2d CM = new Matrix2d(C);
        double error = Mathf.Abs(W - 4 * Mathf.PI * Mathf.Sqrt(CM.Det()));
        error /= W;
        //Debug.Log("Like a ellipse error: " + error); //10^-2 may be a good threshold

        return error;
    }

    public double FitEllipse()
    {
        List<Vector2> points = IExtension.GetMaskPoints(img);
        foreach (Vector2 p in points)
            mean += p;

        mean /= points.Count;
        float W = points.Count;


        float[,] C = new float[2, 2];
        foreach (Vector2 point in points)
        {
            C[0, 0] += (point.x - mean.x) * (point.x - mean.x);
            C[0, 1] += (point.x - mean.x) * (point.y - mean.y);

            C[1, 0] += (point.y - mean.y) * (point.x - mean.x);
            C[1, 1] += (point.y - mean.y) * (point.y - mean.y);
        }

        C[0, 0] /= W;
        C[0, 1] /= W;
        C[1, 0] /= W;
        C[1, 1] /= W;

        Matrix2d CM = new Matrix2d(C);
        SVD svd = new SVD(C);
        //svd.w - eigenvalue, start from 1
        //svd.u - eigenvector, start from 1

        int max = 1, min = 2;
        if (svd.w[max] < svd.w[min])
        {
            int temp = max;
            max = min;
            min = temp;
        }

        float major = 2 * Mathf.Sqrt(svd.w[max]);
        Vector2 majoraxis = new Vector2(svd.u[1, max], svd.u[2, max]);
        majoraxis.Normalize();
        float minor = 2 * Mathf.Sqrt(svd.w[min]);
        Vector2 minoraxis = new Vector2(svd.u[1, min], svd.u[2, min]);
        minoraxis.Normalize();

        majorendp = mean + majoraxis * major;
        majorstartp = mean - majoraxis * major;
        minorendp = mean + minoraxis * minor;
        minorstartp = mean - minoraxis * minor;

        double error = Mathf.Abs(W - 4 * Mathf.PI * Mathf.Sqrt(CM.Det()));
        error /= W;
        if (!m_engine.m_is_quiet) Debug.Log("Like a ellipse error: " + error); //10^-2 may be a good threshold

        return error;
    }

    private Circle3 SolveTopCircle()
    {
        // Camera
        Vector3 eyePos = m_projector.m_mainCamera.transform.position;

        // Step 1: Find Long Axis and short Axis
        // long Axis: majorendp majorstartp
        // Short Axis: minorendp minorstartp

        // Step 2: Set radius, Center & two point on Long Axis
        float dist_c = 10;
        Vector2 c_2d = (majorstartp + majorendp) / 2;
        Vector3 c = eyePos + dist_c / m_projector.GenerateRay(c_2d).direction.z * m_projector.GenerateRay(c_2d).direction;
        majorstartp3 = eyePos + dist_c / m_projector.GenerateRay(majorstartp).direction.z * m_projector.GenerateRay(majorstartp).direction;
        majorendp3 = eyePos + dist_c / m_projector.GenerateRay(majorendp).direction.z * m_projector.GenerateRay(majorendp).direction;
        float radius = Vector3.Distance(majorstartp3, majorendp3) / 2;

        // Step 3: Get Project Ray from end point of short Axis
        minorstartp3 = IExtension.RayHitShpere(eyePos, m_projector.GenerateRay(minorstartp).direction, c, (double)radius);

        // Step 4: Get one from two results
        // chenxin      to be improved
        // Step 5: Use 3 points to get Circle Plane, then this Circle


        return new Circle3(c, radius, new Plane(majorstartp3, majorendp3, minorstartp3), boundary2.Count);
    }

    public void FitTopCircle()
    {
        boundary2 = IExtension.GetBoundary(img);
        topCircle = SolveTopCircle();
        m_renderEngine.DrawPoints(topCircle.CirclePoints, Color.yellow);
        //List<Vector2> circle2d = m_projector.GetProjectionPoints_2D(topCircle.CirclePoints);
        //Image<Bgr, byte> blobimg = new Image<Bgr, byte>(this.img.Width, this.img.Height);

        //foreach (Vector2 p in circle2d)
        //{
        //    int i = (int)p.y;
        //    int j = (int)p.x;
        //    blobimg[i, j] = new Bgr(255, 255, 255);
        //}

        //blobimg.Save("C:/Users/Dell/Desktop/blob.jpg");
        //foreach (Vector2 p in boundary2)
        //{
        //    int i = (int)p.y;
        //    int j = (int)p.x;
        //    blobimg[i, j] = new Bgr(0, 0, 255);
        //}
        //blobimg.Save("C:/Users/Dell/Desktop/blob2.jpg");


        Optimize();
        ReOptimize();
        //topCircle.Save(imgPath);
    }

    private Vector2 ComputeStartDirecFromAxis(List<Vector2> axis)
    {
        // average dires of start 5 frame
        int sub_frame = 5;
        Vector2 dire_aver = new Vector2(0, 0);
        List<Vector2> dires = FitExtension.SetPointsTangential(axis);
        foreach (var dire in dires.GetRange(0, Mathf.Min(dires.Count, sub_frame))) dire_aver += dire;
        return dire_aver.normalized;
    }

    public void Optimize()
    {
        // Init FOV
        m_engine.m_fovOptimizer.SetFOV(20);

        double thetax, thetay, thetaz;
        Utility.NormalToRotation(topCircle.Normal, out thetax, out thetay, out thetaz);
        start_dire = ComputeStartDirecFromAxis(axis);
        float fov_init = m_projector.m_mainCamera.fieldOfView;

        // Optimal Solution
        double[] bndl = new double[] { 0, 0, 0, 0.001, 1 / fov_optimal_scale };
        double[] x = new double[] { thetax, thetay, thetaz, topCircle.Radius, fov_init / fov_optimal_scale }; //add center
        double[] bndu = new double[] { Mathf.PI, Mathf.PI, Mathf.PI, 10, 179 / fov_optimal_scale };

        int funNum = 4;

        double diffstep = 0.0002;
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
        alglib.minlmoptimize(state, function_project, null, null);
        alglib.minlmresults(state, out x, out rep);
        stopwatch.Stop();
        if (!m_engine.m_is_quiet) Debug.Log("End fitting , Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        // Update FOV
        m_engine.m_fovOptimizer.SetFOV((float)x[4] * fov_optimal_scale);
        fov_face = (float)x[4] * fov_optimal_scale;

        // Update Circle
        Vector3 center = Utility.NewVector3(topCircle.Center);

        Vector3 normal = Utility.RotationToNormal(x[0], x[1], x[2]); normal.Normalize();
        //Vector3 normal = new Vector3((float)x[0], (float)x[1], (float)x[2]); normal.Normalize();
        float radius = (float)x[3];

        topCircle = new Circle3(center, radius, new Plane(normal, center));
        CirclePoints_2d = m_projector.WorldToImage(topCircle.CirclePoints);

        m_renderEngine.DrawLine(axis.First(), axis.First() + 500 * start_dire.normalized, Color.red);
        m_renderEngine.DrawLine(topCircle.Center, topCircle.Center + 2 * topCircle.Normal, Color.blue);

    }

    public void ReOptimize()
    {
        double[] bndl = new double[] { -10, -10, 0 };
        double[] x = new double[] { topCircle.Center.x, topCircle.Center.y, topCircle.Center.z }; //add center
        double[] bndu = new double[] { 10, 10, 20 };

        int funNum = 1;

        double diffstep = 0.00002;
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
        alglib.minlmoptimize(state, function_withcenter, null, null);
        alglib.minlmresults(state, out x, out rep);
        stopwatch.Stop();
        if (!m_engine.m_is_quiet) Debug.Log("End fitting center, Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        // Update Circle
        Vector3 center = new Vector3((float)x[0], (float)x[1], (float)x[2]);
        Vector3 normal = topCircle.Normal;
        double radius = topCircle.Radius;

        topCircle = new Circle3(center, (float)radius, new Plane(normal, center));
        CirclePoints_2d = m_projector.WorldToImage(topCircle.CirclePoints);
    }

    private void function_project(double[] x, double[] fi, object obj)
    {
        m_engine.m_fovOptimizer.SetFOV((float)x[4] * fov_optimal_scale);
        topCircle = SolveTopCircle();

        // Step 1: Init Params
        Inter_Num++;
        Vector3 center = Utility.NewVector3(topCircle.Center);
        Vector3 normal = Utility.NewVector3(Utility.RotationToNormal(x[0], x[1], x[2])); normal.Normalize();

        double radius = x[3];
        Circle3 myC = new Circle3(center, (float)radius, new Plane(normal, center), this.boundary2.Count);

        // Step 2: Do projection
        CirclePoints_2d = m_projector.WorldToImage(myC.CirclePoints);

        // Step 3: Do the evaluation
        //double dist_all = 0;
        //for (int i = 0; i < CirclePoints_2d.Count; i++)
        //{
        //    double dist = double.MaxValue;
        //    for (int j = 0; j < boundary2.Count; j++)
        //    {
        //        dist = Math.Min(dist, (CirclePoints_2d[i] - boundary2[j]).sqrMagnitude);
        //    }
        //    dist_all += dist;
        //}
        //fi[0] = dist_all;

        // Step 3': Mask cover
        int notinmask_count = 0;
        for (int i = 0; i < CirclePoints_2d.Count; i++)
        {
            int xx = (int)CirclePoints_2d[i].x;
            xx = Math.Max(0, xx);
            xx = Math.Min(xx, this.img.Width - 1);

            int yy = (int)CirclePoints_2d[i].y;
            yy = Math.Max(0, yy);
            yy = Math.Min(xx, this.img.Height - 1);

            //Debug.Log(yy + " " + xx);

            if (!this.img[yy, xx].Equals(new Gray(255)))
            {
                notinmask_count++;
            }
        }

        // Step 4: FOV Axis 3D
        //Vector3 sectionNormal = Vector3.Cross(Vector3.Cross(normal, new Vector3(1, 0, 1).normalized),normal);
        //Plane sectionPlane = new Plane(sectionNormal.normalized, center);
        //Vector3 axis_dire = m_projector.Proj2dToPlane_v(sectionPlane, m_projector.WorldToImage(center), start_dire);
        //fi[3] = 10f * Mathf.Min(180 - Vector2.Angle(axis_dire.normalized, normal.normalized), Vector2.Angle(axis_dire.normalized, normal.normalized));
        //fi[3] = 1 / Mathf.Abs(Vector3.Dot(axis_dire.normalized, normal.normalized));

        // Step 4': FOV Axis 2D
        Vector2 center2 = m_projector.WorldToImage(center);
        Vector2 end2 = m_projector.WorldToImage(center + 100 * normal);
        Vector2 normal2 = (end2 - center2).normalized;

        // Set Cost function
        fi[0] = notinmask_count / CirclePoints_2d.Count;
        fi[1] = normal.sqrMagnitude - 1;
        fi[2] = 1 / 40 * radius;
        fi[3] = Mathf.Min(180 - Vector2.Angle(normal2, start_dire.normalized), Vector2.Angle(normal2, start_dire.normalized));
        //Debug.Log("FOV: " + x[4] * fov_optimal_scale + " Cost: " + fi[3]);
    }

    private void function_withcenter(double[] x, double[] fi, object obj)
    {
        Inter_Num++;
        Vector3 center = new Vector3((float)x[0], (float)x[1], (float)x[2]);
        Vector3 normal = topCircle.Normal;
        double radius = topCircle.Radius;
        Circle3 myC = new Circle3(center, (float)radius, new Plane(normal, center), this.boundary2.Count);

        CirclePoints_2d = m_projector.WorldToImage(myC.CirclePoints);
        //double dist_all = 0;
        //for (int i = 0; i < CirclePoints_2d.Count; i++)
        //{
        //    double dist = double.MaxValue;
        //    for (int j = 0; j < boundary2.Count; j++)
        //    {
        //        dist = Math.Min(dist, (CirclePoints_2d[i] - boundary2[j]).sqrMagnitude);
        //    }
        //    dist_all += dist;
        //}

        //// Set Cost function
        //fi[0] = dist_all;

        int notinmask_count = 0;
        for (int i = 0; i < CirclePoints_2d.Count; i++)
        {
            int xx = (int)CirclePoints_2d[i].x;
            xx = Math.Max(0, xx);
            xx = Math.Min(xx, this.img.Width - 1);

            int yy = (int)CirclePoints_2d[i].y;
            yy = Math.Max(0, yy);
            yy = Math.Min(xx, this.img.Height - 1);

            //Debug.Log(yy + " " + xx);

            if (!this.img[yy, xx].Equals(new Gray(255)))
            {
                notinmask_count++;
            }
        }
        fi[0] = notinmask_count / CirclePoints_2d.Count;


        // if (Inter_Num % 20 == 0)
        //Debug.Log(Inter_Num + "|| N: " + x[0] + " " + x[1] + " " + x[2] + " cost:" + dist_all);
    }
    #endregion

    Vector3 initcenter = new Vector3();
    float depth = 10;
    public void FitTopRectangle()
    {
        // Init FOV
        m_engine.m_fovOptimizer.SetFOV(20);

        initcenter = new Vector3();
        this.inlier_list.Clear();
        //m_renderEngine.ClearAll();
        this.boundary2 = IExtension.GetBoundary(this.img);
        this.FitQuad();
        Optimize_4CP();
        //topRect.Save(imgPath);

        //// Compute center
        //Vector2 center_2d = new Vector2();
        //foreach (Vector2 p in this.boundary2)
        //    center_2d += p;
        //center_2d /= this.boundary2.Count;
        //initcenter = m_projector.ImageToWorld(center_2d);
        //m_renderEngine.DrawPoint(initcenter, Color.blue);

        //Vector2 diagonalIntersection = this.FindTrapezoidDiagonalIntersection();
        //if (!Utility.IsNull(diagonalIntersection))
        //{
        //    if (!m_engine.m_is_quiet) Debug.Log("Use diagonal intersection.");
        //    initcenter = m_projector.ImageToWorld(diagonalIntersection);
        //    //m_renderEngine.DrawPoint(initcenter, Color.red);
        //}
    }

    Ray ray1, ray2, ray3, ray4;
    public void Optimize_4CP()
    {
        start_dire = ComputeStartDirecFromAxis(axis);
        float fov_init = m_projector.m_mainCamera.fieldOfView;

        Vector2 v1 = corner_points[0];
        Vector2 v2 = corner_points[1];
        Vector2 v3 = corner_points[2];
        Vector2 v4 = corner_points[3];

        ray1 = m_projector.GenerateRay(v1);
        ray2 = m_projector.GenerateRay(v2);
        ray3 = m_projector.GenerateRay(v3);
        ray4 = m_projector.GenerateRay(v4);

        Vector3 v1_3 = ray1.GetPoint(depth);
        Vector3 v2_3 = ray2.GetPoint(depth);
        Vector3 v3_3 = ray3.GetPoint(depth);

        // Optimal Solution
        double[] bndl = new double[] { 0, 0, 0, 0, 1 / fov_optimal_scale };
        double[] x = new double[] { depth - 0.02, depth, depth + 0.02, depth, fov_init / fov_optimal_scale };
        double[] bndu = new double[] { 20, 20, 20, 20, 179 / fov_optimal_scale };
        int funNum = 11;

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
        alglib.minlmoptimize(state, function_project_rect_4CP, null, null);
        alglib.minlmresults(state, out x, out rep);

        stopwatch.Stop();
        if (!m_engine.m_is_quiet) Debug.Log("End fitting , Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        m_engine.m_fovOptimizer.SetFOV((float)x[4] * fov_optimal_scale);
        fov_face = (float)x[4] * fov_optimal_scale;

        ray1 = m_projector.GenerateRay(v1);
        ray2 = m_projector.GenerateRay(v2);
        ray3 = m_projector.GenerateRay(v3);
        ray4 = m_projector.GenerateRay(v4);

        x0 = (float)x[0]; x1 = (float)x[1]; x2 = (float)x[2]; x3 = (float)x[3];
        v1_3 = ray1.GetPoint((float)x[0]);
        v2_3 = ray2.GetPoint((float)x[1]);
        v3_3 = ray3.GetPoint((float)x[2]);
        Vector3 v4_3 = ray4.GetPoint((float)x[3]);
        // project to a new plane
        List<Vector3> points = new List<Vector3>();
        points.Add(v1_3);
        points.Add(v2_3);
        points.Add(v3_3);
        points.Add(v4_3);
        Vector3 mean = v1_3 + v2_3 + v3_3 + v4_3;
        mean /= 4;
        Vector3 normal = Utility.GetNormal(points);
        Plane newplane = new Plane(normal, mean);
        v1_3 = Utility.PlaneProjectPoint(newplane, v1_3);
        v2_3 = Utility.PlaneProjectPoint(newplane, v2_3);
        v3_3 = Utility.PlaneProjectPoint(newplane, v3_3);
        v4_3 = Utility.PlaneProjectPoint(newplane, v4_3);
        if (!m_engine.m_is_quiet) Debug.Log("angle: " + Vector3.Angle(v1_3 - v2_3, v3_3 - v2_3));
        topRect = new Quad(v1_3, v2_3, v3_3);
        topRect.FitRect();
        m_renderEngine.DrawRect3(topRect, Color.green);
        m_renderEngine.DrawLine(axis.First(), axis.First() + 500 * start_dire.normalized, Color.red);
        m_renderEngine.DrawLine(topRect.Center, topRect.Center + 2 * topRect.Normal, Color.blue);

        if (!m_engine.m_is_quiet) Debug.Log("angle: " + Vector3.Angle(topRect.CornerPoints3d[0] - topRect.CornerPoints3d[1], topRect.CornerPoints3d[2] - topRect.CornerPoints3d[1]));
        //List<Quad> rects = new List<Quad>();
        //rects.Add(topRect);
        //m_engine.SaveCubeCorner(rects);
    }
    private void function_project_rect_4CP(double[] x, double[] fi, object obj)
    {
        //m_engine.m_fovOptimizer.SetFOV((float)x[4] * fov_optimal_scale);

        Vector2 v1_2 = corner_points[0];
        Vector2 v2_2 = corner_points[1];
        Vector2 v3_2 = corner_points[2];
        Vector2 v4_2 = corner_points[3];

        ray1 = m_projector.GenerateRay(v1_2);
        ray2 = m_projector.GenerateRay(v2_2);
        ray3 = m_projector.GenerateRay(v3_2);
        ray4 = m_projector.GenerateRay(v4_2);

        // Step 1: Init Params
        Inter_Num++;
        Vector3 v1 = ray1.GetPoint((float)x[0]);
        Vector3 v2 = ray2.GetPoint((float)x[1]);
        Vector3 v3 = ray3.GetPoint((float)x[2]);
        Vector3 v4 = ray4.GetPoint((float)x[3]);

        // equal side length 
        float l1 = Vector3.Distance(v1, v2);
        float l2 = Vector3.Distance(v2, v3);

        float l1_2 = Vector2.Distance(v1_2, v2_2);
        float l2_2 = Vector2.Distance(v2_2, v3_2);

        fi[0] = (Vector3.Distance(v1, v2) - Vector3.Distance(v3, v4)) * 90;
        fi[1] = (Vector3.Distance(v2, v3) - Vector3.Distance(v1, v4)) * 90;

        // ratio not boo big
        fi[9] = Mathf.Max(l1, l2) / Mathf.Min(l1, l2) - Mathf.Max(l1_2, l2_2) / Mathf.Min(l1_2, l2_2);

        // vertical 0-90
        fi[2] = Mathf.Abs(Vector3.Angle(v2 - v1, v4 - v1) - 90);
        fi[3] = Mathf.Abs(Vector3.Angle(v1 - v2, v3 - v2) - 90);
        fi[4] = Mathf.Abs(Vector3.Angle(v2 - v3, v4 - v3) - 90);
        fi[5] = Mathf.Abs(Vector3.Angle(v1 - v4, v3 - v4) - 90);

        // on the same plane 0-180
        fi[6] = Vector3.Angle(Vector3.Cross(v2 - v1, v4 - v1), Vector3.Cross(v4 - v3, v2 - v3));
        fi[7] = Vector3.Angle(Vector3.Cross(v1 - v2, v3 - v2), Vector3.Cross(v3 - v4, v1 - v4));

        // mask cover
        Vector3 v1_3 = ray1.GetPoint((float)x[0]);
        Vector3 v2_3 = ray2.GetPoint((float)x[1]);
        Vector3 v3_3 = ray3.GetPoint((float)x[2]);
        Vector3 v4_3 = ray4.GetPoint((float)x[3]);

        // project to a new plane
        List<Vector3> points = new List<Vector3>();
        points.Add(v1_3);
        points.Add(v2_3);
        points.Add(v3_3);
        points.Add(v4_3);
        Vector3 mean = v1_3 + v2_3 + v3_3 + v4_3;
        mean /= 4;
        Vector3 normal = Utility.GetNormal(points);
        Plane newplane = new Plane(normal, mean);
        v1_3 = Utility.PlaneProjectPoint(newplane, v1_3);
        v2_3 = Utility.PlaneProjectPoint(newplane, v2_3);
        v3_3 = Utility.PlaneProjectPoint(newplane, v3_3);
        v4_3 = Utility.PlaneProjectPoint(newplane, v4_3);
        if (!m_engine.m_is_quiet) Debug.Log("angle: " + Vector3.Angle(v1_3 - v2_3, v3_3 - v2_3));
        Quad tempRect = new Quad(v1_3, v2_3, v3_3);
        tempRect.FitRect();
        m_renderEngine.DrawRect3(tempRect,Color.gray);

        List<Vector2> QuadPoints_2d = m_projector.WorldToImage(tempRect.SampleBoundPoints());
        int notinmask_count = 0;
        for (int i = 0; i < QuadPoints_2d.Count; i++)
        {
            int xx = (int)QuadPoints_2d[i].x;
            xx = Math.Max(0, xx);
            xx = Math.Min(xx, this.img.Width - 1);

            int yy = (int)QuadPoints_2d[i].y;
            yy = Math.Max(0, yy);
            yy = Math.Min(xx, this.img.Height - 1);

            //Debug.Log(yy + " " + xx);

            if (!this.img[yy, xx].Equals(new Gray(255)))
            {
                notinmask_count++;
            }
        }
        //fi[8] = notinmask_count / (float)QuadPoints_2d.Count;
        fi[8] = 10f*notinmask_count / (float)QuadPoints_2d.Count;

        // axis angle with normal
        //Vector3 rect_normal = tempRect.Normal;
        //Plane cutplane = new Plane();
        //List<Vector2> boundary2_face = IExtension.GetBoundary(this.img);
        //List<Vector2> boundary2_body = ExtractOutline(this.body_img, boundary2_face);
        //int[] i_corner = SelectTwoCorners(m_projector.Proj3dToImage(tempRect.CornerPoints3d), boundary2_body);
        //Vector3 v_inSection = tempRect.CornerPoints3d[i_corner[0]] - tempRect.CornerPoints3d[i_corner[1]];
        //cutplane = new Plane(Vector3.Cross(tempRect.Normal, v_inSection).normalized, tempRect.Center);
        //Vector3 axis_dire = m_projector.Proj2dToPlane_v(cutplane, axis.First(), start_dire);

        Vector2 center2 = m_projector.WorldToImage(tempRect.Center);
        Vector2 end2 = m_projector.WorldToImage(tempRect.Center + 10 * tempRect.Normal);
        Vector2 normal2 = (end2 - center2).normalized;
        fi[10] = 10f * Vector2.Angle(normal2, start_dire.normalized);
        //fi[10] = 100f*1 / Mathf.Abs(Vector2.Dot(normal2, start_dire.normalized));
        // Visualization
        Debug.Log("Fi[8]"+ fi[8] + "  FOV: " + x[4] * fov_optimal_scale + "Cost: " + fi[10]);
        double cost = fi[0] + fi[1] + fi[2] + fi[3] + fi[4] + fi[5] + fi[6] + fi[7] + fi[8] + fi[9] + fi[10];
    }

    List<List<Vector2>> inlier_list = new List<List<Vector2>>();
    public List<Vector2> corner_points = new List<Vector2>();
    int firstbase = -1;
    int secondbase = -1;

    public Vector2 FindTrapezoidDiagonalIntersection()
    {

        List<Line2> edges = new List<Line2>();
        // m_renderEngine.DrawPoints(m_projector.ImageToWorld(this.boundary2), Color.yellow);

        List<Vector2> boundpoints = new List<Vector2>(this.boundary2);
        //find all lines
        double thred = 5;
        RansacLine2d ransac = new RansacLine2d(thred, 0.9);
        edges.Clear();
        do
        {
            Line2 oneline = ransac.Estimate(boundpoints);
            inlier_list.Add(new List<Vector2>(ransac.inliers));
            boundpoints = boundpoints.Except(ransac.inliers).ToList();
            edges.Add(oneline);
        } while (edges.Last() != null);
        edges.RemoveAt(edges.Count - 1);
        Console.WriteLine("edges number: {0}", edges.Count());


        //find two most parallel lines, also not the same line
        double minangle = -1;
        for (int i = 0; i < edges.Count - 1; i++)
        {
            for (int j = i + 1; j < edges.Count; j++)
            {
                double angle = Mathf.Abs(Vector3.Dot(edges[i].dir, edges[j].dir));
                if (angle > minangle && MinDisBetweenLine2(edges[i], edges[j]) > 2 * thred)
                {
                    minangle = angle;
                    firstbase = i;
                    secondbase = j;
                }
            }
        }
        //refine bases
        foreach (Vector2 p in boundary2)
        {
            if (edges[firstbase].DistanceToLine(p) < 2 * thred)
            {
                this.inlier_list[firstbase].Add(p);
            }
            if (edges[secondbase].DistanceToLine(p) < 2 * thred)
            {
                this.inlier_list[secondbase].Add(p);
            }
        }

        edges[firstbase] = ransac.Estimate(this.inlier_list[firstbase]);
        edges[secondbase] = ransac.Estimate(this.inlier_list[secondbase]);
        edges[firstbase].UpdateEnd(this.inlier_list[firstbase]);
        edges[secondbase].UpdateEnd(this.inlier_list[secondbase]);
        //m_renderEngine.DrawLine(m_projector.Line2ToLine3(edges[firstbase]), Color.red);
        //m_renderEngine.DrawLine(m_projector.Line2ToLine3(edges[secondbase]), Color.blue);


        corner_points.Add(edges[firstbase].start);
        corner_points.Add(edges[firstbase].end);
        corner_points.Add(edges[secondbase].end);
        corner_points.Add(edges[secondbase].start);

        // compute Diagonal Intersection point as 3d rectangle center
        // same direction
        Line2 diagonal1 = new Line2(edges[firstbase].start, edges[secondbase].end);
        Line2 diagonal2 = new Line2(edges[secondbase].start, edges[firstbase].end);

        // m_renderEngine.DrawLine(m_projector.Line2ToLine3(diagonal1), Color.yellow);
        // m_renderEngine.DrawLine(m_projector.Line2ToLine3(diagonal2), Color.yellow);

        Vector2 inter = diagonal1.Intersection(diagonal2);
        if (Utility.PointInTrapezoid(inter, corner_points))
            return inter;



        // not same direction
        diagonal1 = new Line2(edges[firstbase].start, edges[secondbase].start);
        diagonal2 = new Line2(edges[secondbase].end, edges[firstbase].end);

        //m_renderEngine.DrawLine(m_projector.Line2ToLine3(diagonal1), Color.white);
        //m_renderEngine.DrawLine(m_projector.Line2ToLine3(diagonal2), Color.white);

        Vector2 inter2 = diagonal1.Intersection(diagonal2);
        if (Utility.PointInTrapezoid(inter2, corner_points))
        {
            edges[secondbase].Flip();
            return inter2;
        }


        return new Vector2();

        ////compute area
        //double area = (edges[firstbase].Length() + edges[secondbase].Length()) * MeanDisBetweenLine2(edges[firstbase], edges[secondbase]) / 2.0;
        //double areafromimg = IExtension.GetMaskPoints(this.img).Count();
        //double error = Math.Abs(areafromimg - area);
        //error /= areafromimg;
        //Debug.Log("Like a trapezoid error: " + error);
        //return error;
    }
    public void FitQuad()
    {
        VectorOfVectorOfPoint con = new VectorOfVectorOfPoint();
        Image<Gray, byte> img_copy = this.img.Copy();
        CvInvoke.FindContours(img_copy, con, img_copy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
        int maxcomp = 0, max_con_idx = -1;
        for (int i = 0; i < con.Size; i++)
        {
            if (con[i].Size > maxcomp)
            {
                maxcomp = con[i].Size;
                max_con_idx = i;
            }
        }
        // found max component's contour
        // simplify contours
        VectorOfPoint con2 = new VectorOfPoint();
        con2 = con[max_con_idx];
        double alpha = 0.01; // 越小越接近曲线
        int iter = 0;
        while (con2.Size != 4)
        {
            if (iter++ > 200) break;
            double epsilon = alpha * CvInvoke.ArcLength(con[max_con_idx], true);
            CvInvoke.ApproxPolyDP(con[max_con_idx], con2, epsilon, true);
            if (con2.Size > 4)
            {
                alpha += 0.01;
                corner_points.Clear();
                for (int i = 0; i < con2.Size; i++)
                    corner_points.Add(new Vector2(con2[i].X, con2[i].Y));
            }
            if (con2.Size < 4)
                alpha -= 0.003;
        }

        if (con2.Size == 4)
        {
            corner_points.Clear();
            for (int i = 0; i < con2.Size; i++)
            {
                corner_points.Add(new Vector2(con2[i].X, con2[i].Y));
            }
        }
        else
        {
            int removei = corner_points.Count - 4;
            for (int i = 0; i < removei; i++)
            {
                corner_points.RemoveAt(1);
            }

        }



        for (int i = 0; i < con2.Size; i++)
        {
            m_renderEngine.DrawLine(new Vector2(con2[i].X, con2[i].Y), new Vector2(con2[(i + 1) % con2.Size].X, con2[(i + 1) % con2.Size].Y), Color.yellow);
        }
    }

    public double MinDisBetweenLine2(Line2 a, Line2 b)
    {
        if (!Line2.IsParallel(a, b, 5))
        {
            double dis1 = a.DistanceToLine(b.start);
            double dis2 = a.DistanceToLine(b.end);
            double dis3 = b.DistanceToLine(a.start);
            double dis4 = b.DistanceToLine(a.end);

            double meandis = Math.Min(Math.Min(dis1, dis2), Math.Min(dis3, dis4));
            //double meandis = (dis1 + dis2 + dis3 + dis4) / 4.0;
            return meandis;
        }
        else
            return 0;

    }
    public List<Vector2> GetCornerPoints()
    {
        Image<Gray, float> cornerimg = new Image<Gray, float>(this.img.Size);
        Image<Gray, Byte> cornerthrimg = new Image<Gray, Byte>(this.img.Size);
        Image<Gray, Byte> cannyimg = this.img.Canny(60, 100);
        CvInvoke.CornerHarris(cannyimg, cornerimg, 3, 3, 0.04);

        //CvInvoke.cvNormalize(cornerimg, cornerimg, 0, 255, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);  //标准化处理

        double min = 0, max = 0;
        System.Drawing.Point minp = new System.Drawing.Point(0, 0);
        System.Drawing.Point maxp = new System.Drawing.Point(0, 0);
        CvInvoke.MinMaxLoc(cornerimg, ref min, ref max, ref minp, ref maxp);
        double scale = 255 / (max - min);
        double shift = min * scale;
        CvInvoke.ConvertScaleAbs(cornerimg, cornerthrimg, scale, shift);//进行缩放，转化为byte类型
        byte[] data = cornerthrimg.Bytes;
        List<Vector2> corners = new List<Vector2>();
        List<Vector3> corners_3 = new List<Vector3>();

        for (int i = 0; i < cornerimg.Height; i++)
        {
            for (int j = 0; j < cornerimg.Width; j++)
            {
                int k = i * cornerthrimg.Width + j;
                if (data[k] > 80)    //通过阈值判断
                {
                    corners.Add(new Vector2(j, i));
                    corners_3.Add(m_projector.ImageToWorld(corners.Last()));
                }
            }
        }
        m_renderEngine.DrawPoints(corners_3);
        return corners;
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

}

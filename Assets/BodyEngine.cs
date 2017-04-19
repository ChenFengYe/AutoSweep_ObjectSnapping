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

public class BodyEngine : MonoBehaviour
{

    public Projector m_projector;
    public FaceEngine m_faceEngine;
    public RenderEngine m_renderEngine;

    Mesh cyliner = null;
    Image<Gray, byte> img = null;
    Circle3 topCircle = new Circle3();

    List<Vector3> boundary3 = null;
    List<Vector3> GeneratedCenters = null;

    Vector3 cur_p = new Vector3();
    Vector3 cur_dire = new Vector3();
    Vector3 Insection1 = new Vector3();
    Vector3 Insection2 = new Vector3();
    Ray ray;

    // Temp
    Ray setLine1;
    Ray setLine2;
    Ray test1 = new Ray(new Vector3(0,0,0),new Vector3(1,1,1));
    Ray test2;
    Ray setdirecLine;

    private struct NearPoint
    {
        public Vector3 v;
        public double dist;
        public int index;
    }

    void Start()
    {
    }

    void OnRenderObject()
    {
        m_renderEngine.DrawRay(ref test1);
    }

    void Update()
    {
        m_renderEngine.DrawMesh(cyliner);
    }

    public void SnapCylinder(Image<Gray, byte> image)
    {
        // Set Image
        img = image;
        topCircle = m_faceEngine.topCircle;

        // Get Boundary3 body
        List<Vector2> boundary2_face = IExtension.GetBoundary(m_faceEngine.img);
        List<Vector2> boundary2_body = ExtractOutline(img, boundary2_face);

        // Project 2D edge points
        Vector3 normal = Vector3.Cross(
            Vector3.Cross(topCircle.Normal, m_projector.m_mainCamera.transform.forward),
            topCircle.Normal);
        Plane sectionPlane = new Plane(normal.normalized, topCircle.Center);
        boundary3 = m_projector.Proj2dToPlane(sectionPlane, boundary2_body);

        topCircle = FixTopCircle(topCircle, boundary3);

        // Algorithm Init Params
        float offset = topCircle.Radius / 20f;
        cur_p = topCircle.Center - offset * topCircle.Normal;
        cur_dire = topCircle.Normal;
        Vector3 cur_dire_new = Utility.NewVector3(cur_dire);
        Vector3 cur_p_new = Utility.NewVector3(-1 * cur_p);
        Insection1 = new Vector3(1, 1, 1);
        Insection2 = new Vector3(0, 0, 0);
        int norInsec = -1;
        int notNorInsec = -1;
        Vector3 tangential1 = new Vector3(1, 1, 1);
        Vector3 tangential2 = new Vector3(1, 1, 1);

        List<Circle3> CircleLists = new List<Circle3>();
        CircleLists.Add(topCircle);     // Fix first circle

        int iter = 0;
        float r = float.MaxValue;
        System.Console.WriteLine(Vector3.Dot(Insection1, tangential2));
        System.Console.WriteLine(Mathf.Cos(2.0f / 3.0f * Mathf.PI));
        int MaxInter = 1000;

        GeneratedCenters = new List<Vector3>();
        List<float> radius = new List<float>();
        List<float> weights = new List<float>();
        List<Vector3> dires = new List<Vector3>();

        while (--MaxInter > 0) //
        {
            if (Insection1 == Insection2)                                       // 交点一直保持相同
            {
                Debug.Log("Warning: Insection is same!");        // 半径过小
                break;
            }
            if (Vector3.Dot(cur_dire, cur_dire_new) < 0)                                 // 移动方向反向
            {
                Debug.Log("Warning: Move Direction！");
                break;
            }
            if (cur_p + offset * cur_dire == cur_p_new)                         // 中心点没有移动
            {
                Debug.Log("Warning: Center not move!");
                break;
            }

            RayTracein3DPlane(boundary3,
                cur_p_new,
                Vector3.Cross(cur_dire_new, sectionPlane.normal),
                sectionPlane.normal,
                out norInsec,
                out notNorInsec);
            System.Console.WriteLine("{0} , {1}",
                Vector3.Distance(boundary3[norInsec], cur_p_new),
                Vector3.Distance(boundary3[notNorInsec], cur_p_new));
            test1 = new Ray(boundary3[norInsec], cur_p_new - boundary3[norInsec]);
            test2 = new Ray(boundary3[notNorInsec], cur_p_new - boundary3[notNorInsec]);

            if (Vector3.Distance(boundary3[norInsec], cur_p_new) < topCircle.Radius / 20    // close to bottom
                || Vector3.Distance(boundary3[notNorInsec], cur_p_new) < topCircle.Radius / 20)
            {
                Debug.Log("Warning: Close to bottom!");
                break;
            }

            if (Vector3.Dot(tangential1, tangential2) < Mathf.Cos(2.0f / 3.0f * Mathf.PI))   //切线相向
            {
                Debug.Log("Warning: tangential get oppsite direction!");
                break;
            }
            if (r < 0.0001)
            {
                Debug.Log("Warning: Radius is too small!");      // 半径过小
                break;
            }
            //if (Vector3.Distance(cur_p, cur_p_new) )
            //{
            //    Debug.Log("Warning: Radius is too small!");    // 半径过小
            //    break;
            //}

            if (iter != 0)
            {
                //offset = 1 / Vector3.Distance(cur_p, cur_p_new) * 0.000001 + 0.5 * offset;
                offset = topCircle.Radius / 20;
                //System.Console.WriteLine("{0}", offset);
                cur_dire = cur_dire_new;
                cur_p = cur_p_new + offset * cur_dire;
                CircleLists.Add(new Circle3(cur_p, r, cur_dire));

                // Get Data for Fit
                float weight = Mathf.Abs(Vector3.Dot(cur_dire_new, cur_dire));
                GeneratedCenters.Add(cur_p_new);
                weights.Add(weight);
                radius.Add(r);
                dires.Add(cur_dire);
            }

            // Step1: Get IntersectionPoitn
            RayTracein3DPlane(boundary3, cur_p, cur_dire, sectionPlane.normal, out norInsec, out notNorInsec);

            // Step2 : Get Two Local Tangential
            Insection1 = boundary3[norInsec];
            Insection2 = boundary3[notNorInsec];
            tangential1 = GetLocalTangential(norInsec, boundary3, cur_dire);
            tangential2 = GetLocalTangential(notNorInsec, boundary3, cur_dire);

            // Visualization
            setdirecLine = new Ray(cur_p, cur_dire);
            setLine1 = new Ray(Insection1, tangential1);
            setLine2 = new Ray(Insection2, tangential2);

            // Step3 : Get New Cur Direction and Cur Point
            cur_dire_new = (tangential1 + tangential2) / 2;
            RayTracein3DPlane(boundary3, cur_p, cur_dire_new, sectionPlane.normal, out norInsec, out notNorInsec);
            cur_p_new = (boundary3[norInsec] + boundary3[notNorInsec]) / 2;
            r = 0.5f * Vector3.Distance(boundary3[norInsec], boundary3[notNorInsec]);
            iter++;
            if (GUI.changed) EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        // Fit centers and radius;
        GeneratedCenters = FitExtension.FitCenterCurve(GeneratedCenters, weights);
        int inter = 1;
        while (inter-- > 0)
        {
            radius = FitRadius(radius);
        }

        // ReBuild Object
        CircleLists.Clear();
        CircleLists.Add(topCircle);         // Fix first circle
        for (int i = 0; i < GeneratedCenters.Count; i++)
        {
            CircleLists.Add(new Circle3(GeneratedCenters[i], radius[i], dires[i]));
        }

        cyliner = Utility.MeshCreateFromCircleList(CircleLists);
    }

    private Circle3 FixTopCircle(Circle3 topCircle, List<Vector3> boundary3)
    {
        Vector3 boundary_mean = new Vector3(0, 0, 0);
        foreach (var p in boundary3)
        {
            boundary_mean += p;
        }
        boundary_mean /= boundary3.Count;
        if (Vector3.Dot(topCircle.Normal, boundary_mean - topCircle.Center) > 0)
            return topCircle;
        else
            return new Circle3(topCircle.Center, topCircle.Radius, -1.0f * topCircle.Normal);
    }

    private List<float> FitRadius(List<float> radius)
    {
        List<Vector2> tempR = new List<Vector2>();
        for (int i = 0; i < radius.Count; i++)
        {
            tempR.Add(new Vector2(i, radius[i]));
        }
        tempR = FitExtension.FitCurve(tempR);

        radius.Clear();
        foreach (var r in tempR)
        {
            radius.Add(r[1]);
        }
        return radius;
    }

    private List<Vector2> ExtractOutline(Image<Gray, byte> imgs, List<Vector2> topOutline)
    {
        double lineDistTheshold = 5;
        List<Vector2> ObjectOutline = IExtension.GetBoundary(img);
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

    private Vector3 GetLocalTangential(int p, List<Vector3> boundary3, Vector3 curDir)
    {
        // Get Nearnest Pointss
        List<NearPoint> nearPs = new List<NearPoint>();
        for (int i = 0; i < boundary3.Count; i++)
        {
            NearPoint p_i = new NearPoint();
            p_i.v = boundary3[i];
            p_i.dist = (boundary3[p] - p_i.v).magnitude;
            p_i.index = i;
            nearPs.Add(p_i);
        }

        // Fit Line with k nearest points
        int k = 100;
        List<NearPoint> kNearPs = (from a in nearPs orderby a.dist ascending select a).Take(k).ToList();
        List<Vector3> kNearPv = new List<Vector3>();
        foreach (var kNearP in kNearPs)
        {
            kNearPv.Add(kNearP.v);
        }
        var fitline = new RansacLine3d(0.00005, 0.9);

        Ray Rayd;
        if (fitline.Estimate(kNearPv))
        {
            Rayd = fitline.bestline;
            // Check Local Tangential Direction
            if (Vector3.Dot(Rayd.direction, curDir) < 0)
            {
                Rayd.direction = -1 * Rayd.direction;
            }
            return Rayd.direction.normalized;
        }
        else
        {
            Debug.Log("RANSAC Fit failure!");
            return new Vector3(0, 0, 0);
        }
    }

    private void RayTracein3DPlane(List<Vector3> points, Vector3 curp, Vector3 curdire, Vector3 sectionPlaneNormal, out int norInsec, out int notNorInsec)
    {
        // Param
        double insecPs_Dist_theshold = 0.01;
        double insecP_DistBetweenRay_theshold = 20;

        Vector3 cutNormal = Vector3.Cross(sectionPlaneNormal, curdire).normalized;
        ray = new Ray(curp, cutNormal);

        norInsec = -1; // Normal side
        notNorInsec = -1; // Not Normal side
        double dist_left = double.MaxValue;
        double dist_right = double.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            double dist_temp = Utility.DistanceToRay(ray, points[i]);
            if (Vector3.Dot(points[i] - curp, cutNormal) > 0)
            {
                // Normal side
                if (dist_left > dist_temp)
                {
                    dist_left = dist_temp;
                    norInsec = i;
                }
            }
            else
            {
                // Not Normal side
                if (dist_right > dist_temp)
                {
                    dist_right = dist_temp;
                    notNorInsec = i;
                }
            }
        }

        if (norInsec == -1)
        {
            norInsec = notNorInsec;
            Debug.Log("Warining: norInsec == -1");
            return;
        }
        else if (notNorInsec == -1)
        {
            notNorInsec = norInsec;
            Debug.Log("Warining: notNorInsec == -1");
            return;
        }
        else if (norInsec == -1 && notNorInsec == -1)
        {
            Debug.Log("Error: Ray Tracein3DPlane, no intersection points");
            return;
        }

        if (Vector3.Distance(points[norInsec], points[notNorInsec]) < insecPs_Dist_theshold)
        {
            // this two intersection is too close, so let them become same one.s
            Debug.Log("Warining: two intersection is too close");
            norInsec = notNorInsec;
            return;
        }

        if (Utility.DistanceToRay(ray, points[norInsec]) > insecP_DistBetweenRay_theshold
            || Utility.DistanceToRay(ray, points[notNorInsec]) > insecP_DistBetweenRay_theshold)
        {
            Debug.Log("Warining: two intersection is too far");
            // this two intersection is too far, so let them become same one.s
            norInsec = notNorInsec;
            return;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using NumericalRecipes;
using MyGeometry;
using System;

using UnityExtension;
using UnityEngine;
using UnityEngine.UI;//导入UI包

public class FaceEngine : MonoBehaviour
{
    public Projector m_projector;

    public RenderEngine m_renderEngine;
    [HideInInspector]
    public Image<Gray, byte> img;                                   // Mark of Image
    [HideInInspector]
    public Circle3 topCircle = new Circle3();                       // The Top Circle of the cylinder
    [HideInInspector]
    public List<Vector2> boundary2 = null;                          // The boundary of the Mark of the Image
    List<Vector2> CirclePoints_2d = null;                           // The Circle Sample Points projected on the screen

    // the following points are all image pixels
    Vector2 mean = new Vector2();
    Vector2 majorstartp = new Vector2();
    Vector2 majorendp = new Vector2();
    Vector2 minorstartp = new Vector2();
    Vector2 minorendp = new Vector2();

    int Inter_Num = -1;                                             // The interaction time of optima

    Vector3 majorstartp3;
    Vector3 majorendp3;
    Vector3 minorstartp3;
    public void Solve(Image<Gray, byte> mask)
    {
        this.img = mask;

        FitEllipse();
        FitTopCircle();

    }

    void OnRenderObject()
    {

        //m_renderEngine.DrawCircle(ref c1, Color.red);
        m_renderEngine.DrawCircle(ref topCircle, Color.red);
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
        Debug.Log("Like a ellipse error: " + error); //10^-2 may be a good threshold

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
        // Optimize();
        topCircle.Save();
    }

    public void Optimize()
    {

        double thetax, thetay, thetaz;
        Utility.NormalToRotation(topCircle.Normal, out thetax, out thetay, out thetaz);

        // Optimal Solution
        double[] bndl = new double[] { 0, 0, 0, 0.001 };
        double[] x = new double[] { thetax, thetay, thetaz, topCircle.Radius }; //add center
        double[] bndu = new double[] { Mathf.PI, Mathf.PI, Mathf.PI, 10 };

        int funNum = 2;

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
        Debug.Log("End fitting , Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");


        // Update Circle
        Vector3 center = Utility.NewVector3(topCircle.Center);

        Vector3 normal = Utility.RotationToNormal(x[0], x[1], x[2]); normal.Normalize();
        //Vector3 normal = new Vector3((float)x[0], (float)x[1], (float)x[2]); normal.Normalize();
        float radius = (float)x[3];

        topCircle = new Circle3(center, radius, new Plane(normal, center));
        CirclePoints_2d = m_projector.GetProjectionPoints_2D(topCircle.CirclePoints);
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
        Debug.Log("End fitting center, Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        // Update Circle
        Vector3 center = new Vector3((float)x[0], (float)x[1], (float)x[2]);
        Vector3 normal = topCircle.Normal;
        double radius = topCircle.Radius;

        topCircle = new Circle3(center, (float)radius, new Plane(normal, center));
        CirclePoints_2d = m_projector.GetProjectionPoints_2D(topCircle.CirclePoints);
    }

    private void function_project(double[] x, double[] fi, object obj)
    {
        // Step 1: Init Params
        Inter_Num++;
        Vector3 center = Utility.NewVector3(topCircle.Center);
        Vector3 normal = Utility.NewVector3(Utility.RotationToNormal(x[0], x[1], x[2])); normal.Normalize();
        //Vector3 normal = new Vector3((float)x[0], (float)x[1], (float)x[2]);

        double radius = x[3];
        Circle3 myC = new Circle3(center, (float)radius, new Plane(normal, center), this.boundary2.Count);

        // Step 2: Do projection
        CirclePoints_2d = m_projector.GetProjectionPoints_2D(myC.CirclePoints);

        // Step 3: Do the evaluation
        double dist_all = 0;
        for (int i = 0; i < CirclePoints_2d.Count; i++)
        {
            double dist = double.MaxValue;
            for (int j = 0; j < boundary2.Count; j++)
            {
                dist = Math.Min(dist, (CirclePoints_2d[i] - boundary2[j]).sqrMagnitude);
            }
            dist_all += dist;
        }

        // Set Cost function
        fi[0] = dist_all;
        fi[1] = normal.sqrMagnitude - 1;
        //if (Inter_Num % 20 == 0)
        Debug.Log(Inter_Num + "|| N: " + x[0] + " " + x[1] + " " + x[2] + "r: " + x[3] + " cost:" + dist_all);
    }

    private void function_withcenter(double[] x, double[] fi, object obj)
    {
        Inter_Num++;
        Vector3 center = new Vector3((float)x[0], (float)x[1], (float)x[2]);
        Vector3 normal = topCircle.Normal;
        double radius = topCircle.Radius;
        Circle3 myC = new Circle3(center, (float)radius, new Plane(normal, center), this.boundary2.Count);

        CirclePoints_2d = m_projector.GetProjectionPoints_2D(myC.CirclePoints);
        double dist_all = 0;
        for (int i = 0; i < CirclePoints_2d.Count; i++)
        {
            double dist = double.MaxValue;
            for (int j = 0; j < boundary2.Count; j++)
            {
                dist = Math.Min(dist, (CirclePoints_2d[i] - boundary2[j]).sqrMagnitude);
            }
            dist_all += dist;
        }

        // Set Cost function
        fi[0] = dist_all;

        // if (Inter_Num % 20 == 0)
        Debug.Log(Inter_Num + "|| N: " + x[0] + " " + x[1] + " " + x[2] + " cost:" + dist_all);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGeometry;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;


public class FaceEngineMulti : MonoBehaviour {

    GraphicsEngine m_engine;
    Projector m_projector;
    RenderEngine m_renderEngine;
    List<FaceEngine> m_FEs = new List<FaceEngine>();
    List<Circle3> m_circles = new List<Circle3>();
    List<Quad> m_quads = new List<Quad>();
    float fov_optimal_scale = 10f;

    // Optimal Params
    int Inter_Num;
    List<FaceEngine> REs = new List<FaceEngine>();
    List<List<Vector2>> corners_list = new List<List<Vector2>>();
    public FaceEngineMulti(GraphicsEngine engine, Projector projector, RenderEngine renderEngine, List<FaceEngine> fe_list)
    {
        m_engine = engine;
        m_FEs = fe_list;
        m_projector = projector;
        m_renderEngine = renderEngine;

        // Assume all cuboid
    }
	void Start () {}	
	void Update () {}
    public void Optimize_allRects()
    {
        // Init FOV
        float fov_aver = 0;
        foreach (var fe in m_FEs) fov_aver = fe.fov_face;
        fov_aver /= m_FEs.Count;
        m_engine.m_fovOptimizer.SetFOV(fov_aver);
        float fov_init = m_projector.m_mainCamera.fieldOfView;

        // Count rect engines
        foreach (var fe in m_FEs)
            if (!fe.isTopCircle_Current) REs.Add(fe);

        // Params Prepare
        //foreach (var re in rectEngines) corners_list.Add(re.corner_points);
        int params_num = 4*REs.Count + 1;
        double[] bndl = new double[params_num];
        double[] x = new double[params_num];
        double[] bndu = new double[params_num];
        for (int i = 0; i < REs.Count; i++){
			bndl[4*i] = 0; 
            bndl[4*i+1] = 0; 
            bndl[4*i+2] = 0; 
            bndl[4*i+3] = 0;
            x[4 * i] = REs[i].x0; 
            x[4 * i + 1] = REs[i].x1; 
            x[4 * i + 2] = REs[i].x2; 
            x[4 * i + 3] = REs[i].x3;
			bndu[4*i] = 20; 
            bndu[4*i+1] = 20; 
            bndu[4*i+2] = 20; 
            bndu[4*i+3] = 20;
		}
        bndl[params_num-1] = 1 / fov_optimal_scale;
        x[params_num - 1] = fov_aver / fov_optimal_scale;
        bndu[params_num - 1] = 179 / fov_optimal_scale;

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
        alglib.minlmcreatev(funNum, x, diffstep, out state);
        alglib.minlmsetbc(state, bndl, bndu);
        alglib.minlmsetcond(state, epsg, epsf, epsx, maxits);
        alglib.minlmoptimize(state, function_multi, null, null);
        alglib.minlmresults(state, out x, out rep);


        stopwatch.Stop();
        if (!m_engine.m_is_quiet) Debug.Log("End fitting , Total time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");

        m_engine.m_fovOptimizer.SetFOV((float)x[x.Length-1] * fov_optimal_scale);
        for (int i = 0; i < REs.Count; i++)
        {
            Ray ray1 = m_projector.GenerateRay(REs[i].corner_points[0]);
            Ray ray2 = m_projector.GenerateRay(REs[i].corner_points[1]);
            Ray ray3 = m_projector.GenerateRay(REs[i].corner_points[2]);
            Ray ray4 = m_projector.GenerateRay(REs[i].corner_points[3]);

            Vector3 v1_3 = ray1.GetPoint((float)x[4*i]);
            Vector3 v2_3 = ray2.GetPoint((float)x[4 * i + 1]);
            Vector3 v3_3 = ray3.GetPoint((float)x[4 * i + 2]);
            Vector3 v4_3 = ray4.GetPoint((float)x[4 * i+3]);
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
            REs[i].topRect = new Quad(v1_3, v2_3, v3_3);
            REs[i].topRect.FitRect();
            m_renderEngine.DrawRect3(REs[i].topRect, Color.green);
            m_renderEngine.DrawLine(REs[i].topRect.Center, REs[i].topRect.Center + 2 * REs[i].topRect.Normal, Color.blue);
            if (!m_engine.m_is_quiet) Debug.Log("angle: " + Vector3.Angle(REs[i].topRect.CornerPoints3d[0] - REs[i].topRect.CornerPoints3d[1], REs[i].topRect.CornerPoints3d[2] - REs[i].topRect.CornerPoints3d[1]));
        }
    }

    private void function_multi(double[] x, double[] fi, object obj)
    {
        float[] weight = new float[11];
        weight[8] = 5f;
        weight[9] = 5f;
        weight[10] = 10f;

        m_engine.m_fovOptimizer.SetFOV((float)x[x.Length-1] * fov_optimal_scale);
        // Clear fi
        for (int i = 0; i < fi.Length; i++) fi[i] = 0;

        for (int i = 0; i < REs.Count; i++)
        {
            Vector2 v1_2 = REs[i].corner_points[0];
            Vector2 v2_2 = REs[i].corner_points[1];
            Vector2 v3_2 = REs[i].corner_points[2];
            Vector2 v4_2 = REs[i].corner_points[3];

            Ray ray1 = m_projector.GenerateRay(v1_2);
            Ray ray2 = m_projector.GenerateRay(v2_2);
            Ray ray3 = m_projector.GenerateRay(v3_2);
            Ray ray4 = m_projector.GenerateRay(v4_2);

            // Step 1: Init Params
            Inter_Num++;
            Vector3 v1 = ray1.GetPoint((float)x[4 * i]);
            Vector3 v2 = ray2.GetPoint((float)x[4 * i + 1]);
            Vector3 v3 = ray3.GetPoint((float)x[4 * i + 2]);
            Vector3 v4 = ray4.GetPoint((float)x[4 * i + 3]);

            // equal side length 
            float l1 = Vector3.Distance(v1, v2);
            float l2 = Vector3.Distance(v2, v3);

            float l1_2 = Vector2.Distance(v1_2, v2_2);
            float l2_2 = Vector2.Distance(v2_2, v3_2);

            fi[0] += (Vector3.Distance(v1, v2) - Vector3.Distance(v3, v4)) * 90;
            fi[1] += (Vector3.Distance(v2, v3) - Vector3.Distance(v1, v4)) * 90;

            // ratio not boo big
            fi[9] +=  weight[9]*Mathf.Max(l1, l2) / Mathf.Min(l1, l2) - Mathf.Max(l1_2, l2_2) / Mathf.Min(l1_2, l2_2);

            // vertical 0-90
            fi[2] += Mathf.Abs(Vector3.Angle(v2 - v1, v4 - v1) - 90);
            fi[3] += Mathf.Abs(Vector3.Angle(v1 - v2, v3 - v2) - 90);
            fi[4] += Mathf.Abs(Vector3.Angle(v2 - v3, v4 - v3) - 90);
            fi[5] += Mathf.Abs(Vector3.Angle(v1 - v4, v3 - v4) - 90);

            // on the same plane 0-180
            fi[6] += Vector3.Angle(Vector3.Cross(v2 - v1, v4 - v1), Vector3.Cross(v4 - v3, v2 - v3));
            fi[7] += Vector3.Angle(Vector3.Cross(v1 - v2, v3 - v2), Vector3.Cross(v3 - v4, v1 - v4));

            // mask cover
            Vector3 v1_3 = ray1.GetPoint((float)x[4 * i]);
            Vector3 v2_3 = ray2.GetPoint((float)x[4 * i + 1]);
            Vector3 v3_3 = ray3.GetPoint((float)x[4 * i + 2]);
            Vector3 v4_3 = ray4.GetPoint((float)x[4 * i + 3]);

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

            List<Vector2> QuadPoints_2d = m_projector.WorldToImage(tempRect.SampleBoundPoints());
            int notinmask_count = 0;
            for (int i_quad = 0; i_quad < QuadPoints_2d.Count; i_quad++)
            {
                int xx = (int)QuadPoints_2d[i_quad].x;
                xx = Mathf.Max(0, xx);
                xx = Mathf.Min(xx, REs[i].img.Width - 1);

                int yy = (int)QuadPoints_2d[i_quad].y;
                yy = Mathf.Max(0, yy);
                yy = Mathf.Min(xx, REs[i].img.Height - 1);

                if (!REs[i].img[yy, xx].Equals(new Gray(255)))
                    notinmask_count++;
            }
            //fi[8] += notinmask_count / (float)QuadPoints_2d.Count;
            fi[8] += weight[8] * notinmask_count;

            Vector2 center2 = m_projector.WorldToImage(tempRect.Center);
            Vector2 end2 = m_projector.WorldToImage(tempRect.Center + 10 * tempRect.Normal);
            Vector2 normal2 = (end2 - center2).normalized;
            fi[10] += weight[10] * Vector2.Angle(normal2, REs[i].start_dire.normalized);

            // Visualization
            //m_renderEngine.DrawRect3(tempRect);
            //m_renderEngine.DrawLine(tempRect.Center, tempRect.Center + 10 * rect_normal, Color.blue);
        }
        double cost = fi[0] + fi[1] + fi[2] + fi[3] + fi[4] + fi[5] + fi[6] + fi[7] + fi[8] + fi[9] + fi[10];
        //Debug.Log("Fi[8]" + fi[8] + "  FOV: " + x[x.Length-1] * fov_optimal_scale + "Axis: " + fi[10] + "cost: " + cost );
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using Accord.MachineLearning;
using Accord.Statistics.Distributions.DensityKernels;

using MyGeometry;

namespace UnityExtension
{
    public class IExtension
    {
        //--------------------------------------Handle Image-------------------------------------------//
        // Convert Image to Texture
        public static Texture2D ImageToTexture2D<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            Size size = image.Size;

            if (typeof(TColor) == typeof(Rgb) && typeof(TDepth) == typeof(Byte))
            {
                Texture2D texture = new Texture2D(size.Width, size.Height, TextureFormat.RGB24, false);
                byte[] data = new byte[size.Width * size.Height * 3];
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                using (Image<Rgb, byte> rgb = new Image<Rgb, byte>(size.Width, size.Height, size.Width * 3, dataHandle.AddrOfPinnedObject()))
                {
                    rgb.ConvertFrom(image.Flip(FlipType.Vertical));
                }
                dataHandle.Free();
                texture.LoadRawTextureData(data);
                texture.Apply();
                return texture;
            }
            else //if (typeof(TColor) == typeof(Rgba) && typeof(TDepth) == typeof(Byte))
            {
                Texture2D texture = new Texture2D(size.Width, size.Height, TextureFormat.RGBA32, false);
                byte[] data = new byte[size.Width * size.Height * 4];
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                using (Image<Rgba, byte> rgba = new Image<Rgba, byte>(size.Width, size.Height, size.Width * 4, dataHandle.AddrOfPinnedObject()))
                {
                    rgba.ConvertFrom(image.Flip(FlipType.Vertical));
                }
                dataHandle.Free();
                texture.LoadRawTextureData(data);

                texture.Apply();
                return texture;
            }
        }
        public static Image<Rgb, byte> Texture2DToImage(Texture2D tex)
        {
            Image<Rgb, byte> img = new Image<Rgb, byte>(tex.width, tex.height);
            for (int w = 0; w < tex.width; w++)
            {
                for (int h = 0; h < tex.height; h++)
                {
                    UnityEngine.Color color = tex.GetPixel(w, tex.height - h);
                    img.Data[h, w, 0] = (byte)(color.b * 255);
                    img.Data[h, w, 1] = (byte)(color.g * 255);
                    img.Data[h, w, 2] = (byte)(color.r * 255);
                }
            }
            return img;
        }

        public static void SetTransparency(UnityEngine.UI.Image p_image, float p_transparency)
        {
            if (p_image != null)
            {
                UnityEngine.Color __alpha = p_image.color;
                __alpha.a = p_transparency;
                p_image.color = __alpha;
            }
        }

        // Get Boundary Points from Image
        public static List<Vector2> GetBoundary(Image<Gray, byte> markImg, double cannyThreshold = 60, double cannyThresholdLinking = 100)
        {
            Image<Gray, Byte> cannyimg = markImg.Canny(cannyThreshold, cannyThresholdLinking);
            //new ImageViewer(cannyimg, "boundary").Show();
            double backGround_threshold = 50;
            List<Vector2> b_points = new List<Vector2>();
            for (int i = 0; i < cannyimg.Height; i++)
            {
                for (int j = 0; j < cannyimg.Width; j++)
                {
                    if (cannyimg[i, j].Intensity > backGround_threshold)
                        b_points.Add(new Vector2(j, i));
                }
            }
            return b_points;
        }


        public static List<Vector2> GetBoundary(List<Vector2> maskpoints, int width, int height)
        {
            Image<Gray, byte> img = new Image<Gray, byte>(width, height);
            img.SetZero();
            foreach (Vector2 v in maskpoints)
                img[(int)v.y, (int)v.x] = new Gray(255);
            return GetBoundary(img);
        }

        // Get Mask Points from Image
        public static List<Vector2> GetMaskPoints(Image<Gray, byte> img)
        {
            double backGround_threshold = 50;
            List<Vector2> b_points = new List<Vector2>();
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    if (img[i, j].Intensity > backGround_threshold)
                    {
                        b_points.Add(new Vector2(j, i));
                    }
                }
            }
            return b_points;
        }




        //---------------------------------------------------------------------------------------------//
        // Ray tracing: line between sphere
        public static Vector3 RayHitShpere(Vector3 orig, Vector3 dire, Vector3 m_center, double m_r)
        {
            Vector3 l = m_center - orig;
            double dist = l.magnitude;

            // Sphere is on the opposite direction of ray
            double s = Vector3.Dot(l, dire);
            if (s < 0 && dist > m_r) return new Vector3(0.0f, 0.0f, 0.0f);

            // Distance between line and sphere is too far
            double m = Math.Sqrt(l.sqrMagnitude - s * s);
            if (m > m_r) return new Vector3(0.0f, 0.0f, 0.0f);

            // Get the length between origin and intersection
            double t;
            double q = Math.Sqrt(m_r * m_r - m * m);
            if (dist > m_r) t = s - q;
            else t = s + q;

            return (float)t * dire + orig;
        }

        public static List<List<Vector3>> ClusterPoints(List<Vector3> points)//delete "outliers"
        {
            double[][] input = new double[points.Count][];
            for (int i = 0; i < points.Count; i++)
            {
                input[i] = new double[] { points[i].x, points[i].y, points[i].z };
            }
            UniformKernel kernel = new UniformKernel();
            MeanShift meanShift = new MeanShift(dimension: 3, kernel: kernel, bandwidth: 1e-2);
            MeanShiftClusterCollection clustering = meanShift.Learn(input);
            int[] labels = clustering.Decide(input);

            List<List<Vector3>> classedPoints = new List<List<Vector3>>();
            for (int i = 0; i <= Mathf.Max(labels); i++)
            {
                List<Vector3> iClass = new List<Vector3>();
                foreach (var p in points)
                {
                    iClass.Add(p);
                }
                classedPoints.Add(iClass);
            }
            return classedPoints;
        }

        public static List<List<Vector3>> MyCluster(List<Vector3> ps, float dist_inclass)
        {
            int[] labels = new int[ps.Count];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = -1;

            int index_label = -1;
            for (int i = 0; i < ps.Count; i++)
            {
                if (labels[i] == -1)
                {
                    labels[i] = ++index_label;
                }

                for (int j = 0; j < ps.Count; j++)
                {
                    if (i == j || labels[i] == labels[j]) continue;
                    labels[j] = Vector3.Distance(ps[i], ps[j]) < dist_inclass ? labels[i] : labels[j];
                }
            }
            List<List<Vector3>> classedPoints = new List<List<Vector3>>();
            for (int i = 0; i <= Mathf.Max(labels); i++)
            {
                List<Vector3> iClass = new List<Vector3>();
                for (int j = 0; j < ps.Count; j++)
                {
                    if (labels[j] == i)
                    {

                        iClass.Add(ps[j]);
                    }
                }
                classedPoints.Add(iClass);
            }
            return classedPoints;
        }

        public static List<List<Vector2>> ClusterPoints(List<Vector2> points)//delete "outliers"
        {
            double[][] input = new double[points.Count][];
            for (int i = 0; i < points.Count; i++)
            {
                input[i] = new double[] { points[i].x, points[i].y };
            }
            UniformKernel kernel = new UniformKernel();
            MeanShift meanShift = new MeanShift(dimension: 2, kernel: kernel, bandwidth: 1e-2);
            MeanShiftClusterCollection clustering = meanShift.Learn(input);
            int[] labels = clustering.Decide(input);

            List<List<Vector2>> classedPoints = new List<List<Vector2>>();
            for (int i = 0; i <= Mathf.Max(labels); i++)
            {
                List<Vector2> iClass = new List<Vector2>();
                foreach (var p in points)
                {
                    iClass.Add(p);
                }
                classedPoints.Add(iClass);
            }
            return classedPoints;
        }

        public static List<List<Vector2>> MyCluster(List<Vector2> ps, float dist_inclass)
        {
            int[] labels = new int[ps.Count];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = -1;

            int index_label = -1;
            for (int i = 0; i < ps.Count; i++)
            {
                if (labels[i] == -1)
                {
                    labels[i] = ++index_label;
                }

                for (int j = 0; j < ps.Count; j++)
                {
                    if (i == j || labels[i] == labels[j]) continue;
                    labels[j] = Vector2.Distance(ps[i], ps[j]) < dist_inclass ? labels[i] : labels[j];
                }
            }
            List<List<Vector2>> classedPoints = new List<List<Vector2>>();
            for (int i = 0; i <= Mathf.Max(labels); i++)
            {
                List<Vector2> iClass = new List<Vector2>();
                for (int j = 0; j < ps.Count; j++)
                {
                    if (labels[j] == i)
                    {

                        iClass.Add(ps[j]);
                    }
                }
                classedPoints.Add(iClass);
            }
            return classedPoints;
        }

        public static List<Vector2> ResetPath(List<Vector2> pts, int step)
        {
            int i;
            float left;
            List<Vector2> list = new List<Vector2>();

            list.Add(pts[0]);
            left = step;
            for (i = 0; i < pts.Count - 1; i++)
            {
                float tmp = Vector2.Distance(pts[i], pts[(i + 1) % pts.Count]);
                if (tmp < left)
                {
                    left -= tmp;
                }
                else
                {
                    float offset = left;
                    while (offset < tmp)
                    {
                        Vector2 p = pts[i] + (pts[(i + 1) % pts.Count] - pts[i]) * (offset / tmp);
                        //p.o() = 1.0;
                        list.Add(p);
                        offset += step;
                    }
                    left = offset - tmp;
                }
            }
            //if (list.length() == count) list.insert(pts[pts.length()-1]);
            return list;

        }
    }

    public class FitExtension
    {
        public static List<Vector2> FitCurve_RBFmodel(List<Vector2> points)
        {
            double[,] xy0 = new double[points.Count, 3];

            for (int i = 0; i < points.Count; i++)
            {
                xy0[i, 0] = points[i].x;
                xy0[i, 1] = 0;
                xy0[i, 2] = points[i].y;
            }

            alglib.rbfmodel model;
            alglib.rbfreport rep;

            alglib.rbfcreate(2, 1, out model);
            alglib.rbfsetpoints(model, xy0);

            alglib.rbfsetalgomultilayer(model, 100, 1, 1.0e-3);
            alglib.rbfbuildmodel(model, out rep);

            List<Vector2> output = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
            {
                double x = points[i].x;
                double zero = 0;
                double y = alglib.rbfcalc2(model, x, zero);
                output.Add(new Vector2((float)x, (float)y));
            }
            return output;
        }
        public static List<Vector2> FitCurve_BilateralFilter(List<Vector2> pts, int loop = 1)
        {
            //if (mode == Utils.OPEN_STROKE || pts.Count < 5) return;
            if (pts.Count < 5) return pts;

            List<Vector2> tmp = new List<Vector2>();
            tmp.AddRange(pts);

            List<Vector2> result_points = new List<Vector2>();
            //get every point's new position.
            int num_points = tmp.Count;
            Vector2 temp_p;
            //Vector2 temp_p = null;
            while (loop > 0)
            {
                result_points.Clear();
                List<Vector2> pts_normal = SetPointsNormal(tmp);
                if (pts_normal == null)
                {
                    Console.WriteLine("Not enough points.");
                    return null;
                }
                result_points.Add(tmp[0]);
                for (int i = 1; i < num_points - 1; i++)
                {
                    temp_p = SmoothGetNewSinglePoint(tmp, pts_normal, i);
                    result_points.Add(temp_p);
                }
                result_points.Add(tmp[num_points - 1]);

                tmp.Clear();
                tmp.AddRange(result_points);
                loop--;
            }

            //reset stroke           
            return result_points;
        }
        public static List<Vector3> FitCenterCurve(List<Vector3> centers, List<float> weights)
        {
            // sparse item
            alglib.sparsematrix a;
            alglib.sparsecreate(centers.Count, centers.Count, out a);
            double[] b_x = new double[centers.Count];
            double[] b_y = new double[centers.Count];
            double[] b_z = new double[centers.Count];

            for (int i = 0; i < centers.Count; i++)
            {
                // Build A
                for (int j = 0; j < centers.Count; j++)
                {
                    #region old veision
                    //if (i == j)
                    //    alglib.sparseset(a, i, j, 2.0 + weights[i]);
                    //if (i == j - 1)
                    //    alglib.sparseset(a, i, j, -1.0);
                    //if (i == j - 2)
                    //    alglib.sparseset(a, i, j, -1.0);

                    //// handle the boundary
                    //if (i == centers.Count - 2 && i == j)
                    //    alglib.sparseset(a, i, j, 1.0 + weights[i]);
                    //if (i == centers.Count - 2 && i == j - 1)
                    //    alglib.sparseset(a, i, j, -1.0);
                    //if (i == centers.Count - 1 && i == j)
                    //    alglib.sparseset(a, i, j, weights[i]); 
                    #endregion

                    if (i == j)
                        alglib.sparseset(a, i, j, 2.0 + weights[i]);
                    if (i == j - 1)
                        alglib.sparseset(a, i, j, -1.0);
                    if (i == j + 1)
                        alglib.sparseset(a, i, j, -1.0);

                    // handle the boundary
                    if ((i == 0 || i == centers.Count - 1) && i == j)
                        alglib.sparseset(a, i, j,  weights[i]);
                    if (i == centers.Count - 1 && i == j + 1)
                        alglib.sparseset(a, i, j, 0);
                    if (i == 0 && i == j - 1)
                        alglib.sparseset(a, i, j, 0);

                }

                // BUild b_x b_y b_z
                b_x[i] = centers[i][0] * weights[i];
                b_y[i] = centers[i][1] * weights[i];
                b_z[i] = centers[i][2] * weights[i];
            }

            alglib.sparseconverttocrs(a);

            alglib.linlsqrstate s;
            alglib.linlsqrreport rep;
            alglib.linlsqrcreate(centers.Count, centers.Count, out s);

            double[] centers_x;
            alglib.linlsqrsolvesparse(s, a, b_x);
            alglib.linlsqrresults(s, out centers_x, out rep);

            double[] centers_y;
            alglib.linlsqrsolvesparse(s, a, b_y);
            alglib.linlsqrresults(s, out centers_y, out rep);

            double[] centers_z;
            alglib.linlsqrsolvesparse(s, a, b_z);
            alglib.linlsqrresults(s, out centers_z, out rep);

            // Build new vecter
            List<Vector3> FitedCenters = new List<Vector3>();
            for (int i = 0; i < centers.Count; i++)
            {
                FitedCenters.Add(new Vector3((float)centers_x[i], (float)centers_y[i], (float)centers_z[i]));
            }
            return FitedCenters;
        }

        public static List<Vector2> SetPointsNormal(List<Vector2> points)
        {
            int num_points = points.Count;
            if (num_points < 2)
            {
                return null;
            }
            List<Vector2> normals = new List<Vector2>();
            Vector2 temp = new Vector2();
            Vector2 temp2 = new Vector2();
            int i = 0;
            while (i < num_points)
            {
                // Line 
                int next_i = Mathf.Min(i + 1, num_points - 1);
                int pre_i = Mathf.Max(i - 1, 0);
                // Loop
                //int next_i =(i + 1) % num_points;
                //int pre_i = (num_points + i - 1) % num_points;

                temp = Utility.NewVector2(points[next_i] - points[i]).normalized;
                temp2 = Utility.NewVector2(points[i] - points[pre_i]).normalized;

                normals.Add(Utility.PerpendicularRight(temp + temp2).normalized);
                i++;
            }
            return normals;
        }

        public static List<Vector3> SetPointsNormal(List<Vector3> points3)
        {
            int num_points = points3.Count;
            if (num_points < 2)
            {
                return null;
            }
            List<Vector3> normals3 = new List<Vector3>();
            Vector3 temp = new Vector3();
            Vector3 temp2 = new Vector3();
            int i = 0;
            while (i < num_points)
            {
                // Line 
                int next_i = Mathf.Min(i + 1, num_points - 1);
                int pre_i = Mathf.Max(i - 1, 0);
                // Loop
                //int next_i =(i + 1) % num_points;
                //int pre_i = (num_points + i - 1) % num_points;

                temp = Utility.NewVector3(points3[next_i] - points3[i]).normalized;
                temp2 = Utility.NewVector3(points3[i] - points3[pre_i]).normalized;

                normals3.Add(Utility.PerpendicularRight(temp + temp2).normalized);
                i++;
            }
            return normals3;
        }

        public static List<Vector2> SetPointsTangential(List<Vector2> points)
        {
            int num_points = points.Count;
            if (num_points < 2)
            {
                return null;
            }
            List<Vector2> tangentials = new List<Vector2>();
            Vector2 temp = new Vector2();
            Vector2 temp2 = new Vector2();
            int i = 0;
            while (i < num_points)
            {
                // Line 
                int next_i = Mathf.Min(i + 1, num_points - 1);
                int pre_i = Mathf.Max(i - 1, 0);
                // Loop
                //int next_i =(i + 1) % num_points;
                //int pre_i = (num_points + i - 1) % num_points;

                temp = Utility.NewVector2(points[next_i] - points[i]).normalized;
                temp2 = Utility.NewVector2(points[i] - points[pre_i]).normalized;

                tangentials.Add((temp + temp2).normalized);
                i++;
            }
            return tangentials;
        }

        public static List<Vector3> SetPointsTangential(List<Vector3> points3)
        {
            int num_points = points3.Count;
            if (num_points < 2)
            {
                return null;
            }
            List<Vector3> tangentials3 = new List<Vector3>();
            Vector3 temp = new Vector3();
            Vector3 temp2 = new Vector3();
            int i = 0;
            while (i < num_points)
            {
                // Line 
                int next_i = Mathf.Min(i + 1, num_points - 1);
                int pre_i = Mathf.Max(i - 1, 0);
                // Loop
                //int next_i =(i + 1) % num_points;
                //int pre_i = (num_points + i - 1) % num_points;

                temp = Utility.NewVector3(points3[next_i] - points3[i]).normalized;
                temp2 = Utility.NewVector3(points3[i] - points3[pre_i]).normalized;

                tangentials3.Add((temp + temp2).normalized);
                i++;
            }
            return tangentials3;
        }

        private static Vector2 SmoothGetNewSinglePoint(List<Vector2> points, List<Vector2> pts_normal, int index)
        {
            float BILA_SIGMA_D2 = 200;
            float BILA_SIGMA_S2 = 0.1f;
            float sigmad2 = BILA_SIGMA_D2;
            float sigmas2 = BILA_SIGMA_S2;

            int num_points = points.Count;

            Vector2 resultpoint = Utility.NewVector2(points[index]);
            Vector2 origv2 = new Vector2(0.0f, 0.0f);
            List<float> w_spatial = new List<float>(), w_signal = new List<float>();
            float w_sum_spatial = 0.0f, w_sum_signal = 0.0f, w_sum = 0.0f;

            int scope = 3;
            for (int i = -scope; i <= scope; i++)
            {
                //spatial w
                int otherindex = i + index;
                if (i + index < 0 || i + index >= points.Count)
                {
                    //otherindex = num_points + otherindex;
                    continue;
                }

                float spa_dis = Vector2.Distance(points[otherindex % num_points], points[index]);
                w_spatial.Add(Mathf.Exp(-Mathf.Pow(spa_dis, 2) / sigmad2));
                //signal w
                float sig_dis = 1 - Vector2.Dot(pts_normal[otherindex % num_points], pts_normal[index]);
                w_signal.Add(Mathf.Exp(-Mathf.Pow(sig_dis, 2) / sigmas2));

                w_sum_spatial += w_spatial[w_spatial.Count - 1];
                w_sum_signal += w_signal[w_signal.Count - 1];
            }
            int j = 0;
            for (int i = -scope; i <= scope; i++)
            {
                int otherindex = i + index;
                if (i + index < 0 || i + index >= points.Count)
                {
                    //otherindex = num_points + otherindex;
                    continue;
                }
                //spatial w
                w_spatial[j] /= w_sum_spatial;
                //signal w
                w_signal[j] /= w_sum_signal;

                w_sum += w_spatial[j] * w_signal[j];

                origv2 += points[otherindex % num_points] * w_spatial[j] * w_signal[j];
                j++;
            }
            //normalize w
            origv2 /= w_sum;

            //get new point
            resultpoint = origv2;

            return resultpoint;
        }

    }
}
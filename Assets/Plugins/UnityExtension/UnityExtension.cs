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
    }

    public class FitExtension
    {
        public static List<Vector2> FitCurve(List<Vector2> points)
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
                    if (i == j)
                        alglib.sparseset(a, i, j, 2.0 + weights[i]);
                    if (i == j - 1)
                        alglib.sparseset(a, i, j, -1.0);
                    if (i == j - 2)
                        alglib.sparseset(a, i, j, -1.0);

                    // handle the boundary
                    if (i == centers.Count - 2 && i == j)
                        alglib.sparseset(a, i, j, 1.0 + weights[i]);
                    if (i == centers.Count - 2 && i == j - 1)
                        alglib.sparseset(a, i, j, -1.0);
                    if (i == centers.Count - 1 && i == j)
                        alglib.sparseset(a, i, j, weights[i]);
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
    }
}
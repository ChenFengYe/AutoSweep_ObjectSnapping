using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MyGeometry;

namespace NumericalRecipes
{

    public class RansacLine3d
    {
        public List<Vector3> inliers = new List<Vector3>();
        public Ray bestline;
        public Vector3 startpoint, endpoint;

        private double thres;
        private double probability;
        //* thres: if the distance between points and line are below this threshold, we regard points to be in this line;
        //* probability: the condition when we should stop current iteration. */
        public RansacLine3d(double thres_, double probability_)
        {
            this.thres = thres_;
            this.probability = probability_;
        }
        public RansacLine3d()
        {
            this.thres = 0.00005;
            this.probability = 0.9;
        }

        public bool Estimate(List<Vector3> points)
        {
            this.inliers.Clear();
            this.bestline = new Ray();

            int iter = 200;
            if (points.Count == 0) return false;
            System.Random rd = new System.Random();

            Vector3 A = new Vector3();
            Vector3 B = new Vector3();

            int maxpointinline = int.MinValue;

            for (int i = 0; i < iter; i++)
            {
                A = points[rd.Next(points.Count)];
                B = points[rd.Next(points.Count)];
                if (A == B) continue;  //if can't generate line

                Ray testline = new Ray(A, (B - A).normalized);
                List<Vector3> tempinliers = new List<Vector3>();
                int inlierscount = 0;
                for (int j = 0; j < points.Count; j++)
                {
                    if (points[j] != A && points[j] != B)
                    {
                        if (Utility.DistanceToRay(testline, points[j]) < thres)
                        {
                            tempinliers.Add(points[j]);
                            inlierscount++;
                        }
                    }
                }

                if (inlierscount > maxpointinline)
                {
                    maxpointinline = inlierscount;
                    this.bestline = new Ray(testline.origin, testline.direction);
                    this.inliers.Clear();
                    foreach (Vector3 p in tempinliers)
                        this.inliers.Add(p);
                }

                if (inlierscount >= probability * points.Count)
                    break;
            }

            if (this.inliers.Count != 0)
            {
                float mint = float.MaxValue;
                float maxt = float.MinValue;
                foreach (Vector3 p in this.inliers)
                {
                    float t = Utility.ComputeT(bestline, p);
                    if (t > maxt)
                        maxt = t;
                    if (t < mint)
                        mint = t;
                }
                this.startpoint = this.bestline.GetPoint(mint);
                this.endpoint = this.bestline.GetPoint(maxt);
            }

            if (startpoint.x != float.NaN && startpoint.y != float.NaN && startpoint.z != float.NaN &&
            endpoint.x != float.NaN && endpoint.y != float.NaN && endpoint.z != float.NaN &&
                (!Utility.IsNull(startpoint) && !Utility.IsNull(endpoint)))
                return true;
            else
                return false;
        }
    }

    public class RansacLine2d
    {
        public List<Vector2> inliers = new List<Vector2>();

        private double thres;
        private double probability;
        public Ray2D bestline;

        public Vector2 startpoint, endpoint;
        //* thres: if the distance between points and line are below this threshold, we regard points to be in this line;
        //* probability: the condition when we should stop current iteration. */
        public RansacLine2d(double thres_, double probability_)
        {
            this.thres = thres_;
            this.probability = probability_;
        }
        public RansacLine2d()
        {
            this.thres = 5;
            this.probability = 0.9;
        }

        public bool Estimate(List<Vector2> points)
        {
            this.inliers.Clear();
            this.bestline = new Ray2D();

            int iter = 200;
            if (points.Count == 0) return false;
            System.Random rd = new System.Random();

            Vector2 A = new Vector2();
            Vector2 B = new Vector2();

            //int maxpointinline = int.MinValue;
            double maxpointinline = 2;
            for (int i = 0; i < iter; i++)
            {
                A = points[rd.Next(points.Count)];
                B = points[rd.Next(points.Count)];
                if (A == B) continue;  //if can't generate line

                Ray2D testline = new Ray2D(A, (B - A).normalized);

                List<Vector2> tempinliers = new List<Vector2>();
                int inlierscount = 0;
                for (int j = 0; j < points.Count; j++)
                {
                    if (points[j] != A && points[j] != B)
                    {
                        if (Utility.DistanceToRay(testline, points[j]) < thres)
                        {
                            tempinliers.Add(points[j]);
                            inlierscount++;
                        }
                    }
                }

                if (inlierscount > maxpointinline)
                {
                    maxpointinline = inlierscount;
                    bestline = new Ray2D(testline.origin, testline.direction);
                    this.inliers.Clear();
                    foreach (Vector2 p in tempinliers)
                        this.inliers.Add(p);
                }

                if (inlierscount >= probability * points.Count)
                    break;
            }
            if (this.inliers.Count != 0)
            {
                double mint = double.MaxValue;
                double maxt = double.MinValue;
                foreach (Vector2 p in this.inliers)
                {
                    double t = Utility.ComputeT(this.bestline, p);
                    if (t > maxt)
                        maxt = t;
                    if (t < mint)
                        mint = t;
                }
                this.startpoint = this.bestline.GetPoint((float)mint);
                this.endpoint = this.bestline.GetPoint((float)maxt);
            }


            if (this.inliers.Count() > 0)
                return true;
            else
                return false;

        }

    }


}
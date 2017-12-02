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
        public Line3 bestline;

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

        public Line3 Estimate(List<Vector3> points)
        {
            this.inliers.Clear();
            this.bestline = null;

            int iter = 200;
            if (points.Count == 0) return null;
            System.Random rd = new System.Random();

            Vector3 A = new Vector3();
            Vector3 B = new Vector3();

            int maxpointinline = 2;

            for (int i = 0; i < iter; i++)
            {
                A = points[rd.Next(points.Count)];
                B = points[rd.Next(points.Count)];
                if (A == B) continue;  //if can't generate line

                Line3 testline = new Line3(A, (B - A).normalized);
                List<Vector3> tempinliers = new List<Vector3>();
                int inlierscount = 0;
                for (int j = 0; j < points.Count; j++)
                {
                    if (points[j] != A && points[j] != B)
                    {
                        if (testline.DistanceToLine(points[j]) < thres)
                        {
                            tempinliers.Add(points[j]);
                            inlierscount++;
                        }
                    }
                }

                if (inlierscount > maxpointinline)
                {
                    maxpointinline = inlierscount;
                    this.bestline = testline.Copy();
                    this.inliers.Clear();
                    foreach (Vector3 p in tempinliers)
                        this.inliers.Add(p);
                }

                if (inlierscount >= probability * points.Count)
                    break;
            }

            if (this.inliers.Count != 0)
            {
                double mint = double.MaxValue;
                double maxt = double.MinValue;
                foreach (Vector3 p in this.inliers)
                {
                    double t = this.bestline.ComputeT(p);
                    if (t > maxt)
                        maxt = t;
                    if (t < mint)
                        mint = t;
                }
                bestline.SetPoints((float)mint, (float)maxt);
            }

            if (bestline != null && bestline.start.x != double.NaN && bestline.start.y != double.NaN && bestline.start.z != double.NaN &&
            bestline.end.x != double.NaN && bestline.end.y != double.NaN && bestline.end.z != double.NaN &&
                (!Utility.IsNull(bestline.start) && !Utility.IsNull(bestline.end)))
                return bestline;
            else
                return null;

        }

    }

    public class RansacLine2d
    {
        public List<Vector2> inliers = new List<Vector2>();

        private double thres;
        private double probability;
        public Line2 bestline;
        //* thres: if the distance between points and line are below this threshold, we regard points to be in this line;
        //* probability: the condition when we should stop current iteration. */
        public RansacLine2d(double thres_, double probability_)
        {
            this.thres = thres_;
            this.probability = probability_;
        }
        public RansacLine2d()
        {
            this.thres = 0.06;
            this.probability = 0.98;
        }

        public Line2 Estimate(List<Vector2> points)
        {
            this.inliers.Clear();
            this.bestline = null;

            int iter = 200;
            if (points.Count == 0) return null;
            System.Random rd = new System.Random();

            Vector2 A = new Vector2();
            Vector2 B = new Vector2();

            //int maxpointinline = int.MinValue;
            double maxpointinline = 2; //points.Count/2;
            bestline = null;
            for (int i = 0; i < iter; i++)
            {
                A = points[rd.Next(points.Count)];
                B = points[rd.Next(points.Count)];
                if (A == B) continue;  //if can't generate line

                Line2 testline = new Line2(A, B);

                List<Vector2> tempinliers = new List<Vector2>();
                int inlierscount = 0;
                for (int j = 0; j < points.Count; j++)
                {
                    if (points[j] != A && points[j] != B)
                    {
                        if (testline.DistanceToLine(points[j]) < thres)
                        {
                            tempinliers.Add(points[j]);
                            inlierscount++;
                        }
                    }
                }

                if (inlierscount > maxpointinline)
                {
                    maxpointinline = inlierscount;
                    bestline = new Line2(testline);
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
                    double t = this.bestline.ComputeT(p);
                    if (t > maxt)
                        maxt = t;
                    if (t < mint)
                        mint = t;
                }
                bestline.SetPoints((float)mint, (float)maxt);
            }


            if (bestline != null && this.inliers.Count() > 0)
                return bestline;
            else
                return null;

        }

    }


}

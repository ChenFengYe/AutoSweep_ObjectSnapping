using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

namespace MyGeometry
{
    public class Line2
    {
        public Vector2 start, end, dir;

        public Line2(Vector2 a, Vector2 b)
        {
            this.start = a;
            this.end = b;
            this.dir = (end - start).normalized;
        }

        public Line2(Vector2 a, Vector2 dir, bool flag)
        {
            this.start = a;
            this.dir = dir.normalized;
            this.end = a + this.dir * 10;
        }

        public double Length()
        {
            return Vector2.Distance(this.start, this.end);
        }

        public Line2(Line2 another)
        {
            this.start = new Vector2(another.start.x, another.start.y);
            this.end = new Vector2(another.end.x, another.end.y);
            this.dir = new Vector2(another.dir.x, another.dir.y);
        }

        public double DistanceToLine(Vector2 point)
        {
            // v = y2 u = y1
            double a = end.y - start.y;
            double b = start.x - end.x;
            double c = end.x * start.y - start.x * end.y;

            return Math.Abs((a * point.x + b * point.y + c) / Math.Sqrt(a * a + b * b));
        }
        public Vector2 ProjToLine(Vector2 p)
        {
            float t = Vector2.Dot((p - start), dir) / dir.sqrMagnitude;
            return start + dir * t;
        }

        public float ComputeT(Vector2 p)
        {
            //project to line
            float t = Vector2.Dot((p - start), this.dir) / this.dir.sqrMagnitude;
            return t;
        }

        public void SetPoints(float startt, float endt)
        {
            this.start = this.start + startt * this.dir;
            this.end = this.start + endt * this.dir;
            this.dir = (end - start).normalized;

        }

        public void UpdateEnd(Vector2 p)
        {
            float t = this.ComputeT(p);
            if (t < 0)
                this.start = this.start + t * this.dir;
            if (t > this.ComputeT(this.end))
                this.end = this.start + t * this.dir;
        }

        public void UpdateEnd(List<Vector2> points)
        {
            float mint = float.MaxValue;
            float maxt = float.MinValue;
            foreach (Vector2 p in points)
            {
                float t = this.ComputeT(p);
                if (t > maxt)
                    maxt = t;
                if (t < mint)
                    mint = t;
            }
            this.SetPoints(mint, maxt);
        }

        public Vector2 Intersection(Line2 another)
        {
            float A1, B1, C1, A2, B2, C2;
            this.GetEquation(out A1, out B1, out C1);
            another.GetEquation(out A2, out B2, out C2);

            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
                throw new ArgumentException("Lines are parallel");

            float x = (B2 * C1 - B1 * C2) / delta;
            float y = (A1 * C2 - A2 * C1) / delta;

            return new Vector2(x, y);

        }

        public Vector2 GetPointwithT(float t)
        {
            return start + dir * t;
        }

        public void GetEquation(out float A, out float B, out float C)
        {
            // AX + BY = C
            A = this.end.y - this.start.y;
            B = this.start.x - this.end.x;
            C = this.start.x * this.end.y - this.end.x * this.start.y;

        }
        static public bool IsParallel(Line2 a, Line2 b, double angle = 15)
        {
            if (Math.Abs(Vector2.Dot(a.dir, b.dir)) >= Math.Cos(angle * Math.PI / 180))
                return true;
            else
                return false;
        }

        public Ray2D ToRay2D()
        {
            return new Ray2D(start, dir);
        }

        public LineSegment2D ToLineSegment2D()
        {
            System.Drawing.Point s = new System.Drawing.Point((int)this.start.x,(int)this.start.y);
            System.Drawing.Point e = new System.Drawing.Point((int)this.end.x, (int)this.end.y);

            return new LineSegment2D(s,e);
        }

        public void Flip()
        {
            Vector2 temp = this.start;
            this.start = this.end;
            this.end = temp;
            this.dir = (this.end - this.start).normalized;

        }

        public List<Vector2> SamplePoints()
        {
            List<Vector2> samplepoints = new List<Vector2>();
            double len = this.Length();
            for(int i=0;i<=len;i++)
            {
                samplepoints.Add(GetPointwithT((float)i));
            }
            return samplepoints;
        }
    }
    public class Line3
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 dir;
        public Line3() { }
        public Line3(Vector3 point_, Vector3 dir_)
        {
            this.start = point_;
            this.dir = dir_;
        }
        public Line3(Line3 another)
        {
            this.start = new Vector3(another.start.x, another.start.y, another.start.z);
            this.end = new Vector3(another.end.x, another.end.y, another.end.z);
            this.dir = new Vector3(another.dir.x, another.dir.y, another.dir.z);
        }

        public Line3(Vector3 startpoint, Vector3 endpoint, int tag)
        {
            this.start = startpoint;
            this.end = endpoint;
            this.dir = (endpoint - startpoint).normalized;
        }
        public float DistanceToLine(Vector3 pos)
        {
            float dis = (pos - start).sqrMagnitude - Mathf.Pow(Vector3.Dot(dir, (pos - start)), 2.0f) / dir.sqrMagnitude;
            dis = Mathf.Sqrt(dis);
            return dis;
        }
        public Vector3 ProjectToLine(Vector3 p)
        {
            float t = Vector3.Dot((p - start), dir) / dir.sqrMagnitude;
            return start + dir * t;
        }

        public bool PositiveDirection(Vector3 p)
        {
            float t = Vector3.Dot((p - start), dir) / dir.sqrMagnitude;
            return t >= 0;
        }


        public Line3 Copy()
        {
            return new Line3(this.start, this.dir);
        }

        public void SetPoints(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
            this.dir = (end - start).normalized;
        }
        public void SetPoints(float startt, float endt)
        {
            this.start = this.start + startt * dir;
            this.end = this.start + endt * dir;
            this.dir = (end - start).normalized;
        }


        public double ComputeT(Vector3 p)
        {
            //project to line
            double t = Vector3.Dot((p - start), dir) / dir.sqrMagnitude;
            return t;

            //double tx = (p.x - point.x) / dir.x;
            //double ty = (p.y - point.y) / dir.y;
            //double tz = (p.z - point.z) / dir.z;
            //return (tx + ty + tz) / 3;
        }
        public Vector3 GetPointwithT(float t)
        {
            return start + dir * t;
        }
        public double Length()
        {
            return (this.start - this.end).magnitude;
        }
        public double LineSquareLength()
        {
            return (this.start - this.end).sqrMagnitude;
        }
        public void Scale(float scale)
        {
            float t = (this.end.x - this.start.x) / this.dir.x;
            t *= scale;
            end = this.start + this.dir * t;
        }
        public Ray ToRay()
        {
            return new Ray(start, dir);
        }
    }
}

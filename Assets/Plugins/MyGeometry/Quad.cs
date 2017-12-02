using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using UnityEngine;
using NumericalRecipes;
namespace MyGeometry
{
    public class Quad
    {
        private Vector3 center;
        // 0 ---- 3
        // |      |
        // 1 ---- 2
        private List<Vector3> cornerPoints3d = new List<Vector3>(); // in counterclockwise order
        private float d1, d2;
        public Quad() { }

        public Quad(List<Vector3> corners)
        {
            cornerPoints3d = corners;
            this.ComputeCenter();
            this.GetSideLength();
        }


        public Quad(Quad other)
        {
            this.d1 = other.D1;
            this.d2 = other.D2;
            this.center = other.center;
            this.cornerPoints3d.Clear();
            foreach (Vector3 p in other.cornerPoints3d)
                this.cornerPoints3d.Add(Utility.NewVector3(p));
        }

        public Quad(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            this.cornerPoints3d.Add(v1);
            this.cornerPoints3d.Add(v2);
            this.cornerPoints3d.Add(v3);

            // because v1 + v3 = v2 + v4 = center;
            Vector4 v4 = v1 + v3 - v2;
            this.cornerPoints3d.Add(v4);
            this.center = (v1 + v3) / 2;
            this.GetSideLength();
        }

        public Quad(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.Write("No such rect!");
                return;
            }
            StreamReader sr = new StreamReader(File.Open(filename, FileMode.Open));
            Vector3 v1 = new Vector3(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
            Vector3 v2 = new Vector3(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
            Vector3 v3 = new Vector3(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
            sr.Close();

            this.cornerPoints3d.Add(v1);
            this.cornerPoints3d.Add(v2);
            this.cornerPoints3d.Add(v3);

            // because v1 + v3 = v2 + v4 = center;
            Vector4 v4 = v1 + v3 - v2;
            this.cornerPoints3d.Add(v4);
            this.center = (v1 + v3) / 2;
            this.GetSideLength();
        }

        public Quad(Quad other, Vector3 c, Vector3 dire)
        {
            this.d1 = other.D1;
            this.d2 = other.D2;
            this.center = other.center;
            this.cornerPoints3d.Clear();
            foreach (Vector3 p in other.cornerPoints3d)
                this.cornerPoints3d.Add(Utility.NewVector3(p));

            this.Rotate(Quaternion.FromToRotation(this.Normal, dire));
            this.TranslateTo(c);
        }
        public Quad(Quad other, Vector3 c, Vector3 dire, float scale)
        {
            this.d1 = other.D1;
            this.d2 = other.D2;
            this.center = other.center;
            this.cornerPoints3d.Clear();
            foreach (Vector3 p in other.cornerPoints3d)
                this.cornerPoints3d.Add(Utility.NewVector3(p));

            this.Rotate(Quaternion.FromToRotation(this.Normal, dire));
            this.TranslateTo(c);
            this.Scale(this.d1 * scale, this.d2 * scale);
        }

        public Quad(Quad other, Vector3 c, Vector3 dire, float Scale_d1, float Scale_d2)
        {
            this.d1 = other.D1;
            this.d2 = other.D2;
            this.center = other.center;
            this.cornerPoints3d.Clear();
            foreach (Vector3 p in other.cornerPoints3d)
                this.cornerPoints3d.Add(Utility.NewVector3(p));

            this.Rotate(Quaternion.FromToRotation(this.Normal, dire));
            this.TranslateTo(c);
            this.Scale(this.d1 * Scale_d1, this.d2 * Scale_d2);
        }

        public Vector3 Center
        {
            get { return center; }
        }

        public float D1 // horizontal
        {
            //get { return Vector3.Distance(cornerPoints3d[3], cornerPoints3d[0]); }
            get { return d1; }
        }

        public float D2 // vertical
        {
            //get { return Vector3.Distance(cornerPoints3d[1], cornerPoints3d[0]); }
            get { return d2; }
        }

        public Vector3 Origin
        {
            get { return cornerPoints3d[0]; }
        }
        public List<Vector3> CornerPoints3d
        {
            get { return cornerPoints3d; }
        }
        public Plane BelongPlane
        {
            get { return new Plane(this.cornerPoints3d[0], this.cornerPoints3d[1], this.cornerPoints3d[2]); }
        }
        public Vector3 Normal
        {
            get { return BelongPlane.normal.normalized; }
            set { }
        }
        public List<Vector3> quadPoints3d
        {
            get
            {
                int sampleNum = 200;
                List<Vector3> samplePoints = new List<Vector3>();
                for (int i = 0; i < cornerPoints3d.Count; i++)
                {
                    int i_next = (i + 1) % cornerPoints3d.Count;
                    Vector3 dire = cornerPoints3d[i_next] - cornerPoints3d[i];
                    for (int j = 0; j < sampleNum / 4; j++)
                        samplePoints.Add(cornerPoints3d[i] + (float)j / (sampleNum / 4) * dire);
                }
                return samplePoints;
            }
        }

        public void ComputeCenter()
        {
            this.center = new Vector3();
            foreach (Vector3 p in cornerPoints3d)
                this.center += p;
            this.center /= cornerPoints3d.Count;
        }

        public void FlipNormal()
        {
            Vector3 temp = cornerPoints3d[1];
            cornerPoints3d[1] = cornerPoints3d[3];
            cornerPoints3d[3] = temp;
            float d_temp = d1;
            d1 = d2;
            d2 = d_temp;
        }

        public void GetSideLength()
        {
            if (this.cornerPoints3d.Count == 0) throw new Exception("empty quad");
            this.d1 = Vector3.Distance(cornerPoints3d[3], cornerPoints3d[0]);
            this.d2 = Vector3.Distance(cornerPoints3d[1], cornerPoints3d[0]);
        }

        public List<Vector3> SampleBoundPoints(int number = 200)
        {
            List<Line3> rays = this.GetBoundaryLines();
            List<Vector3> boundPoints3d = new List<Vector3>();

            int sample1 = (int)(number * d1 / (d1 + d2));
            int sample2 = (int)(number * d2 / (d1 + d2));

            List<Vector3> horizontalup = new List<Vector3>();
            List<Vector3> horizontalbottom = new List<Vector3>();
            for (int i = 0; i < sample1; i++)
            {
                horizontalup.Add(rays[1].GetPointwithT(d1 - i * d1 / sample1));
                horizontalbottom.Add(rays[2].GetPointwithT(d1 - i * d1 / sample1));

            }

            List<Vector3> verticalleft = new List<Vector3>();
            List<Vector3> verticalright = new List<Vector3>();
            for (int i = 0; i < sample2; i++)
            {
                verticalleft.Add(rays[0].GetPointwithT(i * d2 / sample2));
                verticalright.Add(rays[3].GetPointwithT(i * d2 / sample2));
            }

            //counterclockwise
            boundPoints3d.Clear();
            boundPoints3d.AddRange(verticalleft);
            boundPoints3d.AddRange(horizontalbottom);
            boundPoints3d.AddRange(verticalright);
            boundPoints3d.AddRange(horizontalup);

            return boundPoints3d;
        }

        public List<Line3> GetBoundaryLines()
        {
            //          line1
            //        0 ----> 3
            // line0  |       ^  line3
            //        v       |
            //        1 <---- 2
            //          line2

            List<Line3> bounds = new List<Line3>();
            bounds.Add(new Line3(this.cornerPoints3d[0], this.cornerPoints3d[1] - this.cornerPoints3d[0]));
            bounds.Add(new Line3(this.cornerPoints3d[0], this.cornerPoints3d[3] - this.cornerPoints3d[0]));
            bounds.Add(new Line3(this.cornerPoints3d[2], this.cornerPoints3d[1] - this.cornerPoints3d[2]));
            bounds.Add(new Line3(this.cornerPoints3d[2], this.cornerPoints3d[3] - this.cornerPoints3d[2]));
            return bounds;
        }

        public void FitRect()
        {
            //     y
            //     ^
            //     |
            // 0 ---- 3
            // |   c--|-----> x
            // 1 ---- 2
            //
            // build local coordinate
            Vector3 vx = this.cornerPoints3d[2] - this.cornerPoints3d[1];
            vx.Normalize();
            Vector3 vy = Vector3.Cross(this.Normal, vx);
            CoordinateFrame localcoordinate = new CoordinateFrame(vx, vy, this.center);

            List<Vector2> corner2d = new List<Vector2>();
            foreach (Vector3 p in this.cornerPoints3d)
            {
                corner2d.Add(localcoordinate.WorldToLocal(p));
            }

            Line2 axisx = new Line2(Vector2.zero, new Vector2(1, 0));

            Line2 l01 = new Line2(corner2d[0], corner2d[1]);
            Line2 l23 = new Line2(corner2d[2], corner2d[3]);

            Line2 l12 = new Line2(corner2d[1], corner2d[2]);
            Line2 l03 = new Line2(corner2d[0], corner2d[3]);

            Vector2 mean01 = axisx.Intersection(l01);
            Vector2 mean23 = axisx.Intersection(l23);

            Vector2 new0 = l03.ProjToLine(mean01);
            Vector2 new1 = l12.ProjToLine(mean01);
            Vector2 new2 = l12.ProjToLine(mean23);
            Vector2 new3 = l03.ProjToLine(mean23);

            this.cornerPoints3d.Clear();
            this.cornerPoints3d.Add(localcoordinate.LocalToWorld(new0));
            this.cornerPoints3d.Add(localcoordinate.LocalToWorld(new1));
            this.cornerPoints3d.Add(localcoordinate.LocalToWorld(new2));
            this.cornerPoints3d.Add(localcoordinate.LocalToWorld(new3));

            this.ComputeCenter();
            this.GetSideLength();
        }


        public void Rotate(Quaternion Q)
        {
            Vector3 ori_center = this.center;
            this.TranslateTo(Vector3.zero);

            for (int i = 0; i < this.cornerPoints3d.Count; i++)
                this.cornerPoints3d[i] = Q * this.cornerPoints3d[i];
            this.ComputeCenter();

            this.TranslateTo(ori_center);
        }

        public void TranslateTo(Vector3 newcenter)
        {
            Vector3 delta = newcenter - this.center;
            for (int i = 0; i < this.cornerPoints3d.Count; i++)
                this.cornerPoints3d[i] = delta + this.cornerPoints3d[i];
            center = newcenter;
        }

        public void Scale(float d1, float d2)
        {
            this.d1 = d1;
            this.d2 = d2;

            Vector3 upmid = (this.cornerPoints3d[0] + this.cornerPoints3d[3]) / 2;
            Ray leftup = new Ray(upmid, this.cornerPoints3d[0] - upmid);
            Vector3 newv0 = leftup.GetPoint(d1 / 2);
            Ray rightup = new Ray(upmid, this.cornerPoints3d[3] - upmid);
            Vector3 newv3 = rightup.GetPoint(d1 / 2);

            Vector3 downmid = (this.cornerPoints3d[1] + this.cornerPoints3d[2]) / 2;
            Ray leftdown = new Ray(downmid, this.cornerPoints3d[1] - downmid);
            Vector3 newv1 = leftdown.GetPoint(d1 / 2);
            Ray rightdown = new Ray(downmid, this.cornerPoints3d[2] - downmid);
            Vector3 newv2 = rightdown.GetPoint(d1 / 2);


            Vector3 leftmid = (newv0 + newv1) / 2;
            Ray upleft = new Ray(leftmid, newv0 - leftmid);
            newv0 = upleft.GetPoint(d2 / 2);
            Ray downleft = new Ray(leftmid, newv1 - leftmid);
            newv1 = downleft.GetPoint(d2 / 2);

            Vector3 rightmid = (newv2 + newv3) / 2;
            Ray upright = new Ray(rightmid, newv3 - rightmid);
            newv3 = upright.GetPoint(d2 / 2);
            Ray downright = new Ray(rightmid, newv2 - rightmid);
            newv2 = downright.GetPoint(d2 / 2);

            this.cornerPoints3d.Clear();
            this.cornerPoints3d.Add(newv0);
            this.cornerPoints3d.Add(newv1);
            this.cornerPoints3d.Add(newv2);
            this.cornerPoints3d.Add(newv3);
            this.ComputeCenter();

        }


        public void Save(string path)
        {
            string dir = Environment.CurrentDirectory;
            string fileName = Path.GetFileNameWithoutExtension(path);
            string filePath = Path.GetDirectoryName(path);
            FileStream fs = new FileStream(filePath + "/" + fileName + ".quad", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(this.cornerPoints3d[0].x);
            sw.WriteLine(this.cornerPoints3d[0].y);
            sw.WriteLine(this.cornerPoints3d[0].z);
            sw.WriteLine(this.cornerPoints3d[1].x);
            sw.WriteLine(this.cornerPoints3d[1].y);
            sw.WriteLine(this.cornerPoints3d[1].z);
            sw.WriteLine(this.cornerPoints3d[2].x);
            sw.WriteLine(this.cornerPoints3d[2].y);
            sw.WriteLine(this.cornerPoints3d[2].z);
            sw.Flush();
            sw.Close();
            fs.Close();
        }
    }

    public class CoordinateFrame
    {
        public Vector3 x, y;
        public Vector3 centerinworld;

        public CoordinateFrame(Vector3 x_, Vector3 y_, Vector3 centerinworld_)
        {
            this.x = x_;
            this.y = y_;
            this.centerinworld = centerinworld_;
        }

        public Vector2 WorldToLocal(Vector3 p)
        {
            Vector2 p2;
            Line3 axisx = new Line3(centerinworld, x);
            p2.y = axisx.DistanceToLine(p);
            Line3 axisy = new Line3(centerinworld, y);
            p2.x = axisy.DistanceToLine(p);

            if (!axisx.PositiveDirection(p))
                p2.x = -p2.x;
            if (!axisy.PositiveDirection(p))
                p2.y = -p2.y;

            return p2;
        }

        public Vector3 LocalToWorld(Vector2 p)
        {
            Vector3 p3 = p.x * this.x + p.y * this.y + centerinworld;
            return p3;
        }
    }

}

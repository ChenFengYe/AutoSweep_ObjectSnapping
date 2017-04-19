using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using UnityEngine;
namespace MyGeometry
{
    public class Circle3
    {
        private Vector3 center;
        private Vector2 center2d;
        private Vector3 normal;
        private float radius;
        private float area;
        private Plane belongPlane;
        private List<Vector3> circlePoints3d = new List<Vector3>(); // better rename to be "circleSamples", since they are all sampled points
        private List<Vector3> circleSlice3d = new List<Vector3>();
        private List<Vector2> circleSlice2d = new List<Vector2>();
        private List<Circle3> neighbors = new List<Circle3>();

        public Circle3()
        {
        }
        public Circle3(Vector3 c, float r)
        {
            center = c;
            radius = r;
            ComputeArea();
        }
        public Circle3(Vector3 c, float r, Vector3 n, List<Vector3> cp)
        {
            center = c;
            radius = r;
            normal = n.normalized;
            circlePoints3d = cp;
            ComputeArea();
        }
        public Circle3(Vector3 c, float r, Plane p, int sample = 100)
        {
            center = c;
            radius = r;
            normal = p.normal;
            belongPlane = p;
            ComputeArea();
            SampleCirclePoints3d(c, p.normal, r, sample);
        }
        public Circle3(Vector3 c, float r, Vector3 n, int sample = 100)
        {
            center = c;
            radius = r;
            belongPlane = new Plane(n.normalized, c);
            normal = belongPlane.normal;
            ComputeArea();
            SampleCirclePoints3d(c, belongPlane.normal, r, sample);
        }
        public Circle3(Vector3 c, float r, Plane p, List<Vector3> cp)
        {
            center = c;
            radius = r;
            normal = p.normal;
            belongPlane = p;
            circlePoints3d = cp;
            ComputeArea();
        }
        public Circle3(Circle3 circle)
        {
            center = circle.Center;
            radius = circle.Radius;
            belongPlane = circle.BelongPlane;
            circlePoints3d = circle.CirclePoints;
            ComputeArea();
            neighbors = circle.Neighbors;
        }

        public Circle3(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.Write("No such circle!");
                return;
            }
            StreamReader sr = new StreamReader(File.Open(filename, FileMode.Open));
            normal = new Vector3(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
            center = new Vector3(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
            radius = float.Parse(sr.ReadLine());
            this.belongPlane = new Plane(normal, center);
            ComputeArea();
            SampleCirclePoints3d(center, normal, radius, 100);
        }
        public Vector3 Center   // If use same name,"public Vector3 Center { get; set; }" is ok
        {
            get { return center; }
            set { center = value; }
        }
        public Vector2 Center2d
        {
            get { return center2d; }
            set { center2d = value; }
        }
        public Vector3 Normal
        {
            get { return normal; }
        }
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }
        public float Area
        {
            get { return area; }
            set { area = value; }
        }
        public Plane BelongPlane
        {
            get { return belongPlane; }
            set { belongPlane = value; }
        }
        public List<Vector3> CirclePoints
        {
            get { return circlePoints3d; }
            set { circlePoints3d = value; }
        }
        public List<Vector3> CircleSlice3d
        {
            get { return circleSlice3d; }
            set { circleSlice3d = value; }
        }
        public List<Vector2> CircleSlice2d
        {
            get { return circleSlice2d; }
            set { circleSlice2d = value; }
        }
        public List<Circle3> Neighbors
        {
            get { return neighbors; }
            set { neighbors = value; }
        }

        private void SampleCirclePoints3d(Vector3 center, Vector3 normal, float radius, int sample)
        {
            sample = sample + 1;
            Plane circleplane = new Plane(normal.normalized, center);
            //Vector3 pointonplane = circleplane.ProjectPoint(center - new Vector3(0.01, 0.01, 0.01));
            //Vector3 u = pointonplane - center;
            Vector3 pointonplane = Utility.PlaneProjectPoint(circleplane, center + new Vector3(1.0f, 0f, 0f));
            Vector3 u = pointonplane - center;
            u.Normalize();
            Vector3 v = Vector3.Cross(u, normal);
            v.Normalize();
            int num = sample;

            float theta = 2.0f * Mathf.PI / (num - 1);

            for (int j = 0; j < num; j++)
            {
                float x = center.x + radius * (u.x * Mathf.Cos(j * theta) + v.x * Mathf.Sin(j * theta));
                float y = center.y + radius * (u.y * Mathf.Cos(j * theta) + v.y * Mathf.Sin(j * theta));
                float z = center.z + radius * (u.z * Mathf.Cos(j * theta) + v.z * Mathf.Sin(j * theta));
                this.circlePoints3d.Add(new Vector3(x, y, z));
            }
        }

        public void AddToCircleSlice(Vector3 sample)
        {
            this.circleSlice3d.Add(sample);
        }
        public void ComputeArea()
        {
            this.area = Mathf.PI * Mathf.Pow(radius, 2.0f);
        }
        public void AddNeighbor(Circle3 c)
        {
            neighbors.Add(c);
        }

        public void Save()
        {
            string dir = Environment.CurrentDirectory;
            FileStream fs = new FileStream("top.circle", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(normal.x);
            sw.WriteLine(normal.y);
            sw.WriteLine(normal.z);
            sw.WriteLine(center.x);
            sw.WriteLine(center.y);
            sw.WriteLine(center.z);
            sw.WriteLine(radius);
            sw.Flush();
            sw.Close();
            fs.Close();
        }



    }
}

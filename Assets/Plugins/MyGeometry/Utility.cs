using UnityEngine;
using System.Collections.Generic;
using System;

namespace MyGeometry
{
    public class Utility
    {
        //vector2
        public static bool IsNull(Vector2 v)
        {
            return v.x == 0 && v.y == 0;
        }
        public static Vector2 PerpendicularLeft(Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }
        public static Vector2 PerpendicularRight(Vector2 v)
        {
            return new Vector2(v.y, -v.x);
        }
        public static Vector2 Average(List<Vector2> vs)
        {
            Vector2 a = new Vector2(0, 0);
            foreach (var p in vs)
            {
                a += p;
            }
            a /= vs.Count;
            return a;
        }
        //vector3
        public static bool IsNull(Vector3 v)
        {
            return v.x == 0 && v.y == 0 && v.z == 0;
        }
        public static Matrix3d OuterCross(Vector3 v1, Vector3 v2)
        {
            Matrix3d m = new Matrix3d();
            m[0, 0] = v2.x * v1.x;
            m[0, 1] = v2.x * v1.y;
            m[0, 2] = v2.x * v1.z;
            m[1, 0] = v2.y * v1.x;
            m[1, 1] = v2.y * v1.y;
            m[1, 2] = v2.y * v1.z;
            m[2, 0] = v2.z * v1.x;
            m[2, 1] = v2.z * v1.y;
            m[2, 2] = v2.z * v1.z;
            return m;
        }
        public static Vector2 NewVector2(Vector2 v)
        {
            return new Vector2(v.x, v.y);
        }
        public static Vector3 NewVector3(Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
        public static Vector3 NewVector3(Vector2 v)
        {
            return new Vector3(v.x, v.y, 0);
        }
        public static Vector3 RotationToNormal(double thetaX, double thetaY, double thetaZ)
        {
            //Matrix4d RotationX = Matrix4d.RotationMatrix(new Vector3(1, 0, 0), (float)(thetaX));
            //Matrix4d RotationY = Matrix4d.RotationMatrix(new Vector3(0, 1, 0), (float)(thetaY));
            //Matrix4d RotationZ = Matrix4d.RotationMatrix(new Vector3(0, 0, 1), (float)(thetaZ));
            //Vector4 normal_init = new Vector3(0, 1, 0);
            //Vector3 normal_cur = RotationZ * RotationY * RotationX * normal_init;
            //return normal_cur.normalized;

            return new Vector3(Mathf.Cos((float)thetaX), Mathf.Cos((float)thetaY), Mathf.Cos((float)thetaZ));


        }
        public static void NormalToRotation(Vector3 normal, out double thetaX, out double thetaY, out double thetaZ)
        {
            thetaX = Vector3.Angle(normal, new Vector3(1, 0, 0));
            thetaX = thetaX * Mathf.PI / 180.0;
            thetaY = Vector3.Angle(normal, new Vector3(0, 1, 0));
            thetaY = thetaY * Mathf.PI / 180.0;
            thetaZ = Vector3.Angle(normal, new Vector3(0, 0, 1));
            thetaZ = thetaZ * Mathf.PI / 180.0;
        }
        public static Vector3 Average(List<Vector3> vs)
        {
            Vector3 a = new Vector3(0, 0, 0);
            foreach (var p in vs)
            {
                a += p;
            }
            a /= vs.Count;
            return a;
        }

        //vector4
        public static bool IsNull(Vector4 v)
        {
            return v.x == 0 && v.y == 0 && v.z == 0 && v.w == 0;
        }
        public static Matrix4d OuterCross(Vector4 v1, Vector4 v2)
        {
            Matrix4d m = new Matrix4d();
            m[0, 0] = v1.x * v2.x;
            m[0, 1] = v1.x * v2.y;
            m[0, 2] = v1.x * v2.z;
            m[0, 3] = v1.x * v2.w;
            m[1, 0] = v1.y * v2.x;
            m[1, 1] = v1.y * v2.y;
            m[1, 2] = v1.y * v2.z;
            m[1, 3] = v1.y * v2.w;
            m[2, 0] = v1.z * v2.x;
            m[2, 1] = v1.z * v2.y;
            m[2, 2] = v1.z * v2.z;
            m[2, 3] = v1.z * v2.w;
            m[3, 0] = v1.w * v2.x;
            m[3, 1] = v1.w * v2.y;
            m[3, 2] = v1.w * v2.z;
            m[3, 3] = v1.w * v2.w;
            return m;
        }

        //line3
        public static bool IsNull(Ray ray)
        {
            if (ray.origin == new Ray().origin && ray.direction == new Ray().direction)
                return true;
            return false;
        }
        public static float DistanceToRay(Ray ray, Vector3 pos)
        {
            float dis = (pos - ray.origin).sqrMagnitude - Mathf.Pow(Vector3.Dot(ray.direction, pos - ray.origin), 2.0f) / ray.direction.sqrMagnitude;
            dis = Mathf.Sqrt(dis);
            return dis;
        }
        public static Vector3 ProjectToRay(Ray ray, Vector3 p)
        {
            float t = Vector3.Dot(p - ray.origin, ray.direction) / ray.direction.sqrMagnitude;
            return ray.origin + ray.direction * t;
        }

        public static float ComputeT(Ray ray, Vector3 p)
        {
            float t = Vector3.Dot(p - ray.origin, ray.direction) / ray.direction.sqrMagnitude;
            return t;
        }

        //line2
        public static bool IsNull(Ray2D ray)
        {
            if (ray.origin == new Ray2D().origin && ray.direction == new Ray2D().direction)
                return true;
            return false;
        }
        public static float DistanceToRay(Ray2D ray, Vector2 pos)
        {
            float dis = (pos - ray.origin).sqrMagnitude - Mathf.Pow(Vector2.Dot(ray.direction, pos - ray.origin), 2.0f) / ray.direction.sqrMagnitude;
            dis = Mathf.Sqrt(dis);
            return dis;
        }
        public static Vector3 ProjectToRay(Ray2D ray, Vector2 p)
        {
            float t = Vector2.Dot(p - ray.origin, ray.direction) / ray.direction.sqrMagnitude;
            return ray.origin + ray.direction * t;
        }

        public static float ComputeT(Ray2D ray, Vector2 p)
        {
            float t = Vector2.Dot(p - ray.origin, ray.direction) / ray.direction.sqrMagnitude;
            return t;
        }

        //Plane
        //public static Vector3 PlaneCenter(Plane plane)
        //{
        //    return plane.distance * plane.normal.normalized;
        //}
        //public static float PlaneOffset(Plane plane)
        //{
        //    return -Vector3.Dot(Utility.PlaneCenter(plane), plane.normal);
        //}
        public static Vector3 PlaneProjectPoint(Plane plane, Vector3 p)
        {
            Ray ray = new Ray(p, plane.normal);
            return PlaneRayIntersect(plane, ray);
            //float a = plane.normal.x;
            //float b = plane.normal.y;
            //float c = plane.normal.z;
            //float offset = Utility.PlaneOffset(plane);
            //float t = -(p.x * a + b * p.y + c * p.z + offset) / plane.normal.sqrMagnitude;
            //return new Vector3(a * t + p.x, b * t + p.y, c * t + p.z);
        }
        public static bool PlaneIsVertical(Plane p1, Plane p2, float angle = 85)
        {
            Vector3 _v1 = p1.normal;
            Vector3 _v2 = p2.normal;
            if (Mathf.Abs(Vector3.Dot(_v1, _v2) / (_v1.magnitude * _v2.magnitude)) <= Mathf.Cos(angle * Mathf.PI / 180))
                return true;
            else
                return false;
        }
        public static bool PlaneIsParallel(Plane p1, Plane p2, float angle = 5)
        {
            Vector3 _v1 = p1.normal;
            Vector3 _v2 = p2.normal;
            if (Mathf.Abs(Vector3.Dot(_v1, _v2) / (_v1.magnitude * _v2.magnitude)) >= Mathf.Cos(angle * Mathf.PI / 180))
                return true;
            else
                return false;
        }
        public static Vector3 PlaneRayIntersect(Plane plane, Ray ray)
        {
            float dis = 0;
            plane.Raycast(ray, out dis);
            return ray.GetPoint(dis);
        }
        //public static List<Vector3> PlaneSampleBoundary(Plane plane)
        //{
        //    List<Vector3> boundary = new List<Vector3>();
        //    Vector3 center = Utility.PlaneProjectPoint(plane, new Vector3(0, 0, 0));
        //    //sample a circle around the plane center, four vertices, radius = 0.5;
        //    Vector3 pointonplane = Utility.PlaneProjectPoint(plane, center + new Vector3(-0.1f, 0f, 0f));
        //    Vector3 u = pointonplane - center;
        //    Vector3 v = Vector3.Cross(u, plane.normal);
        //    u.Normalize();
        //    v.Normalize();
        //    int num = 4;
        //    float radius = 0.5f;
        //    float theta = 2.0f * Mathf.PI / (num - 1);

        //    for (int j = 0; j < num; j++)
        //    {
        //        float x = center.x + radius * (u.x * Mathf.Cos(j * theta) + v.x * Mathf.Sin(j * theta));
        //        float y = center.y + radius * (u.y * Mathf.Cos(j * theta) + v.y * Mathf.Sin(j * theta));
        //        float z = center.z + radius * (u.z * Mathf.Cos(j * theta) + v.z * Mathf.Sin(j * theta));
        //        boundary.Add(new Vector3(x, y, z));
        //    }
        //    return boundary;
        //}

        //mesh

        public static Vector3 PlaneSymmetryPoint(Plane p, Vector3 v)
        {
            Vector3 v_sym = v - 2f * p.GetDistanceToPoint(v) * p.normal.normalized;
            return v_sym;
        }

        // IndexList: x: num of circle  y: num of circleSamples z: num of vertex index
        public static Mesh MeshCreateFromCircleList(List<Circle3> CircleList, List<Vector3> endface, out List<Vector3> IndexList)
        {
            Mesh mesh = new Mesh();
            IndexList = new List<Vector3>();

            int n = CircleList[0].CirclePoints.Count;   // base stroke point count
            int m = CircleList.Count;   // ref stroke point count

            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < CircleList.Count; i++)
            {
                for (int j = 0; j < CircleList[i].CirclePoints.Count; j++)
                {
                    vertices.Add(CircleList[i].CirclePoints[j]);
                    IndexList.Add(new Vector3(i, j, vertices.Count - 1));
                }
            }

            List<int> findices = new List<int>();
            for (int i = 0; i < m - 1; ++i)
            {
                for (int j = 0; j < n - 1; ++j)
                {
                    int s = i * n + j, t = i * n + j + 1;
                    int p = (i + 1) * n + j, q = (i + 1) * n + j + 1;

                    findices.Add(s); findices.Add(p); findices.Add(t);
                    findices.Add(t); findices.Add(p); findices.Add(q);
                }
            }

            //close top face and bottom face
            //first circle center
            Vector3 topcenter = CircleList[0].Center;
            vertices.Add(topcenter);
            IndexList.Add(new Vector3(0, CircleList[0].CirclePoints.Count, vertices.Count - 1));

            int topcenterindex = vertices.Count - 1;
            for (int i = 0; i < n; i++)
            {
                findices.Add(i % n); findices.Add((i + 1) % n); findices.Add(topcenterindex);
            }

            Vector3 bottomcenter = CircleList[m - 1].Center;
            vertices.Add(bottomcenter);
            IndexList.Add(new Vector3(CircleList.Count - 1, CircleList[0].CirclePoints.Count, vertices.Count - 1));
            int bottomcenterindex = vertices.Count - 1;
            for (int j = m * n - 1; j >= m * n - n; j--)
            {
                if (j == m * n - 1)
                { findices.Add(j); findices.Add(m * n - n); findices.Add(bottomcenterindex); }
                else
                { findices.Add(j); findices.Add(bottomcenterindex); findices.Add(j + 1); }
            }
            // Build End face
            if (endface != null && endface.Count == n)
            {
                int curren_num = vertices.Count;
                foreach (var p in endface) vertices.Add(p);
                int index = 0;
                int face_num = m - 1;
                for (int j = face_num * n - n; j <= face_num * n - 1; j++)
                {
                    if (j == face_num * n - 1)
                    {
                        findices.Add(j); findices.Add(curren_num + index); findices.Add(face_num * n - n);
                        findices.Add(face_num * n - n); findices.Add(curren_num + index); findices.Add(curren_num + 0);
                    }
                    else
                    {
                        findices.Add(j); findices.Add(curren_num + index); findices.Add(j + 1);
                        findices.Add(j + 1); findices.Add(curren_num + index); findices.Add(curren_num + index + 1);
                    }
                    index++;
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = findices.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
        public static Mesh MeshCreateFromRectList(List<Quad> RectList, out List<Vector3> IndexList)
        {
            Mesh mesh = new Mesh();
            IndexList = new List<Vector3>();

            int n = RectList[0].CornerPoints3d.Count + 1;   // base stroke point count
            int m = RectList.Count;   // ref stroke point count

            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < RectList.Count; i++)
            {
                for (int j = 0; j < RectList[i].CornerPoints3d.Count; j++)
                {
                    vertices.Add(RectList[i].CornerPoints3d[j]);
                    IndexList.Add(new Vector3(i, j, vertices.Count - 1));
                }
                vertices.Add(RectList[i].CornerPoints3d[0]);
                IndexList.Add(new Vector3(i, 0, vertices.Count - 1));
            }
            mesh.vertices = vertices.ToArray();


            List<int> findices = new List<int>();
            for (int i = 0; i < m - 1; ++i)
            {
                for (int j = 0; j < n - 1; ++j)
                {
                    int s = i * n + j, t = i * n + j + 1;
                    int p = (i + 1) * n + j, q = (i + 1) * n + j + 1;

                    findices.Add(s); findices.Add(t); findices.Add(p);
                    findices.Add(t); findices.Add(q); findices.Add(p);
                }
            }

            // Top Face
            findices.Add(0); findices.Add(2); findices.Add(1);
            findices.Add(0); findices.Add(3); findices.Add(2);

            // Bottom Face
            int i_bottom = 5 * (m - 1);
            findices.Add(i_bottom); findices.Add(i_bottom + 1); findices.Add(i_bottom + 2);
            findices.Add(i_bottom); findices.Add(i_bottom + 2); findices.Add(i_bottom + 3);

            mesh.triangles = findices.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        // find if a point in inside a polygon
        public static bool PointInPoly(Vector2 p, Vector2[] points)
        {
            bool c = false;
            int n = points.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((points[i].y > p.y) != (points[j].y > p.y)) &&
                    (p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x))
                    c = !c;
            }
            return c;
        }
        public static bool PointInPoly(Vector2 p, List<Vector2> points)
        {
            bool c = false;
            int n = points.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((points[i].y > p.y) != (points[j].y > p.y)) &&
                    (p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x))
                    c = !c;
            }
            return c;
        }
        public static bool PointInTrapezoid(Vector2 p, List<Vector2> points)
        {
            float minx = float.MaxValue, maxx = float.MinValue, miny = float.MaxValue, maxy = float.MinValue;
            foreach (Vector2 v in points)
            {
                if (v.x < minx)
                    minx = v.x;
                if (v.x > maxx)
                    maxx = v.x;
                if (v.y < miny)
                    miny = v.y;
                if (v.y > maxy)
                    maxy = v.y;
            }
            if (p.x > minx && p.x < maxx && p.y > miny && p.y < maxy)
                return true;
            else
                return false;
        }


        public static Vector3 GetNormal(List<Vector3> points)
        {
            int pointcount = points.Count;

            Vector3 mean = new Vector3(0, 0, 0);
            float Exx = 0;
            float Exy = 0;
            float Exz = 0;
            float Eyy = 0;
            float Eyz = 0;
            float Ezz = 0;
            foreach (Vector3 p in points)
            {
                mean = p + mean;
                Exx += p.x * p.x;
                Exy += p.x * p.y;
                Exz += p.x * p.z;
                Eyy += p.y * p.y;
                Eyz += p.y * p.z;
                Ezz += p.z * p.z;
            }
            mean = mean / pointcount;
            Exx /= pointcount;
            Exy /= pointcount;
            Exz /= pointcount;
            Eyy /= pointcount;
            Eyz /= pointcount;
            Ezz /= pointcount;

            float[,] cov = new float[3, 3];
            cov[0, 0] = Exx - mean.x * mean.x;
            cov[0, 1] = Exy - mean.x * mean.y;
            cov[0, 2] = Exz - mean.x * mean.z;
            cov[1, 0] = Exy - mean.x * mean.y;
            cov[1, 1] = Eyy - mean.y * mean.y;
            cov[1, 2] = Eyz - mean.y * mean.z;
            cov[2, 0] = Exz - mean.x * mean.z;
            cov[2, 1] = Eyz - mean.y * mean.z;
            cov[2, 2] = Ezz - mean.z * mean.z;

            Matrix3d cov_m = new Matrix3d(cov);
            return cov_m.SmallestEigenVector();
        }

        public static Vector2 GetDirection(List<Vector2> points)
        {
            int pointcount = points.Count;
            Vector2 mean = new Vector2(0, 0);
            float Exx = 0;
            float Exy = 0;
            float Eyy = 0;
            foreach (Vector2 p in points)
            {
                mean = p + mean;
                Exx += p.x * p.x;
                Exy += p.x * p.y;
                Eyy += p.y * p.y;
            }
            mean = mean / pointcount;
            Exx /= pointcount;
            Exy /= pointcount;
            Eyy /= pointcount;

            float[,] cov = new float[2, 2];
            cov[0, 0] = Exx - mean.x * mean.x;
            cov[0, 1] = Exy - mean.x * mean.y;
            cov[1, 0] = Exy - mean.x * mean.y;
            cov[1, 1] = Eyy - mean.y * mean.y;

            Matrix2d cov_m = new Matrix2d(cov);
            return cov_m.LargestEigenVector();
        }

        public static void ReplaceList(List<Vector2> list, int index, List<Vector2> list_part)
        {
            if (list.Count < index + list_part.Count)
            {
                Debug.Log("Error! List is not that long!");
            }
            for (int i = 0; i < list_part.Count; i++)
            {
                list[i + index] = list_part[i];
            }
        }

        public static double HausdorffDistance(List<Vector2> set1, List<Vector2> set2)
        {
            double maxdis12 = -1;
            foreach (Vector2 s1 in set1)
            {
                double mindis = double.MaxValue;
                foreach (Vector2 s2 in set2)
                {
                    double dis = Vector2.Distance(s1, s2);
                    if (dis < mindis)
                        mindis = dis;
                }
                if (mindis > maxdis12)
                    maxdis12 = mindis;
            }

            double maxdis21 = -1;
            foreach (Vector2 s2 in set2)
            {
                double mindis = double.MaxValue;
                foreach (Vector2 s1 in set1)
                {
                    double dis = Vector2.Distance(s1, s2);
                    if (dis < mindis)
                        mindis = dis;
                }
                if (mindis > maxdis21)
                    maxdis21 = mindis;
            }
            return Math.Max(maxdis12, maxdis21);
        }

        public static double DistanceSet2Set(List<Vector2> set1, List<Vector2> set2)
        {
            double mindis = double.MaxValue;
            for (int i = 0; i < set1.Count; i++)
            {
                for (int j = 0; j < set2.Count; j++)
                {
                    double dis = Vector2.Distance(set1[i], set2[j]);
                    if (dis <= 3)
                        return dis;
                    if (dis <= mindis)
                    {
                        mindis = dis;
                    }
                }
            }
            return mindis;
        }

        public static double DistanceWithinSet(List<Vector2> set1)
        {
            double mindis = double.MaxValue;
            for (int i = 0; i < set1.Count; i++)
            {
                for (int j = 0; j < set1.Count; j++)
                {
                    if (i != j)
                    {
                        double dis = Vector2.Distance(set1[i], set1[j]);
                        if (dis <= mindis)
                        {
                            mindis = dis;
                        }
                    }
                }
            }
            return mindis;
        }

        public static double DistancePoint2Set(Vector2 point, List<Vector2> set1)
        {
            double mindis = double.MaxValue;
            for (int i = 0; i < set1.Count; i++)
            {
                double dis = Vector2.Distance(point, set1[i]);
                if (dis < mindis)
                    mindis = dis;
            }
            return mindis;
        }

        public static Vector2 MinV(List<Vector2> points)
        {
            List<Vector2> temp = new List<Vector2>(points);
            temp.Sort((p, q) => p.x.CompareTo(q.x));

            List<Vector2> temp2 = new List<Vector2>(points);
            temp2.Sort((p, q) => p.y.CompareTo(q.y));

            return new Vector2(temp[0].x, temp2[0].y);
        }

        public static Vector2 MaxV(List<Vector2> points)
        {
            List<Vector2> temp = new List<Vector2>(points);
            temp.Sort((p, q) => q.x.CompareTo(p.x));

            List<Vector2> temp2 = new List<Vector2>(points);
            temp2.Sort((p, q) => q.y.CompareTo(p.y));

            return new Vector2(temp[0].x, temp2[0].y);
        }


    }

}
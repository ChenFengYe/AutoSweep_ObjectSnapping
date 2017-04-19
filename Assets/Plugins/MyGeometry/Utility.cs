using UnityEngine;
using System.Collections.Generic;
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
        public static Vector3 NewVector3(Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
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
        public static Mesh MeshCreateFromCircleList(List<Circle3> CircleList)
        {
            Mesh mesh = new Mesh();

            int n = CircleList[0].CirclePoints.Count;   // base stroke point count
            int m = CircleList.Count;   // ref stroke point count

            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < CircleList.Count; i++)
            {
                foreach (var point in CircleList[i].CirclePoints)
                {
                    vertices.Add(point);
                }
            }
            mesh.vertices = vertices.ToArray();


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
            mesh.triangles = findices.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;

        }

        //draw 
        //draw point
        //draw line
        //draw circle
        //draw plane


    }

}
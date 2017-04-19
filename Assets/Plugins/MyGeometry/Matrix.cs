using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using NumericalRecipes;
using UnityEngine;
namespace MyGeometry
{

    public struct Matrix2d
    {
        private const int len = 4;
        private const int row_size = 2;
        float a, b, c, d;

        public Matrix2d(float[] arr)
        {
            a = arr[0];
            b = arr[1];
            c = arr[2];
            d = arr[3];
        }
        public Matrix2d(float[,] arr)
        {
            a = arr[0, 0];
            b = arr[0, 1];
            c = arr[1, 0];
            d = arr[1, 1];
        }

        // using column vectors
        public Matrix2d(Vector2 v1, Vector2 v2)
        {
            a = v1.x;
            b = v2.x;
            c = v1.y;
            d = v2.y;
        }

        public Matrix2d(float a, float b, float c, float d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public static Matrix2d operator +(Matrix2d m1, Matrix2d m2)
        {
            Matrix2d ret = new Matrix2d(
                m1.a + m2.a,
                m1.b + m2.b,
                m1.c + m2.c,
                m1.d + m2.d
                );
            return ret;
        }
        public static Matrix2d operator *(Matrix2d m1, Matrix2d m2)
        {
            Matrix2d ret = new Matrix2d(
                m1.a * m2.a + m1.b * m2.c,
                m1.a * m2.b + m1.b * m2.d,
                m1.c * m2.a + m1.d * m2.c,
                m1.c * m2.b + m1.d * m2.d
                );
            return ret;
        }
        public static Vector2 operator *(Matrix2d m, Vector2 v)
        {
            return new Vector2(m.A * v.x + m.B * v.y, m.C * v.x + m.D * v.y);
        }
        public Matrix2d Inverse()
        {
            float det = (a * d - b * c);
            if (float.IsNaN(det)) throw new ArithmeticException();
            return new Matrix2d(d / det, -b / det, -c / det, a / det);
        }
        public float Det()
        {
            return (a * d - b * c);
        }
        public Matrix2d Transpose()
        {
            return new Matrix2d(a, c, b, d);
        }
        public float Trace()
        {
            return a + d;
        }

        public float A
        {
            get { return a; }
            set { a = value; }
        }
        public float B
        {
            get { return b; }
            set { b = value; }
        }
        public float C
        {
            get { return c; }
            set { c = value; }
        }
        public float D
        {
            get { return d; }
            set { d = value; }
        }
        public override string ToString()
        {
            return
                a.ToString("F5") + " " + b.ToString("F5") + " " +
                c.ToString("F5") + " " + d.ToString("F5");
        }
        public float[] ToArray()
        {
            float[] array = new float[4];
            array[0] = this.a; array[1] = this.b;
            array[2] = this.c; array[3] = this.d;
            return array;
        }
    }

    public class Matrix3d
    {
        public static bool lastSVDIsFullRank = false;
        private const int len = 9;
        private const int row_size = 3;
        private float[] e = new float[len];

        public Matrix3d() { }
        public Matrix3d(float[] arr)
        {
            for (int i = 0; i < len; i++) e[i] = arr[i];
        }
        public Matrix3d(float[,] arr)
        {
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                    this[i, j] = arr[i, j];
        }
        public Matrix3d(Matrix3d m) : this(m.e) { }
        public Matrix3d(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            for (int i = 0; i < row_size; i++)
            {
                this[i, 0] = v1[i];
                this[i, 1] = v2[i];
                this[i, 2] = v3[i];
            }
        }

        public void Clear()
        {
            for (int i = 0; i < len; i++) e[i] = 0;
        }
        public float this[int index]
        {
            get { return e[index]; }
            set { e[index] = value; }
        }
        public float this[int row, int column]
        {
            get { return e[row * row_size + column]; }
            set { e[row * row_size + column] = value; }
        }
        public float[] ToArray()
        {
            return (float[])e.Clone();
        }
        public float Det()
        {
            return e[0] * (e[4] * e[8] - e[5] * e[7])
                  - e[3] * (e[1] * e[8] - e[2] * e[7])
                  + e[6] * (e[1] * e[5] - e[2] * e[4]);
        }
        public float Trace()
        {
            return e[0] + e[4] + e[8];
        }
        public float SqNorm()
        {
            float sq = 0;
            for (int i = 0; i < len; i++) sq += e[i] * e[i];
            return sq;
        }
        public Matrix3d Transpose()
        {
            Matrix3d m = new Matrix3d();
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                    m[j, i] = this[i, j];
            return m;
        }
        public Matrix3d Inverse()
        {
            float a = e[0];
            float b = e[1];
            float c = e[2];
            float d = e[3];
            float E = e[4];
            float f = e[5];
            float g = e[6];
            float h = e[7];
            float i = e[8];
            float det = a * (E * i - f * h) - b * (d * i - f * g) + c * (d * h - E * g);
            if (det == 0) throw new ArithmeticException();

            Matrix3d inv = new Matrix3d();
            inv[0] = (E * i - f * h) / det;
            inv[1] = (c * h - b * i) / det;
            inv[2] = (b * f - c * E) / det;
            inv[3] = (f * g - d * i) / det;
            inv[4] = (a * i - c * g) / det;
            inv[5] = (c * d - a * f) / det;
            inv[6] = (d * h - E * g) / det;
            inv[7] = (b * g - a * h) / det;
            inv[8] = (a * E - b * d) / det;
            return inv;
        }
        public Matrix3d InverseSVD()
        {
            SVD svd = new SVD(e, 3, 3);
            Matrix3d inv = new Matrix3d(svd.Inverse);
            lastSVDIsFullRank = svd.FullRank;
            return inv;
        }
        public Matrix3d InverseTranspose()
        {
            float a = e[0];
            float b = e[1];
            float c = e[2];
            float d = e[3];
            float E = e[4];
            float f = e[5];
            float g = e[6];
            float h = e[7];
            float i = e[8];
            float det = a * (E * i - f * h) - b * (d * i - f * g) + c * (d * h - E * g);
            if (det == 0) throw new ArithmeticException();

            Matrix3d inv = new Matrix3d();
            inv[0] = (E * i - f * h) / det;
            inv[3] = (c * h - b * i) / det;
            inv[6] = (b * f - c * E) / det;
            inv[1] = (f * g - d * i) / det;
            inv[4] = (a * i - c * g) / det;
            inv[7] = (c * d - a * f) / det;
            inv[2] = (d * h - E * g) / det;
            inv[5] = (b * g - a * h) / det;
            inv[8] = (a * E - b * d) / det;
            return inv;
        }
        public Matrix3d OrthogonalFactor(float eps)
        {
            Matrix3d Q = new Matrix3d(this);
            Matrix3d Q2 = new Matrix3d();
            float err = 0;
            do
            {
                Q2 = (Q + Q.InverseTranspose()) / 2.0f;
                err = (Q2 - Q).SqNorm();
                Q = Q2;
            } while (err > eps);

            return Q2;
        }
        public Matrix3d OrthogonalFactorSVD()
        {
            SVD svd = new SVD(e, 3, 3);
            lastSVDIsFullRank = svd.FullRank;
            return new Matrix3d(svd.OrthogonalFactor);
        }
        public Vector3 NullVector()
        {
            SVD svd = new SVD(e, 3, 3);
            for (int i = 1; i < 4; ++i)
            {
                if (Mathf.Abs(svd.w[i]) < 1e-7) // ==0
                {
                    return new Vector3(svd.u[1, i], svd.u[2, i], svd.u[3, i]);
                }
            }
            return new Vector3(0, 0, 0);
        }
        public Vector3 SmallestEigenVector()
        {
            SVD svd = new SVD(e, 3, 3);
            float min = float.MaxValue; int j = -1;
            for (int i = 1; i < 4; ++i)
            {
                if (svd.w[i] < min) // ==0
                {
                    min = svd.w[i];
                    j = i;
                }
            }
            return new Vector3(svd.u[1, j], svd.u[2, j], svd.u[3, j]);
        }
        public float[] SVDSingularMat()
        {
            SVD svd = new SVD(e, 3, 3);
            return svd.w;
        }
        public Matrix3d OrthogonalFactorIter()
        {
            return (this + this.InverseTranspose()) / 2;
        }
        public static Matrix3d IdentityMatrix()
        {
            Matrix3d m = new Matrix3d();
            m[0] = m[4] = m[8] = 1.0f;
            return m;
        }
        public Matrix3d LogMatrix(out Vector3 axis, out float angle)
        {
            Matrix3d m;

            float cos = (this.Trace() - 1) / 2.0f;
            if (cos < -1)
            {
                cos = -1;
            }
            else if (cos > 1)
            {
                cos = 1;
            }
            float theta = Mathf.Acos(cos);
            if (Mathf.Abs(theta) < 0.0001)
            {
                if (theta >= 0.0)
                    theta = 0.0001f;
                else
                    theta = -0.0001f;
            }

            m = (new Matrix3d(this) - this.Transpose()) * (0.5f / Mathf.Sin(theta) * theta);

            Vector3 r = new Vector3(m[2, 1], m[0, 2], m[1, 0]);

            angle = r.magnitude;

            axis = r / angle;

            return m;
        }
        public static Vector3 operator *(Matrix3d m, Vector3 v)
        {
            Vector3 ret = new Vector3();
            ret.x = m[0] * v.x + m[1] * v.y + m[2] * v.z;
            ret.y = m[3] * v.x + m[4] * v.y + m[5] * v.z;
            ret.z = m[6] * v.x + m[7] * v.y + m[8] * v.z;
            return ret;
        }
        public static Matrix3d operator *(Matrix3d m1, Matrix3d m2)
        {
            Matrix3d ret = new Matrix3d();
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                {
                    ret[i, j] = 0.0f;
                    for (int k = 0; k < row_size; k++)
                        ret[i, j] += m1[i, k] * m2[k, j];
                }
            return ret;
        }
        public static Matrix3d operator +(Matrix3d m1, Matrix3d m2)
        {
            Matrix3d ret = new Matrix3d();
            for (int i = 0; i < len; i++) ret[i] = m1[i] + m2[i];
            return ret;
        }
        public static Matrix3d operator -(Matrix3d m1, Matrix3d m2)
        {
            Matrix3d ret = new Matrix3d();
            for (int i = 0; i < len; i++) ret[i] = m1[i] - m2[i];
            return ret;
        }
        public static Matrix3d operator *(Matrix3d m, float d)
        {
            Matrix3d ret = new Matrix3d();
            for (int i = 0; i < len; i++) ret[i] = m[i] * d;
            return ret;
        }
        public static Matrix3d operator /(Matrix3d m, float d)
        {
            Matrix3d ret = new Matrix3d();
            for (int i = 0; i < len; i++) ret[i] = m[i] / d;
            return ret;
        }
        public override string ToString()
        {
            return
                e[0].ToString("F5") + " " + e[1].ToString("F5") + " " + e[2].ToString("F5") +
                e[3].ToString("F5") + " " + e[4].ToString("F5") + " " + e[5].ToString("F5") +
                e[6].ToString("F5") + " " + e[7].ToString("F5") + " " + e[08].ToString("F5");
        }
    }

    public class Matrix4d
    {
        private const int len = 16;
        private const int row_size = 4;
        private float[] e = new float[len];

        public Matrix4d()
        {
        }
        public Matrix4d(float[] arr)
        {
            for (int i = 0; i < len; i++) e[i] = arr[i];
        }
        public Matrix4d(float[,] arr)
        {
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                    this[i, j] = arr[i, j];
        }
        public Matrix4d(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            for (int i = 0; i < 3; i++)
            {
                this[i, 0] = v1[i];
                this[i, 1] = v2[i];
                this[i, 2] = v3[i];
                this[i, 3] = v4[i];
            }
        }
        public Matrix4d(Matrix4d m) : this(m.e) { }
        public Matrix4d(Matrix3d m)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    this[i, j] = m[i, j];
        }
        public Matrix4d(StreamReader sr)
        {
            int c = 0;
            char[] delimiters = { ' ', '\t' };

            while (sr.Peek() > -1)
            {
                string s = sr.ReadLine();
                string[] tokens = s.Split(delimiters);
                for (int i = 0; i < tokens.Length; i++)
                {
                    e[c++] = float.Parse(tokens[i]);
                    if (c >= len) return;
                }
            }
        }


        public float this[int index]
        {
            get { return e[index]; }
            set { e[index] = value; }
        }
        public float this[int row, int column]
        {
            get { return e[row * row_size + column]; }
            set { e[row * row_size + column] = value; }
        }
        public float[] Element
        {
            get { return e; }
            set
            {
                if (value.Length < len)
                    throw new Exception();
                e = value;
            }
        }
        public float[] ToArray()
        {
            return (float[])e.Clone();
        }
        public Matrix3d ToMyMatrix3d()
        {
            Matrix3d ret = new Matrix3d();
            ret[0] = e[0];
            ret[1] = e[1];
            ret[2] = e[2];
            ret[3] = e[4];
            ret[4] = e[5];
            ret[5] = e[6];
            ret[6] = e[8];
            ret[7] = e[9];
            ret[8] = e[10];
            return ret;
        }
        public float Trace()
        {
            return e[0] + e[5] + e[10] + e[15];
        }
        public Matrix4d Inverse()
        {
            SVD svd = new SVD(e, row_size, row_size);
            if (svd.State == false) throw new ArithmeticException();
            return new Matrix4d(svd.Inverse);
        }
        public Matrix4d Inverse_alglib()
        {
            double[,] data = new double[row_size, row_size];
            for (int i = 0; i < row_size; ++i)
            {
                for (int j = 0; j < row_size; ++j)
                    data[i, j] = this[i, j];
            }
            int info;
            alglib.matinvreport rep;
            alglib.rmatrixinverse(ref data, out info, out rep);
            float[,] data_f = new float[row_size, row_size];
            for (int i = 0; i < row_size; ++i)
            {
                for (int j = 0; j < row_size; ++j)
                    data_f[i, j] = Convert.ToSingle(data[i, j]);
            }

            if (info == 1)
                return new Matrix4d(data_f);
            else
                throw new ArithmeticException();

        }
        public Matrix4d Transpose()
        {
            Matrix4d m = new Matrix4d();
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                    m[j, i] = this[i, j];
            return m;
        }
        public static Matrix4d IdentityMatrix()
        {
            Matrix4d m = new Matrix4d();
            m[0] = m[5] = m[10] = m[15] = 1.0f;
            return m;
        }
        public static Matrix4d CompressMatrix(float alpha)
        {
            Matrix4d m = IdentityMatrix();

            m[3, 2] = alpha;
            return m;
        }
        public static Matrix4d RotationMatrix(Vector3 axis, float angle)
        {
            Matrix4d m = IdentityMatrix();

            float c = Mathf.Cos(angle);
            float s = Mathf.Sin(angle);

            axis.Normalize();

            float nx = axis[0];
            float ny = axis[1];
            float nz = axis[2];

            m[0, 0] = c + (1 - c) * nx * nx;
            m[0, 1] = -s * nz + (1 - c) * nx * ny;
            m[0, 2] = s * ny + (1 - c) * nx * nz;
            m[0, 3] = 0.0f;

            m[1, 0] = s * nz + (1 - c) * nx * ny;
            m[1, 1] = c + (1 - c) * ny * ny;
            m[1, 2] = -s * nx + (1 - c) * ny * nz;
            m[1, 3] = 0.0f;

            m[2, 0] = -s * ny + (1 - c) * nz * nx;
            m[2, 1] = s * nx + (1 - c) * nz * ny;
            m[2, 2] = c + (1 - c) * nz * nz;
            m[2, 3] = 0.0f;

            m[3, 0] = 0.0f;
            m[3, 1] = 0.0f;
            m[3, 2] = 0.0f;
            m[3, 3] = 1.0f;

            return m;
        }
        public static Matrix4d RotationMatrix(Vector3 axis, float cos, float sin)
        {
            Matrix4d m = IdentityMatrix();

            float c = cos;
            float s = sin;

            axis.Normalize();

            float nx = axis[0];
            float ny = axis[1];
            float nz = axis[2];

            m[0, 0] = c + (1 - c) * nx * nx;
            m[0, 1] = -s * nz + (1 - c) * nx * ny;
            m[0, 2] = s * ny + (1 - c) * nx * nz;
            m[0, 3] = 0.0f;

            m[1, 0] = s * nz + (1 - c) * nx * ny;
            m[1, 1] = c + (1 - c) * ny * ny;
            m[1, 2] = -s * nx + (1 - c) * ny * nz;
            m[1, 3] = 0.0f;

            m[2, 0] = -s * ny + (1 - c) * nz * nx;
            m[2, 1] = s * nx + (1 - c) * nz * ny;
            m[2, 2] = c + (1 - c) * nz * nz;
            m[2, 3] = 0.0f;

            m[3, 0] = 0.0f;
            m[3, 1] = 0.0f;
            m[3, 2] = 0.0f;
            m[3, 3] = 1.0f;

            return m;
        }
        public static Matrix4d RotationMatrixU2V(Vector3 u, Vector3 v)
        {
            // find the rotational matrix which rotate u to v
            // one should be extremely careful here, very small viboration
            // will make a lot of difference
            // e.g., u = (0.5*e-10, 0.1*e-10, 1), v = (0,0,-1), will make
            // an fliping around axie (1, 5, 0) with angele Pi
            Matrix4d R = Matrix4d.IdentityMatrix();
            float cos = Vector3.Dot(u.normalized, v.normalized);
            if (Mathf.Abs(Mathf.Abs(cos) - 1.0f) < 1e-5) // coincident, do nothing
                return R;
            Vector3 axis = Vector3.Normalize(Vector3.Cross(u, v));

            if (!float.IsNaN(axis.x))
            {
                if (cos < -1) cos = -1;
                if (cos > 1) cos = 1;
                float angle = Mathf.Acos(cos);
                R = Matrix4d.RotationMatrix(axis, angle);
            }
            return R;
        }
        public static void FindRotAxisAngle(Matrix4d rotMat, out Vector3 rotAxis, out float angle)
        {
            float trace = rotMat[0, 0] + rotMat[1, 1] + rotMat[2, 2];
            angle = Mathf.Acos((trace - 1) / 2);

            if (rotMat[0, 1] > 0) angle = -angle; /// this may be violated...

            float sin2 = 2 * Mathf.Sin(angle);
            float x = (rotMat[2, 1] - rotMat[1, 2]) / sin2;
            float y = (rotMat[0, 2] - rotMat[2, 0]) / sin2;
            float z = (rotMat[1, 0] - rotMat[0, 1]) / sin2;

            rotAxis = new Vector3(x, y, z).normalized;
        }
        public static Matrix4d TranslationMatrix(Vector3 t)
        {
            Matrix4d m = IdentityMatrix();

            m[0, 3] = t[0];
            m[1, 3] = t[1];
            m[2, 3] = t[2];

            return m;
        }
        public static Vector3 GetTranslation(Matrix4d T)
        {
            return new Vector3(T[0, 3], T[1, 3], T[2, 3]);
        }
        public static Matrix4d ScalingMatrix(float sx, float sy, float sz)
        {
            Matrix4d m = IdentityMatrix();

            m[0, 0] = sx;
            m[1, 1] = sy;
            m[2, 2] = sz;
            m[3, 3] = 1.0f;

            return m;
        }
        // compute the reflect point of p according to the Canvas3 (Canvas3_normal, Canvas3_center)
        public static Vector3 Reflect(Vector3 p, Vector3 Canvas3_normal, Vector3 Canvas3_center)
        {
            Vector3 u = p - Canvas3_center;

            // create a coordinate system (x,y,z), project to that sys, reflect and project back
            Vector3 x = Canvas3_normal;
            Vector3 y;
            if (x.x == 0 && x.y == 0)
                y = new Vector3(0, -x.z, x.y);
            else
                y = new Vector3(x.y, -x.x, 0);
            Vector3 z = Vector3.Cross(x, y);
            Matrix3d R = new Matrix3d(x, y, z);
            Matrix3d InvR = R.InverseSVD();
            Matrix4d U = new Matrix4d(R);
            Matrix4d V = new Matrix4d(InvR);
            Matrix4d I = Matrix4d.IdentityMatrix();
            I[0, 0] = -1; // reflect matrix along yz Canvas3
            Matrix4d T = V * I * U; // the reflection matrix

            // reflect
            Vector4 r = new Vector4(u.x, u.y, u.z, 0);
            Vector3 temp = T * r;
            Vector3 q = Canvas3_center + temp;

            return q;
        }
        public static Vector4 operator *(Vector4 v, Matrix4d m)
        {
            return m.Transpose() * v;
        }
        public static Matrix4d operator *(Matrix4d m1, Matrix4d m2)
        {
            Matrix4d ret = new Matrix4d();
            for (int i = 0; i < row_size; i++)
                for (int j = 0; j < row_size; j++)
                {
                    ret[i, j] = 0.0f;
                    for (int k = 0; k < row_size; k++)
                        ret[i, j] += m1[i, k] * m2[k, j];
                }
            return ret;
        }
        public static Vector4 operator *(Matrix4d m, Vector4 v)
        {
            Vector4 ret = new Vector4();
            ret.x = m[0] * v.x + m[1] * v.y + m[2] * v.z + m[3] * v.w;
            ret.y = m[4] * v.x + m[5] * v.y + m[6] * v.z + m[7] * v.w;
            ret.z = m[8] * v.x + m[9] * v.y + m[10] * v.z + m[11] * v.w;
            ret.w = m[12] * v.x + m[13] * v.y + m[14] * v.z + m[15] * v.w;
            return ret;
        }
        public static Matrix4d operator +(Matrix4d m1, Matrix4d m2)
        {
            Matrix4d ret = new Matrix4d();
            for (int i = 0; i < len; i++) ret[i] = m1[i] + m2[i];
            return ret;
        }
        public static Matrix4d operator -(Matrix4d m1, Matrix4d m2)
        {
            Matrix4d ret = new Matrix4d();
            for (int i = 0; i < len; i++) ret[i] = m1[i] - m2[i];
            return ret;
        }
        public static Matrix4d operator *(Matrix4d m, float d)
        {
            Matrix4d ret = new Matrix4d();
            for (int i = 0; i < len; i++) ret[i] = m[i] * d;
            return ret;
        }

        public override string ToString()
        {
            string s = "";
            foreach (float d in e)
                s += d.ToString() + "\n";
            return s;
        }
    }

    public class MatrixNd
    {
        private int m;
        private int n;
        private float[] e;

        public MatrixNd(int m, int n)
        {
            this.m = m;
            this.n = n;
            e = new float[m * n];
            for (int i = 0; i < m * n; i++)
                e[i] = 0;
        }
        public MatrixNd(SparseMatrix right)
            : this(right.RowSize, right.ColumnSize)
        {
            int b = 0;
            foreach (List<SparseMatrix.Element> row in right.Rows)
            {
                foreach (SparseMatrix.Element element in row)
                    e[b + element.j] = element.value;
                b += n;
            }
        }
        public MatrixNd(SparseMatrix right, bool transpose)
            : this(right.ColumnSize, right.RowSize)
        {
            int b = 0;
            foreach (List<SparseMatrix.Element> col in right.Columns)
            {
                foreach (SparseMatrix.Element element in col)
                    e[b + element.i] = element.value;
                b += n;
            }
        }
        public MatrixNd(Vector3[] v)
        {
            this.m = 3;
            this.n = v.Length;
            e = new float[m * n];
            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    this[i, j] = v[j][i];
                }
            }
        }

        public MatrixNd(float[,] val)
        {
            this.m = val.GetLength(0);
            this.n = val.GetLength(1);
            this.e = new float[this.m * this.n];
            for (int i = 0; i < this.m; ++i)
            {
                for (int j = 0; j < this.n; ++j)
                {
                    e[i + j * this.n] = val[i, j];
                }
            }
        }

        public float this[int row, int column]
        {
            get { return e[row * n + column]; }
            set { e[row * n + column] = value; }
        }
        public int RowSize
        {
            get { return m; }
        }
        public int ColumnSize
        {
            get { return n; }
        }

        public void Multiply(float[] xIn, float[] xOut)
        {
            if (xIn.Length < n || xOut.Length < m) throw new ArgumentException();

            for (int i = 0, b = 0; i < m; i++, b += n)
            {
                float sum = 0;
                for (int j = 0; j < n; j++)
                    sum += e[b + j] * xIn[j];
                xOut[i] = sum;
            }
        }

        public MatrixNd Mul(MatrixNd right)
        {
            if (right.RowSize != this.ColumnSize) throw new ArgumentException();
            int l = right.ColumnSize;
            MatrixNd matrix = new MatrixNd(m, l);
            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < l; ++j)
                {
                    for (int k = 0; k < n; ++k)
                    {
                        matrix[i, j] += this[i, k] * right[k, j];
                    }
                }
            }
            return matrix;
        }
        public MatrixNd Transpose()
        {
            MatrixNd matrix = new MatrixNd(n, m);
            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    matrix[j, i] = this[i, j];
                }
            }
            return matrix;
        }
        public Matrix3d ToMyMatrix3d()
        {
            Matrix3d ret = new Matrix3d();
            ret[0] = this[0, 0];
            ret[1] = this[0, 1];
            ret[2] = this[0, 2];
            ret[3] = this[1, 0];
            ret[4] = this[1, 1];
            ret[5] = this[1, 2];
            ret[6] = this[2, 0];
            ret[7] = this[2, 1];
            ret[8] = this[2, 2];
            return ret;
        }
        public Matrix2d ToMatrix2D()
        {
            return new Matrix2d(this[0, 0], this[0, 1], this[1, 0], this[1, 1]);
        }
    }
}

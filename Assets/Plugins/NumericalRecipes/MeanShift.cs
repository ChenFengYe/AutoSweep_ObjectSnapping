// Copyright (c)2010, State Key Laboratory of CAD&CG, Zhejiang University. All rights reserved
// @author DeathKnight

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NumericalRecipes
{
    public class VectorN : IEquatable<VectorN>
    {
        private float[] _data;

        public int Dim { get { return this._data.Length; } }
        public float this[int i]
        {
            get { return this._data[i]; }
            set { this._data[i] = value; }
        }

        public VectorN(int n)
        {
            this._data = new float[n];
        }
        public VectorN(float[] data)
        {
            this._data = (float[])data.Clone();
        }
        public VectorN(VectorN p)
        {
            this._data = (float[])p._data.Clone();
        }
        public VectorN Clone()
        {
            return new VectorN(this._data);
        }

        public float Dot(VectorN v)
        {
            float result = 0;
            for (int i = 0; i < this.Dim; ++i)
                result += this[i] * v[i];
            return result;
        }
        public float Length()
        {
            return Mathf.Sqrt(this.LengthSquare());
        }
        public float LengthSquare()
        {
            float result = 0;
            foreach (float val in this._data)
                result += val * val;
            return result / this.Dim;
        }
        public void Normalize()
        {
            float norm = this.Length();
            for (int i = 1; i < this.Dim; ++i)
                this[i] /= norm;
        }

        public override string ToString()
        {
            string s = "[ " + this[0];
            for (int i = 1; i < this.Dim; ++i)
                s += ", " + this[i];
            return s + " ]";
        }

        public static VectorN Max(VectorN v1, VectorN v2)
        {
            VectorN result = new VectorN(v1.Dim);
            for (int i = 0; i < v1.Dim; ++i)
                result[i] = Math.Max(v1[i], v2[i]);
            return result;
        }
        public static VectorN Min(VectorN v1, VectorN v2)
        {
            VectorN result = new VectorN(v1.Dim);
            for (int i = 0; i < v1.Dim; ++i)
                result[i] = Math.Min(v1[i], v2[i]);
            return result;
        }
        public bool Equals(VectorN v)
        {
            for (int i = 0; i < this.Dim; ++i)
                if (this[i] != v[i])
                    return false;
            return true;
        }

        static public VectorN operator +(VectorN v1, VectorN v2)
        {
            VectorN result = new VectorN(v1.Dim);
            for (int i = 0; i < v1.Dim; ++i)
                result[i] = v1[i] + v2[i];
            return result;
        }
        static public VectorN operator -(VectorN v1, VectorN v2)
        {
            VectorN result = new VectorN(v1.Dim);
            for (int i = 0; i < v1.Dim; ++i)
                result[i] = v1[i] - v2[i];
            return result;
        }
        static public VectorN operator *(VectorN v, float s)
        {
            VectorN result = new VectorN(v.Dim);
            for (int i = 0; i < v.Dim; ++i)
                result[i] = v[i] * s;
            return result;
        }
        static public VectorN operator *(float s, VectorN v)
        {
            VectorN result = new VectorN(v.Dim);
            for (int i = 0; i < v.Dim; ++i)
                result[i] = v[i] * s;
            return result;
        }
        static public VectorN operator /(VectorN v, float s)
        {
            VectorN result = new VectorN(v.Dim);
            for (int i = 0; i < v.Dim; ++i)
                result[i] = v[i] / s;
            return result;
        }
    }

    public class MeanShift
    {
        private static float THRESHOLD = 0.00001f;

        public int N { get; private set; }
        public int Dim { get; private set; }
        public float H { get; set; }   // bandwidth.

        private List<VectorN> _modes = new List<VectorN>();
        private List<List<int>> _modebasins;
        private List<int> _modespt = new List<int>();
        private VectorN[] _data;
        private VectorN _minv, _maxv;

        public VectorN[] Modes { get { return _modes.ToArray(); } }
        public int[][] ModeBasins
        {
            get
            {
                int[][] result = new int[_modebasins.Count][];
                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = _modebasins[i].ToArray();
                }
                return result;
            }
        }

        public MeanShift(VectorN[] transformPoints)
        {
            this.N = transformPoints.Length;
            this.Dim = transformPoints[0].Dim;
            this._data = new VectorN[transformPoints.Length];
            for (int i = 0; i < transformPoints.Length; ++i)
                this._data[i] = transformPoints[i].Clone();
            this.H = 0.3f;

            Normalize();
        }

        public void FindModes()
        {
            System.Random rand = new System.Random();

            for (int i = 0; i < _data.Length; ++i)
            {
                VectorN mode = Iterate(_data[i]);
                VectorN newmode = mode.Clone();

                VectorN p = new VectorN(this.Dim);
                for (int j = 0; j < Dim; ++j)
                    p[j] = Convert.ToSingle(rand.NextDouble()) * 2 - 1;
                p.Normalize();
                float d = H / 2;

                newmode = newmode + d * p;
                newmode = Iterate(newmode);

                if ((mode - newmode).LengthSquare() < THRESHOLD)
                {
                    _modes.Add(mode);
                    _modespt.Add(i);
                }
            }

            Unique();
            Estimate();
            UnNormalize();
        }

        private void UnNormalize()
        {
            for (int i = 0; i < this._modes.Count; ++i)
            {
                for (int j = 0; j < this.Dim; ++j)
                {
                    _modes[i][j] = _modes[i][j] * (this._maxv[j] - this._minv[j]) + this._minv[j];
                }
            }
        }

        private void Estimate()
        {
            List<float> densList = new List<float>();
            List<int> indiciesList = new List<int>();
            for (int i = 0; i < _modes.Count; ++i)
            {
                float sum = 0.0f;
                for (int j = 0; j < _data.Length; ++j)
                {
                    VectorN v = (_modes[i] - _data[j]) / H;
                    float temp = v.LengthSquare();
                    temp = (temp > 1) ? 0 : (1 - temp);
                    sum += temp;
                }
                sum /= _data.Length;
                densList.Add(sum);
                indiciesList.Add(i);
            }

            float[] densities = densList.ToArray();
            int[] indicies = indiciesList.ToArray();

            Array.Sort(densities, indicies);
            Array.Reverse(densities);
            Array.Reverse(indicies);

            VectorN[] tempmodes = _modes.ToArray();
            List<int>[] tempbasins = _modebasins.ToArray();
            for (int i = 0; i < indicies.Length; ++i)
            {
                _modes[i] = tempmodes[indicies[i]];
                _modebasins[i] = tempbasins[indicies[i]];
            }
        }

        private void Unique()
        {
            List<VectorN> unique = new List<VectorN>();
            List<List<int>> modespts = new List<List<int>>();
            for (int i = 0; i < _modes.Count; ++i)
            {
                int index = unique.IndexOf(_modes[i]);
                if (index == -1)
                {
                    unique.Add(_modes[i]);
                    List<int> pts = new List<int>();
                    pts.Add(_modespt[i]);
                    modespts.Add(pts);
                }
                else
                {
                    modespts[index].Add(_modespt[i]);
                }
            }

            List<List<VectorN>> modesets = new List<List<VectorN>>();
            List<VectorN> centers = new List<VectorN>();
            List<List<int>> centerpts = new List<List<int>>();

            for (int i = 0; i < unique.Count; ++i)
            {
                bool find = false;
                for (int j = 0; j < centers.Count; ++j)
                {
                    if (((unique[i] - centers[j]) / this.H).LengthSquare() < 1)
                    {
                        modesets[j].Add(unique[i]);
                        VectorN sum = new VectorN(this.Dim);
                        foreach (VectorN vm in modesets[j])
                        {
                            sum += vm;
                        }
                        centers[j] = sum / (float)modesets[j].Count;
                        centerpts[j].AddRange(modespts[i]);
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    List<VectorN> s = new List<VectorN>();
                    s.Add(unique[i]);
                    modesets.Add(s);
                    centers.Add(unique[i]);
                    centerpts.Add(modespts[i]);
                }
            }

            _modes = centers;
            _modebasins = centerpts;
        }

        private VectorN Iterate(VectorN initial)
        {
            VectorN y;
            VectorN nexty = initial;
            do
            {
                y = nexty;

                int num = 0;
                VectorN sum = new VectorN(this.Dim);
                for (int i = 0; i < this.N; ++i)
                {
                    VectorN temp = (y - _data[i]) / this.H;
                    if (temp.LengthSquare() < 1.0)
                    {
                        num++;
                        sum = sum + _data[i];
                    }
                }
                nexty = sum / num;
            } while ((y - nexty).LengthSquare() > THRESHOLD);

            return nexty;
        }

        private void Normalize()
        {
            this._minv = new VectorN(this.Dim);
            this._maxv = new VectorN(this.Dim);
            for (int i = 0; i < this.Dim; ++i)
            {
                _minv[i] = float.MaxValue;
                _maxv[i] = float.MinValue;
            }

            foreach (VectorN v in this._data)
            {
                for (int i = 0; i < v.Dim; ++i)
                {
                    _minv[i] = Math.Min(_minv[i], v[i]);
                    _maxv[i] = Math.Max(_maxv[i], v[i]);
                }
            }

            for (int i = 0; i < this.N; ++i)
            {
                for (int j = 0; j < this.Dim; ++j)
                {
                    if (_minv[j] != _maxv[j])
                    {
                        _data[i][j] = (_data[i][j] - _minv[j]) / (_maxv[j] - _minv[j]);
                    }
                }
            }
        }


    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using NumericalRecipes;
using MyGeometry;

public class GraphicsEngine : MonoBehaviour {
    public FaceEngine m_faceEngine;
    public BodyEngine m_bodyEngine;

    public void FitTopFace(Image<Gray, byte> img)
    {
        m_faceEngine.Solve(img);
    }

    public void FitBody(Image<Gray, byte> img)
    {
        m_bodyEngine.SnapCylinder(img);
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

using UnityEngine;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using MyGeometry;
using UnityEditor;
using UnityExtension;
using System.Drawing;

using Accord.Math;
using Accord.Math.Distances;
using Accord.MachineLearning;
using Accord.Statistics.Distributions.DensityKernels;


public class myDistanceClass : IMetric<double[]>
{
    // dir distance
    public double Distance(double[] x, double[] y)
    {

        Vector2 s1 = new Vector2((float)x[0], (float)x[1]);
        Vector2 e1 = new Vector2((float)x[2], (float)x[3]);
        Vector2 s2 = new Vector2((float)y[0], (float)y[1]);
        Vector2 e2 = new Vector2((float)y[2], (float)y[3]);

        Line2 l1 = new Line2(s1, e1);
        Line2 l2 = new Line2(s2, e2);

        double cos = Math.Abs(Vector2.Dot(l1.dir, l2.dir));
        if (cos > Math.Cos(10 * Math.PI / 180)) cos = 1;

        double d1 = Math.Min(l1.DistanceToLine(s2), l1.DistanceToLine(e2));
        double d2 = Math.Min(l2.DistanceToLine(s1), l2.DistanceToLine(e1));
        double mindis = Math.Min(d1, d2);




        return 1 - cos + mindis * 4 / SkeletonExtractor.IMGSIZE;
    }
}

public class OrderItem
{
    public double value;
    public int index;
    public OrderItem(double v, int i)
    {
        value = v;
        index = i;
    }
}
public class SkeletonExtractor
{

    public static double thred = 8;       // lager -- find straight line; 

    private Image<Gray, byte> body_img = null;
    private List<Image<Gray, byte>> face_img = null;
    private Image<Gray, byte> attach_img = null;
    private Image<Gray, byte> ori_thin_img = null;
    private Image<Gray, byte> ori_prune_img = null;
    static public int IMGSIZE = 0;
    private bool noface = false;
    private bool iscube = false;
    private Label label_forname = Label.None;
    private int index_forname = -1;

    private List<Vector2> face_center;
    private List<int> end_points = new List<int>();

    private bool debug = true;

    public SkeletonExtractor(Image<Rgb, byte> _img, List<Image<Rgb, byte>> _face_img, int best_face_idx, int indexforname)
    {
        this.index_forname = indexforname;
        this.label_forname = Label.Cylinder;
        this.body_img = _img.Convert<Gray, byte>();
        this.face_img = new List<Image<Gray, byte>>();
        this.face_img.Add(_face_img[best_face_idx].Convert<Gray, byte>());

        for (int i = 0; i < _face_img.Count; i++)
        {
            if (i != best_face_idx)
                this.face_img.Add(_face_img[i].Convert<Gray, byte>());
        }
    }
    public SkeletonExtractor(Image<Rgb, byte> _img, Image<Rgb, byte> _face_img, int indexforname)
    {
        this.index_forname = indexforname;
        this.label_forname = Label.Cube;
        this.body_img = _img.Convert<Gray, byte>();
        this.face_img = new List<Image<Gray, byte>>();
        this.face_img.Add(_face_img.Convert<Gray, byte>());
    }
    public SkeletonExtractor(int indexforname)
    {
        index_forname = indexforname;
    }
    public void SkeletonExtractorHandle(Image<Rgb, byte> _img, Image<Rgb, byte> _handle_img)
    {
        this.label_forname = Label.Handle;
        this.body_img = _handle_img.Convert<Gray, byte>();
        this.attach_img = _img.Convert<Gray, byte>();
        noface = true;
        // guess top face
        List<Vector2> body_points = IExtension.GetBoundary(this.body_img);
        List<Vector2> attach_points = IExtension.GetBoundary(this.attach_img);
        Vector2 attach_center = Utility.Average(attach_points);

        double mindis_center = double.MaxValue;
        //int counter = 0;

        List<OrderItem> dis_body = new List<OrderItem>();

        Vector2 closest_body_point_to_center = new Vector2();
        for (int i = 0; i < body_points.Count; i++)
        {
            double dis = Utility.DistancePoint2Set(body_points[i], attach_points);
            dis_body.Add(new OrderItem(dis, i));
        }
        dis_body = dis_body.OrderBy(x => x.value).ToList();
        for (int i = 0; i < dis_body.Count * 0.1; i++)
        {
            double dis_to_center = Vector2.Distance(body_points[dis_body[i].index], attach_center);
            if (dis_to_center < mindis_center)
            {
                mindis_center = dis_to_center;
                closest_body_point_to_center = body_points[dis_body[i].index];
            }
        }

        Image<Gray, byte> guess_face = this.body_img.CopyBlank();
        //float guess_radius = 20f * body_img.Height / 600f;
        float guess_radius = 10f * body_img.Height / 600f;
        do
        {
            guess_radius *= 1.1f;
            guess_face = this.body_img.CopyBlank();
            guess_face.Draw(new CircleF(new PointF(closest_body_point_to_center.x, closest_body_point_to_center.y), guess_radius), new Gray(255), -1);
            guess_face.Save("skeleton1guessface" + guess_radius + ".png");
        }
        while (IExtension.GetBoundary(guess_face).Count == 0);


        CvInvoke.BitwiseAnd(guess_face, body_img, guess_face);
        if (debug)
        {
            guess_face.Save("skeleton1guessface.png");
        }


        this.face_img = new List<Image<Gray, byte>>();
        this.face_img.Add(guess_face);
    }



    private List<Line2> Skeletonize(out bool iscurve)
    {
        Image<Gray, byte> img2 = body_img.Copy();
        Image<Gray, byte> eroded = new Image<Gray, byte>(img2.Size);
        Image<Gray, byte> temp = new Image<Gray, byte>(img2.Size);
        Image<Gray, byte> skel = new Image<Gray, byte>(img2.Size);

        body_img.Save("test.png");


        #region with matlab
        string argument1 = "\"" + "test.png" + "\"";
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo.FileName = System.Environment.CurrentDirectory + "\\Assets\\frommatlab\\skeleton.exe";
        process.StartInfo.Arguments = argument1;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        //启动  
        process.Start();
        process.WaitForExit();
        #endregion

        skel = new Image<Gray, byte>("prune.png");
        ori_thin_img = new Image<Gray, byte>("thin.png");
        ori_prune_img = skel;

        #region thining - comment
        //skel.SetValue(0);
        //CvInvoke.Threshold(img2, temp, 127, 256, 0);
        //var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));
        //bool done = false;

        ////skeleton
        //int itr = 0;
        //while (!done)
        //{
        //    CvInvoke.Erode(img2, eroded, element, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
        //    CvInvoke.Dilate(eroded, temp, element, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
        //    CvInvoke.Subtract(img2, temp, temp);
        //    CvInvoke.BitwiseOr(skel, temp, skel);
        //    eroded.CopyTo(img2);
        //    itr++;
        //    if (CvInvoke.CountNonZero(img2) == 0) done = true;
        //}
        //Image<Gray, Byte> cannyimg = body_img.Canny(60, 100);
        //CvInvoke.Dilate(cannyimg, cannyimg, element, new Point(-1, -1), 3, BorderType.Reflect, default(MCvScalar));
        //CvInvoke.Subtract(skel, cannyimg, skel);
        //ori_skel_img = skel.Copy();

        ////thinning
        //if (!noface)
        //{
        //    #region thinning
        //    List<Mat> cs = new List<Mat>();
        //    List<Mat> ds = new List<Mat>();
        //    for (int i = 0; i < 8; i++)
        //    {
        //        cs.Add(CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1)));
        //        ds.Add(CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1)));
        //    }

        //    cs[0].SetTo(new int[] { 0, 0, 0, 0, 1, 0, 1, 1, 1 });
        //    cs[1].SetTo(new int[] { 1, 0, 0, 1, 1, 0, 1, 0, 0 });
        //    cs[2].SetTo(new int[] { 1, 1, 1, 0, 1, 0, 0, 0, 0 });
        //    cs[3].SetTo(new int[] { 0, 0, 1, 0, 1, 1, 0, 0, 1 });

        //    ds[0].SetTo(new int[] { 1, 1, 1, 0, 0, 0, 0, 0, 0 });
        //    ds[1].SetTo(new int[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 });
        //    ds[2].SetTo(new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1 });
        //    ds[3].SetTo(new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 });

        //    cs[4].SetTo(new int[] { 0, 0, 0, 1, 1, 0, 1, 1, 0 });
        //    cs[5].SetTo(new int[] { 1, 1, 0, 1, 1, 0, 0, 0, 0 });
        //    cs[6].SetTo(new int[] { 0, 1, 1, 0, 1, 1, 0, 0, 0 });
        //    cs[7].SetTo(new int[] { 0, 0, 0, 0, 1, 1, 0, 1, 1 });

        //    ds[4].SetTo(new int[] { 0, 1, 1, 0, 0, 1, 0, 0, 0 });
        //    ds[5].SetTo(new int[] { 0, 0, 0, 0, 0, 1, 0, 1, 1 });
        //    ds[6].SetTo(new int[] { 0, 0, 0, 1, 0, 0, 1, 1, 0 });
        //    ds[7].SetTo(new int[] { 1, 1, 0, 1, 0, 0, 0, 0, 0 });

        //    Image<Gray, byte> img3 = skel.Copy();
        //    Image<Gray, byte> temp2 = skel.CopyBlank();
        //    Image<Gray, byte> lastimg3 = skel.Copy();

        //    done = false;
        //    while (!done)
        //    {
        //        for (int i = 0; i < 8; i++)
        //        {
        //            temp = this.HitOrMiss(img3, cs[i], ds[i]);
        //            CvInvoke.Subtract(img3, temp, img3);
        //        }

        //        CvInvoke.Subtract(lastimg3, img3, temp2);
        //        lastimg3 = img3.Copy();
        //        if (CvInvoke.CountNonZero(temp2) == 0) done = true;
        //    }

        //    //img3.Save("thining.png");
        //    #endregion
        //    skel = img3.Copy();
        //    ori_thinning_img = img3.Copy();
        //}
        ////// remove noise
        ////for (int i = 0; i < img3.Height; i++)
        ////{
        ////    for (int j = 0; j < img3.Width; j++)
        ////    {
        ////        if (img3[i, j].Equals(new Gray(255)))
        ////        {
        ////            bool change = false;
        ////            for (int pad = 1; pad < 3; pad++)
        ////            {
        ////                if (i >= pad && i < img3.Height - pad && j >= pad && j < img3.Width - pad)
        ////                {
        ////                    if (img3[i - pad, j].Equals(new Gray(0)) &&
        ////                        img3[i - pad, j - pad].Equals(new Gray(0)) &&
        ////                        img3[i - pad, j + pad].Equals(new Gray(0)) &&
        ////                        img3[i + pad, j].Equals(new Gray(0)) &&
        ////                        img3[i + pad, j - pad].Equals(new Gray(0)) &&
        ////                        img3[i + pad, j + pad].Equals(new Gray(0)) &&
        ////                        img3[i, j - pad].Equals(new Gray(0)) &&
        ////                        img3[i, j + pad].Equals(new Gray(0)))
        ////                        change = true;
        ////                }
        ////            }
        ////            if (change)
        ////                img3[i, j] = new Gray(0);
        ////        }
        ////    }
        ////}
        ////img3.Save("thiningdenoise.png"); 
        #endregion

        // get line
        // consider both straight line and curve
        LineSegment2D[] lines = skel.HoughLinesBinary(
              1, //Distance resolution in pixel-related units
              Math.PI / 180.0, //Angle resolution measured in radians.
              3, //threshold
              4, //min Line width
              1 //gap between lines
              )[0]; //Get the lines from the first channel

        Image<Gray, byte> lineimg = skel.CopyBlank();
        List<Line2> skel_lines = new List<Line2>();
        foreach (LineSegment2D line in lines)
        {
            //remove image boundaries
            //if (line.P1.X > 10 && line.P1.Y > 10 && line.P1.X < body_img.Height - 10 && line.P1.Y < body_img.Width &&
            //   line.P2.X > 10 && line.P2.Y > 10 && line.P2.X < body_img.Height - 10 && line.P2.Y < body_img.Width - 10)
            //{
            skel_lines.Add(new Line2(new Vector2(line.P1.X, line.P1.Y), new Vector2(line.P2.X, line.P2.Y)));
            lineimg.Draw(line, new Gray(255), 2);
            //}
        }
        if (debug) lineimg.Save("skel-line.png");


        // cluster according to direction and relative distance
        // too many cluster means curve axis
        IMGSIZE = Math.Min(body_img.Width, body_img.Height);
        if (skel_lines.Count > 0)
        {
            double[][] xy = new double[skel_lines.Count][];
            for (int i = 0; i < skel_lines.Count; i++)
            {
                xy[i] = new double[] { skel_lines[i].start.x,skel_lines[i].start.y,
                                         skel_lines[i].end.x,skel_lines[i].end.y};
            }

            MeanShift clusterMS = new MeanShift(4, new UniformKernel(), 0.02);
            clusterMS.Distance = new myDistanceClass();
            MeanShiftClusterCollection clustering = clusterMS.Learn(xy);
            var lineLabels = clustering.Decide(xy);
            int clustercount = lineLabels.DistinctCount();
            //Debug.Log("cluster count: " + clustercount);

            if (debug)
            {
                Image<Rgb, byte> lineimg_rgb = lineimg.Convert<Rgb, byte>();
                System.Random rnd = new System.Random();
                Rgb[] colortable = new Rgb[clustering.Count];
                for (int i = 0; i < clustering.Count; i++)
                {
                    colortable[i] = new Rgb(rnd.Next(255), rnd.Next(255), rnd.Next(255));
                }

                for (int i = 0; i < skel_lines.Count; i++)
                {
                    int label = lineLabels[i];
                    lineimg_rgb.Draw(skel_lines[i].ToLineSegment2D(), colortable[label], 2);
                }
                lineimg_rgb.Save("skel-line-cluster.png");
            }


            if (noface)
                thred = 2;   // 2
            if (clustercount > thred)
                iscurve = true;
            else
                iscurve = false;

        }
        else
        {
            iscurve = false;
            NumericalRecipes.RansacLine2d rcl = new NumericalRecipes.RansacLine2d();
            List<Vector2> linepoints = new List<Vector2>();
            linepoints = IExtension.GetMaskPoints(skel);
            Line2 bestline = rcl.Estimate(linepoints);
            skel_lines.Add(bestline);
        }

        return skel_lines;
    }
    private Line2 ApproximateFromCubeTop()
    {
        #region get corner points
        // get corner points
        VectorOfVectorOfPoint con = new VectorOfVectorOfPoint();
        Image<Gray, byte> img_copy = this.face_img[0].Copy();
        CvInvoke.FindContours(img_copy, con, img_copy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
        int maxcomp = 0, max_con_idx = -1;
        for (int i = 0; i < con.Size; i++)
        {
            if (con[i].Size > maxcomp)
            {
                maxcomp = con[i].Size;
                max_con_idx = i;
            }
        }
        // found max component's contour
        // simplify contours
        VectorOfPoint con2 = new VectorOfPoint();
        con2 = con[max_con_idx];
        double alpha = 0.01; // 越小越接近曲线
        int iter = 0;
        List<Vector2> corner_points = new List<Vector2>();
        while (con2.Size != 4)
        {
            if (iter++ > 200) break;
            double epsilon = alpha * CvInvoke.ArcLength(con[max_con_idx], true);
            CvInvoke.ApproxPolyDP(con[max_con_idx], con2, epsilon, true);
            if (con2.Size > 4)
            {
                alpha += 0.01;
                corner_points.Clear();
                for (int i = 0; i < con2.Size; i++)
                    corner_points.Add(new Vector2(con2[i].X, con2[i].Y));
            }
            if (con2.Size < 4)
                alpha -= 0.003;
        }

        if (con2.Size == 4)
        {
            corner_points.Clear();
            for (int i = 0; i < con2.Size; i++)
            {
                corner_points.Add(new Vector2(con2[i].X, con2[i].Y));
            }
        }
        else
        {
            int removei = corner_points.Count - 4;
            for (int i = 0; i < removei; i++)
            {
                corner_points.RemoveAt(1);
            }

        }


        face_center = new List<Vector2>();
        face_center.Add(Utility.Average(corner_points));


        List<Vector2> boundary2_face = IExtension.GetBoundary(this.face_img[0]);
        List<Vector2> boundary2_body = ExtractOutline(this.body_img, boundary2_face);
        int[] i_corner = SelectTwoCorners(corner_points, boundary2_body, this.body_img);
        #endregion


        // body img fit line
        Image<Gray, byte> temp = body_img.Copy();
        Image<Gray, byte> body_bound = temp.Canny(60, 100);

        LineSegment2D[] lines = body_bound.HoughLinesBinary(
              1, //Distance resolution in pixel-related units
              Math.PI / 180.0, //Angle resolution measured in radians.
              3, //threshold
              4, //min Line width
              1 //gap between lines
              )[0]; //Get the lines from the first channel

        var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));
        CvInvoke.Dilate(this.face_img[0], temp, element, new Point(-1, -1), 30, BorderType.Reflect, default(MCvScalar));
        //temp.Save("face_dilate.png");
        Image<Gray, byte> lineimg = temp.CopyBlank();
        List<Line2> body_lines = new List<Line2>();
        foreach (LineSegment2D line in lines)
        {
            if (!temp[line.P1.Y, line.P1.X].Equals(new Gray(255)) && !temp[line.P2.Y, line.P2.X].Equals(new Gray(255)))
            {
                body_lines.Add(new Line2(new Vector2(line.P1.X, line.P1.Y), new Vector2(line.P2.X, line.P2.Y)));
                lineimg.Draw(line, new Gray(255), 2);
            }
        }
        //lineimg.Save("body_line.png");


        /// use corner points to find lines
        double mindis0 = double.MaxValue, mindis1 = double.MaxValue;
        int line0 = -1, line1 = -1;
        for (int i = 0; i < body_lines.Count; i++)
        {
            double dis0 = Mathf.Min(Vector2.Distance(corner_points[i_corner[0]], body_lines[i].start), Vector2.Distance(corner_points[i_corner[0]], body_lines[i].end));
            double dis1 = Mathf.Min(Vector2.Distance(corner_points[i_corner[1]], body_lines[i].start), Vector2.Distance(corner_points[i_corner[1]], body_lines[i].end));
            if (dis0 < mindis0)
            {
                mindis0 = dis0;
                line0 = i;
            }
            if (dis1 < mindis1)
            {
                mindis1 = dis1;
                line1 = i;
            }
        }

        // find similar
        // straight line

        Line2 body_line0 = FitCloseLine(body_lines, body_lines[line0], 50);
        Line2 body_line1 = FitCloseLine(body_lines, body_lines[line1], 50);

        if (Vector2.Distance(corner_points[i_corner[0]], body_line0.start) < Vector2.Distance(corner_points[i_corner[0]], body_line0.end))
            body_line0.Flip();

        if (Vector2.Distance(corner_points[i_corner[1]], body_line1.start) < Vector2.Distance(corner_points[i_corner[1]], body_line1.end))
            body_line1.Flip();

        Vector2 meandir = (body_line0.dir + body_line1.dir).normalized;

        Line2 top_normal_line = new Line2(face_center.First(), meandir, true);
        Vector2 online_point = top_normal_line.GetPointwithT(Vector2.Distance(corner_points[0], corner_points[1]));
        if (online_point.x >= 0 && online_point.y >= 0 && online_point.x < body_img.Width && online_point.y < body_img.Height)
        {
            if (!this.body_img[(int)online_point.y, (int)online_point.x].Equals(new Gray(255)))
                top_normal_line.Flip();
        }
        else
            top_normal_line.Flip();
        top_normal_line.UpdateEnd(new Vector2(500, 500));
        top_normal_line.UpdateEnd(new Vector2(0, 0));

        if (debug)
        {
            Image<Rgb, byte> cube_outline = this.body_img.Copy().Convert<Rgb, byte>();
            cube_outline.Draw(new CircleF(new PointF(corner_points[i_corner[0]].x, corner_points[i_corner[0]].y), 10.0f), new Rgb(255, 0, 0), 1);
            cube_outline.Draw(new CircleF(new PointF(corner_points[i_corner[1]].x, corner_points[i_corner[1]].y), 10.0f), new Rgb(255, 0, 0), 1);
            cube_outline.Draw(new CircleF(new PointF(corner_points[0].x, corner_points[0].y), 8), new Rgb(0, 0, 255), 1);
            cube_outline.Draw(new CircleF(new PointF(corner_points[1].x, corner_points[1].y), 8), new Rgb(0, 0, 255), 1);
            cube_outline.Draw(new CircleF(new PointF(corner_points[2].x, corner_points[2].y), 8), new Rgb(0, 0, 255), 1);
            cube_outline.Draw(new CircleF(new PointF(corner_points[3].x, corner_points[3].y), 8), new Rgb(0, 0, 255), 1);
            cube_outline.Draw(body_lines[line0].ToLineSegment2D(), new Rgb(0, 255, 0), 3);
            cube_outline.Draw(body_lines[line1].ToLineSegment2D(), new Rgb(0, 255, 0), 3);
            cube_outline.Draw(body_line0.ToLineSegment2D(), new Rgb(0, 255, 255), 3);
            cube_outline.Draw(body_line1.ToLineSegment2D(), new Rgb(0, 0, 255), 3);
            cube_outline.Save("cube_outline.png");
            Image<Gray, byte> approx_top = this.body_img.Copy();
            foreach (Vector2 v in top_normal_line.SamplePoints())
            {
                approx_top.Draw(new CircleF(new PointF(v.x, v.y), 2.0f), new Gray(0), 1);
            }
            approx_top.Save("approx.png");
        }

        return top_normal_line;
    }
    private Line2 ApproximateFromTop()
    {
        //guess axis with top face(s)

        Line2 main_axis = null;

        face_center = new List<Vector2>();
        List<Vector2> normal = new List<Vector2>();

        List<double> minoraxislength = new List<double>();

        foreach (Image<Gray, byte> fimg in face_img)
        {
            Vector2 mean = new Vector2();
            List<Vector2> points = IExtension.GetMaskPoints(fimg);
            foreach (Vector2 p in points)
            {
                mean += p;
            }
            mean /= points.Count;
            face_center.Add(mean);
            float W = points.Count;

            float[,] C = new float[2, 2];
            foreach (Vector2 point in points)
            {
                C[0, 0] += (point.x - mean.x) * (point.x - mean.x);
                C[0, 1] += (point.x - mean.x) * (point.y - mean.y);

                C[1, 0] += (point.y - mean.y) * (point.x - mean.x);
                C[1, 1] += (point.y - mean.y) * (point.y - mean.y);
            }

            C[0, 0] /= W;
            C[0, 1] /= W;
            C[1, 0] /= W;
            C[1, 1] /= W;

            Matrix2d CM = new Matrix2d(C);
            NumericalRecipes.SVD svd = new NumericalRecipes.SVD(C);
            //svd.w - eigenvalue, start from 1
            //svd.u - eigenvector, start from 1

            int max = 1, min = 2;
            if (svd.w[max] < svd.w[min])
            {
                int temp = max;
                max = min;
                min = temp;
            }

            float major = 2 * Mathf.Sqrt(svd.w[max]);
            Vector2 majoraxis = new Vector2(svd.u[1, max], svd.u[2, max]);
            majoraxis.Normalize();
            float minor = 2 * Mathf.Sqrt(svd.w[min]);
            Vector2 minoraxis = new Vector2(svd.u[1, min], svd.u[2, min]);
            minoraxis.Normalize();

            Vector2 majorendp = mean + majoraxis * major;
            Vector2 majorstartp = mean - majoraxis * major;
            Vector2 minorendp = mean + minoraxis * minor;
            Vector2 minorstartp = mean - minoraxis * minor;


            //minoraxislength.Add(Vector2.Distance(minorendp, minorstartp));
            minoraxislength.Add((double)minor * 2);
            normal.Add(Utility.PerpendicularRight(majoraxis));
            //normal.Add(majoraxis);
        }


        if (face_center.Count > 1)
        {
            //Vector2 mean_normal = normal.First();
            Line2 best_top_normal_line = new Line2(face_center.First(), normal.First(), true);
            double maxdis = 0;
            int farthesttopidx = 0;
            for (int i = 1; i < face_center.Count; i++)
            {
                double tofirstcenterdis = Vector2.Distance(face_center[0], face_center[i]);
                if (tofirstcenterdis > maxdis)
                {
                    maxdis = tofirstcenterdis;
                    farthesttopidx = i;
                }
                double dis = best_top_normal_line.DistanceToLine(face_center[i]);
                if (dis > 10)
                    main_axis = null;
            }
            main_axis = new Line2(face_center.First(), face_center[farthesttopidx]);
        }
        else
        {
            Line2 top_normal_line = new Line2(face_center.First(), normal.First(), true);
            Vector2 online_point = top_normal_line.GetPointwithT((float)minoraxislength[0]);
            if (online_point.x >= 0 && online_point.y >= 0 && online_point.x < body_img.Width && online_point.y < body_img.Height)
            {
                if (!this.body_img[(int)online_point.y, (int)online_point.x].Equals(new Gray(255)))
                    top_normal_line.Flip();
            }
            else
                top_normal_line.Flip();
            main_axis = top_normal_line;
        }

        return main_axis;
    }
    private List<Vector2> ApproximateStraightAxis(List<Line2> skel_lines)
    {
        // from skeleton lines(aka. thining image lines) (find lines that have the same direction with guessed axis <- when top face is known)
        // ransac, fit one line as main axis
        // update end points
        // set top circle center as start point
        // extend end point

        Line2 new_main_axis = null;
        Image<Rgb, byte> mainaxis_img = body_img.Copy().Convert<Rgb, byte>();

        Line2 main_axis = null;

        if (iscube)
            main_axis = ApproximateFromCubeTop();
        else
            main_axis = ApproximateFromTop();

        if (main_axis == null)
            return null;

        if (noface)
        {
            NumericalRecipes.RansacLine2d rcl = new NumericalRecipes.RansacLine2d();
            List<Vector2> thin_points = IExtension.GetMaskPoints(this.ori_thin_img);
            List<Vector2> linepoints = new List<Vector2>();
            for (int i = 0; i < skel_lines.Count; i++)
            {
                linepoints.AddRange(skel_lines[i].SamplePoints());
            }
            //Line2 bestline = rcl.Estimate(linepoints);
            Line2 bestline = rcl.Estimate(thin_points);
            double diss = Vector2.Distance(bestline.start, face_center[0]);
            double dise = Vector2.Distance(bestline.end, face_center[0]);
            if (dise < diss)
                bestline.Flip();
            new_main_axis = bestline;
        }
        else
        {
            // use ori thinning image as guidance
            LineSegment2D[] lines = ori_thin_img.HoughLinesBinary(
                    1, //Distance resolution in pixel-related units
                    Math.PI / 180.0, //Angle resolution measured in radians.
                    3, //threshold
                    4, //min Line width
                    1 //gap between lines
                    )[0]; //Get the lines from the first channel

            skel_lines.Clear();
            foreach (LineSegment2D line in lines)
                skel_lines.Add(new Line2(new Vector2(line.P1.X, line.P1.Y), new Vector2(line.P2.X, line.P2.Y)));

            Line2 bestline = FitSimilarLine(skel_lines, main_axis);
            if (Line2.IsParallel(bestline, main_axis, 5))
                new_main_axis = bestline;
            else
                new_main_axis = main_axis;
        }

        // update end
        List<Vector2> ori_skel_points = IExtension.GetMaskPoints(ori_thin_img);
        for (int i = 0; i < ori_skel_points.Count; i++)
        {
            new_main_axis.UpdateEnd(ori_skel_points[i]);
        }
        new_main_axis.start = new_main_axis.ProjToLine(face_center.First());
        new_main_axis.start = new_main_axis.GetPointwithT(4);

        List<Vector2> axis_points = new List<Vector2>();
        // axis_points.Add(face_center.First());
        axis_points.AddRange(new_main_axis.SamplePoints());
        axis_points.Add(new_main_axis.GetPointwithT((float)new_main_axis.Length() * 1.2f));
        axis_points = IExtension.ResetPath(axis_points, 3);

        #region visualize
        Image<Rgb, byte> mainaxis_point_img = body_img.Copy().Convert<Rgb, byte>();
        foreach (Vector2 v in axis_points)
        {
            mainaxis_point_img.Draw(new CircleF(new PointF(v.x, v.y), 2.0f), new Rgb(255, 0, 0), 1);
        }
        mainaxis_point_img.Draw(new CircleF(new PointF(axis_points.First().x, axis_points.First().y), 3.0f), new Rgb(0, 255, 0), 1);
        mainaxis_point_img.Save(index_forname.ToString() + this.label_forname.ToString() + "_axis_straight.png");
        #endregion

        return axis_points;
    }
    private List<Vector2> ApproximateCurveAxis(List<Line2> skel_lines)
    {
        Image<Rgb, byte> curveaxis_img = body_img.Copy().Convert<Rgb, byte>();
        List<Vector2> skel_points = new List<Vector2>();

        //Image<Gray, byte> temp = face_img[0].Copy();
        //var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));
        //CvInvoke.Dilate(temp, temp, element, new Point(-1, -1), 10, BorderType.Reflect, default(MCvScalar));
        //CvInvoke.Subtract(ori_prune_img, temp, ori_prune_img);
        skel_points = IExtension.GetMaskPoints(ori_prune_img);
        for (int i = 0; i < skel_points.Count; i++)
        {
            if (ori_prune_img[(int)skel_points[i].y, (int)skel_points[i].x].Equals(new Gray(128)))
                this.end_points.Add(i);
        }

        List<Vector2> path_points = new List<Vector2>();
        List<Vector2> top_mask_point = IExtension.GetMaskPoints(this.face_img[0]);
        Vector2 top_center = Utility.Average(top_mask_point);
        //double guess_radius = Math.Sqrt(top_mask_point.Count / Math.PI);

        Vector2 start_point = top_center;
        List<Vector2> attach_boundary = new List<Vector2>();
        if (noface)
        {
            // Vector2 attach_center = Utility.Average(IExtension.GetBoundary(this.attach_img));
            // start_point = attach_center;
            attach_boundary = IExtension.GetBoundary(this.attach_img);
        }

        // closest to top face center
        int startidx = 0;
        double mindis_topcenter = double.MaxValue;
        for (int i = 0; i < end_points.Count; i++)
        {
            double dis = double.MaxValue;
            if (noface)
                dis = Utility.DistancePoint2Set(skel_points[end_points[i]], attach_boundary);
            else
                dis = Vector2.Distance(skel_points[end_points[i]], start_point);
            if (dis < mindis_topcenter)
            {
                mindis_topcenter = dis;
                startidx = end_points[i];
            }
        }

        path_points = FindLongestPath(skel_points, startidx);

        double diss = Vector2.Distance(path_points.First(), start_point);
        double dise = Vector2.Distance(path_points.Last(), start_point);
        if (dise < diss)
            path_points.Reverse();

        path_points = IExtension.ResetPath(path_points, 20);
        // add first point
        if (!noface)
        {
            path_points.Insert(0, top_center);

            // extend ending
            Line2 endray = new Line2(path_points[path_points.Count - 2], path_points[path_points.Count - 1]);
            path_points.Add(endray.GetPointwithT((float)endray.Length() * 10));

            //reset path to 3
            path_points = IExtension.ResetPath(path_points, 3);
        }
        else
        {
            path_points = IExtension.ResetPath(path_points, 3);
            path_points.RemoveRange(0, (int)(0.05 * path_points.Count));
            //path_points.Insert(0, top_center);
        }

        #region visualize
        curveaxis_img = body_img.Copy().Convert<Rgb, byte>();
        for (int i = 0; i < path_points.Count; i++)
        {
            curveaxis_img.Draw(new CircleF(new PointF(path_points[i].x, path_points[i].y), 2.0f), new Rgb(255, 0, i * 2), 1);
        }
        curveaxis_img.Draw(new CircleF(new PointF(path_points[0].x, path_points[0].y), 2.0f), new Rgb(0, 255, 0), 1);
        curveaxis_img.Save(index_forname.ToString() + this.label_forname.ToString() + "_axis_curve.png");
        #endregion

        return path_points;
    }




    /*------------------------ API ---------------------------*/
    public List<Vector2> ExtractSkeleton(out bool iscurve)
    {
        iscurve = false;
        iscube = false;
        List<Line2> skel_lines = Skeletonize(out iscurve);
        if (iscurve)
        {
            List<Vector2> sp = ApproximateCurveAxis(skel_lines);
            //NumericalRecipes.RansacLine2d rcl = new NumericalRecipes.RansacLine2d();
            //Line2 bestline = rcl.Estimate(sp);
            //if (rcl.inliers.Count > 0.9 * sp.Count)
            //{
            //    iscurve = false;
            //    return ApproximateStraightAxis(skel_lines);
            //}
            //else
            return sp;
        }
        else
        {
            List<Vector2> sp = ApproximateStraightAxis(skel_lines);
            if (sp != null)
                return sp;
            else
            {
                iscurve = true;
                return ApproximateCurveAxis(skel_lines);
            }
        }
    }
    public List<Vector2> ExtractCubeSkeleton(out bool iscurve)
    {
        List<Line2> skel_lines = Skeletonize(out iscurve);
        iscube = true;
        iscurve = false;
        return ApproximateStraightAxis(skel_lines);
    }




    /*----------------------- OTHERS -------------------------*/

    private List<Vector2> ExtractOutline(Image<Gray, byte> image, List<Vector2> topOutline = null)
    {
        // not cut face
        if (topOutline == null)
        {
            List<Vector2> allBoudnary2 = IExtension.GetBoundary(image);
            return allBoudnary2;
        }

        // cut face
        double lineDistTheshold = 3;
        List<Vector2> ObjectOutline = IExtension.GetBoundary(image);
        List<Vector2> Boundary2 = new List<Vector2>();
        foreach (var p_obj in ObjectOutline)
        {
            bool IsColosed = false;
            foreach (var p_top in topOutline)
            {
                if (Vector2.Distance(p_obj, p_top) < lineDistTheshold)
                {
                    IsColosed = true;
                }
            }
            if (!IsColosed)
            {
                Boundary2.Add(p_obj);
            }
        }
        return Boundary2;
    }
    private int[] SelectTwoCorners(List<Vector2> cornerPoints2, List<Vector2> boundary2, Image<Gray, byte> bodyimg)
    {

        Image<Gray, byte> bodyshrink = bodyimg.Copy();
        var element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
        CvInvoke.Erode(bodyshrink, bodyshrink, element, new Point(-1, -1), 20, BorderType.Reflect, default(MCvScalar));
        bodyshrink.Save("bodyshrink.png");

        List<float> shortestDist = new List<float>();
        for (int i = 0; i < cornerPoints2.Count; i++)
        {
            float dist = float.MaxValue;
            foreach (var p in boundary2)
            {
                dist = Mathf.Min(Vector2.Distance(cornerPoints2[i], p), dist);
            }
            shortestDist.Add(dist);
        }
        List<float> sortedList = (from a in shortestDist orderby a ascending select a).Take(4).ToList();
        int[] i_corner = new int[2];
        i_corner[0] = shortestDist.IndexOf(sortedList[0]);
        int nexti = 1;
        do
        {
            i_corner[1] = shortestDist.IndexOf(sortedList[nexti++]);
            if (i_corner[0] == i_corner[1])
            {
                shortestDist.RemoveAt(i_corner[0]);
                i_corner[1] = shortestDist.IndexOf(sortedList[1]);

                if (i_corner[1] >= i_corner[0])
                    i_corner[1] += 1;
            }
        }
        while (i_corner[0] == i_corner[1] ||
                bodyshrink[(int)cornerPoints2[i_corner[1]].y, (int)cornerPoints2[i_corner[1]].x].Equals(new Gray(255)));

        return i_corner;
    }
    private List<Vector2> FindLongestPath(List<Vector2> skel_points, int startidx)
    {
        List<bool> packed = new List<bool>();
        for (int i = 0; i < skel_points.Count; i++)
        {
            packed.Add(false);
        }

        Stack<Vector2> Q = new Stack<Vector2>();
        //Queue<Vector2> Q = new Queue<Vector2>();
        List<Vector2> L = new List<Vector2>();
        List<int> L_idx = new List<int>();
        Q.Push(skel_points[startidx]);
        //Q.Enqueue(skel_points[startidx]);
        L.Add(skel_points[startidx]);
        packed[startidx] = true;

        List<List<Vector2>> pathes = new List<List<Vector2>>();

        while (Q.Count() > 0)
        {
            Vector2 p = Q.Pop();
            //Vector2 p = Q.Dequeue();

            List<int> neighbors = GetNeighbor(p, skel_points);
            // delete packed points
            for (int k = 0; k < neighbors.Count; k++)
            {
                if (packed[neighbors[k]] == true)
                {
                    neighbors.RemoveAt(k);
                    k--;
                }
            }

            if (neighbors.Count == 0)
            {
                pathes.Add(new List<Vector2>(L));
                L.Clear();
                L.Add(skel_points[startidx]);
                continue;
            }

            foreach (int ni in neighbors)
            {
                Q.Push(skel_points[ni]);
                //Q.Enqueue(skel_points[ni]);
                L.Add(skel_points[ni]);
                packed[ni] = true;
            }
        }

        pathes = pathes.OrderByDescending(x => x.Count()).ToList();
        return pathes.First();
    }
    private List<int> GetNeighbor(Vector2 v, List<Vector2> skel_points)
    {
        float x = v.x;
        float y = v.y;
        List<Vector2> possible_neighbors = new List<Vector2>();
        List<int> neighbors = new List<int>();
        if (x - 1 >= 0 && y - 1 >= 0) possible_neighbors.Add(new Vector2(x - 1, y - 1));
        if (x - 1 >= 0) possible_neighbors.Add(new Vector2(x - 1, y));
        if (x - 1 >= 0 && y + 1 < body_img.Height) possible_neighbors.Add(new Vector2(x - 1, y + 1));

        if (y - 1 >= 0) possible_neighbors.Add(new Vector2(x, y - 1));
        if (y + 1 <= body_img.Height) possible_neighbors.Add(new Vector2(x, y + 1));

        if (x + 1 < body_img.Width && y - 1 >= 0) possible_neighbors.Add(new Vector2(x + 1, y - 1));
        if (x + 1 < body_img.Width) possible_neighbors.Add(new Vector2(x + 1, y));
        if (x + 1 < body_img.Width && y + 1 < body_img.Height) possible_neighbors.Add(new Vector2(x + 1, y + 1));

        foreach (Vector2 nv in possible_neighbors)
        {
            if (ori_prune_img[(int)nv.y, (int)nv.x].Equals(new Gray(255)))
            {
                neighbors.Add(skel_points.IndexOf(nv));
            }
        }
        return neighbors;
    }
    private Line2 FitSimilarLine(List<Line2> lines, Line2 target, double angle = 20)
    {
        NumericalRecipes.RansacLine2d rcl = new NumericalRecipes.RansacLine2d();
        List<Vector2> linepoints = new List<Vector2>();
        linepoints.AddRange(target.SamplePoints());
        //ransac
        for (int i = 0; i < lines.Count; i++)
        {
            if (Line2.IsParallel(lines[i], target, angle))
            {
                if (target.DistanceToLine(lines[i].start) < 30 && target.DistanceToLine(lines[i].end) < 30)
                    linepoints.AddRange(lines[i].SamplePoints());
            }
        }
        Line2 bestline = rcl.Estimate(linepoints);
        if (bestline == null)
            return target;
        if (Vector2.Dot(bestline.dir, target.dir) < 0)
        {
            bestline.Flip();
        }
        return bestline;
    }
    private Line2 FitCloseLine(List<Line2> lines, Line2 target, double dis = 30)
    {
        List<Vector2> linepoints = new List<Vector2>();
        linepoints.AddRange(target.SamplePoints());
        //ransac
        for (int i = 0; i < lines.Count; i++)
        {
            if (Line2.IsParallel(lines[i], target, 45))
            {
                double d = double.MaxValue;
                foreach (Vector2 v in linepoints)
                    d = Math.Min(Math.Min(Vector2.Distance(lines[i].start, v), Vector2.Distance(lines[i].end, v)), d);
                if (d < dis)
                    linepoints.AddRange(lines[i].SamplePoints());
            }
        }

        // least square
        //Vector2 dir = Utility.GetDirection(linepoints);
        //Line2 bestline = new Line2(linepoints[0], dir, true);
        //foreach (Vector2 v in linepoints)
        //    bestline.UpdateEnd(v);

        //ransac
        NumericalRecipes.RansacLine2d rcl = new NumericalRecipes.RansacLine2d();
        Line2 bestline = rcl.Estimate(linepoints);
        if (bestline == null)
            return target;

        if (Vector2.Dot(bestline.dir, target.dir) < 0)
        {
            bestline.Flip();
        }
        return bestline;
    }

    private Image<Gray, byte> HitOrMiss(Image<Gray, byte> skel, Mat c, Mat d)
    {
        Image<Gray, byte> skel_hm = skel.Copy();
        Image<Gray, byte> temp2 = new Image<Gray, byte>(skel.Size);
        Image<Gray, byte> temp = new Image<Gray, byte>(skel.Size);

        CvInvoke.Erode(skel_hm, temp, c, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
        CvInvoke.BitwiseNot(skel_hm, temp2);
        CvInvoke.Erode(temp2, temp2, d, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
        CvInvoke.BitwiseAnd(temp, temp2, skel_hm);
        return skel_hm;
    }
    private bool IsCurveAxis(List<Line2> skel_lines, Line2 main_axis_straight)
    {
        List<Vector2> skel_points = new List<Vector2>();
        foreach (Line2 sl in skel_lines)
            skel_points.AddRange(sl.SamplePoints());
        int onlinecount = 0;
        foreach (Vector2 p in skel_points)
            if (main_axis_straight.DistanceToLine(p) < 2)
                onlinecount++;
        double ratio = onlinecount * 1.0 / skel_points.Count;
        Debug.Log("is curve ratio: " + ratio);
        if (ratio <= 0.4)
            return true;
        else
            return false;
    }
}


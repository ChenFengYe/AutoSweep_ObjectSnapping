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
using NumericalRecipes;
public enum Label
{
    CubeFace, CylinderFace, CubeBody, CylinderBody, Handle, Background, Cube, Cylinder, None
}

public class Geometry
{
    private List<Vector2> body = new List<Vector2>();
    private List<List<Vector2>> faces = new List<List<Vector2>>();
    private List<List<Vector2>> handles = new List<List<Vector2>>();

    public Vector2 ori_body_center = new Vector2();
    public Vector2[] body_axis = new Vector2[2];
    public Vector2 min_bodyV = new Vector2();
    public Vector2 max_bodyV = new Vector2();

    public Label label = Label.None;
    public int width, height;
    public Geometry(int width, int height, Label label)
    {
        this.width = width;
        this.height = height;
        this.label = label;
    }
    public Geometry() { }
    public Image<Rgb, byte> GetBodyImage(bool withhandle = false)
    {
        Image<Rgb, byte> body_img = new Image<Rgb, byte>(width, height);
        body_img.SetZero();
        foreach (Vector2 p in this.body)
        {
            body_img[(int)p.y, (int)p.x] = new Rgb(255, 255, 255);
        }

        MaskParser.FillGap(body_img);
        //MaskParser.FillHole(body_img);

        if (withhandle)
        {
            List<Image<Rgb, byte>> handles = this.GetHandleImage();
            foreach (Image<Rgb, byte> hi in handles)
            {
                hi.SetValue(new Rgb(255, 0, 0), hi.Convert<Gray, byte>());
                CvInvoke.BitwiseOr(body_img, hi, body_img);
            }
            MaskParser.FillGap(body_img, 5);
            body_img.Save("xxx.png");
        }


        //if (label == Label.Cylinder)
        //{
        //    body_img = MaskParser.FillBodyWithHandle(body_img, this.GetHandleImage());
        //}
        return body_img;
    }
    public List<Image<Rgb, byte>> GetFaceImage()
    {
        List<Image<Rgb, byte>> img_list = new List<Image<Rgb, byte>>();

        foreach (List<Vector2> face in this.faces)
        {
            Image<Rgb, byte> face_img = new Image<Rgb, byte>(width, height);
            face_img.SetZero();
            foreach (Vector2 p in face)
            {
                face_img[(int)p.y, (int)p.x] = new Rgb(255, 255, 255);
            }

            img_list.Add(face_img);

        }
        return img_list;
    }
    public List<Image<Rgb, byte>> GetHandleImage()
    {
        List<Image<Rgb, byte>> img_list = new List<Image<Rgb, byte>>();
        foreach (List<Vector2> handle in this.handles)
        {
            Image<Rgb, byte> handle_img = new Image<Rgb, byte>(width, height);
            handle_img.SetZero();
            foreach (Vector2 p in handle)
            {
                handle_img[(int)p.y, (int)p.x] = new Rgb(255, 255, 255);
            }
            img_list.Add(handle_img);
        }
        return img_list;
    }
    public Image<Rgb, byte> GetBoundaryImage()
    {
        Image<Rgb, byte> boundimg = this.GetBodyImage();
        List<Image<Rgb, byte>> handleimgs = this.GetHandleImage();
        foreach (Image<Rgb, byte> x in handleimgs)
            CvInvoke.BitwiseOr(boundimg, x, boundimg);
        Image<Gray, Byte> cannyimg = boundimg.Canny(60, 100);
        var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));
        CvInvoke.Dilate(cannyimg, cannyimg, element, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
        boundimg.SetZero();
        boundimg.SetValue(new Rgb(0, 0, 255), cannyimg);

        return boundimg;
    }
    public List<Vector2> GetBoundaryPoints()
    {
        return IExtension.GetBoundary(body, width, height);
    }

    ///
    public List<List<Vector2>> Faces() { return faces; }
    public List<Vector2> Body() { return body; }
    public void AddFace(List<Vector2> a_face)
    {
        this.faces.Add(a_face);
        this.body.AddRange(a_face); //用top face 填充body image
    }
    public void AddHandle(List<Vector2> a_handle)
    {
        this.handles.Add(a_handle);
        //this.body.AddRange(a_handle);
    }
    public void SetInitialBody(List<Vector2> ini_body)
    {
        this.body = ini_body;
        List<Vector2> body_boundary = this.GetBoundaryPoints();

        #region PCA find axis
        Vector2 mean = new Vector2();
        foreach (Vector2 p in body_boundary)
            mean += p;

        mean /= body_boundary.Count;
        float W = body_boundary.Count;

        float[,] C = new float[2, 2];
        foreach (Vector2 point in body_boundary)
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
        SVD svd = new SVD(C);
        //svd.w - eigenvalue, start from 1
        //svd.u - eigenvector, start from 1

        int max = 1, min = 2;
        if (svd.w[max] < svd.w[min])
        {
            int temp = max;
            max = min;
            min = temp;
        }

        Vector2 majoraxis = new Vector2(svd.u[1, max], svd.u[2, max]);
        majoraxis.Normalize();
        Vector2 minoraxis = new Vector2(svd.u[1, min], svd.u[2, min]);
        minoraxis.Normalize();
        #endregion

        this.ori_body_center = mean;
        this.body_axis[0] = majoraxis;
        this.body_axis[1] = minoraxis;

        // axis change
        // center to ori_body_center
        // x(0,1) to body_axis[0]
        // y(1,0) to body_axis[1]

        float theta = Vector2.Angle(new Vector2(0, 1), body_axis[0]);
        float cos_theta = Mathf.Cos(theta * Mathf.PI / 180.0f);
        float sin_theta = Mathf.Sin(theta * Mathf.PI / 180.0f);

        List<Vector2> new_axis_body_boundary = new List<Vector2>();
        foreach (Vector2 p in body_boundary)
        {
            float x = p.x;
            float y = p.y;
            // rotate
            float n_x = x * cos_theta + y * sin_theta;
            float n_y = y * cos_theta - x * sin_theta;
            // translate
            n_x = n_x - ori_body_center.x;
            n_y = n_y - ori_body_center.y;
            new_axis_body_boundary.Add(new Vector2(n_x, n_y));
        }

        min_bodyV = Utility.MinV(new_axis_body_boundary);
        max_bodyV = Utility.MaxV(new_axis_body_boundary);

    }

}


public class MaskParser
{
    Image<Rgb, byte> img;
    List<Geometry> geometries = new List<Geometry>();
    public MaskParser(Image<Rgb, byte> rawmask)
    {
        this.img = rawmask;
        NOT_CONNECTED_DIS_THRES = img.Height * 0.03f;
        geometries.Clear();
    }

    List<List<Vector2>> instance = new List<List<Vector2>>();
    List<List<Vector2>> instance_boundary = new List<List<Vector2>>();
    List<Label> instance_label = new List<Label>();
    List<Rgb> instance_color = new List<Rgb>();

    static private double MAX_COST = 2;
    static public float NOT_CONNECTED_DIS_THRES = 30;
    public List<Geometry> Parse()
    {
        geometries.Clear();
        IsolateInstance();
        AlignFaceWithBody();
        AlignHandleWithBody();

        #region save image
        //Debug.Log("saving images....");

        if (geometries.Count == 0)
            Debug.Log("No body detected!");

        Image<Rgb, byte> instance_img = new Image<Rgb, byte>(img.Width, img.Height);
        //instance_img.SetValue(new Rgba(255, 255, 255, 255));
        instance_img.SetZero();
        for (int j = 0; j < geometries.Count; j++)
        {
            Image<Rgb, byte> body_img = geometries[j].GetBodyImage();
            Image<Rgb, byte> bound_img = geometries[j].GetBoundaryImage();
            List<Image<Rgb, byte>> face_imgs = geometries[j].GetFaceImage();
            List<Image<Rgb, byte>> handle_imgs = geometries[j].GetHandleImage();

            //// checking
            if (body_img == null) { Debug.Log("No body!"); geometries.RemoveAt(j); j--; continue; }
            if (face_imgs.Count == 0) { Debug.Log("No Face!"); geometries.RemoveAt(j); j--; continue; }

            instance_img.SetValue(new Rgb(31, 120, 180), body_img.Convert<Gray, byte>());
            for (int i = 0; i < face_imgs.Count; i++)
            {
                instance_img.SetValue(new Rgb(166, 206, 227), face_imgs[i].Convert<Gray, byte>());
            }
            for (int i = 0; i < handle_imgs.Count; i++)
            {
                instance_img.SetValue(new Rgb(178, 223, 138), handle_imgs[i].Convert<Gray, byte>());
            }
            CvInvoke.Add(instance_img, bound_img, instance_img);
        }
        //CvInvoke.AddWeighted(instance_img, 1.0, img, 0.8, 0, instance_img);

        img.Save("instance_mask.png");
        instance_img.Save("instance_labelling.png");

        #endregion save image

        return this.geometries;
    }


    public void IsolateInstance()
    {
        #region classify
        for (int i = 0; i < img.Height; i++)
        {
            for (int j = 0; j < img.Width; j++)
            {
                Label pixellabel = ClassifyPixelColor(img[i, j]);
                // if this pixel is foreground
                if (pixellabel != Label.Background && pixellabel != Label.None)
                {
                    int instance_idx = instance_color.IndexOf(img[i, j]);
                    // this pixel belongs to which instance
                    if (instance_idx == -1)
                    {
                        instance_color.Add(img[i, j]);
                        instance.Add(new List<Vector2>());
                        instance_label.Add(pixellabel);
                        instance[instance.Count - 1].Add(new Vector2(j, i));
                    }
                    else
                        instance[instance_idx].Add(new Vector2(j, i));
                }
            }
        }
        #endregion

        #region check valid
        for (int i = 0; i < instance.Count; i++)
        {
            if (!IsInstanceValid(instance[i]))
            {
                instance.RemoveAt(i);
                instance_label.RemoveAt(i);
                instance_color.RemoveAt(i);
                i--;
            }
        }
        #endregion

        #region Boundary extraction & seperate body with others
        List<List<Vector2>> body = new List<List<Vector2>>();
        List<List<Vector2>> body_boundary = new List<List<Vector2>>();
        List<Label> body_label = new List<Label>();

        for (int i = 0; i < instance.Count; i++)
        {
            //Image<Rgb, byte> instance_img = img.CopyBlank();
            //for (int j = 0; j < instance[i].Count; j++)
            //    instance_img[(int)instance[i][j].y, (int)instance[i][j].x] = new Rgb(255, 255, 255);
            //instance_img.Save(instance_label[i].ToString() + "_" + i.ToString() + ".png");

            if (instance_label[i] == Label.CubeBody || instance_label[i] == Label.CylinderBody)
            {
                body.Add(new List<Vector2>(instance[i]));
                body_label.Add(instance_label[i]);
                body_boundary.Add(IExtension.GetBoundary(instance[i], img.Width, img.Height));

                instance.RemoveAt(i);
                instance_label.RemoveAt(i);
                instance_color.RemoveAt(i);
                i--;
            }
            else
                instance_boundary.Add(IExtension.GetBoundary(instance[i], img.Width, img.Height));

        }
        #endregion

        #region create geometry
        for (int i = 0; i < body.Count; i++)
        {
            if (body_label[i] == Label.CubeBody)
            {
                Geometry a_cuboid = new Geometry(img.Width, img.Height, Label.Cube);
                a_cuboid.SetInitialBody(body[i]);
                geometries.Add(a_cuboid);
                //geometries.Last().GetBodyImage().Save("cube_body.png");
            }
            else if (body_label[i] == Label.CylinderBody)
            {
                Geometry a_cylinder = new Geometry(img.Width, img.Height, Label.Cylinder);
                a_cylinder.SetInitialBody(body[i]);
                geometries.Add(a_cylinder);
                //geometries.Last().GetBodyImage().Save("cylinder_body.png");
            }
        }
        #endregion

    }

    public void AlignFaceWithBody()
    {
        //double[,] pairwise_cost = new double[instance.Count, geometries.Count];
        //for (int i = 0; i < instance.Count; i++)
        //{
        //    for (int k = 0; k < geometries.Count; k++)
        //    {
        //        for (int j = 0; j < instance.Count; j++)
        //        {
        //            pairwise_cost[i, k] += Cost(i, j, k);
        //        }
        //        pairwise_cost[i, k] /= instance.Count;
        //    }
        //}

        for (int i = 0; i < instance.Count; i++)
        {
            if (instance_label[i] != Label.Handle)
            {
                double mincost = MAX_COST;
                int instance_body_label = -1;
                for (int j = 0; j < geometries.Count; j++)
                {
                    double e = Cost(i, j);
                    if (e < mincost)
                    {
                        mincost = e;
                        instance_body_label = j;
                    }
                }
                // found body
                if (instance_body_label != -1)
                    geometries[instance_body_label].AddFace(instance[i]);
            }
        }

        // body that has no face->handle
        for (int i = 0; i < geometries.Count; i++)
        {
            if (geometries[i].Faces().Count <= 0) // make it handle
            {
                instance_color.Add(img[(int)geometries[i].Body().First().y, (int)geometries[i].Body().First().x]);
                instance.Add(geometries[i].Body());
                instance_label.Add(Label.Handle);
                instance_boundary.Add(geometries[i].GetBoundaryPoints());
                geometries.RemoveAt(i);
                i--;
            }
        }
    }

    public void AlignHandleWithBody()
    {
        for (int i = 0; i < instance.Count; i++)
        {
            if (instance_label[i] == Label.Handle)
            {
                double mincost = MAX_COST;
                int instance_body_label = -1;
                for (int j = 0; j < geometries.Count; j++)
                {
                    double e = Cost(i, j);
                    if (e < mincost)
                    {
                        mincost = e;
                        instance_body_label = j;
                    }
                }

                // found body
                if (instance_body_label != -1)
                    geometries[instance_body_label].AddHandle(instance[i]);
            }
        }
    }

    // instance i for body j
    public double Cost(int I_instance, int J_body)
    {
        Label instance_label = this.instance_label[I_instance];
        Label body_label = this.geometries[J_body].label;
        List<Vector2> body_boundary = this.geometries[J_body].GetBoundaryPoints();

        // same category
        if (body_label == Label.Cube && instance_label == Label.CylinderFace) return double.MaxValue;
        if (body_label == Label.Cylinder && instance_label == Label.CubeFace) return double.MaxValue;
        //if (instance_label == Label.Handle && body_label != Label.Cylinder) return double.MaxValue;
        double cost = double.MaxValue;

        double dis = Utility.DistanceSet2Set(instance_boundary[I_instance], body_boundary);
        double c1 = double.MaxValue;
        if (dis < NOT_CONNECTED_DIS_THRES)
        {
            float dis_to = (float)dis / NOT_CONNECTED_DIS_THRES;
            float sigma_sqr_1 = 0.15f;
            float miu = 1f;
            //c1 = Mathf.Exp(-(dis_to - miu) * (dis_to - miu) / (2 * sigma_sqr_1));
            c1 = dis_to;
        }


        // rotate instance to body axis
        float theta = Vector2.Angle(new Vector2(0, 1), this.geometries[J_body].body_axis[0]);
        float cos_theta = Mathf.Cos(theta * Mathf.PI / 180.0f);
        float sin_theta = Mathf.Sin(theta * Mathf.PI / 180.0f);

        List<Vector2> new_axis_instance = new List<Vector2>();
        foreach (Vector2 p in this.instance_boundary[I_instance])
        {
            float x = p.x;
            float y = p.y;
            // rotate
            float n_x = x * cos_theta + y * sin_theta;
            float n_y = y * cos_theta - x * sin_theta;
            // translate
            n_x = n_x - this.geometries[J_body].ori_body_center.x;
            n_y = n_y - this.geometries[J_body].ori_body_center.y;
            new_axis_instance.Add(new Vector2(n_x, n_y));
        }

        // above body or below body
        int in_x_axis = 0;
        int in_y_axis = 0;
        int in_bb = 0;
        Vector2 minV = this.geometries[J_body].min_bodyV;
        Vector2 maxV = this.geometries[J_body].max_bodyV;

        // enlarge bbx 
        float window = Mathf.Max(maxV.x - minV.x, maxV.y - minV.y) * 0.1f;
        minV = minV - new Vector2(window, window);
        maxV = maxV + new Vector2(window, window);

        foreach (Vector2 p in new_axis_instance)
        {
            //if (p.x >= minV.x && p.x <= maxV.x) 
            //    in_x_axis++;
            //if (p.y >= minV.y && p.y <= maxV.y)
            //    in_y_axis++;

            if ((p.x >= minV.x && p.x <= maxV.x) && (p.y >= minV.y && p.y <= maxV.y))
                in_bb++;
        }
        //float xx = in_x_axis * 1.0f / new_axis_instance.Count + in_y_axis * 1.0f / new_axis_instance.Count;
        float xx = in_bb * 1.0f / new_axis_instance.Count;
        //float sigma_sqr_2 = 2f;
        float sigma_sqr_2 = 0.3f;

        double c2 = Mathf.Exp(-(xx * xx) / (2 * sigma_sqr_2 * sigma_sqr_2));
        cost = 0.3 * c1 + 0.7 * c2;
        // Debug.Log("instance: " + I_instance.ToString() + ",body: " + J_body.ToString() + ",cost: " + c1.ToString() + " , " + c2.ToString() + "(" + in_x_axis * 1.0 / new_axis_instance.Count + "," + in_y_axis * 1.0 / new_axis_instance.Count + " )" + " , " + cost.ToString());
        return cost;
    }

    //// instance i and j for same body k
    //public double Cost(int I_instance, int J_instance, int K_body)
    //{
    //    //List<Vector2> instance_points = this.instance[I_instance];
    //    Label i_instance_label = this.instance_label[I_instance];
    //    Label j_instance_label = this.instance_label[J_instance];
    //    //List<Vector2> body_points = this.body[K_body];
    //    Label body_label = this.geometries[K_body].label;
    //    List<Vector2> body_boundary = this.geometries[K_body].GetBoundaryPoints();

    //    if (I_instance == J_instance) return 0;
    //    if (i_instance_label != j_instance_label) return 1;
    //    if (i_instance_label == Label.CubeFace || j_instance_label == Label.CubeFace) return 1;

    //    // both cylinder face
    //    Vector2 i_instance_center = Utility.Average(instance_boundary[I_instance]);
    //    Vector2 j_instance_center = Utility.Average(instance_boundary[J_instance]);
    //    Vector2 k_body_center = Utility.Average(body_boundary);

    //    // one one line
    //    Line2 start_end = new Line2(i_instance_center, j_instance_center);
    //    double dis = start_end.DistanceToLine(k_body_center);

    //    // or curve
    //    /// no idea how to do this????

    //    double c1 = double.MaxValue;
    //    if (dis < NOT_CONNECTED_DIS_THRES)
    //    {
    //        float dis_to = (float)dis / NOT_CONNECTED_DIS_THRES;
    //        float sigma_sqr_1 = 0.15f;
    //        float miu = 1f;
    //        //c1 = Mathf.Exp(-(dis_to - miu) * (dis_to - miu) / (2 * sigma_sqr_1));
    //        c1 = dis_to;
    //    }
    //    return c1;
    //}


    public bool IsInstanceValid(List<Vector2> instance)
    {
        //if (instance.Count < 20) return false;
        Image<Gray, byte> img = new Image<Gray, byte>(this.img.Width, this.img.Height);
        img.SetZero();
        foreach (Vector2 p in instance)
        {
            img[(int)p.y, (int)p.x] = new Gray(255);
        }

        VectorOfVectorOfPoint con = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(img, con, img, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
        int maxcomp = 0;
        for (int i = 0; i < con.Size; i++)
        {
            if (con[i].Size > maxcomp)
                maxcomp = con[i].Size;
        }
        if (maxcomp < 20) return false;
        return true;
    }


    //-----------------------------------------------static functions

    public static void FillGap(Image<Rgb, byte> img, int iteration = 10)
    {
        var element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));

        // padding image to avoid meeting edge
        Image<Rgb, byte> larger_img = new Image<Rgb, byte>(img.Width + iteration * 2, img.Height + iteration * 2);
        larger_img.SetZero();
        for (int i = iteration; i < img.Height; i++)
        {
            for (int j = iteration; j < img.Width; j++)
            {
                larger_img[i, j] = img[i - iteration, j - iteration];
            }
        }

        CvInvoke.Dilate(larger_img, larger_img, element, new Point(-1, -1), iteration, BorderType.Reflect, default(MCvScalar));
        CvInvoke.Erode(larger_img, larger_img, element, new Point(-1, -1), iteration, BorderType.Reflect, default(MCvScalar));

        for (int i = iteration; i < img.Height; i++)
        {
            for (int j = iteration; j < img.Width; j++)
            {
                img[i - iteration, j - iteration] = larger_img[i, j];
            }
        }
    }

    public static void FillGap(Image<Gray, byte> img, int iteration = 10)
    {
        var element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));

        // padding image to avoid meeting edge
        Image<Gray, byte> larger_img = new Image<Gray, byte>(img.Width + iteration * 2, img.Height + iteration * 2);
        larger_img.SetZero();
        for (int i = iteration; i < img.Height; i++)
        {
            for (int j = iteration; j < img.Width; j++)
            {
                larger_img[i, j] = img[i - iteration, j - iteration];
            }
        }

        CvInvoke.Dilate(larger_img, larger_img, element, new Point(-1, -1), iteration, BorderType.Reflect, default(MCvScalar));
        CvInvoke.Erode(larger_img, larger_img, element, new Point(-1, -1), iteration, BorderType.Reflect, default(MCvScalar));

        for (int i = iteration; i < img.Height; i++)
        {
            for (int j = iteration; j < img.Width; j++)
            {
                img[i - iteration, j - iteration] = larger_img[i, j];
            }
        }
    }

    public static void FillHole(Image<Rgb, byte> img)
    {
        Image<Gray, byte> img_copy = img.Copy().Convert<Gray, byte>();
        VectorOfVectorOfPoint con = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(img_copy, con, img_copy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
        int maxcomp = 0;
        int idx = 0;
        for (int i = 0; i < con.Size; i++)
        {
            if (con[i].Size > maxcomp)
            {
                maxcomp = con[i].Size;
                idx = i;
            }
        }

        CvInvoke.DrawContours(img, con, idx, new MCvScalar(255, 255, 255), -1);
    }

    public static Image<Rgb, byte> FillBodyWithHandle(Image<Rgb, byte> body_img, List<Image<Rgb, byte>> handle_imgs)
    {
        int padding = (int)NOT_CONNECTED_DIS_THRES;
        if (handle_imgs.Count == 0) return body_img;
        Image<Rgb, byte> handle_img_merge = body_img.CopyBlank();
        handle_img_merge.SetZero();
        foreach (Image<Rgb, byte> img in handle_imgs)
            CvInvoke.BitwiseOr(img, handle_img_merge, handle_img_merge);
        var element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
        CvInvoke.Dilate(handle_img_merge, handle_img_merge, element, new Point(-1, -1), padding, BorderType.Reflect, default(MCvScalar));
        Image<Rgb, byte> to_fill = body_img.CopyBlank();
        CvInvoke.BitwiseAnd(body_img, handle_img_merge, to_fill);
        FillGap(to_fill, padding * 3);
        CvInvoke.BitwiseOr(body_img, to_fill, body_img);
        return body_img;
        #region old solution
        //// padding img
        //Image<Rgb, byte> larger_img = new Image<Rgb, byte>(body_img.Width + padding * 2, body_img.Height + padding * 2);
        //larger_img.SetZero();
        //for (int i = padding; i < body_img.Height; i++)
        //{
        //    for (int j = padding; j < body_img.Width; j++)
        //    {
        //        larger_img[i, j] = body_img[i - padding, j - padding];
        //    }
        //}

        //// dilation
        //for (int i = padding; i < body_img.Height; i++)
        //{
        //    for (int j = padding; j < body_img.Width; j++)
        //    {
        //        double dis = body_points_dis2handle[i - padding, j - padding];

        //        if (dis != double.MaxValue)
        //        {
        //            for (int k = (int)-dis / 2; k < dis; k++)
        //            {
        //                // rectangle
        //                for (int m = (int)-dis / 2; m < dis; m++)
        //                {
        //                    larger_img[i + k, j + m] = new Rgb(255, 255, 255);
        //                }

        //                //// cross
        //                //larger_img[i - k, j - k] = new Rgb(255, 255, 255);
        //                //larger_img[i - k, j + k] = new Rgb(255, 255, 255);
        //                //larger_img[i + k, j - k] = new Rgb(255, 255, 255);
        //                //larger_img[i + k, j + k] = new Rgb(255, 255, 255);
        //            }
        //        }
        //    }
        //}

        //larger_img.Save("fillhole_dilation_larger.png");

        //// update distance
        //for (int i = padding; i < body_img.Height; i++)
        //{
        //    for (int j = padding; j < body_img.Width; j++)
        //    {
        //        Vector2 point_in_ori_img = new Vector2(j - padding, i - padding);
        //        double dis_after_dilation = Utility.DistancePoint2Set(point_in_ori_img, handle_points);
        //        if (dis_after_dilation <= padding)
        //            body_points_dis2handle[i - padding, j - padding] = dis_after_dilation;
        //    }
        //}

        //// erosion
        //Image<Rgb, byte> erosion_img = new Image<Rgb, byte>(body_img.Width + padding * 2, body_img.Height + padding * 2);
        //for (int i = padding; i < body_img.Height; i++)
        //{
        //    for (int j = padding; j < body_img.Width; j++)
        //    {
        //        bool save = true;
        //        double dis = body_points_dis2handle[i - padding, j - padding];
        //        if (dis != double.MaxValue)
        //        {
        //            for (int k = (int)-dis / 2; k < dis; k++)
        //            {
        //                // rectangle
        //                for (int m = (int)-dis / 2; m < dis; m++)
        //                {
        //                    if (!larger_img[i + k, j + m].Equals(new Rgb(255, 255, 255)))
        //                    {
        //                        save = false;
        //                    }
        //                }
        //            }
        //        }
        //        if (save)
        //            erosion_img[i, j] = larger_img[i, j];
        //    }
        //}

        //larger_img.Save("fillhole_erosion_larger.png");

        //// save back to body_img
        //for (int i = padding; i < erosion_img.Height; i++)
        //{
        //    for (int j = padding; j < erosion_img.Width; j++)
        //    {
        //        erosion_img[i - padding, j - padding] = larger_img[i, j];
        //    }
        //}

        #endregion
    }

    public static Label ClassifyPixelColor(Rgb c)
    {
        switch ((int)c.Blue)
        {
            case 255:
                if (c.Green == 0 && c.Red % 10 == 0 && c.Red != 0)
                    return Label.CubeBody;
                else if (c.Red % 10 == 0 && c.Green == c.Red && c.Red != 0)
                    return Label.CubeFace;
                break;
            case 200:
                if (c.Green == 0 && c.Red % 10 == 0 && c.Red != 0)
                    return Label.CylinderBody;
                else if (c.Red % 10 == 0 && c.Green == c.Red && c.Red != 0)
                    return Label.CylinderFace;
                break;
            case 150:
                if (c.Green == 0 && c.Red % 10 == 0 && c.Red != 0)
                    //return Label.Handle;
                    return Label.CylinderBody;
                break;
        }
        if (c.Red == 0 && c.Green == 0 && c.Blue == 0)
            return Label.Background;
        else
            return Label.None;
    }

    public static void VectorizeImage(Image<Rgb, byte> img)
    {
        for (int i = 0; i < img.Height; i++)
        {
            for (int j = 0; j < img.Width; j++)
            {
                Label pixellabel = ClassifyPixelColor(img[i, j]);
                if (pixellabel == Label.None)
                    img[i, j] = new Rgb(255, 255, 255);
            }
        }
        for (int i = 0; i < img.Height; i++)
        {
            for (int j = 0; j < img.Width; j++)
            {
                if (img[i, j].Equals(new Rgb(255, 255, 255)))
                {
                    for (int window = 1; window < 5; window++)
                    {
                        if (i - window >= 0)
                        {
                            if (ClassifyPixelColor(img[i - window, j]) != Label.None)
                            {
                                img[i, j] = img[i - window, j];
                                break;
                            }
                            if (j - window >= 0)
                                if (ClassifyPixelColor(img[i - window, j - window]) != Label.None)
                                {
                                    img[i, j] = img[i - window, j - window];
                                    break;
                                }
                            if (j + window < img.Width)
                                if (ClassifyPixelColor(img[i - window, j + window]) != Label.None)
                                {
                                    img[i, j] = img[i - window, j + window];
                                    break;
                                }
                        }
                        if (i + window < img.Height)
                        {
                            if (ClassifyPixelColor(img[i + window, j]) != Label.None)
                            {
                                img[i, j] = img[i + window, j];
                                break;
                            }
                            if (j - window >= 0)
                                if (ClassifyPixelColor(img[i + window, j - window]) != Label.None)
                                {
                                    img[i, j] = img[i + window, j - window];
                                    break;
                                }
                            if (j + window < img.Width)
                                if (ClassifyPixelColor(img[i + window, j + window]) != Label.None)
                                {
                                    img[i, j] = img[i + window, j + window];
                                    break;
                                }
                        }
                        if (j + window < img.Width)
                            if (ClassifyPixelColor(img[i, j + window]) != Label.None)
                            {
                                img[i, j] = img[i, j + window];
                                break;
                            }
                        if (j - window >= 0)
                            if (ClassifyPixelColor(img[i, j - window]) != Label.None)
                            {
                                img[i, j] = img[i, j - window];
                                break;
                            }
                    }
                }
            }
        }

    }
}

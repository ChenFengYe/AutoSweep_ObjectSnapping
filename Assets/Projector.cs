using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGeometry;
using UnityEngine.UI;

public class Projector : MonoBehaviour
{
    public Canvas m_canvas;
    public Camera m_mainCamera;
    public Image m_canvasImg;

    private Rect canvasRect3d;
    private float imagewidth, imageheight;
    float m_canvas_heigth_origon = 0;
    float m_canvas_scale = 0;

    void Start()
    {
        m_canvas_heigth_origon = Mathf.Tan(Mathf.Deg2Rad * m_mainCamera.fieldOfView / 2f) / m_canvas.transform.lossyScale.x;
        m_canvas_scale = m_canvas.transform.lossyScale.x;
    }

    void Update()
    {

    }

    public void UpdateProjector()
    {
        m_canvas_scale = Mathf.Tan(Mathf.Deg2Rad * m_mainCamera.fieldOfView / 2f) / m_canvas_heigth_origon;
        float height3D = m_canvas.pixelRect.height * m_canvas_scale;
        float width3D = m_canvas.pixelRect.width * m_canvas_scale;

        Vector3 anchor = m_canvas.transform.position - new Vector3(width3D / 2.0f, height3D / 2.0f, 0);
        canvasRect3d = new Rect(anchor, new Vector2(width3D, height3D));

        imagewidth = m_canvasImg.rectTransform.sizeDelta.x;
        imageheight = m_canvasImg.rectTransform.sizeDelta.y;
    }

    public Vector2 WorldToImage(Vector3 worldp, bool clamp = false)
    {
        UpdateProjector();

        // Need canvas size to clamp point position
        Plane canvasplane = new Plane((m_mainCamera.transform.position - m_canvas.transform.position).normalized, m_canvas.transform.position);
        Vector3 imgp = Utility.PlaneRayIntersect(canvasplane,
            new Ray(m_mainCamera.transform.position, (worldp - m_mainCamera.transform.position).normalized));

        //// clamping
        if (clamp)
        {
            imgp.x = Mathf.Max(imgp.x, canvasRect3d.xMin);
            imgp.x = Mathf.Min(imgp.x, canvasRect3d.xMax);
            imgp.y = Mathf.Max(imgp.y, canvasRect3d.yMin);
            imgp.y = Mathf.Min(imgp.y, canvasRect3d.yMax);
        }
        // move axes center to top left
        float imagewidth3d = imagewidth * m_canvas_scale;
        float imageheight3d = imageheight * m_canvas_scale;
        imgp.x += 0.5f * imagewidth3d;
        imgp.y -= 0.5f * imageheight3d;
        imgp.y = -imgp.y;
        // change to 2d scale
        imgp.x = imgp.x / m_canvas_scale;
        imgp.y = imgp.y / m_canvas_scale;

        return new Vector2(imgp.x, imgp.y);
    }

    public Vector2 WorldToImage_v(Vector3 worldv)
    {
        UpdateProjector();

        worldv = worldv.normalized;
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = p1 + worldv * 10;
        Vector2 imgv = (WorldToImage(p2) - WorldToImage(p1)).normalized;

        return new Vector2(imgv.x, imgv.y);
    }

    public Vector3 ImageToWorld(Vector2 imgp)
    {
        UpdateProjector();

        Vector3 worldp = new Vector3(imgp.x, imgp.y, m_canvas.transform.position.z);
        
        // change to world scale
        worldp.x -= 0.5f * imagewidth;
        worldp.y -= 0.5f * imageheight;
        worldp.y = -worldp.y;

        worldp.x = worldp.x * m_canvas_scale;
        worldp.y = worldp.y * m_canvas_scale;
    
        return worldp;
    }

    public List<Vector3> Proj2dToPlane(Plane plane, List<Vector2> points)
    {
        Vector3 camera = m_mainCamera.transform.position;
        List<Vector3> output = new List<Vector3>();
        foreach (Vector2 p in points)
        {
            Vector3 p3 = this.ImageToWorld(p);
            Ray ray = new Ray(camera, (p3 - camera).normalized);
            output.Add(Utility.PlaneRayIntersect(plane, ray));
        }
        return output;
    }

    public Vector3 Proj2dToPlane(Plane plane, Vector2 p)
    {
        Vector3 camera = m_mainCamera.transform.position;
        Vector3 p3 = this.ImageToWorld(p);
        Ray ray = new Ray(camera, (p3 - camera).normalized);
        return Utility.PlaneRayIntersect(plane, ray);
    }

    # region Project Vector
    //  If vector begion point is not same, then the vector(be projected) is not same 
    public Vector3 Proj2dToPlane_v(Plane plane, Vector2 startPoint, Vector2 dire)
    {
        float offset = 5;
        Vector2 end_p = startPoint + offset * dire;
        Vector3 worldv = (Proj2dToPlane(plane, end_p) - Proj2dToPlane(plane, startPoint)).normalized;
        return worldv;
    }

    public List<Vector3> Proj2dToPlane_v(Plane plane, List<Vector2> startPoints, List<Vector2> dires)
    {
        List<Vector3> dires3 = new List<Vector3>();
        if (startPoints.Count != dires.Count) Debug.Log("Error: No matching Start Point!");
        for (int i = 0; i < dires.Count; i++)
            dires3.Add(Proj2dToPlane_v(plane, startPoints[i], dires[i]));
        return dires3;
    }
    # endregion

    public List<Vector2> Proj3dToImage(List<Vector3> points_3d)
    {
        List<Vector2> points_2d = new List<Vector2>();
        for (int i = 0; i < points_3d.Count; i++)
        {
            points_2d.Add(this.WorldToImage(points_3d[i]));
        }
        return points_2d;
    }

    public Ray GenerateRay(Vector2 imgp)
    {
        return new Ray(m_mainCamera.transform.position, (this.ImageToWorld(imgp) - m_mainCamera.transform.position).normalized);
    }

    public List<Vector2> WorldToImage(List<Vector3> points_3d)
    {
        List<Vector2> points_2d = new List<Vector2>();
        for (int i = 0; i < points_3d.Count; i++)
        {
            points_2d.Add(this.WorldToImage(points_3d[i]));
        }
        return points_2d;
    }

    public List<Vector3> ImageToWorld(List<Vector2> points_2d)
    {
        List<Vector3> points_3d = new List<Vector3>();
        for (int i = 0; i < points_2d.Count; i++)
        {
            points_3d.Add(this.ImageToWorld(points_2d[i]));
        }
        return points_3d;
    }

    public Line3 Line2ToLine3(Line2 line)
    {
        Vector3 start3 = this.ImageToWorld(line.start);
        Vector3 end3 = this.ImageToWorld(line.end);
        return new Line3(start3, end3, 1);
    }

    public Quad ProjRectToImage3d(Quad world_rect, bool clamp = false)
    {
        List<Vector3> imgpoints = new List<Vector3>();
        foreach (Vector3 p in world_rect.CornerPoints3d)
            imgpoints.Add(ImageToWorld(WorldToImage(p, clamp)));
        return new Quad(imgpoints);
    }

}

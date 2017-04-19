using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using MyGeometry;
using UnityEditor;//导入UI包

public class RenderEngine : MonoBehaviour
{
    public Material material_default;
    Material material_Mesh;

    // Render Data
    List<Vector3> Vertex_Line = new List<Vector3>();
    List<Vector3> Vertex_Line_Real = new List<Vector3>();
    List<Vector3> Vertex_Point = new List<Vector3>();
    List<Color> Color_Line = new List<Color>();
    List<Color> Color_Line_Real = new List<Color>();
    List<Color> Color_Point = new List<Color>();
    List<Mesh> Mesh_List = new List<Mesh>();
    public Mesh meshTest;
    // Render Params
    Color Color_default = Color.white;
    float Length_Ray = 2f;
    float Length_Point = 0.01f;

    Color[] ColorMall;
    int coloroffset = 16;

    ////  Test
    //Vector3 movingV1 = new Vector3(0, 0, 0);
    //Vector3 movingV2 = new Vector3(0, 0, 1);
    //Vector3 movingV3 = new Vector3(0, 0, 0);
    //Vector3 movingV4 = new Vector3(0, 1, 0);

    void Start()
    {
        // Render Init
        material_Mesh = new Material(Shader.Find("Custom/DoubleSideShader"));

        // Render Test
        //DrawLine(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        //DrawLine(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.red);
        //Circle3 c1 = new Circle3(new Vector3(0, 1, 0), 0.5f, new Plane(new Vector3(0, 1, 0), new Vector3(0, 1, 0)));
        //Circle3 c2 = new Circle3(new Vector3(0, 0, 0), 0.5f, new Plane(new Vector3(0, 1, 0), new Vector3(0, 0, 0)));
        //List<Vector3> ps = new List<Vector3>();
        //DrawPoints(c1.CirclePoints);
        //DrawCircle(c2);
        //DrawMesh(meshTest);
    }

    void Update()
    {
        //movingV1 += new Vector3(0.01f, 0.01f, 0.01f);
        //movingV2 += new Vector3(0.01f, 0.01f, 0.01f);
        //movingV3 += new Vector3(0.01f, 0.01f, 0.01f);
        //movingV4 += new Vector3(0.01f, 0.01f, 0.01f);
        //DrawLine(ref movingV1, ref movingV2);
        //DrawLine(ref movingV3, ref movingV4, Color.yellow);
        MeshRender();
    }

    void OnRenderObject()
    {
        LineRender();
        PointRender();
    }

    void LineRender()
    {
        GL.PushMatrix();
        for (var i = 0; i < material_default.passCount; ++i)
        {
            material_default.SetPass(i);
            GL.Begin(GL.LINES);
            for (int j = 0; j < Vertex_Line.Count; j++)
            {
                GL.Color(Color_Line[j]);
                GL.Vertex(Vertex_Line[j]);
            }

            for (int j = 0; j < Vertex_Line_Real.Count; j++)
            {
                GL.Color(Color_Line_Real[j]);
                GL.Vertex(Vertex_Line_Real[j]);
            }

            GL.End();
        }
        GL.PopMatrix();

        Color_Line_Real.Clear();
        Vertex_Line_Real.Clear();
    }

    void PointRender()
    {
        GL.PushMatrix();
        for (var i = 0; i < material_default.passCount; ++i)
        {
            material_default.SetPass(i);
            GL.Begin(GL.LINES);
            for (int j = 0; j < Vertex_Point.Count; j++)
            {
                GL.Color(Color_Point[j]);
                GL.Vertex(Vertex_Point[j]);
            }
            GL.End();
        }
        GL.PopMatrix();
    }

    void MeshRender()
    {
        foreach (var mesh in Mesh_List)
        {
            //Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material_Mesh, 0);
        }
    }

    public void DrawPoints(List<Vector3> ps)
    {
        foreach (var p in ps)
        {
            Vertex_Point.Add(p);
            Color_Point.Add(Color_default);

            Vertex_Point.Add(p + new Vector3(1, 0, 0) * Length_Point);
            Color_Point.Add(Color_default);
        }
    }

    public void DrawPoints(List<Vector3> ps, Color color)
    {
        foreach (var p in ps)
        {
            Vertex_Point.Add(p);
            Color_Point.Add(color);

            Vertex_Point.Add(p + new Vector3(1, 0, 0) * Length_Point);
            Color_Point.Add(color);
        }
    }

    public void DrawRay(Ray r)
    {
        Vertex_Line.Add(r.origin);
        Color_Line.Add(Color_default);

        Vertex_Line.Add(r.origin + Length_Ray * r.direction);
        Color_Line.Add(Color_default);
    }

    public void DrawRay(Ray r, Color color)
    {
        Vertex_Line.Add(r.origin);
        Color_Line.Add(color);

        Vertex_Line.Add(r.origin + Length_Ray * r.direction);
        Color_Line.Add(color);
    }

    public void DrawLine(Vector3 v1, Vector3 v2)
    {
        Vertex_Line.Add(v1);
        Color_Line.Add(Color_default);

        Vertex_Line.Add(v2);
        Color_Line.Add(Color_default);
    }

    public void DrawLine(Vector3 v1, Vector3 v2, Color color)
    {
        Vertex_Line.Add(v1);
        Color_Line.Add(color);

        Vertex_Line.Add(v2);
        Color_Line.Add(color);
    }

    public void DrawCircle(Circle3 c)
    {
        for (int i = 0; i < c.CirclePoints.Count; i++)
        {
            Vertex_Line.Add(c.CirclePoints[i]);
            Color_Line.Add(Color_default);

            int indexNext = (i + 1) % c.CirclePoints.Count;
            Vertex_Line.Add(c.CirclePoints[indexNext]);
            Color_Line.Add(Color_default);
        }
    }

    public void DrawMesh(Mesh mesh)
    {
        if (mesh == null)
            return;
        Mesh_List.Add(mesh);
    }

    //-----Using These Function Under void Updata()-------------------------
    public void DrawRay(ref Ray r)
    {
        Vertex_Line_Real.Add(r.origin);
        Color_Line_Real.Add(Color_default);

        Vertex_Line_Real.Add(r.origin + Length_Ray * r.direction);
        Color_Line_Real.Add(Color_default);
    }

    public void DrawRay(ref Ray r, Color color)
    {
        Vertex_Line_Real.Add(r.origin);
        Color_Line_Real.Add(color);

        Vertex_Line_Real.Add(r.origin + Length_Ray * r.direction);
        Color_Line_Real.Add(color);
    }

    public void DrawLine(ref Vector3 v1, ref Vector3 v2)
    {
        Vertex_Line_Real.Add(v1);
        Color_Line_Real.Add(Color_default);

        Vertex_Line_Real.Add(v2);
        Color_Line_Real.Add(Color_default);
    }

    public void DrawLine(ref Vector3 v1, ref Vector3 v2, Color color)
    {
        Vertex_Line_Real.Add(v1);
        Color_Line_Real.Add(color);

        Vertex_Line_Real.Add(v2);
        Color_Line_Real.Add(color);
    }

    public void DrawCircle(ref Circle3 c)
    {
        for (int i = 0; i < c.CirclePoints.Count; i++)
        {
            Vertex_Line_Real.Add(c.CirclePoints[i]);
            Color_Line_Real.Add(Color_default);

            int indexNext = (i + 1) % c.CirclePoints.Count;
            Vertex_Line_Real.Add(c.CirclePoints[indexNext]);
            Color_Line_Real.Add(Color_default);
        }
    }

    public void DrawCircle(ref Circle3 c, Color color)
    {
        for (int i = 0; i < c.CirclePoints.Count; i++)
        {
            Vertex_Line_Real.Add(c.CirclePoints[i]);
            Color_Line_Real.Add(color);

            int indexNext = (i + 1) % c.CirclePoints.Count;
            Vertex_Line_Real.Add(c.CirclePoints[indexNext]);
            Color_Line_Real.Add(color);
        }
    }
    //----------------------------------------------------------------------

    void InitialRandomColors()
    {
        ColorMall = new Color[500];
        int offset = coloroffset;
        int i = 0;
        //sequential - green - 1
        ColorMall[i * offset + 0] = new Color(247, 252, 253);
        ColorMall[i * offset + 1] = new Color(229, 245, 249);
        ColorMall[i * offset + 2] = new Color(204, 236, 230);
        ColorMall[i * offset + 3] = new Color(153, 216, 201);
        ColorMall[i * offset + 4] = new Color(102, 194, 164);
        ColorMall[i * offset + 5] = new Color(65, 174, 118);
        ColorMall[i * offset + 6] = new Color(35, 139, 69);
        ColorMall[i * offset + 7] = new Color(0, 109, 44);
        ColorMall[i * offset + 8] = new Color(0, 68, 27);

        i++;
        //sequential - purple - 2
        ColorMall[i * offset + 0] = new Color(247, 252, 253);
        ColorMall[i * offset + 1] = new Color(224, 236, 244);
        ColorMall[i * offset + 2] = new Color(191, 211, 230);
        ColorMall[i * offset + 3] = new Color(158, 188, 218);
        ColorMall[i * offset + 4] = new Color(140, 150, 198);
        ColorMall[i * offset + 5] = new Color(140, 107, 177);
        ColorMall[i * offset + 6] = new Color(136, 65, 157);
        ColorMall[i * offset + 7] = new Color(129, 15, 124);
        ColorMall[i * offset + 8] = new Color(77, 0, 75);

        i++;
        //sequential - blue - 3
        ColorMall[i * offset + 0] = new Color(247, 252, 240);
        ColorMall[i * offset + 1] = new Color(224, 243, 219);
        ColorMall[i * offset + 2] = new Color(204, 235, 197);
        ColorMall[i * offset + 3] = new Color(168, 221, 181);
        ColorMall[i * offset + 4] = new Color(123, 204, 196);
        ColorMall[i * offset + 5] = new Color(78, 179, 211);
        ColorMall[i * offset + 6] = new Color(43, 140, 190);
        ColorMall[i * offset + 7] = new Color(8, 104, 172);
        ColorMall[i * offset + 8] = new Color(8, 64, 129);

        i++;
        //sequential - orange - 4
        ColorMall[i * offset + 0] = new Color(255, 247, 236);
        ColorMall[i * offset + 1] = new Color(254, 232, 200);
        ColorMall[i * offset + 2] = new Color(253, 212, 158);
        ColorMall[i * offset + 3] = new Color(253, 187, 132);
        ColorMall[i * offset + 4] = new Color(252, 141, 89);
        ColorMall[i * offset + 5] = new Color(239, 101, 72);
        ColorMall[i * offset + 6] = new Color(215, 48, 31);
        ColorMall[i * offset + 7] = new Color(179, 0, 0);
        ColorMall[i * offset + 8] = new Color(127, 0, 0);

        i++;
        //sequential - rose red - 5
        ColorMall[i * offset + 0] = new Color(247, 244, 249);
        ColorMall[i * offset + 1] = new Color(231, 225, 239);
        ColorMall[i * offset + 2] = new Color(212, 185, 218);
        ColorMall[i * offset + 3] = new Color(201, 148, 199);
        ColorMall[i * offset + 4] = new Color(223, 101, 176);
        ColorMall[i * offset + 5] = new Color(231, 41, 138);
        ColorMall[i * offset + 6] = new Color(206, 18, 86);
        ColorMall[i * offset + 7] = new Color(152, 0, 67);
        ColorMall[i * offset + 8] = new Color(103, 0, 31);

        i++;
        //sequential - blue - 6
        ColorMall[i * offset + 0] = new Color(255, 247, 251);
        ColorMall[i * offset + 1] = new Color(236, 231, 242);
        ColorMall[i * offset + 2] = new Color(208, 209, 230);
        ColorMall[i * offset + 3] = new Color(166, 189, 219);
        ColorMall[i * offset + 4] = new Color(116, 169, 207);
        ColorMall[i * offset + 5] = new Color(54, 144, 192);
        ColorMall[i * offset + 6] = new Color(5, 112, 176);
        ColorMall[i * offset + 7] = new Color(4, 90, 141);
        ColorMall[i * offset + 8] = new Color(2, 56, 88);


        i++;
        //sequential - another purple - 7
        ColorMall[i * offset + 0] = new Color(255, 247, 243);
        ColorMall[i * offset + 1] = new Color(253, 224, 221);
        ColorMall[i * offset + 2] = new Color(252, 197, 192);
        ColorMall[i * offset + 3] = new Color(250, 159, 181);
        ColorMall[i * offset + 4] = new Color(247, 104, 161);
        ColorMall[i * offset + 5] = new Color(221, 52, 151);
        ColorMall[i * offset + 6] = new Color(174, 1, 126);
        ColorMall[i * offset + 7] = new Color(122, 1, 119);
        ColorMall[i * offset + 8] = new Color(73, 0, 106);


        i++;
        //diverging - 1
        ColorMall[i * offset + 0] = new Color(140, 81, 10);
        ColorMall[i * offset + 1] = new Color(191, 129, 45);
        ColorMall[i * offset + 2] = new Color(223, 194, 125);
        ColorMall[i * offset + 3] = new Color(246, 232, 195);
        ColorMall[i * offset + 4] = new Color(245, 245, 245);
        ColorMall[i * offset + 5] = new Color(199, 234, 229);
        ColorMall[i * offset + 6] = new Color(128, 205, 193);
        ColorMall[i * offset + 7] = new Color(53, 151, 143);
        ColorMall[i * offset + 8] = new Color(1, 102, 94);


        i++;
        //diverging - 2
        ColorMall[i * offset + 0] = new Color(215, 48, 39);
        ColorMall[i * offset + 1] = new Color(244, 109, 67);
        ColorMall[i * offset + 2] = new Color(253, 174, 97);
        ColorMall[i * offset + 3] = new Color(254, 224, 144);
        ColorMall[i * offset + 4] = new Color(255, 255, 191);
        ColorMall[i * offset + 5] = new Color(224, 243, 248);
        ColorMall[i * offset + 6] = new Color(171, 217, 233);
        ColorMall[i * offset + 7] = new Color(116, 173, 209);
        ColorMall[i * offset + 8] = new Color(69, 117, 180);

        i++;
        //diverging - 3
        ColorMall[i * offset + 0] = new Color(215, 48, 39);
        ColorMall[i * offset + 1] = new Color(244, 109, 67);
        ColorMall[i * offset + 2] = new Color(253, 174, 97);
        ColorMall[i * offset + 3] = new Color(254, 224, 139);
        ColorMall[i * offset + 4] = new Color(255, 255, 191);
        ColorMall[i * offset + 5] = new Color(217, 239, 139);
        ColorMall[i * offset + 6] = new Color(166, 217, 106);
        ColorMall[i * offset + 7] = new Color(102, 189, 99);
        ColorMall[i * offset + 8] = new Color(26, 152, 80);

        i++;
        //diverging - 4
        ColorMall[i * offset + 0] = new Color(213, 62, 79);
        ColorMall[i * offset + 1] = new Color(244, 109, 67);
        ColorMall[i * offset + 2] = new Color(253, 174, 97);
        ColorMall[i * offset + 3] = new Color(254, 224, 139);
        ColorMall[i * offset + 4] = new Color(255, 255, 191);
        ColorMall[i * offset + 5] = new Color(230, 245, 152);
        ColorMall[i * offset + 6] = new Color(171, 221, 164);
        ColorMall[i * offset + 7] = new Color(102, 194, 165);
        ColorMall[i * offset + 8] = new Color(50, 136, 189);

        i++;
        //diverging - 5
        ColorMall[i * offset + 0] = new Color(178, 24, 43);
        ColorMall[i * offset + 1] = new Color(214, 96, 77);
        ColorMall[i * offset + 2] = new Color(244, 165, 130);
        ColorMall[i * offset + 3] = new Color(253, 219, 199);
        ColorMall[i * offset + 4] = new Color(247, 247, 247);
        ColorMall[i * offset + 5] = new Color(209, 229, 240);
        ColorMall[i * offset + 6] = new Color(146, 197, 222);
        ColorMall[i * offset + 7] = new Color(67, 147, 195);
        ColorMall[i * offset + 8] = new Color(33, 102, 172);

        i++;
        //qualitative - 1
        ColorMall[i * offset + 0] = new Color(166, 206, 227);
        ColorMall[i * offset + 1] = new Color(31, 120, 180);
        ColorMall[i * offset + 2] = new Color(178, 223, 138);
        ColorMall[i * offset + 3] = new Color(51, 160, 44);
        ColorMall[i * offset + 4] = new Color(251, 154, 153);
        ColorMall[i * offset + 5] = new Color(227, 26, 28);
        ColorMall[i * offset + 6] = new Color(253, 191, 111);
        ColorMall[i * offset + 7] = new Color(255, 127, 0);
        ColorMall[i * offset + 8] = new Color(202, 178, 214);

        i++;
        //qualitative = 2
        ColorMall[i * offset + 0] = new Color(251, 180, 174);
        ColorMall[i * offset + 1] = new Color(179, 205, 227);
        ColorMall[i * offset + 2] = new Color(204, 235, 197);
        ColorMall[i * offset + 3] = new Color(222, 203, 228);
        ColorMall[i * offset + 4] = new Color(254, 217, 166);
        ColorMall[i * offset + 5] = new Color(255, 255, 204);
        ColorMall[i * offset + 6] = new Color(229, 216, 189);
        ColorMall[i * offset + 7] = new Color(253, 218, 236);
        ColorMall[i * offset + 8] = new Color(242, 242, 242);

        i++;
        //qualitative = 3
        ColorMall[i * offset + 0] = new Color(228, 26, 28);
        ColorMall[i * offset + 1] = new Color(55, 126, 184);
        ColorMall[i * offset + 2] = new Color(77, 175, 74);
        ColorMall[i * offset + 3] = new Color(152, 78, 163);
        ColorMall[i * offset + 4] = new Color(255, 127, 0);
        ColorMall[i * offset + 5] = new Color(255, 255, 51);
        ColorMall[i * offset + 6] = new Color(166, 86, 40);
        ColorMall[i * offset + 7] = new Color(247, 129, 191);
        ColorMall[i * offset + 8] = new Color(153, 153, 153);

        i++;
        //qualitative = 4
        ColorMall[i * offset + 0] = new Color(141, 211, 199);
        ColorMall[i * offset + 1] = new Color(255, 255, 179);
        ColorMall[i * offset + 2] = new Color(190, 186, 218);
        ColorMall[i * offset + 3] = new Color(251, 128, 114);
        ColorMall[i * offset + 4] = new Color(128, 177, 211);
        ColorMall[i * offset + 5] = new Color(253, 180, 98);
        ColorMall[i * offset + 6] = new Color(179, 222, 105);
        ColorMall[i * offset + 7] = new Color(252, 205, 229);
        ColorMall[i * offset + 8] = new Color(217, 217, 217);



        //Random rand = new Random();
        //ColorMall = new Color[K];
        //for (int i = 0; i < K; ++i)
        //{
        //    ColorMall[i] = new Color(
        //        rand.Next(0, 255),
        //        rand.Next(0, 255),
        //        rand.Next(0, 255)
        //    );
        //}

        //// 8-data, color scheme
        //if (true)
        //{
        //    int off_set = 0;
        //    ColorMall[0 + off_set] = new Color(228, 26, 28);
        //    ColorMall[1 + off_set] = new Color(77, 175, 74);
        //    ColorMall[2 + off_set] = new Color(55, 126, 184);
        //    //ColorMall[1 + off_set] = new Color(55, 126, 184);
        //    //ColorMall[2 + off_set] = new Color(77, 175, 74);

        //    ColorMall[3 + off_set] = new Color(152, 78, 163);
        //    ColorMall[4 + off_set] = new Color(255, 127, 0);
        //    ColorMall[5 + off_set] = new Color(0, 128, 255);
        //    ColorMall[6 + off_set] = new Color(166, 86, 40);
        //    ColorMall[7 + off_set] = new Color(247, 129, 191);
        //}
    }

    Color ColorRand()
    {
        System.Random randomGenerator = new System.Random();
        Color randomColor;
        int R, G, B;
        R = randomGenerator.Next(0, 255);
        G = randomGenerator.Next(0, 255);
        B = randomGenerator.Next(0, 255);
        randomColor = new Color(R, G, B);
        return randomColor;
    }
}
using UnityEngine;
using System.Collections;
using UnityEngine.UI;//导入UI包

public class RenderEngine : MonoBehaviour
{

    private Vector2 mMouseStart, mMouseEnd;
    private bool mBDrawMouseRect;
    public Material mat;

    void Start()
    {

        mBDrawMouseRect = false;

        //rectMat = new Material("Shader \"Lines/Colored Blended\" {" +
        //    "SubShader { Pass { " +
        //    "    Blend SrcAlpha OneMinusSrcAlpha " +
        //    "    ZWrite Off Cull Off Fog { Mode Off } " +
        //    "    BindChannels {" +
        //    "      Bind \"vertex\", vertex Bind \"color\", color }" +
        //    "} } }");//生成画线的材质    
        //rectMat.hideFlags = HideFlags.HideAndDontSave;
        //rectMat.shader.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnRenderObject()
    {

        // 清屏操作（可以不要此操作，这样场景中的对象才可以显示）
        //GL.Clear(true, true, Color.black);

        // 将当前矩阵变换对象push缓存下来，防止自己的操作影响到其它渲染操作
        GL.PushMatrix();

        // 设置绘制模式为2D绘制，设置这个模式之后屏幕左下角变为(0,0)，屏幕右上角变为(1,1)，注释之后变为3D真实坐标
        //GL.LoadOrtho();

        // 绘制过程
        for (var i = 0; i < mat.passCount; ++i)
        {
            // 设置shader，用过OpenGL ES2.0的同志应该知道，这个类似于combine glsl的过程，这里么有自己写，用的unity自带的sprite的shader
            // 由于shader可能存在多个pass通道，所以采用遍历的方式将每个通道都绘制一遍，当然有些shader只有一个通道，比如这个自带的sprite的shader
            // 也可以设置成SetPass(0)，就是使用默认的第一个通道进行渲染
            mat.SetPass(i);
            // 设置绘制 模式为线条模式（这个模式每两个顶点为一组）
            GL.Begin(GL.LINES);

            // 设置顶点颜色（设置下一个顶点的颜色，如果后面没有更改，继续保留这个颜色属性直至被更改）
            GL.Color(Color.red);
            // 向GL中增加一个点的坐标
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(100F, 100F, 0);

            GL.Color(Color.white);
            GL.Vertex3(0.5F, 0.5F, 0);
            GL.Color(Color.blue);
            GL.Vertex3(1F, 0F, 0);

            // 通知GL关闭当前绘制模式
            GL.End();
        }

        // 将矩阵对象还原，与之前的push操作相对应
        GL.PopMatrix();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        //按下鼠标左键    
        {
            Vector3 mousePosition = Input.mousePosition;
            mMouseStart = new Vector2(mousePosition.x, mousePosition.y);
            //OnGUI();
        }

        if (Input.GetMouseButton(0))
        //持续按下鼠标左键    
        {
            mBDrawMouseRect = true;
            Vector3 mousePosition = Input.mousePosition;
            mMouseEnd = new Vector2(mousePosition.x, mousePosition.y);
        }

        if (Input.GetMouseButtonUp(0))
        {
            mBDrawMouseRect = false;
        }
    }

    void OnGUI()
    {
        Draw(new Vector2(50, 100), new Vector2(200,400));

        if (mBDrawMouseRect)
            Draw(mMouseStart, mMouseEnd);
    }

    //渲染2D框    
    void Draw(Vector2 start, Vector2 end)
    {
        //rectMat.SetPass(0);

        GL.PushMatrix();//保存摄像机变换矩阵    

        Color clr = Color.green;
        clr.a = 0.1f;

        GL.LoadPixelMatrix();//设置用屏幕坐标绘图    
        //透明框    
        GL.Begin(GL.QUADS);
        GL.Color(clr);
        GL.Vertex3(start.x, start.y, 1);
        GL.Vertex3(end.x, start.y, 1);
        GL.Vertex3(end.x, end.y, 1);
        GL.Vertex3(start.x, end.y, 1);
        GL.End();

        //线    
        //上    
        GL.Begin(GL.LINES);
        GL.Color(Color.black);
        GL.Vertex3(start.x, start.y, 1);
        GL.Vertex3(end.x, start.y, 1);
        GL.End();

        //下    
        GL.Begin(GL.LINES);
        GL.Color(Color.black);
        GL.Vertex3(start.x, end.y, 1);
        GL.Vertex3(end.x, end.y, 1);
        GL.End();

        //左    
        GL.Begin(GL.LINES);
        GL.Color(Color.black);
        GL.Vertex3(start.x, start.y, 1);
        GL.Vertex3(start.x, end.y, 1);
        GL.End();

        //右    
        GL.Begin(GL.LINES);
        GL.Color(Color.black);
        GL.Vertex3(end.x, start.y, 1);
        GL.Vertex3(end.x, end.y, 1);
        GL.End();

        GL.PopMatrix();//还原    
    }
}
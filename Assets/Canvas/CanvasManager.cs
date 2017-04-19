using UnityEngine.UI;
using UnityEngine;
using UnityEditor;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.Util;

using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityExtension;

public class CanvasManager : MonoBehaviour
{
    // Canvas params
    public Canvas m_canvas;
    public CanvasRenderer m_canvasRender;
    Vector3 anchor;
    float scale;                            // Scale Canvas2D to RealWorld3D 
    Rect canvasPlane2D;                     // Canvas Rect 2D, Canvas Space
    Rect canvasPlane3D;                     // Canvas Rect 3D, World Space

    // UI Button
    public UnityEngine.UI.Button button;
    public UnityEngine.UI.Button button1;
    public UnityEngine.UI.Button button2;
    public UnityEngine.UI.Image backimg;

    // Engine
    public GraphicsEngine m_engine;
    Image<Rgb, byte> img = null;

    void Awake()
    {
        // Canvas
        scale = m_canvas.transform.lossyScale.x;
        float height3D = m_canvas.pixelRect.height * scale;
        float width3D = m_canvas.pixelRect.width * scale;

        anchor = m_canvas.transform.position - new Vector3(width3D / 2, height3D / 2, 0);
        canvasPlane2D = m_canvas.pixelRect;
        canvasPlane3D = new Rect(anchor, new Vector2(width3D, height3D));
        backimg.rectTransform.sizeDelta = canvasPlane2D.size;

        // Add Event to Button
        button.onClick.AddListener(LoadImage);
        button1.onClick.AddListener(FitTop);
        button2.onClick.AddListener(Sweep);
    }

    void Start() { }

    void Update() { }

    void LoadImage()
    {
        string path = EditorUtility.OpenFilePanel("Load Image", "", "png");
        if (path.Length != 0)
        {
            img = new Image<Rgb, byte>(path);

            //  Resize
            img = img.Resize((int)((float)img.Width / (float)img.Height * canvasPlane2D.height),
                (int)canvasPlane2D.height,
                Inter.Linear);

            Texture2D CanvasImg = IExtension.ImageToTexture2D(img);
            backimg.sprite = Sprite.Create(CanvasImg, new Rect(0, 0, CanvasImg.width, CanvasImg.height), Vector2.zero);
            backimg.rectTransform.sizeDelta = new Vector2(CanvasImg.width, CanvasImg.height);
            IExtension.SetTransparency(backimg, 0.3f);
        }
    }

    void FitTop()
    {
        m_engine.FitTopFace(img.Convert<Gray, byte>());
    }

    void Sweep()
    {
        LoadImage();
        m_engine.FitBody(img.Convert<Gray, byte>());
    }
}

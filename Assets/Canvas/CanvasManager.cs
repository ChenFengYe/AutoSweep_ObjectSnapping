using UnityEngine.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
using System.IO;
using MyGeometry;

public class CanvasManager : MonoBehaviour
{
    // Canvas params
    public Canvas m_canvas;
    public RenderEngine m_renderEngine;
    Vector3 anchor;
    float scale;                            // Scale Canvas2D to RealWorld3D 
    Rect canvasPlane2D;                     // Canvas Rect 2D, Canvas Space
    Rect canvasPlane3D;                     // Canvas Rect 3D, World Space

    // UI Button
    public UnityEngine.UI.Button button;
    //public UnityEngine.UI.Button button1;
    //public UnityEngine.UI.Button button2;
    //public UnityEngine.UI.Button button3;
    public UnityEngine.UI.Button button4;
    public UnityEngine.UI.Button button5;
    public UnityEngine.UI.Button button6;
    public UnityEngine.UI.Button button7;
    public UnityEngine.UI.Button button8;
    public UnityEngine.UI.Button button9;

    public UnityEngine.UI.Toggle toggle1;
    public UnityEngine.UI.Toggle toggle2;
    public UnityEngine.UI.Toggle toggle3;
    public UnityEngine.UI.Toggle toggle4;
    public UnityEngine.UI.Toggle toggle5;
    public UnityEngine.UI.Toggle toggle6;

    public UnityEngine.UI.Slider slider1;
    public UnityEngine.UI.Slider slider2;
    public UnityEngine.UI.Slider slider3;
    public UnityEngine.UI.Slider slider4;
    public UnityEngine.UI.Slider slider5;

    public UnityEngine.UI.Image backImg;
    public UnityEngine.UI.Image maskImg;
    public UnityEngine.UI.Image lineImg;
    public UnityEngine.UI.Image realPhoto;

    public UnityEngine.UI.Text text0;
    // Engine
    [HideInInspector]
    public GraphicsEngine m_engine;
    Image<Rgb, byte> m_MaskTopImg = null;
    Image<Rgb, byte> m_MaskBodyImg = null;
    Image<Rgb, byte> m_MaskRawImg = null;

    Texture2D m_MaskTex = null;
    Texture2D m_ImgTex = null;

    bool IsTopGeometryLoaded = false;
    //string m_ImgPath = null;

    void Awake()
    {
        // Canvas
        scale = m_canvas.transform.lossyScale.x;
        float height3D = m_canvas.pixelRect.height * scale;
        float width3D = m_canvas.pixelRect.width * scale;

        anchor = m_canvas.transform.position - new Vector3(width3D / 2, height3D / 2, 0);
        canvasPlane2D = m_canvas.pixelRect;
        canvasPlane3D = new Rect(anchor, new Vector2(width3D, height3D));
        backImg.rectTransform.sizeDelta = canvasPlane2D.size;

        // Add Event to Button
        button.onClick.AddListener(LoadImageAndCircle_Action);
        //button1.onClick.AddListener(FitTopCircle);
        //button2.onClick.AddListener(FitTopRect);
        //button3.onClick.AddListener(Sweep);
        button4.onClick.AddListener(ReStartSweep);
        button5.onClick.AddListener(StopSweep);
        button6.onClick.AddListener(StartCubeExperiments);
        button7.onClick.AddListener(StartMultiExperiments);
        button8.onClick.AddListener(SkipCurrentExperiment);
        button9.onClick.AddListener(StartSingleCubeExperiment);

        toggle1.onValueChanged.AddListener(ControlMask);
        toggle2.onValueChanged.AddListener(ControlMesh);
        toggle3.onValueChanged.AddListener(ControlGizmos);
        toggle4.onValueChanged.AddListener(ControlSymmetry);
        toggle5.onValueChanged.AddListener(ControlTexture);
        toggle6.onValueChanged.AddListener(ControlSnapSketch);

        slider1.onValueChanged.AddListener(ControlOffset);
        slider2.onValueChanged.AddListener(ControlBottomCover);
        slider3.onValueChanged.AddListener(ControlCameraFOV);
        slider4.onValueChanged.AddListener(ControlBackground);
        slider5.onValueChanged.AddListener(ControlAxisThred);
        slider1.value = 0.1f;
        slider2.value = 0.58f;
        slider3.value = 20f;
        slider4.value = 0f;
    }

    void Start()
    {
        ControlOffset(slider1.value);
        ControlBottomCover(slider2.value);
        ControlSymmetry(toggle4.isOn);
        ControlSnapSketch(toggle6.isOn);
    }

    void Update()
    {
        if (m_experStatus == ExperimentsStatus.notDone && m_engine.exper_isCubeExper) RunCubeExperiments();
        if (m_experStatus == ExperimentsStatus.notDone && m_engine.exper_isMultiExper) RunMultiExperiments();
    }

    # region Load Sweeping Image
    //bool LoadSweepImage(string directory = "")
    //{
    //    string path = EditorUtility.OpenFilePanel("Load Image", directory, "png");
    //    if (path.Length != 0)
    //    {
    //        // Background Image
    //        m_MaskBodyImg = new Image<Rgb, byte>(path);
    //        Texture2D CanvasImg = ResizeImgWithHeight(m_MaskBodyImg, canvasPlane2D.height);
    //        m_MaskBodyImg = m_MaskBodyImg.Resize(CanvasImg.width, CanvasImg.height, Inter.Nearest);

    //        backimg.sprite = Sprite.Create(CanvasImg, new Rect(0, 0, CanvasImg.width, CanvasImg.height), Vector2.zero);
    //        backimg.rectTransform.sizeDelta = new Vector2(CanvasImg.width, CanvasImg.height);
    //        IExtension.SetTransparency(backimg, 0.3f);

    //        // MaskBody Image
    //        Texture2D maskBody_Img = ResizeImgWithHeight(m_MaskBodyImg, maskBody.rectTransform.rect.height);
    //        maskBody.sprite = Sprite.Create(maskBody_Img, new Rect(0, 0, maskBody_Img.width, maskBody_Img.height), Vector2.zero);
    //        maskBody.rectTransform.sizeDelta = new Vector2(maskBody_Img.width, maskBody_Img.height);

    //        // Real Image
    //        string realPhoto_path = EditorUtility.OpenFilePanel("Load Image", directory, "");
    //        Texture2D realPhoto_Img = ResizeImgWithHeight(new Image<Rgb, byte>(realPhoto_path), realPhoto.rectTransform.rect.height);
    //        realPhoto.sprite = Sprite.Create(realPhoto_Img, new Rect(0, 0, realPhoto_Img.width, realPhoto_Img.height), Vector2.zero);
    //        realPhoto.rectTransform.sizeDelta = new Vector2(realPhoto_Img.width, realPhoto_Img.height);

    //        // Updata Path
    //        string folderPath = Path.GetDirectoryName(path);
    //        string fileName = Path.GetFileName(path);
    //        m_engine.UpdataFileName(folderPath, fileName.Substring(0, fileName.Length - 9));

    //        return true;
    //    }
    //    return false;
    //}
    # endregion
    void LoadImageAndCircle_Action()
    {
        LoadImageAndCircle(null);
    }
    void LoadImageAndCircle(string realPhoto_path = null)
    {
        IsTopGeometryLoaded = false;
        if (realPhoto_path == null) realPhoto_path = EditorUtility.OpenFilePanel("Load Image", "", "jpg");
        if (realPhoto_path.Length != 0)
        {
            //--------------------------------------------------------------------
            // Visulization
            // Real Image
            m_ImgTex = ResizeImgWithHeight(new Image<Rgb, byte>(realPhoto_path), realPhoto.rectTransform.rect.height);
            realPhoto.sprite = Sprite.Create(m_ImgTex, new Rect(0, 0, m_ImgTex.width, m_ImgTex.height), Vector2.zero);
            realPhoto.rectTransform.sizeDelta = new Vector2(m_ImgTex.width, m_ImgTex.height);

            # region Single Mask Input
            ////MaskTop Image
            //string maskTopImg_path = FixFilePath(realPhoto_path, "_top", "png");
            //if (File.Exists(maskTopImg_path))
            //{
            //    Texture2D maskTop_Img = ResizeImgWithHeight(new Image<Rgb, byte>(maskTopImg_path), maskimg.rectTransform.rect.height);
            //    maskimg.sprite = Sprite.Create(maskTop_Img, new Rect(0, 0, maskTop_Img.width, maskTop_Img.height), Vector2.zero);
            //    maskimg.rectTransform.sizeDelta = new Vector2(maskTop_Img.width, maskTop_Img.height);
            //}

            ////MaskBody Image
            //string maskBodyImg_path = FixFilePath(realPhoto_path, "_body", "png");
            //if (File.Exists(maskBodyImg_path))
            //{
            //    Texture2D maskBody_Img = ResizeImgWithHeight(new Image<Rgb, byte>(maskBodyImg_path), maskBody.rectTransform.rect.height);
            //    maskBody.sprite = Sprite.Create(maskBody_Img, new Rect(0, 0, maskBody_Img.width, maskBody_Img.height), Vector2.zero);
            //    maskBody.rectTransform.sizeDelta = new Vector2(maskBody_Img.width, maskBody_Img.height);
            //}
            # endregion

            //--------------------------------------------------------------------
            // Data Prepare -- Background Image

            string maskRawImg_path = FixFilePath(realPhoto_path, "_mask", "png");
            string lineRawImg_path;//= FixFilePath(realPhoto_path, "_line", "png");
            string lineRawTxt_path;//= FixFilePath(realPhoto_path, "", "txt");
            if (toggle6.isOn)
            {
                lineRawImg_path = FixFilePath(realPhoto_path, "_line", "png");
                lineRawTxt_path = FixFilePath(realPhoto_path, "", "txt");
                if (m_engine.be_isSnap && File.Exists(lineRawImg_path))
                {
                    Texture2D LineTex = ResizeImgWithHeight(new Image<Rgb, byte>(lineRawImg_path), lineImg.rectTransform.rect.height);
                    lineImg.sprite = Sprite.Create(LineTex, new Rect(0, 0, LineTex.width, LineTex.height), Vector2.zero);
                    lineImg.rectTransform.sizeDelta = new Vector2(LineTex.width, LineTex.height);

                    LoadLineMask(lineRawImg_path);
                    LoadLineText(lineRawTxt_path);
                    CheckLineMaskSize(m_MaskTex, m_engine.m_LineImg);
                }
            }

                if (File.Exists(maskRawImg_path) )
            {
                m_MaskRawImg = new Image<Rgb, byte>(maskRawImg_path);
                m_MaskTex = ResizeImgWithHeight(m_MaskRawImg, canvasPlane2D.height);
                m_MaskRawImg = m_MaskRawImg.Resize(m_MaskTex.width, m_MaskTex.height, Inter.Nearest);

                Image<Rgb, byte> m_Img = new Image<Rgb, byte>(realPhoto_path);
                Texture2D CanvasImg = ResizeImgWithHeight(m_Img, canvasPlane2D.height);
                backImg.sprite = Sprite.Create(CanvasImg, new Rect(0, 0, CanvasImg.width, CanvasImg.height), Vector2.zero);
                backImg.rectTransform.sizeDelta = new Vector2(CanvasImg.width, CanvasImg.height);
                IExtension.SetTransparency(backImg, 0.3f);

                Texture2D MaskTex = ResizeImgWithHeight(new Image<Rgb, byte>(maskRawImg_path), maskImg.rectTransform.rect.height);
                maskImg.sprite = Sprite.Create(MaskTex, new Rect(0, 0, MaskTex.width, MaskTex.height), Vector2.zero);
                maskImg.rectTransform.sizeDelta = new Vector2(MaskTex.width, MaskTex.height);

                m_engine.m_Img = CanvasImg;
                m_engine.m_MaskImg = m_MaskTex;
                m_engine.m_fovOptimizer.m_Img = m_MaskTex;
                m_engine.m_OrigImg = new Image<Rgb, byte>(realPhoto_path);

                if(!m_engine.m_is_quiet) Debug.Log(realPhoto_path);
                this.SplitRawMask();

                //m_engine.m_faceEngine.img = m_MaskRawImg.Convert<Gray, byte>();
                //m_engine.m_faceEngine.imgPath = maskRawImg_path;

                IsTopGeometryLoaded = false;
                m_engine.InitEngine();
            }
            else
            {
                #region Load Face Data
                //// Mask Top Data
                //m_MaskTopImg = new Image<Rgb, byte>(maskTopImg_path);
                //Texture2D CanvasMaskTopImg = ResizeImgWithHeight(m_MaskTopImg, canvasPlane2D.height);
                //m_MaskTopImg = m_MaskTopImg.Resize(CanvasMaskTopImg.width, CanvasMaskTopImg.height, Inter.Linear);

                //// Mask Body Data
                //m_MaskBodyImg = new Image<Rgb, byte>(maskBodyImg_path);
                //Texture2D CanvasMaskBodyImg = ResizeImgWithHeight(m_MaskBodyImg, canvasPlane2D.height);
                //m_MaskBodyImg = m_MaskBodyImg.Resize(CanvasMaskBodyImg.width, CanvasMaskBodyImg.height, Inter.Linear);

                //// Background Image
                //backimg.sprite = Sprite.Create(CanvasMaskBodyImg, new Rect(0, 0, CanvasMaskBodyImg.width, CanvasMaskBodyImg.height), Vector2.zero);
                //backimg.rectTransform.sizeDelta = new Vector2(CanvasMaskBodyImg.width, CanvasMaskBodyImg.height);
                //IExtension.SetTransparency(backimg, 0.3f);

                //// Load Top Circle
                //string TopCirclePath = Path.GetDirectoryName(maskTopImg_path) + "/" + Path.GetFileNameWithoutExtension(maskTopImg_path) + ".circle";
                //string TopRectPath = Path.GetDirectoryName(maskTopImg_path) + "/" + Path.GetFileNameWithoutExtension(maskTopImg_path) + ".quad";
                //if (File.Exists(TopCirclePath)) // liyuwei - load rect
                //{
                //    m_engine.m_faceEngine.topCircle = new Circle3(TopCirclePath);
                //    m_engine.m_faceEngine.isTopCircle_Current = true;
                //    IsTopGeometryLoaded = true;
                //}
                //else if (File.Exists(TopRectPath))
                //{
                //    m_engine.m_faceEngine.topRect = new Quad(TopRectPath);
                //    m_engine.m_faceEngine.isTopCircle_Current = false;
                //    IsTopGeometryLoaded = true;
                //}
                //else
                //{
                //    IsTopGeometryLoaded = false;
                //}

                //m_engine.m_faceEngine.img = m_MaskTopImg.Convert<Gray, byte>();
                //m_engine.m_faceEngine.imgPath = maskTopImg_path;
                #endregion
            }




            // Updata Path
            string folderPath = Path.GetDirectoryName(realPhoto_path);
            string fileName = Path.GetFileName(realPhoto_path);
            text0.text = fileName;
            m_engine.UpdataFileName(folderPath, fileName.Substring(0, fileName.Length - 4));
            ReStartSweep();
        }
    }
    void LoadLineMask(string lineRawImg_path)
    {
        // Load Img
        Image<Gray, byte> m_LineMaskImg = new Image<Gray, byte>(lineRawImg_path);

        m_engine.m_LineImg = m_LineMaskImg;
        //m_engine.m_LineImg = ResizeImgWithHeight(m_LineMaskImg, canvasPlane2D.height);
        //m_engine.m_LineImg.Save("test.png");
    }
    void LoadLineText(string txt_path)
    {
        if (!File.Exists(txt_path))
        {
            Debug.Log("No such File!");
            return;
        }
        m_engine.m_LineData = new List<List<Vector2>>();
        List<Vector2> line = new List<Vector2>();

        StreamReader sr = new StreamReader(File.Open(txt_path, FileMode.Open));
        string s = sr.ReadLine();
        while (s != null)
        {
            if (s.Length == 0) 
            {
                if (line.Count > 0) m_engine.m_LineData.Add(line);
                line = new List<Vector2>();
            }
            if (s.Length > 0)
            {
                string[] ss = s.Split(' ');
                line.Add(new Vector2(int.Parse(ss[0]), int.Parse(ss[1])));
            }

            s = sr.ReadLine();            
        }
        sr.Close();
    }

    #region Cube Experiments
    enum ExperimentsStatus { notDo, notDone, Done };
    ExperimentsStatus m_experStatus = ExperimentsStatus.notDo;
    bool exper_isSkip = false;

    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    string[] m_objectPaths;
    string[] m_objectPaths_seg;
    string[] m_objectPaths_obj;
    string[] m_cubePaths_corner;

    int m_objectIndex = -1;
    bool CheckContinueCurrentExper(int m_objectIndex)
    {
        //if (stopwatch.ElapsedMilliseconds / 1000.0 > 60) return false;

        // Is this First experiment
        if (m_objectIndex == -1 ) return false;

        // Is Skip?
        if (exper_isSkip)
        {
            exper_isSkip = false;
            return false;
        }

        // Check The Last Experiment is finished?
        if (m_engine.is_experiment_finished)
        {
            m_engine.is_experiment_finished = false;
            return false;
        }
        else return true;
        //if(m_objectPaths_seg != null )
        //{
        //    if (File.Exists(m_objectPaths_seg[m_objectIndex]))
        //        return false;
        //    else
        //        return true;
        //}

        //if (m_objectPaths_obj != null)
        //{
        //    if (File.Exists(m_objectPaths_obj[m_objectIndex]))
        //        return false;
        //    else
        //        return true;
        //}
        return true;
    }
    void StartCubeExperiments()
    {
        //set timer
        stopwatch.Stop();
        stopwatch.Start();

        m_experStatus = ExperimentsStatus.notDone;
        // 53
        m_objectIndex = -1;
        m_engine.exper_isCubeExper = true;
        
        // Load Data
        string curr_path = System.Environment.CurrentDirectory;
        string input_folder = curr_path + "\\Assets\\Resources\\ImageData\\Experiments_Cube\\Input";
        string output_folder = curr_path +  "\\Assets\\Resources\\ImageData\\Experiments_Cube\\Result";

        m_objectPaths = Directory.GetFiles(input_folder, "*.jpg");

        m_objectPaths_seg = new string[m_objectPaths.Length];
        m_cubePaths_corner = new string[m_objectPaths.Length];
        for (int i = 0; i < m_objectPaths.Length; i++)
            m_objectPaths_seg[i] = FixFilePath(output_folder, m_objectPaths[i], "_seg", "png");
        for (int i = 0; i < m_objectPaths.Length; i++)
            m_cubePaths_corner[i] = FixFilePath(output_folder, m_objectPaths[i], "_corner", "txt");
    }
    void RunCubeExperiments()
    {
        // Check is Continues this exper
        if (CheckContinueCurrentExper(m_objectIndex)) return;
        
        // Process Visualization

        // Init data
        m_objectIndex++;
        if (m_objectIndex == m_objectPaths.Length)
        {

            m_objectIndex = 0;
            m_experStatus = ExperimentsStatus.Done;
            m_objectPaths = null;
            m_engine.m_fs.Close();
            m_engine.m_fs.Dispose();
            return;
        }
        stopwatch.Stop();
        if (m_objectIndex > 0)
            Debug.Log(m_objectIndex + 1 + "/" + m_objectPaths.Length + "  cost time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");
        stopwatch.Reset();
        stopwatch.Start();
        
        // Load Data
        LoadImageAndCircle(m_objectPaths[m_objectIndex]);

        // Prepare for Save txt
        if (m_engine.m_fs != null) m_engine.m_fs.Close();
        m_engine.m_fs = new FileStream(m_cubePaths_corner[m_objectIndex], FileMode.Create);

        ReStartSweep();
        
    }
    void StartSingleCubeExperiment()
    {
        //set timer
        stopwatch.Stop();
        stopwatch.Start();

        m_experStatus = ExperimentsStatus.notDone;
        m_objectIndex = -1;
        m_engine.exper_isCubeExper = true;
        
        // Load Data
        string file_path = EditorUtility.OpenFilePanel("Load Image", "", "jpg");
        string curr_path = System.Environment.CurrentDirectory;
        string input_folder = curr_path + "\\Assets\\Resources\\ImageData\\Experiments_Cube\\Input";
        string output_folder = curr_path + "\\Assets\\Resources\\ImageData\\Experiments_Cube\\Result";
        m_objectPaths = new string[1];
        m_objectPaths_seg = new string[1];
        m_cubePaths_corner = new string[1];
        m_objectPaths[0] = file_path;
        m_objectPaths_seg[0] = FixFilePath(output_folder, m_objectPaths[0], "_seg", "png");
        m_cubePaths_corner[0] = FixFilePath(output_folder, m_objectPaths[0], "_corner", "txt");
    }
    # endregion

    #region Multi Experiments
    void StartMultiExperiments()
    {
        //set timer
        stopwatch.Stop();
        stopwatch.Start();
        
        // Load Data
        string curr_path = System.Environment.CurrentDirectory;
        string input_folder = curr_path + "\\Assets\\Resources\\ImageData\\Experiments_Multi\\Input\\newTest";
        string output_folder = curr_path + "\\Assets\\Resources\\ImageData\\Experiments_Multi\\Result\\newTest";
        EditorObjExporter.targetFolder = "Assets\\Resources\\ImageData\\Experiments_Multi\\Result\\newTest";

        m_experStatus = ExperimentsStatus.notDone;
        m_objectIndex = -1;
        m_engine.exper_isMultiExper = true;


        m_objectPaths = Directory.GetFiles(input_folder, "*.jpg");

        m_objectPaths_obj = new string[m_objectPaths.Length];
        for (int i = 0; i < m_objectPaths.Length; i++)
            m_objectPaths_obj[i] = FixFilePath(output_folder, m_objectPaths[i], "", "obj");
    }
    void RunMultiExperiments()
    {
        // Check is Continues this exper
        if (CheckContinueCurrentExper(m_objectIndex)) return;

        // Process Visualization

        // Init data
        m_objectIndex++;
        if (m_objectIndex == m_objectPaths.Length)
        {
            m_objectIndex = 0;
            m_experStatus = ExperimentsStatus.Done;
            m_objectPaths = null;
            return;
        }

        stopwatch.Stop();
        if (m_objectIndex > 0)
            Debug.Log(m_objectIndex + 1 + "/" + m_objectPaths.Length + "  cost time:" + stopwatch.ElapsedMilliseconds / 1000.0 + "s");
        stopwatch.Start();

        // Load Data
        LoadImageAndCircle(m_objectPaths[m_objectIndex]);

        ReStartSweep();
    }
    void SkipCurrentExperiment()
    {
        exper_isSkip = true;
    }
    # endregion

    Texture2D ResizeImgWithHeight(Image<Rgb, byte> img, float height)
    {
        img = img.Resize((int)((float)img.Width / (float)img.Height * height),
                (int)height,
                Inter.Nearest);
        return IExtension.ImageToTexture2D(img);
    }
    Image<Gray, byte> ResizeImgWithHeight(Image<Gray, byte> img, float height)
    {
        img = img.Resize((int)((float)img.Width / (float)img.Height * height),
                (int)height,
                Inter.Nearest);
        return img;
    }
    void CheckLineMaskSize(Texture2D img, Image<Gray, byte> lineImg)
    {
        bool isSame = (img.height == lineImg.Height && img.width == lineImg.Width) ? true : false;
        if (!isSame)
        {
            if(!m_engine.m_is_quiet) Debug.Log("Error: the line mask input is not same size with img resized!");
        }
    }
    public void SplitRawMask()
    {
        if (m_MaskRawImg == null) Debug.Log("No Raw mask is loaded!");
        m_engine.ParseRawMask(m_MaskRawImg);
    }
    # region Single Primitive Backup
    //void FitTopCircle()
    //{
    //    if (m_MaskTopImg == null) Debug.Log("No Image is loaded!");
    //    ChangeBackgroundImg(m_MaskTopImg);
    //    m_engine.FitTopCircle(m_engine.m_faceEngine_temp, m_MaskTopImg.Convert<Gray, byte>());
    //    IsTopGeometryLoaded = true;
    //}
    //void FitTopRect()
    //{
    //    if (m_MaskTopImg == null) Debug.Log("No Image is loaded!");

    //    ChangeBackgroundImg(m_MaskTopImg);
    //    m_engine.FitTopRect(m_engine.m_faceEngine_temp, m_MaskTopImg.Convert<Gray, byte>());
    //}
    //void Sweep()
    //{
    //    //bool isImgLoad = LoadSweepImage(Path.GetDirectoryName(m_ImgPath));
    //    if (m_MaskBodyImg == null) Debug.Log("No Image is loaded!");
    //    ChangeBackgroundImg(m_MaskBodyImg);
    //    for (int i = 0; i < m_engine.m_bodyEngine_list.Count; i++)
    //    {

    //        m_engine.FitBody(m_engine.m_bodyEngine_list[i], m_engine.m_meshViewer_list[i], m_MaskBodyImg.Convert<Gray, byte>());
    //    }
    //}
    # endregion
    void ControlMask(bool toggleFlag)
    {
        if (toggleFlag)
            this.backImg.gameObject.SetActive(true);
        else
            this.backImg.gameObject.SetActive(false);
    }
    void ControlOffset(float scale)
    {
        m_engine.be_offsetScale = scale;
    }
    void ControlBottomCover(float scale)
    {
        m_engine.be_bottomCover = scale;
    }


    void ControlAxisThred(float scale)
    {
        SkeletonExtractor.thred = scale;
    }
    

    void ControlCameraFOV(float scale)
    {
        m_engine.m_projector.m_mainCamera.fieldOfView = scale;
        m_engine.m_meshCaptor.fieldOfView = scale;
    }
    void ControlBackground(float scale)
    {

        backImg.sprite = scale > 0.5f ?
            Sprite.Create(m_MaskTex, new Rect(0, 0, m_MaskTex.width, m_MaskTex.height), Vector2.zero) :
            Sprite.Create(m_ImgTex, new Rect(0, 0, m_ImgTex.width, m_ImgTex.height), Vector2.zero);
    }
    void ReStartSweep()
    {
        m_engine.InitEngine();
        m_engine.ReconstructAllGeometries();
    }
    void ControlMesh(bool isRenderingMesh)
    {
        m_renderEngine.isRenderingMesh = isRenderingMesh;
    }
    void ControlGizmos(bool isRenderingAll)
    {
        m_renderEngine.isRenderingGizmos = isRenderingAll;
    }
    void ControlSymmetry(bool isSymmetry)
    {
        m_engine.be_isSymmetry = isSymmetry;
    }
    void ControlTexture(bool isTexture)
    {
        //string notexture_path = "Assets/Resources/OBJViewer_empty.mat";
        string notexture_path = "Assets/Materials/Materials/SamplePotLidLiner.mat";
        foreach (var gb in m_engine.m_meshViewer_best_list)
        {
            string texture_path = m_engine.m_prefab_path + "/" + gb.name + ".mat";
            gb.GetComponent<MeshRenderer>().material = isTexture ?
                (Material)AssetDatabase.LoadAssetAtPath(texture_path, typeof(Material)) :
                (Material)AssetDatabase.LoadAssetAtPath(notexture_path, typeof(Material));
        }
    }
    void ControlSnapSketch(bool isSnap)
    {
        m_engine.be_isSnap = isSnap;
    }
    void StopSweep()
    {
        m_engine.be_isStop = true;
    }
    void SetStage()
    {
        //Transform[] selection = Selection.GetTransforms(UnityEditor.SelectionMode.Editable | UnityEditor.SelectionMode.ExcludePrefab);

        //if (selection.Length == 0)
        //{
        //    EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "");
        //    return;
        //}

        //int exportedObjects = 0;

        //ArrayList mfList = new ArrayList();

        //for (int i = 0; i < selection.Length; i++)
        //{
        //    Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

    }
    string FixFilePath(string photo, string keyword_add, string extens)
    {
        return Path.GetDirectoryName(photo) + "\\" + Path.GetFileNameWithoutExtension(photo) + keyword_add + "." + extens;
    }
    string FixFilePath(string folder, string photo, string keyword_add, string extens)
    {
        return folder + "\\" + Path.GetFileNameWithoutExtension(photo) + keyword_add + "." + extens;
    }
    Image<Rgb, byte> FixLineMaskTooDark(Image<Rgb, byte> img)
    {
        return img.Convert<byte>(delegate(byte b) { return (byte)(255 - b); });
    }
    void ChangeBackgroundImg(Image<Rgb, byte> img)
    {
        // Background Image
        Texture2D CanvasTexture = IExtension.ImageToTexture2D(img);
        backImg.sprite = Sprite.Create(CanvasTexture, new Rect(0, 0, CanvasTexture.width, CanvasTexture.height), Vector2.zero);
        backImg.rectTransform.sizeDelta = new Vector2(CanvasTexture.width, CanvasTexture.height);
        IExtension.SetTransparency(backImg, 0.3f);
    }
}
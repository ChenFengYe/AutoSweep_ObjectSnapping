//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using MyGeometry;
//using System.Drawing;
//using UnityEngine;

//using OpenTK.Graphics.OpenGL;

//namespace SmartCanvas
//{
//    public static class GLBasicDraw
//    {
//        public static float[] matAmbient = { 0.1f, 0.1f, 0.1f, 1.0f };
//        public static float[] matDiffuse = { 0.4f, 0.4f, 0.4f, 1.0f };
//        public static float[] matSpecular = { 0.5f, 0.5f, 0.5f, 1.0f };
//        public static float[] shine = { 7.0f };

//        public static List<float[]> lightPositions = new List<float[]>();
//        public static List<float[]> lightcolors = new List<float[]>();
//        public static List<int> lightIDs = new List<int>();

//        public static void InitGlMaterialLights()
//        {
//            GL.Enable(EnableCap.ColorMaterial);

//            // Material
//            SetDefaultMaterial();

//            // Lighting
//            SetDefaultLight();

//            // Fog
//            float[] fogColor = new float[] { 0.3f, 0.3f, 0.4f, 1.0f };
//            GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
//            GL.Fog(FogParameter.FogColor, fogColor);
//            GL.Fog(FogParameter.FogDensity, 0.35f);
//            GL.Hint(HintTarget.FogHint, HintMode.DontCare);
//            GL.Fog(FogParameter.FogStart, 5.0f);
//            GL.Fog(FogParameter.FogEnd, 25.0f);

//        }
//        private static void SetDefaultLight()
//        {
//            float[] pos1 = new float[4] { 0f, -10f, -10f, 0.0f };
//            float[] pos2 = new float[4] { 0f, 5f, -5f, 0.0f };
//            float[] col1 = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };


//            GL.Enable(EnableCap.Light0);
//            GL.Light(LightName.Light0, LightParameter.Position, pos1);
//            GL.Light(LightName.Light0, LightParameter.Ambient, col1);
//            GL.Light(LightName.Light0, LightParameter.Diffuse, col1);
//            GL.Light(LightName.Light0, LightParameter.Specular, col1);

//            //GL.Enable(EnableCap.Light1);
//            //GL.Light(LightName.Light1, LightParameter.Position, pos2);
//            //GL.Light(LightName.Light1, LightParameter.Diffuse, col2);
//            //GL.Light(LightName.Light1, LightParameter.Specular, col2);

//            //GL.Enable(EnableCap.Light2);
//            //GL.Light(LightName.Light2, LightParameter.Position, pos3);
//            //GL.Light(LightName.Light2, LightParameter.Diffuse, col3);
//            //GL.Light(LightName.Light2, LightParameter.Specular, col3);

//        }
//        private static void SetDefaultMaterial()
//        {
//            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, matAmbient);
//            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, matDiffuse);
//            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, matSpecular);
//            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, shine);

//        }

//        // 2d draw functions
//        public static void DrawRect(Vector2 start, Vector2 end, Color c)
//        {
//            GL.PushMatrix();
//            GL.LoadIdentity();

//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

//            GL.Color3(c.R, c.G, c.B);
//            GL.Rect(start.x, start.y, end.x, end.y);

//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

//            GL.PopMatrix();
//        }
//        public static void DrawRectFill(Vector2 start, Vector2 end, Color c)
//        {
//            GL.PushMatrix();
//            GL.LoadIdentity();

//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

//            GL.Color3(c.R, c.G, c.B);
//            GL.Rect(start.x, start.y, end.x, end.y);

//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

//            GL.PopMatrix();
//        }
//        public static void DrawCircleTextured(Vector2 p, Color c, int size, uint texId)
//        {
//            GL.Color4(c.R, c.G, c.B, c.A);

//            GL.Enable(EnableCap.Blend);
//            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

//            GL.Enable(EnableCap.Texture2D);
//            GL.BindTexture(TextureTarget.Texture2D, texId);

//            GL.Begin(PrimitiveType.Quads);
//            GL.TexCoord2(0, 0);
//            GL.Vertex2(p.x - size, p.y - size);
//            GL.TexCoord2(0, 1);
//            GL.Vertex2(p.x - size, p.y + size);
//            GL.TexCoord2(1, 1);
//            GL.Vertex2(p.x + size, p.y + size);
//            GL.TexCoord2(1, 0);
//            GL.Vertex2(p.x + size, p.y - size);
//            GL.End();
//            GL.Disable(EnableCap.Texture2D);

//            GL.Disable(EnableCap.Blend);
//        }
//        public static void DrawCircle(Vector2 p, Color c, float radius)
//        {
//            GL.Enable(EnableCap.Blend);
//            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

//            GL.Color4(c.R, c.G, c.B, (byte)50);

//            int nsample = 50;
//            double delta = Math.PI * 2 / nsample;

//            GL.LineWidth(1.0f);
//            GL.Begin(PrimitiveType.Lines);
//            for (int i = 0; i < nsample; ++i)
//            {
//                double theta1 = i * delta;
//                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
//                double theta2 = (i + 1) * delta;
//                double x2 = p.x + radius * Math.Cos(theta2), y2 = p.y + radius * Math.Sin(theta2);
//                GL.Vertex2(x1, y1);
//                GL.Vertex2(x2, y2);
//            }
//            GL.End();
//            GL.LineWidth(1.0f);

//            GL.Begin(PrimitiveType.Polygon);
//            for (int i = 0; i < nsample; ++i)
//            {
//                double theta1 = i * delta;
//                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
//                GL.Vertex2(x1, y1);
//            }
//            GL.End();

//            //	GL.Disable(EnableCap.Blend);
//        }
//        public static void DrawCircle3(MyVector3 center, int axis, double[,] coords, bool highlight, MyMatrix4d IdenMat)
//        {
//            GL.Enable(EnableCap.Blend);

//            Color c = Color.Black;
//            switch (axis)
//            {
//                case 0:
//                    {
//                        c = Color.Red;
//                        break;
//                    }
//                case 1:
//                    {
//                        c = Color.Green;
//                        break;
//                    }
//                case 2:
//                    {
//                        c = Color.Blue;
//                        break;
//                    }
//            }

//            if (highlight)
//            {
//                GL.Color4(c.R, c.G, c.B, (byte)250);
//                GL.LineWidth(3.0f);
//            }
//            else
//            {
//                GL.Color4(c.R, c.G, c.B, (byte)160);
//                GL.LineWidth(2.0f);
//            }

//            int n = coords.GetLength(0);

//            GL.Begin(PrimitiveType.Lines);
//            for (int i = 0; i < n; ++i)
//            {
//                double x1 = coords[i, 0];
//                double y1 = coords[i, 1];
//                double x2 = coords[(i + 1) % n, 0];
//                double y2 = coords[(i + 1) % n, 1];
//                MyVector3 v1 = center, v2 = center;

//                MyVector3 v11 = new MyVector3(), v22 = new MyVector3();
//                switch (axis)
//                {
//                    case 0:
//                        {
//                            v11 += new MyVector3(0, x1, y1);
//                            v22 += new MyVector3(0, x2, y2);
//                            break;
//                        }
//                    case 1:
//                        {
//                            v11 += new MyVector3(x1, 0, y1);
//                            v22 += new MyVector3(x2, 0, y2);
//                            break;
//                        }
//                    case 2:
//                        {
//                            v11 += new MyVector3(x1, y1, 0);
//                            v22 += new MyVector3(x2, y2, 0);
//                            break;
//                        }
//                    default:
//                        break;
//                }
//                //v1 += (IdenMat * new MyVector4(v11, 1)).XYZ();
//                //v2 += (IdenMat * new MyVector4(v22, 1)).XYZ();
//                v1 += v11;
//                v2 += v22;
//                //v1 = (IdenMat * new MyVector4(v1, 1)).XYZ();
//                //v2 = (IdenMat * new MyVector4(v2, 1)).XYZ();
//                GL.Vertex3(v1.x, v1.y, v1.z);
//                GL.Vertex3(v2.x, v2.y, v2.z);
//            }
//            GL.End();
//            GL.LineWidth(1.0f);
//        }//DrawRotationCircle3D

//        public static void DrawLineSegment2(Vector2 u, Vector2 v, Color c, float size)
//        {
//            GL.Enable(EnableCap.LineSmooth);
//            GL.LineWidth(size);
//            GL.Color3(c.R, c.G, c.B);
//            GL.Begin(PrimitiveType.Lines);
//            GL.Vertex2(u.x, u.y);
//            GL.Vertex2(v.x, v.y);
//            GL.End();
//            GL.LineWidth(1.0f);
//        }
//        public static void DrawLineSegment3(MyVector3 u, MyVector3 v, Color c, float size)
//        {
//            GL.Enable(EnableCap.Blend);
//            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
//            GL.Enable(EnableCap.LineSmooth);
//            GL.LineWidth(size);
//            GL.Color3(c.R, c.G, c.B);
//            GL.Begin(PrimitiveType.Lines);
//            GL.Vertex3(u.x, u.y, u.z);
//            GL.Vertex3(v.x, v.y, v.z);
//            GL.End();
//            GL.LineWidth(1.0f);
//            GL.Disable(EnableCap.Blend);
//        }

//        public static void DrawCross2D(Vector2 p, float size, Color c)
//        {
//            GL.Color3(c.R, c.G, c.B);
//            GL.LineWidth(2.0f);
//            GL.Begin(PrimitiveType.Lines);
//            GL.Vertex2(p.x - size, p.y - size);
//            GL.Vertex2(p.x + size, p.y + size);
//            GL.Vertex2(p.x + size, p.y - size);
//            GL.Vertex2(p.x - size, p.y + size);
//            GL.End();
//            GL.LineWidth(1.0f);
//        }
//        public static void DrawCross2DPlus(Vector2 p, float size, Color c, float linewidth = 2.0f)
//        {
//            GL.Color3(c.R, c.G, c.B);
//            GL.LineWidth(linewidth);
//            GL.Begin(PrimitiveType.Lines);
//            GL.Vertex2(p.x - size, p.y);
//            GL.Vertex2(p.x + size, p.y);
//            GL.Vertex2(p.x, p.y - size);
//            GL.Vertex2(p.x, p.y + size);
//            GL.End();
//            GL.LineWidth(1.0f);

//            DrawRect(p - new Vector2(size, size), p + new Vector2(size, size), Color.Gray);
//        }

//        public static void DrawTexture(int w, int h, uint texid, float opacity)
//        {
//            GL.Enable(EnableCap.Blend);
//            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
//            GL.Enable(EnableCap.Texture2D);
//            GL.BindTexture(TextureTarget.Texture2D, texid);
//            GL.Color4(1, 1, 1, opacity);
//            GL.Begin(PrimitiveType.Polygon);
//            GL.TexCoord2(0, 0);
//            GL.Vertex2(0, 0);
//            GL.TexCoord2(0, 1);
//            GL.Vertex2(0, h);
//            GL.TexCoord2(1, 1);
//            GL.Vertex2(w, h);
//            GL.TexCoord2(1, 0);
//            GL.Vertex2(w, 0);
//            GL.End();
//            GL.Disable(EnableCap.Texture2D);
//            GL.Disable(EnableCap.Blend);
//        }

//    }
//}

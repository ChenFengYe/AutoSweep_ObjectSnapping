//using System;

//using OpenTK.Graphics.OpenGL;

//using MyGeometry;

//namespace SmartCanvas
//{
//    public class OpenGLProjector 
//    {
//        #region Private Instance Fields
//        private double[] modelView = new double[16];
//        private double[] projection = new double[16];
//        private int[] viewport = new int[4];
//        #endregion

//        #region Public Properties
//        public double[] ModelViewMatrix
//        {
//            get { return modelView; }
//        }
//        public double[] ProjectionMatrix 
//        {
//            get { return projection; }
//        }
//        public int[] Viewport 
//        {
//            get { return viewport; }
//        }

//        #endregion

//        public OpenGLProjector() 
//        {
//            this.UpdateContext();
//        }

//        public void UpdateContext()
//        {
//            GL.GetDouble(GetPName.ModelviewMatrix, modelView);
//            GL.GetDouble(GetPName.ProjectionMatrix, projection);
//            GL.GetInteger(GetPName.Viewport, viewport);
//        }

//        public MyVector3 UnProject(double inX, double inY, double inZ) 
//        {
//            double x,y,z;
			
//            glUnProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }
//        public MyVector3 UnProject(MyVector3 p) 
//        {
//            double x,y,z;
//            glUnProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }
//        public MyVector3 UnProject(double[] arr, int index) 
//        {
//            double x,y,z;
//            glUnProject(arr[index], arr[index+1], arr[index+2], modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }
//        public MyVector3 Project(double inX, double inY, double inZ) 
//        {
//            double x,y,z;
//            glProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }
//        public MyVector3 Project(MyVector3 p) 
//        {
//            double x,y,z;
//            glProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }
//        public MyVector3 Project(double[] arr, int index) 
//        {
//            double x,y,z;
//            glProject(arr[index], arr[index+1], arr[index+2], modelView, projection, viewport, out x, out y, out z);
//            return new MyVector3(x,y,z);
//        }

//        int glProject(double objx, double objy, double objz, double[] modelview, double[] projection, int[] viewport, out double x, out double y, out double z)
//        {
//            //Transformation vectors
//            double[] fTempo = new double[8];
//            //Modelview transform
//            fTempo[0]=modelview[0]*objx+modelview[4]*objy+modelview[8]*objz+modelview[12];  //w is always 1
//            fTempo[1]=modelview[1]*objx+modelview[5]*objy+modelview[9]*objz+modelview[13];
//            fTempo[2]=modelview[2]*objx+modelview[6]*objy+modelview[10]*objz+modelview[14];
//            fTempo[3]=modelview[3]*objx+modelview[7]*objy+modelview[11]*objz+modelview[15];
//            //Projection transform, the final row of projection matrix is always [0 0 -1 0]
//            //so we optimize for that.
//            fTempo[4]=projection[0]*fTempo[0]+projection[4]*fTempo[1]+projection[8]*fTempo[2]+projection[12]*fTempo[3];
//            fTempo[5]=projection[1]*fTempo[0]+projection[5]*fTempo[1]+projection[9]*fTempo[2]+projection[13]*fTempo[3];
//            fTempo[6]=projection[2]*fTempo[0]+projection[6]*fTempo[1]+projection[10]*fTempo[2]+projection[14]*fTempo[3];
//            fTempo[7]=-fTempo[2];
//            //The result normalizes between -1 and 1
//            if (fTempo[7] == 0.0)	//The w value
//            {
//                x = 0; y = 0; z = 0;
//                return 0;
//            }
//            fTempo[7]=1.0/fTempo[7];
//            //Perspective division
//            fTempo[4]*=fTempo[7];
//            fTempo[5]*=fTempo[7];
//            fTempo[6]*=fTempo[7];
//            //Window coordinates
//            //Map x, y to range 0-1
//            x=(fTempo[4]*0.5+0.5)*viewport[2]+viewport[0];
//            y=(fTempo[5]*0.5+0.5)*viewport[3]+viewport[1];
//            //This is only correct when glDepthRange(0.0, 1.0)
//            z=(1.0+fTempo[6])*0.5;	//Between 0 and 1
//            return 1;
//        }

//        int glUnProject(double winx, double winy, double winz, double[] modelview, double[] projection, int[] viewport, out double x, out double y, out double z)
//        {
//            x = 0; y = 0; z = 0;

//            //Transformation matrices
//            double[] m = new double[16]; 
//            double[] A = new double[16];
//            double[] input  = new double[4];
//            double[] output = new double[4];
//            //Calculation for inverting a matrix, compute projection x modelview
//            //and store in A[16]
//            MultiplyMatrices4by4OpenGL(A, projection, modelview);
//            //Now compute the inverse of matrix A
//            if(glInvertMatrix2(A, m)==0)
//                return 0;
//            //Transformation of normalized coordinates between -1 and 1
//            input[0]=(winx-(double)viewport[0])/(double)viewport[2]*2.0-1.0;
//            input[1]=(winy-(double)viewport[1])/(double)viewport[3]*2.0-1.0;
//            input[2]=2.0*winz-1.0;
//            input[3]=1.0;
//            //Objects coordinates
//            MultiplyMatrixByVector4by4OpenGL(output, m, input);
//            if(output[3]==0.0)
//                return 0;
//            output[3]=1.0/output[3];
//            x=output[0]*output[3];
//            y=output[1]*output[3];
//            z=output[2]*output[3];
//            return 1;
//        }

//        void MultiplyMatrices4by4OpenGL(double[] result, double[] matrix1, double[] matrix2)
//        {
//        result[0]=matrix1[0]*matrix2[0]+
//            matrix1[4]*matrix2[1]+
//            matrix1[8]*matrix2[2]+
//            matrix1[12]*matrix2[3];
//        result[4]=matrix1[0]*matrix2[4]+
//            matrix1[4]*matrix2[5]+
//            matrix1[8]*matrix2[6]+
//            matrix1[12]*matrix2[7];
//        result[8]=matrix1[0]*matrix2[8]+
//            matrix1[4]*matrix2[9]+
//            matrix1[8]*matrix2[10]+
//            matrix1[12]*matrix2[11];
//        result[12]=matrix1[0]*matrix2[12]+
//            matrix1[4]*matrix2[13]+
//            matrix1[8]*matrix2[14]+
//            matrix1[12]*matrix2[15];
//        result[1]=matrix1[1]*matrix2[0]+
//            matrix1[5]*matrix2[1]+
//            matrix1[9]*matrix2[2]+
//            matrix1[13]*matrix2[3];
//        result[5]=matrix1[1]*matrix2[4]+
//            matrix1[5]*matrix2[5]+
//            matrix1[9]*matrix2[6]+
//            matrix1[13]*matrix2[7];
//        result[9]=matrix1[1]*matrix2[8]+
//            matrix1[5]*matrix2[9]+
//            matrix1[9]*matrix2[10]+
//            matrix1[13]*matrix2[11];
//        result[13]=matrix1[1]*matrix2[12]+
//            matrix1[5]*matrix2[13]+
//            matrix1[9]*matrix2[14]+
//            matrix1[13]*matrix2[15];
//        result[2]=matrix1[2]*matrix2[0]+
//            matrix1[6]*matrix2[1]+
//            matrix1[10]*matrix2[2]+
//            matrix1[14]*matrix2[3];
//        result[6]=matrix1[2]*matrix2[4]+
//            matrix1[6]*matrix2[5]+
//            matrix1[10]*matrix2[6]+
//            matrix1[14]*matrix2[7];
//        result[10]=matrix1[2]*matrix2[8]+
//            matrix1[6]*matrix2[9]+
//            matrix1[10]*matrix2[10]+
//            matrix1[14]*matrix2[11];
//        result[14]=matrix1[2]*matrix2[12]+
//            matrix1[6]*matrix2[13]+
//            matrix1[10]*matrix2[14]+
//            matrix1[14]*matrix2[15];
//        result[3]=matrix1[3]*matrix2[0]+
//            matrix1[7]*matrix2[1]+
//            matrix1[11]*matrix2[2]+
//            matrix1[15]*matrix2[3];
//        result[7]=matrix1[3]*matrix2[4]+
//            matrix1[7]*matrix2[5]+
//            matrix1[11]*matrix2[6]+
//            matrix1[15]*matrix2[7];
//        result[11]=matrix1[3]*matrix2[8]+
//            matrix1[7]*matrix2[9]+
//            matrix1[11]*matrix2[10]+
//            matrix1[15]*matrix2[11];
//        result[15]=matrix1[3]*matrix2[12]+
//            matrix1[7]*matrix2[13]+
//            matrix1[11]*matrix2[14]+
//            matrix1[15]*matrix2[15];
//        }

//        void MultiplyMatrixByVector4by4OpenGL(double[] resultvector, double[] matrix, double[] pvector)
//        {
//            resultvector[0]=matrix[0]*pvector[0]+matrix[4]*pvector[1]+matrix[8]*pvector[2]+matrix[12]*pvector[3];
//            resultvector[1]=matrix[1]*pvector[0]+matrix[5]*pvector[1]+matrix[9]*pvector[2]+matrix[13]*pvector[3];
//            resultvector[2]=matrix[2]*pvector[0]+matrix[6]*pvector[1]+matrix[10]*pvector[2]+matrix[14]*pvector[3];
//            resultvector[3]=matrix[3]*pvector[0]+matrix[7]*pvector[1]+matrix[11]*pvector[2]+matrix[15]*pvector[3];
//        }

//        void swapRows(ref double[] a, ref double[] b)
//        {
//            double[] tmp = b;
//            b = a;
//            a = tmp;
//        }

//        //This code comes directly from GLU except that it is for double
//        int glInvertMatrix2(double[] m, double[] output)
//        {
//            double[][] wtmp = new double[4][];
//            for (int i = 0; i < 4; ++i) wtmp[i] = new double[8];
//            double m0, m1, m2, m3, s;
//            double[]r0 = wtmp[0]; double[] r1 = wtmp[1]; double[]r2 = wtmp[2]; double[] r3 = wtmp[3];

//            r0[0] = m[0+0*4]; r0[1] = m[0+1*4];
//            r0[2] = m[0+2*4]; r0[3] = m[0+3*4];
//            r0[4] = 1.0; r0[5] = r0[6] = r0[7] = 0.0;
//            r1[0] = m[1+0*4]; r1[1] = m[1+1*4];
//            r1[2] = m[1+2*4]; r1[3] = m[1+3*4];
//            r1[5] = 1.0; r1[4] = r1[6] = r1[7] = 0.0;
//            r2[0] = m[2+0*4]; r2[1] = m[2+1*4];
//            r2[2] = m[2+2*4]; r2[3] = m[2+3*4];
//            r2[6] = 1.0; r2[4] = r2[5] = r2[7] = 0.0;
//            r3[0] = m[3+0*4]; r3[1] = m[3+1*4];
//            r3[2] = m[3+2*4]; r3[3] = m[3+3*4];
//            r3[7] = 1.0; r3[4] = r3[5] = r3[6] = 0.0;

//            /* choose pivot - or die */
//            if (Math.Abs(r3[0]) > Math.Abs(r2[0]))
//                swapRows(ref r3, ref r2);
//            if (Math.Abs(r2[0]) > Math.Abs(r1[0]))
//                swapRows(ref r2, ref r1);
//            if (Math.Abs(r1[0]) > Math.Abs(r0[0]))
//                swapRows(ref r1, ref r0);
//            if (0.0 == r0[0])
//                return 0;
//            /* eliminate first variable     */
//            m1 = r1[0] / r0[0];
//            m2 = r2[0] / r0[0];
//            m3 = r3[0] / r0[0];
//            s = r0[1];
//            r1[1] -= m1 * s;
//            r2[1] -= m2 * s;
//            r3[1] -= m3 * s;
//            s = r0[2];
//            r1[2] -= m1 * s;
//            r2[2] -= m2 * s;
//            r3[2] -= m3 * s;
//            s = r0[3];
//            r1[3] -= m1 * s;
//            r2[3] -= m2 * s;
//            r3[3] -= m3 * s;
//            s = r0[4];
//            if (s != 0.0) {
//                r1[4] -= m1 * s;
//                r2[4] -= m2 * s;
//                r3[4] -= m3 * s;
//            }
//            s = r0[5];
//            if (s != 0.0) {
//                r1[5] -= m1 * s;
//                r2[5] -= m2 * s;
//                r3[5] -= m3 * s;
//            }
//            s = r0[6];
//            if (s != 0.0) {
//                r1[6] -= m1 * s;
//                r2[6] -= m2 * s;
//                r3[6] -= m3 * s;
//            }
//            s = r0[7];
//            if (s != 0.0) {
//                r1[7] -= m1 * s;
//                r2[7] -= m2 * s;
//                r3[7] -= m3 * s;
//            }
//            /* choose pivot - or die */
//            if (Math.Abs(r3[1]) > Math.Abs(r2[1]))
//                swapRows(ref r3, ref r2);
//            if (Math.Abs(r2[1]) > Math.Abs(r1[1]))
//                swapRows(ref r2, ref r1);
//            if (0.0 == r1[1])
//                return 0;
//            /* eliminate second variable */
//            m2 = r2[1] / r1[1];
//            m3 = r3[1] / r1[1];
//            r2[2] -= m2 * r1[2];
//            r3[2] -= m3 * r1[2];
//            r2[3] -= m2 * r1[3];
//            r3[3] -= m3 * r1[3];
//            s = r1[4];
//            if (0.0 != s) {
//                r2[4] -= m2 * s;
//                r3[4] -= m3 * s;
//            }
//            s = r1[5];
//            if (0.0 != s) {
//                r2[5] -= m2 * s;
//                r3[5] -= m3 * s;
//            }
//            s = r1[6];
//            if (0.0 != s) {
//                r2[6] -= m2 * s;
//                r3[6] -= m3 * s;
//            }
//            s = r1[7];
//            if (0.0 != s) {
//                r2[7] -= m2 * s;
//                r3[7] -= m3 * s;
//            }
//            /* choose pivot - or die */
//            if (Math.Abs(r3[2]) > Math.Abs(r2[2]))
//                swapRows(ref r3, ref r2);
//            if (0.0 == r2[2])
//                return 0;
//            /* eliminate third variable */
//            m3 = r3[2] / r2[2];
//            r3[3] -= m3 * r2[3]; r3[4] -= m3 * r2[4];
//            r3[5] -= m3 * r2[5]; r3[6] -= m3 * r2[6]; r3[7] -= m3 * r2[7];
//            /* last check */
//            if (0.0 == r3[3])
//                return 0;
//            s = 1.0 / r3[3];		/* now back substitute row 3 */
//            r3[4] *= s;
//            r3[5] *= s;
//            r3[6] *= s;
//            r3[7] *= s;
//            m2 = r2[3];			/* now back substitute row 2 */
//            s = 1.0 / r2[2];
//            r2[4] = s * (r2[4] - r3[4] * m2); r2[5] = s * (r2[5] - r3[5] * m2);
//            r2[6] = s * (r2[6] - r3[6] * m2); r2[7] = s * (r2[7] - r3[7] * m2);
//            m1 = r1[3];
//            r1[4] -= r3[4] * m1; r1[5] -= r3[5] * m1;
//            r1[6] -= r3[6] * m1; r1[7] -= r3[7] * m1;
//            m0 = r0[3];
//            r0[4] -= r3[4] * m0; r0[5] -= r3[5] * m0;
//            r0[6] -= r3[6] * m0; r0[7] -= r3[7] * m0;
//            m1 = r1[2];			/* now back substitute row 1 */
//            s = 1.0 / r1[1];
//            r1[4] = s * (r1[4] - r2[4] * m1); r1[5] = s * (r1[5] - r2[5] * m1);
//            r1[6] = s * (r1[6] - r2[6] * m1); r1[7] = s * (r1[7] - r2[7] * m1);
//            m0 = r0[2];
//            r0[4] -= r2[4] * m0; r0[5] -= r2[5] * m0;
//            r0[6] -= r2[6] * m0; r0[7] -= r2[7] * m0;
//            m0 = r0[1];			/* now back substitute row 0 */
//            s = 1.0 / r0[0];
//            r0[4] = s * (r0[4] - r1[4] * m0); r0[5] = s * (r0[5] - r1[5] * m0);
//            r0[6] = s * (r0[6] - r1[6] * m0); r0[7] = s * (r0[7] - r1[7] * m0);
			
//            output[0+4*0] = r0[4];
//            output[0+4*1] = r0[5]; output[0+4*2] = r0[6];
//            output[0+4*3] = r0[7]; output[1+4*0] = r1[4];
//            output[1+4*1] = r1[5]; output[1+4*2] = r1[6];
//            output[1+4*3] = r1[7]; output[2+4*0] = r2[4];
//            output[2+4*1] = r2[5]; output[2+4*2] = r2[6];
//            output[2+4*3] = r2[7]; output[3+4*0] = r3[4];
//            output[3+4*1] = r3[5]; output[3+4*2] = r3[6];
//            output[3+4*3] = r3[7];

//            return 1;
//        }

//    }
//}

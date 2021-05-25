using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Program
{
    public partial class Form1 : Form
    {
        bool loaded = false;
        // Камера
        Camera camera;
        float lastX;
        float lastY;
        bool firstMouse = true;
        bool isCapture = false;
        // Изображения
        Bitmap leftBitmap, rightBitmap;

        List<Vector3> points; // Массив точек
        List<Tuple<List<int>, Vector3>> polygons; // Массив полигонов
        bool visualMode = true;
        public Form1()
        {
            InitializeComponent();
            glControl1.MouseWheel += new MouseEventHandler(glControl1_MouseWheel);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Изменить
            openFileDialog1.InitialDirectory = @"//../";
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            loaded = true;
            GL.ClearColor(Color.FromArgb(150,150,150));
            camera = new Camera(new Vector3(110.0f));
            GL.Enable(EnableCap.DepthTest);
            float aspect = glControl1.AspectRatio;
            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView((float)(camera.Zoom * Math.PI / 180), aspect, 1, 1000);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref p);
            Matrix4 modelview = camera.GetViewMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);
            GL.Enable(EnableCap.Normalize);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Light(LightName.Light0, LightParameter.Position, new float[4] { 0, 30, 70, 1 });
            GL.Light(LightName.Light0, LightParameter.Diffuse, new float[3] { 1, 1, 1 });

            lastX = glControl1.Width / 2.0f;
            lastY = glControl1.Height / 2.0f;
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded)
                return;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float aspect = glControl1.AspectRatio;
            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView((float)(camera.Zoom * Math.PI / 180), aspect, 20, 1000);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref p);
            Matrix4 modelview = camera.GetViewMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            //Оси
            GL.Disable(EnableCap.Lighting);
            //Ox
            GL.Color3(Color.Blue);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(1000, 0, 0);
            GL.End();
            GL.Color3(Color.FromArgb(150, 150, 190));
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(-1000, 0, 0);
            GL.Vertex3(0, 0, 0);
            GL.End();
            //Oy
            GL.Color3(Color.Red);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1000, 0);
            GL.End();
            GL.Color3(Color.FromArgb(180, 150, 150));
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, -1000, 0);
            GL.Vertex3(0, 0, 0);
            GL.End();
            //Oz
            GL.Color3(Color.Green);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 0, 1000);
            GL.End();
            GL.Color3(Color.FromArgb(150, 180, 150));
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, -1000);
            GL.Vertex3(0, 0, 0);
            GL.End();
            GL.Enable(EnableCap.Lighting);
            if (visualMode)
            {
                GL.Begin(PrimitiveType.Points);
                foreach (Vector3 vec in points)
                    GL.Vertex3(vec.X, vec.Y, vec.Z);
                GL.End();
            }
            else
            {
                foreach (Tuple<List<int>, Vector3> pair in polygons)
                {
                    GL.Begin(PrimitiveType.Polygon);
                    GL.Normal3(pair.Item2.X, pair.Item2.Y, pair.Item2.Z);
                    foreach (int i in pair.Item1)
                        GL.Vertex3(points[i].X, points[i].Y, points[i].Z);
                    GL.End();
                }
            }

            glControl1.SwapBuffers();
        }

        //Обработка клавиш
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!loaded) return;
            switch (e.KeyCode)
            {
                case (Keys.W): camera.ProcessKeyboard(Camera.Camera_Movement.FORWARD); break;
                case (Keys.S): camera.ProcessKeyboard(Camera.Camera_Movement.BACKWARD); break;
                case (Keys.A): camera.ProcessKeyboard(Camera.Camera_Movement.LEFT); break;
                case (Keys.D): camera.ProcessKeyboard(Camera.Camera_Movement.RIGHT); break;
                case (Keys.M): visualMode = !visualMode; break;
                case (Keys.Escape): Cursor.Show(); isCapture = false; Cursor.Clip = Screen.PrimaryScreen.Bounds; break;
            }
            glControl1.Invalidate();
        }

        #region MouseProcessing
        private void glControl1_Click(object sender, EventArgs e)
        {
            Cursor.Hide();
            Rectangle rect = new Rectangle(this.Location.X + glControl1.Location.X + 20, this.Top + glControl1.Location.Y + 20, glControl1.Size.Width, glControl1.Size.Height);
            Cursor.Clip = rect;
            isCapture = true;
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCapture)
            {
                if (firstMouse)
                {
                    lastX = e.X;
                    lastY = e.Y;
                    firstMouse = false;
                }

                float xoffset = e.X - lastX;
                float yoffset = lastY - e.Y; // перевернуто, так как y-координаты идут снизу вверх

                camera.ProcessMouseMovement(xoffset, yoffset);
                Cursor.Position = new Point(Convert.ToInt32(Cursor.Position.X - xoffset), Convert.ToInt32(Cursor.Position.Y + yoffset));

                glControl1.Invalidate();
            }
        }

        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            camera.ProcessMouseScroll(e.Delta);
        }
        #endregion

        #region ImagesLoad
        private void leftPictureBox_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            leftImageLabel.Visible = false;
            leftPictureBox.Image = Image.FromFile(openFileDialog1.FileName);
            leftBitmap = new Bitmap(openFileDialog1.FileName);

        }

        private void rightPictureBox_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            rightImageLabel.Visible = false;
            rightPictureBox.Image = Image.FromFile(openFileDialog1.FileName);
            rightBitmap = new Bitmap(openFileDialog1.FileName);
        }

        #region picturesColor
        //При наведении на левый Picture
        private void leftPictureBox_MouseEnter(object sender, EventArgs e)
        {
            leftPictureBox.BackColor = Color.NavajoWhite;
            leftImageLabel.BackColor = Color.NavajoWhite;
        }
        //При покидании области левого Picture
        private void leftPictureBox_MouseLeave(object sender, EventArgs e)
        {
            leftPictureBox.BackColor = Color.Moccasin;
            leftImageLabel.BackColor = Color.Moccasin;
        }
        //При наведении на правый Picture
        private void rightPictureBox_MouseEnter(object sender, EventArgs e)
        {
            rightPictureBox.BackColor = Color.NavajoWhite;
            rightImageLabel.BackColor = Color.NavajoWhite;
        }

        //При покидании области правого Picture
        private void rightPictureBox_MouseLeave(object sender, EventArgs e)
        {
            rightPictureBox.BackColor = Color.Moccasin;
            rightImageLabel.BackColor = Color.Moccasin;
        }

        private void calcButton_MouseEnter(object sender, EventArgs e)
        {
            calcButton.BackColor = Color.NavajoWhite;
        }

        private void calcButton_MouseLeave(object sender, EventArgs e)
        {
            calcButton.BackColor = Color.Moccasin;
        }
        #endregion

        #endregion

        private void calcButton_Click(object sender, EventArgs e)
        {
            StereoEffect.calcDepthMap(leftBitmap, rightBitmap, numericUpDown1.Value, numericUpDown2.Value, out points, out polygons);
            leftPictureBox.Visible = false;
            rightPictureBox.Visible = false;
            calcButton.Visible = false;
            label1.Visible = false;
            label2.Visible = false;
            numericUpDown1.Visible = false;
            numericUpDown2.Visible = false;
            glControl1.Visible = true;
            button1.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            glControl1.Visible = false;
            button1.Visible = false;
            leftPictureBox.Visible = true;
            rightPictureBox.Visible = true;
            calcButton.Visible = true;
            label1.Visible = true;
            label2.Visible = true;
            numericUpDown1.Visible = true;
            numericUpDown2.Visible = true;
        }
    }
}
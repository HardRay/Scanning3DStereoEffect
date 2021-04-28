using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace Program
{
    public partial class Form1 : Form
    {
        struct dpoint
        {
            public float d;
            public Vector3 n;
            public int point;
        }

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
        private void glControl1_Click(object sender, EventArgs e)
        {
            Cursor.Hide();
            Rectangle rect = new Rectangle(this.Location.X+glControl1.Location.X+20, this.Top+glControl1.Location.Y+20, glControl1.Size.Width, glControl1.Size.Height);
             Cursor.Clip = rect;
            isCapture = true;
        }

        #region MouseProcessing
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
            leftBitmap = CutBounds(leftBitmap);
        }

        private void rightPictureBox_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            rightImageLabel.Visible = false;
            rightPictureBox.Image = Image.FromFile(openFileDialog1.FileName);
            rightBitmap = new Bitmap(openFileDialog1.FileName);
            rightBitmap = CutBounds(rightBitmap);
        }

        private Bitmap CutBounds(Bitmap bitmap)
        {
            int left = 0, right = bitmap.Width, up = 0, bot = bitmap.Height;
            bool leftFlag = true, rightFlag = true, upFlag = true, botFlag = true;
            Color backColor = bitmap.GetPixel(0, 0);
            //Отрезаем слева и справа
            for (int i = 0; i < bitmap.Width; i++)
                if (!(leftFlag || rightFlag))
                    break;
                else
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        if (!isEquaColors(bitmap.GetPixel(i, j),backColor,3))
                            leftFlag = false;
                        if (!isEquaColors(bitmap.GetPixel(bitmap.Width - i - 1, j),backColor,3))
                            rightFlag = false;
                    }
                    if (leftFlag)
                        left++;
                    if (rightFlag)
                        right--;
                }
            //Отрезаем сверху и снизу
            for (int j = 0; j < bitmap.Height; j++)
                if (!(upFlag || rightFlag))
                    break;
                else
                {
                    for (int i = 0; i < bitmap.Width; i++)
                    {
                        if (!isEquaColors(bitmap.GetPixel(i, j), backColor, 3))
                            upFlag = false;
                        if (!isEquaColors(bitmap.GetPixel(i, bitmap.Height - j - 1), backColor, 3))
                            botFlag = false;
                    }
                    if (upFlag)
                        up++;
                    if (botFlag)
                        bot--;
                }
            Bitmap result = new Bitmap(right - left + 1, bot - up + 1);
            for (int j = up; j < bot; j++)
                for (int i = left; i < right; i++)
                    result.SetPixel(i - left, j - up, bitmap.GetPixel(i, j));
            return result;
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
            calcDepthMap(numericUpDown1.Value, numericUpDown2.Value);
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

        //Рассчёт карты глубины
        private void calcDepthMap(decimal f,decimal b)
        {
            points = new List<Vector3>();
            polygons = new List<Tuple<List<int>, Vector3>>();
            Color backColor = leftBitmap.GetPixel(0, 0); // Исправить
            int width = Math.Max(leftBitmap.Width, rightBitmap.Width);
            dpoint[,] Data = new dpoint[leftBitmap.Height, width];
            for (int i = 0; i < leftBitmap.Height; i++)
                for (int j = 0; j < width; j++)
                {
                    Data[i, j].point = -1;
                }
            for (int y = 0; y < leftBitmap.Height; y++)
            {
                for (int leftX = 0; leftX < leftBitmap.Width; leftX++)
                {
                    if (isEquaColors(leftBitmap.GetPixel(leftX, y), backColor, 3))
                        continue;
                    int maxOffset = 30;
                    //Поиск парного пикселя на правом снимке
                    int from = Math.Max(0, leftX - maxOffset); //Левая граница поиска
                    int before = Math.Min(leftBitmap.Width, leftX + maxOffset); //Правая граница поиска
                    float deviation = 255;
                    int pairX = leftX;
                    for (int rightX = from; rightX < before; rightX++)
                    {
                        float sum = 0;
                        int count = 0;
                        for (int i = -1; i <= 1; i++)
                            if ((y + i >= 0) && (y + i < leftBitmap.Height) && (y + i < rightBitmap.Height))
                                for (int j = -1; j <= 1; j++)
                                    if ((leftX + j >= 0) && (leftX + j < leftBitmap.Width) && (rightX + j >= 0) && (rightX + j < rightBitmap.Width))
                                    {
                                        sum += SubColors(leftBitmap.GetPixel(leftX + j, y + i), rightBitmap.GetPixel(rightX + j, y + i));
                                        count++;
                                    }
                        float newDeviation = (sum / count);
                        if (newDeviation < deviation)
                        {
                            deviation = newDeviation;
                            pairX = rightX;
                        }
                    }
                    //Рассчёт глубины
                    if (leftX != pairX)
                    {
                        float z = (float)f * (float)b / (pairX - leftX);
                        int x = leftX;
                        points.Add(new Vector3(x, y, z));
                        Data[y, x].d = z;
                        Data[y, x].point = points.Count - 1;
                    }
                    
                }
            }
            MessageBox.Show("hi!");
            // построение полигонов
            float Qz, Pz;
            for (int i = 1; i < leftBitmap.Height - 2; i++)
                for (int j = 1; j < width - 2; j++)
                    if (Data[i, j].point != -1 && Data[i + 1, j + 1].point != -1)
                    {
                        // если есть "левый" треугольник
                        if (Data[i + 1, j].point != -1)
                        {
                            Qz = Data[i + 1, j].d - Data[i, j].d;
                            Pz = Data[i + 1, j + 1].d - Data[i, j].d;
                            Vector3 N = new Vector3(Pz - Qz, Qz, 1);
                            List<int> ind1 = new List<int>();
                            ind1.Add(Data[i, j].point);
                            ind1.Add(Data[i + 1, j].point);
                            ind1.Add(ind1[1] + 1);
                            polygons.Add(new Tuple<List<int>, Vector3>(ind1, N));
                        }
                        // если есть "правый" треугольник
                        if (Data[i, j + 1].point != -1)
                        {
                            Qz = Data[i, j + 1].d - Data[i, j].d;
                            Pz = Data[i + 1, j + 1].d - Data[i, j].d;
                            Vector3 N = new Vector3(Qz, Qz - Pz, 1);
                            List<int> ind2 = new List<int>();
                            ind2.Add(Data[i, j].point);
                            ind2.Add(Data[i + 1, j + 1].point);
                            ind2.Add(ind2[0] + 1);
                            polygons.Add(new Tuple<List<int>, Vector3>(ind2, N));
                        }
                    }
        }

        //Находит условную разность между цветами
        private float SubColors(Color color1, Color color2)
        {
            int sub = Math.Abs(color1.A - color2.A);
            sub += Math.Abs(color1.R - color2.R);
            sub += Math.Abs(color1.G - color2.G);
            sub += Math.Abs(color1.B - color2.B);
            return (sub / 4);
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

        //Сравнивает цвета с пределом допустимости limit
        private bool isEquaColors(Color color1, Color color2, int limit)
        {
            if (Math.Abs(color1.R - color2.R) > limit)
                return false;
            if (Math.Abs(color1.G - color2.G) > limit)
                return false;
            if (Math.Abs(color1.B - color2.B) > limit)
                return false;
            return true;
        }
    }
}
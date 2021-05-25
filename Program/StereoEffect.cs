using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace Program
{
    static class StereoEffect
    {
        struct dpoint
        {
            public float d;
            public Vector3 n;
            public int point;
        }

        //Находит условную разность между цветами
        static private float SubColors(Color color1, Color color2)
        {
            int sub = Math.Abs(color1.A - color2.A);
            sub += Math.Abs(color1.R - color2.R);
            sub += Math.Abs(color1.G - color2.G);
            sub += Math.Abs(color1.B - color2.B);
            return (sub / 4);
        }

        //Сравнивает цвета с пределом допустимости limit
        static private bool isEquaColors(Color color1, Color color2, int limit)
        {
            if (Math.Abs(color1.R - color2.R) > limit)
                return false;
            if (Math.Abs(color1.G - color2.G) > limit)
                return false;
            if (Math.Abs(color1.B - color2.B) > limit)
                return false;
            return true;
        }

        static private float NormColor(Color color)
        {
            float sum = color.R;
            sum += color.G;
            sum += color.B;
            return sum / 255;
        }

        static public Bitmap CutBounds(Bitmap bitmap)
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
                        if (!isEquaColors(bitmap.GetPixel(i, j), backColor, 3))
                            leftFlag = false;
                        if (!isEquaColors(bitmap.GetPixel(bitmap.Width - i - 1, j), backColor, 3))
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

        static private List<List<float>> CorrelationCalc(Bitmap bitmap, int[,] mask)
        {
            List<List<float>> corr = new List<List<float>>();
            for (int i = 0; i < bitmap.Height; i++)
            {
                List<float> row = new List<float>();
                for (int j = 0; j < bitmap.Width; j++)
                {
                    //Надеваем маску
                    float sum = 0;
                    int radius = Convert.ToInt32(mask.GetUpperBound(0) / 2);
                    int leftLimit;
                    int rightLimit;
                    int upLimit;
                    int botLimit;
                    //левая граница маски
                    if (j > radius)
                        leftLimit = -radius;
                    else
                        leftLimit = -j;
                    //правая граница маски
                    if (j < (bitmap.Width - radius - 1))
                        rightLimit = radius;
                    else
                        rightLimit = bitmap.Width - 1 - j;
                    //верхняя граница маски
                    if (i > radius)
                        upLimit = -radius;
                    else
                        upLimit = -i;
                    //нижняя граница маски
                    if (i < (bitmap.Height - radius - 1))
                        botLimit = radius;
                    else
                        botLimit = bitmap.Height - 1 - i;
                    for (int mask_i = upLimit; mask_i <= botLimit; mask_i++)
                        for (int mask_j = leftLimit; mask_j < rightLimit; mask_j++)
                            sum += (float)Math.Pow(NormColor(bitmap.GetPixel(j + mask_j, i + mask_i)) * mask[mask_i + radius, mask_j + radius],2);
                    row.Add((float)Math.Sqrt(sum));
                }
                corr.Add(row);
            }
                
            return corr;
        }

        static private string GetRedPoints(Bitmap bitmap)
        {
            string str = "";
            int count = 0;
            for (int i = 0; i < bitmap.Height; i++)
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Color color = bitmap.GetPixel(j, i);
                    if (color.R == 255 && color.G < color.R && color.B < color.R)
                    {
                        count++;
                        str += String.Format("{0}. Позиция: ({1},{2}) Цвет: ({3},{4},{5})\n",count,j,i,color.R,color.G,color.B);
                    }
                }
            return str;
                    
        }

        //Рассчёт карты глубины
        static public void calcDepthMap(Bitmap leftImage, Bitmap rightImage, decimal f, decimal b, out List<Vector3> points, out List<Tuple<List<int>, Vector3>> polygons)
        {
            int[,] mask =
            {
                {0,0,0,1,0,0,0},
                {0,1,1,2,1,1,0},
                {0,1,2,3,2,1,0},
                {1,2,3,4,3,2,1},
                {0,1,2,3,2,1,0},
                {0,1,1,2,1,1,0},
                {0,0,0,1,0,0,0},
            };
            points = new List<Vector3>();
            polygons = new List<Tuple<List<int>, Vector3>>();
            //string str = "Левая:\n" + GetRedPoints(leftImage) + "Правая:\n" + GetRedPoints(rightImage);
            Bitmap leftBitmap = CutBounds(leftImage);
            Bitmap rightBitmap = CutBounds(rightImage);
            //str += "Левая:\n" + GetRedPoints(leftBitmap) + "Правая:\n" + GetRedPoints(rightBitmap);
            //MessageBox.Show(str);
            List<List<float>> leftCorr = CorrelationCalc(leftBitmap,mask);
            List<List<float>> rightCorr = CorrelationCalc(rightBitmap, mask);
            Color backColor = leftBitmap.GetPixel(0, 0); // Исправить
            int width = Math.Max(leftBitmap.Width, rightBitmap.Width);
            dpoint[,] Data = new dpoint[leftBitmap.Height, width];
            for (int i = 0; i < leftBitmap.Height; i++)
                for (int j = 0; j < width; j++)
                {
                    Data[i, j].point = -1;
                }
            for (int y = 0; y < Math.Min(leftBitmap.Height,rightBitmap.Height); y++)
            {
                for (int leftX = 0; leftX < leftBitmap.Width; leftX++)
                {
                    if (isEquaColors(leftBitmap.GetPixel(leftX, y), backColor, 1))
                        continue;
                    int maxOffset = 20;
                    //Поиск парного пикселя на правом снимке
                    int from = Math.Max(0, leftX - maxOffset); //Левая граница поиска
                    int before = Math.Min(rightBitmap.Width, leftX + maxOffset); //Правая граница поиска
                    float deviation = leftCorr[y][leftX];
                    int pairX = leftX;
                    for (int rightX = from; rightX < before; rightX++)
                    {
                        float newDeviation = Math.Abs(leftCorr[y][leftX] - rightCorr[y][rightX]);
                        if (newDeviation < deviation)
                        {
                            deviation = newDeviation;
                            pairX = rightX;
                        }
                    }
                    //Рассчёт глубины
                    if (leftX != pairX)
                    {
                        float z = (pairX - leftX) / ((float)f * (float)b);
                        int x = leftX;
                        points.Add(new Vector3(x, y, z));
                        Data[y, x].d = z;
                        Data[y, x].point = points.Count - 1;
                    }
                }
            }
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
    }
}

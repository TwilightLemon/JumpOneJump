using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpOneJump
{
    class PlayJumpJump
    {
        private static readonly int confidenceItv = 3;    // 两个rgb标准差小于等于3认为是同一元素
        private static readonly Pixel manXRgb = new Pixel { pixel = new byte[] { 54, 63, 102 } };   // 小人X坐标rgb
        private static readonly Pixel manYRgb = new Pixel { pixel = new byte[] { 43, 43, 73 } };   // 小人Y坐标rgb
        private static readonly double startYPer = 0.15625;   // 分数下一行Y为第289,取 300 / 1920 = 0.15625, 从下一行开始搜索目标
        private static readonly double Speed = 17.0 / 24; // 速度,最重要的因素,这也是约摸算出来的
        private static readonly string[] TouchCoor = new string[] { "800", "1700" };    // 触屏位置
        private static readonly string Format = "png";  // 本人用机子截取为png,也可不设格式(实测bitmap与ps cc打开同一jpg,同一像素点rgb值不一致,怀疑是bitmap打开jpg会有失真)
        private static readonly string TempDir = "/sdcard/";
        private static readonly string SaveDir = "temp/";
        private static readonly string CaptureScreen_Command = $"-s {{0}} shell screencap -p {TempDir}{{1}}";
        private static readonly string CopyFile_Command = $"-s {{0}} pull {TempDir}{{1}} \"{SaveDir}{{1}}\"";
        private static readonly string RemoveFile_Command = $"-s {{0}} shell rm  {TempDir}{{1}}";
        private static readonly string LongPress_Command = "shell input touchscreen swipe {0} {1} {0} {1} {2}";
        private Cmd myCmd;
        private string adbCmdPrefix;
        private string result;
        public List<string> devices;

        public PlayJumpJump(string adbPath)
        {
            myCmd = new Cmd();
            adbCmdPrefix = $"\"{adbPath}\" ";
            if (!Directory.Exists(SaveDir))
            {
                Directory.CreateDirectory(SaveDir);
            }
        }
        public void Init()
        {
            myCmd = new Cmd();
        }
        public bool GetDevices()
        {
            devices = new List<string>();
            myCmd.ExecuteCmd(ReturnCommand("devices"));
            result = myCmd.GetExcResult();
            foreach (string line in result.Split(new char[] { '\n' }))
            {
                if (line.Contains("device"))
                {
                    List<string> items = line.Split(new char[] { '\t', '\r' }, StringSplitOptions.None).ToList();
                    if (items.Count > 1)
                    {
                        devices.Add(items[items.IndexOf("device") - 1]);
                    }
                }
            }
            return devices.Count > 0 ? true : false;
        }
        public string CaptureScreen()
        {
            string fileName = $"temp{DateTime.Now.ToString("HHmmssfff")}.{Format}";
            myCmd.ExecuteCmd(ReturnCommand(CaptureScreen_Command, new string[] { devices[0], fileName }));
            myCmd.ExecuteCmd(ReturnCommand(CopyFile_Command, new string[] { devices[0], fileName }));
            myCmd.ExecuteCmd(ReturnCommand(RemoveFile_Command, new string[] { devices[0], fileName }));
            return AppDomain.CurrentDomain.BaseDirectory + SaveDir + fileName;
        }
        public static unsafe Pixel[][] GetPixelArray(string path)
        {
            Bitmap bitmap = new Bitmap(path);
            int depth = Image.GetPixelFormatSize(bitmap.PixelFormat);
            if (depth == 24)
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                Pixel[][] pixelArray = new Pixel[height][];
                for (int i = 0; i < pixelArray.Length; i++) pixelArray[i] = new Pixel[width];

                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                byte* ptr = (byte*)bmpData.Scan0;
                for (int i = 0; i < pixelArray.Length; i++)
                {
                    for (int j = 0; j < pixelArray[i].Length; j++)
                    {
                        pixelArray[i][j] = new Pixel { pixel = new byte[] { *(ptr + 2), *(ptr + 1), *ptr } };
                        ptr += 3;
                    }
                    ptr += bmpData.Stride - 3 * bmpData.Width;  // 减去占位字节(可能出于性能或兼容性考虑,Stride为4的倍数)
                }

                bitmap.UnlockBits(bmpData);
                return pixelArray;
            }
            else if (depth == 32)
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                Pixel[][] pixelArray = new Pixel[height][];
                for (int i = 0; i < pixelArray.Length; i++) pixelArray[i] = new Pixel[width];

                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

                byte* ptr = (byte*)bmpData.Scan0;
                for (int i = 0; i < pixelArray.Length; i++)
                {
                    for (int j = 0; j < pixelArray[i].Length; j++)
                    {
                        pixelArray[i][j] = new Pixel { pixel = new byte[] { *(ptr + 2), *(ptr + 1), *ptr } };
                        ptr += 4;  // 每3个字节忽略1个透明度字节
                    }
                }

                bitmap.UnlockBits(bmpData);
                return pixelArray;
            }
            else
            {
                return null;
            }
        }
        public void Jump2Happy()
        {
            string picture = CaptureScreen();
            Pixel[][] pixelArray = GetPixelArray(picture);
            int[] curCoor = GetCurCoordinates(pixelArray);
            int[] destCoor = GetDestCoordinates(pixelArray, curCoor);
            double distance = Math.Round(Math.Sqrt(Math.Pow(Math.Abs(destCoor[0] - curCoor[0]), 2) + Math.Pow(Math.Abs(destCoor[1] - curCoor[1]), 2)), 3);
            int time = (int)(distance / Speed);
            Console.WriteLine($"from [{curCoor[0]},{curCoor[1]}]\tto [{destCoor[0]},{destCoor[1]}]  distance≈{distance}  take≈{time}ms  ==>> Jump ");
            myCmd.ExecuteCmd(ReturnCommand(LongPress_Command, new string[] { TouchCoor[0], TouchCoor[1], time.ToString() }));
        }
        public static int[] GetCurCoordinates(Pixel[][] pixelArray)
        {
            int[] coordinates = new int[2];
            List<int[]> xList = new List<int[]>();
            List<int[]> yList = new List<int[]>();
            // y从max -> 0,遍历x轴像素
            for (int i = pixelArray.Length - 1; i >= 0; i--)
            {
                for (int j = 0; j < pixelArray[i].Length; j++)
                {
                    if (isSameElement(pixelArray[i][j], manXRgb, confidenceItv))
                    {
                        xList.Add(new int[] { j, i });
                    }
                }
                if (xList.Count > 0) break;
            }
            coordinates[0] = xList.Count > 0 ? (xList[0][0] + xList[xList.Count - 1][0]) / 2 : 0;

            // x从0 -> max,遍历y轴像素
            for (int i = 0; i < pixelArray[0].Length; i++)
            {
                for (int j = pixelArray.Length - 1; j >= 0; j--)
                {
                    if (isSameElement(pixelArray[j][i], manYRgb, confidenceItv))
                    {
                        yList.Add(new int[] { i, j });
                    }
                }
                if (yList.Count > 0) break;
            }
            coordinates[1] = yList.Count > 0 ? (yList[0][1] + yList[yList.Count - 1][1]) / 2 : 0;

            return coordinates;
        }
        public static int[] GetDestCoordinates(Pixel[][] pixelArray, int[] curCoor)
        {
            Pixel enviRgb;   // 排除rgb采样
            Pixel destRgb = null;   // 采样
            int[] coordinates = new int[2];
            List<int[]> xList = new List<int[]>();
            List<int[]> yList = new List<int[]>();
            int startY = (int)(pixelArray.Length * startYPer);
            int start, end, inc;
            if (curCoor[0] < (pixelArray[0].Length / 2))
            {
                start = curCoor[0] + 40;
                end = pixelArray[0].Length;
            }
            else
            {
                start = 0;
                end = curCoor[0] - 40;
            }
            // y从0 -> max,遍历x轴像素
            for (int i = startY; i < pixelArray.Length; i++)
            {
                enviRgb = pixelArray[i][0];
                for (int j = start; j < end; j++)
                {
                    if (!isSameElement(pixelArray[i][j], enviRgb, confidenceItv))
                    {
                        xList.Add(new int[] { j, i });
                        if (destRgb == null) destRgb = pixelArray[i][j];
                    }
                }
                if (xList.Count > 0) break;
            }
            coordinates[0] = xList.Count > 0 ? (xList[0][0] + xList[xList.Count - 1][0]) / 2 : 0;

            // x从0 -> max,遍历y轴像素
            if (coordinates[0] < (pixelArray[0].Length / 2))
            {
                start = 0;
                end = pixelArray[0].Length - 1;
                inc = 1;
            }
            else
            {
                start = pixelArray[0].Length - 1;
                end = 0;
                inc = -1;
            }
            bool isFond = false;
            for (int i = start; i != end; i += inc)
            {
                for (int j = startY; j < curCoor[1]; j++)
                {
                    if (isSameElement(pixelArray[j][i], destRgb, confidenceItv))
                    {
                        coordinates[1] = j;
                        isFond = true;
                        break;
                    }
                }
                if (isFond) break;
            }

            return coordinates;
        }
        public static bool isSameElement(Pixel pixel1, Pixel pixel2, int confidence)
        {
            return Math.Pow(pixel1.pixel[0] - pixel2.pixel[0], 2) + Math.Pow(pixel1.pixel[1] - pixel2.pixel[1], 2) + Math.Pow(pixel1.pixel[2] - pixel2.pixel[2], 2) <= 3 * Math.Pow(confidence, 2);
        }
        public string ReturnCommand(string command, string[] parameter)
        {
            return adbCmdPrefix + string.Format(command, parameter);
        }
        public string ReturnCommand(string command, string parameter)
        {
            return adbCmdPrefix + string.Format(command, parameter);
        }
        public string ReturnCommand(string command)
        {
            return adbCmdPrefix + command;
        }
        public void DisposeProcess()
        {
            myCmd.DisposeProcess();
            myCmd = null;
        }

    }
}
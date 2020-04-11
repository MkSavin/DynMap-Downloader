using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using System.Drawing;

namespace DynMap_Flat_Downloader {
    class Program {

        public static string host;

        public static string imageTemplate = "tiles/world/flat/1_-1/%X%_%Y%.png";

        public static string cacheDir = "cache/";
        public static string resultFile = "result.png";

        public static uint imageWidth = 128;
        public static uint imageHeight = 128;

        public static int xStart;
        public static int yStart;

        public static int width;
        public static int height;

        static object locker = new object();

        static void Main(string[] args) {

            try {
                if (Directory.Exists(cacheDir)) {
                    Directory.Delete(cacheDir, true);
                }

                Console.Write("Введите сокет сервера [по умолч: 217.182.203.129:28537]: ");
                var socket = Console.ReadLine();
                host = "http://" + (socket == "" ? "217.182.203.129:28537" : socket) + "/";

                Thread.Sleep(1);

                xStart = ReadInt("Точка старта. Координата X [по умолч: 41]: ") ?? 41;
                yStart = ReadInt("Точка старта. Координата Y [по умолч: -20]: ") ?? -20;

                width = ReadInt("Ширина карты (в кол-ве тайлов) [по умолч: 12]: ") ?? 12;
                height = ReadInt("Высота карты (в кол-ве тайлов) [по умолч: 12]: ") ?? 12;

                Console.WriteLine("Будет загружено " + (width * height) + " (" + width + " * " + height + ") тайл[а/ов] с сайта " + host + ", начиная с точки (" + xStart + ", " + yStart + ")");
                
                Bitmap resultImage = new Bitmap((int)imageWidth * width, (int)imageHeight * height);
                
                Console.WriteLine("Размер результирующего изображения: (" + (int)imageWidth * width + "px, " + (int)imageHeight * height + "px)");

                Directory.CreateDirectory(cacheDir);

                var processorsCount = Math.Max(Environment.ProcessorCount / 2, 1);

                float widthByProc = (float)width / processorsCount;

                Console.WriteLine("Процесс будет распараллелен на " + processorsCount + " потоков");

                Console.WriteLine("Начало загрузки файлов...");

                Parallel.For(0, processorsCount, (proc) => {
                    using (WebClient wc = new WebClient()) {
                        wc.Headers.Add("Cache-Control", "no-cache");

                        string downloadLink;

                        int xR, yR;

                        for (int x = (int)(widthByProc * proc); x < (int)(widthByProc * (1 + proc)); x++) {
                            for (int y = 0; y < height; y++) {
                                xR = xStart + x;
                                yR = yStart - y;
                                downloadLink = host + imageTemplate.Replace("%X%", xR + "").Replace("%Y%", yR + "");
                                Console.WriteLine("Загрузка: " + downloadLink);

                                wc.DownloadFile(
                                    new Uri(downloadLink),
                                    cacheDir + "i_x" + xR + "_y" + yR + ".png"
                                    );

                                using (Image image = Image.FromFile(cacheDir + "i_x" + xR + "_y" + yR + ".png")) {
                                    lock (locker) {
                                        using (Graphics g = Graphics.FromImage(resultImage)) {
                                            g.DrawImage(image, x * imageWidth, y * imageHeight, imageWidth, imageHeight);
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                var cahceFilesLength = Directory.GetFiles(cacheDir).Length;

                Console.WriteLine("Загрузка " + cahceFilesLength + " файлов завершена");

                Console.WriteLine("Результирующий файл: " + resultFile);

                resultImage.Save(resultFile, System.Drawing.Imaging.ImageFormat.Png);

                Console.WriteLine("Карта успешно создана");
            } catch (Exception e) {
                Console.WriteLine("Ошибка:");
                Console.WriteLine(e);
                Console.ReadKey();
                return;
            }

            Console.WriteLine("... Нажмите что-нибудь, чтобы закрыть ...");
            Console.ReadKey();

        }

        public static int? ReadInt(string message = null) {

            if (message != null) {
                Console.Write(message);
            }

            var input = Console.ReadLine();

            if (input == "" || int.TryParse(input, out int i)) {
                return null;
            }

            return i;

        }

    }
}

using OpenCvSharp;
using System;
using System.IO;

namespace WaferScan.Models
{
    public class WaferImage
    {
        private readonly Random _random = new Random();
        private static int _imageCounter = 0;

        public (string originalName, string savedName, DateTime generatedTime) GenerateAndSaveRandomWaferImage(string imagePath, string saveFolder)
        {
            using (Mat originalImage = Cv2.ImRead(imagePath, ImreadModes.Color))
            {
                if (originalImage.Empty())
                {
                    throw new Exception($"이미지를 불러올 수 없습니다. 경로: {imagePath}");
                }

                return GenerateAndSaveRandomWaferImage(originalImage, saveFolder);
            }
        }

        public (string originalName, string savedName, DateTime generatedTime) GenerateAndSaveRandomWaferImage(Mat originalImage, string saveFolder)
        {
            Mat result = GenerateRandomWaferImage(originalImage);

            // 이미지 저장 로직
            string originalName = $"scan{_imageCounter++:D4}";
            string savedName = $"{Guid.NewGuid()}.png";
            string savePath = Path.Combine(saveFolder, savedName);

            result.SaveImage(savePath);

            DateTime generatedTime = DateTime.Now;

            Console.WriteLine($"이미지 저장됨: {savePath}");

            return (originalName, savedName, generatedTime);
        }

        public Mat GenerateRandomWaferImage(string imagePath)
        {
            using (Mat originalImage = Cv2.ImRead(imagePath, ImreadModes.Color))
            {
                if (originalImage.Empty())
                {
                    throw new Exception($"이미지를 불러올 수 없습니다. 경로: {imagePath}");
                }

                return GenerateRandomWaferImage(originalImage);
            }
        }

        public Mat GenerateRandomWaferImage(Mat originalImage)
        {
            Console.WriteLine($"이미지 로드됨: 채널 수={originalImage.Channels()}, 크기={originalImage.Size()}");

            // 랜덤 매개변수 계산
            double scale = _random.NextDouble() * (1.0 - 0.8) + 0.8; // 80%에서 100% 사이의 크기
            double angle = _random.NextDouble() * 360; // 0도에서 360도 사이의 회전

            // 회전 중심 계산
            Point2f center = new Point2f(originalImage.Cols / 2f, originalImage.Rows / 2f);

            // 회전 행렬 생성
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, scale);

            // 회전된 이미지의 크기 계산
            int boundWidth = originalImage.Cols;
            int boundHeight = originalImage.Rows;

            // 회전 행렬 조정 (중앙 정렬을 위해)
            rotationMatrix.Set(0, 2, rotationMatrix.Get<double>(0, 2) + boundWidth / 2 - center.X);
            rotationMatrix.Set(1, 2, rotationMatrix.Get<double>(1, 2) + boundHeight / 2 - center.Y);

            // 회전 및 크기 조정 적용
            Mat rotatedImage = new Mat();
            Cv2.WarpAffine(originalImage, rotatedImage, rotationMatrix, new Size(boundWidth, boundHeight), InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(0, 0, 0));

            // 랜덤 이동 계산 (이미지가 화면 밖으로 나가지 않도록)
            int maxTranslateX = (int)(originalImage.Cols * (1 - scale) / 2);
            int maxTranslateY = (int)(originalImage.Rows * (1 - scale) / 2);
            int translateX = _random.Next(-maxTranslateX, maxTranslateX + 1);
            int translateY = _random.Next(-maxTranslateY, maxTranslateY + 1);

            // 이동 적용
            Mat translationMatrix = new Mat(2, 3, MatType.CV_64F);
            translationMatrix.Set<double>(0, 0, 1);
            translationMatrix.Set<double>(0, 1, 0);
            translationMatrix.Set<double>(0, 2, translateX);
            translationMatrix.Set<double>(1, 0, 0);
            translationMatrix.Set<double>(1, 1, 1);
            translationMatrix.Set<double>(1, 2, translateY);

            Mat result = new Mat();
            Cv2.WarpAffine(rotatedImage, result, translationMatrix, originalImage.Size(), InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(0, 0, 0));

            Console.WriteLine($"적용된 변환: 크기={scale}, 각도={angle}, 이동X={translateX}, 이동Y={translateY}, 최종크기={result.Size()}");

            return result;
        }
    }
}
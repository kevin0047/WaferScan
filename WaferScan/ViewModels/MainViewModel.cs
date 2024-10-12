using System;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using WaferScan.Models;

namespace WaferScan.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private BitmapSource _originalImage;
        public BitmapSource OriginalImage
        {
            get => _originalImage;
            set
            {
                _originalImage = value;
                OnPropertyChanged(nameof(OriginalImage));
            }
        }

        private BitmapSource _generatedImage;
        public BitmapSource GeneratedImage
        {
            get => _generatedImage;
            set
            {
                _generatedImage = value;
                OnPropertyChanged(nameof(GeneratedImage));
            }
        }

        public ICommand GenerateImageCommand { get; }

        private readonly WaferImage _waferImageModel;
        private readonly string _imagePath = "Images/wafer.png";

        public MainViewModel()
        {
            _waferImageModel = new WaferImage();
            GenerateImageCommand = new RelayCommand(GenerateImage);
            LoadOriginalImage();
            GenerateImage();
        }

        private void LoadOriginalImage()
        {
            using (Mat originalMat = Cv2.ImRead(_imagePath, ImreadModes.Color))
            {
                if (originalMat.Empty())
                {
                    throw new Exception($"Could not load the image from path: {_imagePath}");
                }
                OriginalImage = ConvertMatToBitmapSource(originalMat);
            }
        }

        private void GenerateImage()
        {
            using (Mat generatedImage = _waferImageModel.GenerateRandomWaferImage(_imagePath))
            {
                GeneratedImage = ConvertMatToBitmapSource(generatedImage);
            }
        }


        private BitmapSource ConvertMatToBitmapSource(Mat image)
        {
            // 이미지 형식 확인
            System.Windows.Media.PixelFormat pixelFormat;
            switch (image.Channels())
            {
                case 1:
                    pixelFormat = System.Windows.Media.PixelFormats.Gray8;
                    break;
                case 3:
                    pixelFormat = System.Windows.Media.PixelFormats.Bgr24;
                    break;
                case 4:
                    pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
                    break;
                default:
                    throw new Exception($"Unsupported number of channels: {image.Channels()}");
            }

            // stride 계산
            int stride = image.Cols * image.Channels();

            // 이미지 데이터 추출
            byte[] imageData = new byte[image.Rows * stride];
            System.Runtime.InteropServices.Marshal.Copy(image.Data, imageData, 0, imageData.Length);

            // BitmapSource 생성
            return BitmapSource.Create(
                image.Cols,
                image.Rows,
                96, 96, // DPI
                pixelFormat,
                null,
                imageData,
                stride);
        }
    }
}
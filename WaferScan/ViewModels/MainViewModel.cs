using System;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using WaferScan.Models;
using WaferScan.Services;
using System.IO;
using System.Reflection;

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
        private readonly DatabaseService _databaseService;
        private readonly string _imagePath = "Images/wafer.png";
        private readonly string _saveFolder;

        public MainViewModel()
        {
            _waferImageModel = new WaferImage();
            _databaseService = new DatabaseService("mongodb://localhost:27017", "WaferScanDB");
            GenerateImageCommand = new RelayCommand(GenerateImage);

            // 프로젝트 폴더 경로 가져오기
            string projectFolder = GetProjectFolder();

            // 이미지 경로와 저장 폴더 설정
            _imagePath = Path.Combine(projectFolder, "Images", "wafer.png");
            _saveFolder = Path.Combine(projectFolder, "GeneratedImages");

            Directory.CreateDirectory(_saveFolder);
            LoadOriginalImage();
        }

        private string GetProjectFolder()
        {
            // 실행 파일의 위치를 기준으로 프로젝트 폴더를 찾습니다.
            string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string executingFolder = Path.GetDirectoryName(executingAssemblyPath);

            // bin\Debug\net6.0 (또는 유사한 경로)에서 상위 폴더로 이동
            string projectFolder = Directory.GetParent(Directory.GetParent(Directory.GetParent(executingFolder).FullName).FullName).FullName;

            return projectFolder;
        }

        private void GenerateImage()
        {
            var (originalName, savedName, generatedTime) = _waferImageModel.GenerateAndSaveRandomWaferImage(_imagePath, _saveFolder);

            using (Mat generatedImage = Cv2.ImRead(Path.Combine(_saveFolder, savedName)))
            {
                GeneratedImage = ConvertMatToBitmapSource(generatedImage);
            }

            var imageInfo = new ImageInfo
            {
                OriginalName = originalName,
                SavedName = savedName,
                GeneratedTime = generatedTime
            };

            _databaseService.SaveImageInfo(imageInfo);
        }

        private void LoadOriginalImage()
        {
            using (Mat originalMat = Cv2.ImRead(_imagePath, ImreadModes.Color))
            {
                if (originalMat.Empty())
                {
                    throw new Exception($"원본 이미지를 불러올 수 없습니다. 경로: {_imagePath}");
                }
                OriginalImage = ConvertMatToBitmapSource(originalMat);
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
                    throw new Exception($"지원되지 않는 채널 수: {image.Channels()}");
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
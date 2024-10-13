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

        private string _originalName;
        public string OriginalName
        {
            get => _originalName;
            set
            {
                _originalName = value;
                OnPropertyChanged(nameof(OriginalName));
            }
        }

        private string _savedName;
        public string SavedName
        {
            get => _savedName;
            set
            {
                _savedName = value;
                OnPropertyChanged(nameof(SavedName));
            }
        }

        private string _databaseId;
        public string DatabaseId
        {
            get => _databaseId;
            set
            {
                _databaseId = value;
                OnPropertyChanged(nameof(DatabaseId));
            }
        }

        private DateTime _generatedTime;
        public DateTime GeneratedTime
        {
            get => _generatedTime;
            set
            {
                _generatedTime = value;
                OnPropertyChanged(nameof(GeneratedTime));
            }
        }

        private TimeSpan _processingTime;
        public TimeSpan ProcessingTime
        {
            get => _processingTime;
            set
            {
                _processingTime = value;
                OnPropertyChanged(nameof(ProcessingTime));
            }
        }

        public ICommand GenerateImageCommand { get; }

        private readonly WaferImage _waferImageModel;
        private readonly DatabaseService _databaseService;
        private readonly string _imagePath;
        private readonly string _saveFolder;

        public MainViewModel()
        {
            _waferImageModel = new WaferImage();
            _databaseService = new DatabaseService("mongodb://localhost:27017", "WaferScanDB");
            GenerateImageCommand = new RelayCommand(GenerateImage);

            string projectFolder = GetProjectFolder();
            _saveFolder = GetGeneratedFolder();
            _imagePath = Path.Combine(projectFolder, "Images", "wafer.png");

            LoadOriginalImage();
        }

        private string GetProjectFolder()
        {
            // 실행 파일의 위치를 기준으로 프로젝트 폴더를 찾습니다.
            string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string executingFolder = Path.GetDirectoryName(executingAssemblyPath);
            return Directory.GetParent(Directory.GetParent(Directory.GetParent(executingFolder).FullName).FullName).FullName;
        }

        private string GetGeneratedFolder()
        {
            string generatedFolder = Path.Combine("C:", "GeneratedImages");
            if (!Directory.Exists(generatedFolder))
            {
                Directory.CreateDirectory(generatedFolder);
            }
            return generatedFolder;
        }

        private void GenerateImage()
        {
            DateTime startTime = DateTime.Now;

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

            OriginalName = originalName;
            SavedName = savedName;
            DatabaseId = imageInfo.Id;
            GeneratedTime = generatedTime;
            ProcessingTime = DateTime.Now - startTime;
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

            int stride = image.Cols * image.Channels();
            byte[] imageData = new byte[image.Rows * stride];
            System.Runtime.InteropServices.Marshal.Copy(image.Data, imageData, 0, imageData.Length);

            // BitmapSource 생성
            return BitmapSource.Create(
                image.Cols,
                image.Rows,
                96, 96,
                pixelFormat,
                null,
                imageData,
                stride);
        }
    }
}
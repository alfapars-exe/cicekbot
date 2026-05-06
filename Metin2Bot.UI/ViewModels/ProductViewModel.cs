using CommunityToolkit.Mvvm.ComponentModel;
using Metin2Bot.Domain.Models;
using System.IO;
using System.Windows.Media.Imaging;

namespace Metin2Bot.UI.ViewModels
{
    public partial class ProductViewModel : ObservableObject
    {
        public ProductTemplate Model { get; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private BitmapImage? _thumbnail;

        public Guid Id => Model.Id;
        public string ImagePath => Model.ImagePath;

        public ProductViewModel(ProductTemplate model)
        {
            Model = model;
            _name = model.Name;
            LoadThumbnail();
        }

        partial void OnNameChanged(string value) => Model.Name = value;

        private void LoadThumbnail()
        {
            if (string.IsNullOrWhiteSpace(Model.ImagePath) || !File.Exists(Model.ImagePath))
                return;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(Model.ImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 64;
                bmp.EndInit();
                bmp.Freeze();
                Thumbnail = bmp;
            }
            catch
            {
                Thumbnail = null;
            }
        }
    }
}

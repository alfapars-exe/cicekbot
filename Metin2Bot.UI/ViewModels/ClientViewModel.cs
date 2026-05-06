using CommunityToolkit.Mvvm.ComponentModel;
using Metin2Bot.Domain.Models;
using System.Collections.ObjectModel;

namespace Metin2Bot.UI.ViewModels
{
    public partial class ClientViewModel : ObservableObject
    {
        public ClientConfig Model { get; }

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        private string _windowTitle;

        public ObservableCollection<ProductViewModel> Products { get; } = new();

        public Guid Id => Model.Id;

        public ClientViewModel(ClientConfig model)
        {
            Model = model;
            _displayName = model.DisplayName;
            _windowTitle = model.WindowTitle;

            foreach (var product in model.Products)
            {
                Products.Add(new ProductViewModel(product));
            }
        }

        partial void OnDisplayNameChanged(string value) => Model.DisplayName = value;
        partial void OnWindowTitleChanged(string value) => Model.WindowTitle = value;

        public void AddProduct(ProductTemplate template)
        {
            Model.Products.Add(template);
            Products.Add(new ProductViewModel(template));
        }

        public void RemoveProduct(ProductViewModel product)
        {
            Model.Products.Remove(product.Model);
            Products.Remove(product);
        }
    }
}

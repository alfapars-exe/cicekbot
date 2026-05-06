using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System.Collections.ObjectModel;

namespace Metin2Bot.UI.ViewModels
{
    public partial class WindowPickerViewModel : ObservableObject
    {
        private readonly IWindowService _windowService;
        private readonly HashSet<IntPtr> _excludeHandles;

        [ObservableProperty]
        private ObservableCollection<WindowInfo> _windows = new();

        [ObservableProperty]
        private WindowInfo? _selectedWindow;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _filter = "Metin2";

        public WindowPickerViewModel(IWindowService windowService, IEnumerable<IntPtr>? excludeHandles = null)
        {
            _windowService = windowService;
            _excludeHandles = excludeHandles is null
                ? new HashSet<IntPtr>()
                : new HashSet<IntPtr>(excludeHandles.Where(h => h != IntPtr.Zero));
            Refresh();
        }

        partial void OnFilterChanged(string value) => Refresh();

        partial void OnSelectedWindowChanged(WindowInfo? value)
        {
            if (value is not null && string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = value.Title;
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            Windows.Clear();
            string? filter = string.IsNullOrWhiteSpace(Filter) ? null : Filter;
            foreach (var w in _windowService.GetActiveWindows(filter))
            {
                if (_excludeHandles.Contains(w.Handle)) continue;
                Windows.Add(w);
            }
        }
    }
}

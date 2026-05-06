using Metin2Bot.Application.Interfaces;
using Metin2Bot.UI.ViewModels;
using System.Windows;
using System.Windows.Interop;

namespace Metin2Bot.UI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IHotkeyService _hotkeyService;

        public MainWindow(MainViewModel viewModel, IHotkeyService hotkeyService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _hotkeyService = hotkeyService;
            DataContext = _viewModel;
            Closed += MainWindow_Closed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            bool ok = _hotkeyService.Register(helper.Handle, () => _viewModel.ToggleBotCommand.Execute(null));
            if (!ok)
            {
                _viewModel.AddLog("DEL hotkey kayıtlanamadı (başka uygulama kullanıyor olabilir). UI buton ile açıp kapayabilirsin.");
            }
            else
            {
                _viewModel.AddLog("DEL global hotkey aktif.");
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _hotkeyService.Dispose();
        }
    }
}

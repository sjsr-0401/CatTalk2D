using System.Windows.Controls;
using CatDevTools.ViewModels;

namespace CatDevTools.Views;

public partial class AITuningTab : UserControl
{
    public AITuningTab()
    {
        InitializeComponent();
    }

    private void OnOllamaLogTextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (!vm.OllamaLogAutoScroll) return;
        if (sender is TextBox textBox)
        {
            textBox.ScrollToEnd();
        }
    }
}

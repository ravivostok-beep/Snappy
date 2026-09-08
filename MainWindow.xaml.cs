using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace SNAPPY;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _stations = new();

    public MainWindow()
    {
        InitializeComponent();

        for (int i = 1; i <= 10; i++)
            _stations.Add($"Outdoor {i:00}   |   Not configured");

        OutdoorList.ItemsSource = _stations;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "READY";
        FooterText.Text = "SNAPPY started successfully.";
    }

    private async void AnswerButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync("Answer");
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync("Reject");
    }

    private async void Unlock1Button_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync("Unlock 1");
    }

    private async void Unlock2Button_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync("Unlock 2");
    }

    private async Task RunUiActionAsync(string action)
    {
        SetButtonsEnabled(false);
        try
        {
            FooterText.Text = $"{action}: ready for Hikvision SDK integration...";
            await Task.Yield();
            await Task.Delay(50);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "SNAPPY", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        AnswerButton.IsEnabled = enabled;
        RejectButton.IsEnabled = enabled;
        Unlock1Button.IsEnabled = enabled;
        Unlock2Button.IsEnabled = enabled;
    }
}

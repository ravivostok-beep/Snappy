using System.Windows;
namespace SNAPPY;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        OutdoorList.SelectedIndex = 0;
    }

    private void Answer_Click(object sender, RoutedEventArgs e) =>
        StatusText.Text = "● Call answered";

    private void Reject_Click(object sender, RoutedEventArgs e) =>
        StatusText.Text = "● Call rejected";

    private void Unlock1_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Unlock 1 command placeholder. Hikvision SDK/API integration required.",
                        "SNAPPY");

    private void Unlock2_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Unlock 2 command placeholder. Hikvision SDK/API integration required.",
                        "SNAPPY");

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Device configuration screen will contain IP, port, credentials, RTSP and SIP settings.",
                        "SNAPPY Settings");
}

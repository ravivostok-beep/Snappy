using System.Windows;
using SNAPPY.Models;

namespace SNAPPY;

public partial class OutdoorConfigWindow : Window
{
    private readonly OutdoorStation _station;

    public OutdoorStation Station =>
        _station;


    public OutdoorConfigWindow(
        OutdoorStation station)
    {
        InitializeComponent();

        _station = station;

        LoadStation();
    }


    private void LoadStation()
    {
        NameTextBox.Text =
            _station.Name;


        IpTextBox.Text =
            _station.IpAddress;


        PortTextBox.Text =
            _station.Port > 0
                ? _station.Port.ToString()
                : "80";


        UsernameTextBox.Text =
            _station.Username;


        PasswordTextBox.Password =
            _station.Password;


        DescriptionTextBox.Text =
            _station.Description;


        EnabledCheckBox.IsChecked =
            _station.Enabled;
    }


    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string name =
            NameTextBox.Text.Trim();


        string ip =
            IpTextBox.Text.Trim();


        string username =
            UsernameTextBox.Text.Trim();


        string password =
            PasswordTextBox.Password;


        string description =
            DescriptionTextBox.Text.Trim();


        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                "Please enter the outdoor station name.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            NameTextBox.Focus();

            return;
        }


        if (string.IsNullOrWhiteSpace(ip))
        {
            MessageBox.Show(
                "Please enter the IP address.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            IpTextBox.Focus();

            return;
        }


        if (!int.TryParse(
                PortTextBox.Text.Trim(),
                out int port) ||
            port < 1 ||
            port > 65535)
        {
            MessageBox.Show(
                "Please enter a valid port number.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            PortTextBox.Focus();

            return;
        }


        /*
         * DS-K1T502DBFWX-C:
         *
         * ISAPI:
         * HTTP  = 80
         * HTTPS = 443
         *
         * Port 8000 is normally the Hikvision SDK
         * service port and is NOT used by the
         * ISAPI unlock command.
         */
        if (port != 80 &&
            port != 443)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    "For the DS-K1T502DBFWX-C ISAPI door unlock, " +
                    "the normal ports are 80 (HTTP) or 443 (HTTPS).\n\n" +
                    $"You entered port {port}.\n\n" +
                    "Do you want to save this port anyway?",
                    "SNAPPY",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                PortTextBox.Focus();

                return;
            }
        }


        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show(
                "Please enter the Hikvision username.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            UsernameTextBox.Focus();

            return;
        }


        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                "Please enter the Hikvision password.",
                "SNAPPY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            PasswordTextBox.Focus();

            return;
        }


        _station.Name =
            name;


        _station.IpAddress =
            ip;


        _station.Port =
            port;


        _station.Username =
            username;


        _station.Password =
            password;


        _station.Description =
            description;


        _station.Enabled =
            EnabledCheckBox.IsChecked == true;


        DialogResult =
            true;
    }


    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }
}

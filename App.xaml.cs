using System;
using System.Windows;

namespace SNAPPY;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, a) =>
        {
            MessageBox.Show(a.Exception.Message, "SNAPPY Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            a.Handled = true;
        };
        base.OnStartup(e);
    }
}

```csharp
using System;
using System.Windows;

namespace SNAPPY;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                args.Exception.Message,
                "SNAPPY Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "SNAPPY Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            MessageBox.Show(
                args.Exception.Message,
                "SNAPPY Task Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.SetObserved();
        };

        base.OnStartup(e);
    }
}
```

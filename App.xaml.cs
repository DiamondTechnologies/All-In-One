using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;
namespace All_In_One
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            List<string> incomingPaths = [];
            string[] commandLineArgs = Environment.GetCommandLineArgs();

            if (commandLineArgs.Length > 1)
            {
                for (int i = 1; i < commandLineArgs.Length; i++)
                {
                    string path = commandLineArgs[i];
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        incomingPaths.Add(path);
                    }
                }
            }
            try
            {
                AppActivationArguments activationArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

                if (activationArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
                {
                    if (activationArgs.Data is Windows.ApplicationModel.Activation.FileActivatedEventArgs fileArgs)
                    {
                        foreach (IStorageItem? item in fileArgs.Files)
                        {

                            if (!string.IsNullOrWhiteSpace(item.Path) && !incomingPaths.Contains(item.Path, StringComparer.OrdinalIgnoreCase))
                            {
                                incomingPaths.Add(item.Path);
                            }
                        }
                    }
                }
            }

            catch { }

            _window.Activate();

            if (incomingPaths.Count > 0)
            {
                await ((MainWindow) _window).InitializeWithPathsAsync(incomingPaths);

            }
        }
    }
}


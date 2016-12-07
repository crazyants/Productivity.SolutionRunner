﻿using Neptuo.Activators;
using Neptuo.Converters;
using Neptuo.Formatters;
using Neptuo.Formatters.Converters;
using Neptuo.Formatters.Metadata;
using Neptuo.Observables.Collections;
using Neptuo.Productivity.SolutionRunner.Properties;
using Neptuo.Productivity.SolutionRunner.Services;
using Neptuo.Productivity.SolutionRunner.Services.Applications;
using Neptuo.Productivity.SolutionRunner.Services.Converters;
using Neptuo.Productivity.SolutionRunner.Services.Execution;
using Neptuo.Productivity.SolutionRunner.Services.Searching;
using Neptuo.Productivity.SolutionRunner.Services.StartupFlags;
using Neptuo.Productivity.SolutionRunner.Services.StartupShortcuts;
using Neptuo.Productivity.SolutionRunner.ViewModels;
using Neptuo.Productivity.SolutionRunner.ViewModels.Commands.Factories;
using Neptuo.Productivity.SolutionRunner.Views;
using Neptuo.Windows.HotKeys;
using Neptuo.Windows.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using EventManager = Neptuo.Productivity.SolutionRunner.ViewModels.EventManager;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace Neptuo.Productivity.SolutionRunner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INavigator, INavigatorState, IPinStateService
    {
        private StartupModel startup;
        private DefaultRunHotKeyService runHotKey;

        // THIS must be synchronized with Click-Once deployment settings.
        private readonly ShortcutService shortcutService = new ShortcutService(
            companyName: "Neptuo",
            suiteName: "Productivity",
            productName: "Productivity.SolutionRunner"
        );

        protected DispatcherHelper DispatcherHelper { get; private set; }

        private void PrepareStartup(StartupEventArgs e)
        {
            DispatcherHelper = new DispatcherHelper(Dispatcher);

            StartupModelProvider provider = new StartupModelProvider();
            startup = provider.Get(e.Args);
            startup.IsStartup = true;

            if (!startup.IsHidden)
                startup.IsHidden = Settings.Default.IsHiddentOnStartup;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            PrepareStartup(e);
            base.OnStartup(e);

            Converts.Repository
                .AddJsonPrimitivesSearchHandler()
                .AddJsonObjectSearchHandler()
                .AddJsonEnumSearchHandler()
                .AddEnumSearchHandler(false)
                .AddToStringSearchHandler()
                .Add(new AdditionalApplicationCollectionConverter())
                .Add(new KeyViewModelConverter());

            EventManager.FilePinned += OnFilePinned;
            EventManager.ConfigurationSaved += OnConfigurationSaved;

            // Bind global hotkey.
            runHotKey = new DefaultRunHotKeyService(this, this);
            KeyViewModel runKey;
            if (Converts.Try(Settings.Default.RunKey, out runKey) && runKey != null)
            {
                try
                {
                    runHotKey.Bind(runKey.Key, runKey.Modifier);
                }
                catch (Win32Exception)
                {
                    runHotKey.UnBind();
                    Settings.Default.RunKey = null;
                    Settings.Default.Save();
                }
            }

            if (!runHotKey.IsSet)
                startup.IsHidden = false;

            // Open window.
            if (String.IsNullOrEmpty(Settings.Default.SourceDirectoryPath))
                OpenConfiguration();
            else
                OpenMain();

            if (Settings.Default.IsTrayIcon)
                TryCreateTrayIcon();

            startup.IsStartup = false;
        }
        
        private NotifyIcon trayIcon;

        private void OnConfigurationSaved(ConfigurationViewModel viewModel)
        {
            if (viewModel.IsTrayIcon)
                TryCreateTrayIcon();
            else
                TryDestroyTrayIcon();
        }

        private bool TryCreateTrayIcon()
        {
            if (trayIcon == null)
            {
                trayIcon = new NotifyIcon();
                trayIcon.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
                trayIcon.Text = "SolutionRunner";
                trayIcon.Click += OnTrayIconClick;
                trayIcon.Visible = true;
                return true;
            }

            return false;
        }

        private bool TryDestroyTrayIcon()
        {
            if (trayIcon != null)
            {
                trayIcon.Click -= OnTrayIconClick;
                trayIcon.Dispose();
                trayIcon = null;
                return true;
            }

            return false;
        }

        private void OnTrayIconClick(object sender, EventArgs e)
        {
            OpenMain();
        }

        public void Activate()
        {
            if (mainWindow != null)
                mainWindow.Activate();
            else if (configurationWindow != null)
                configurationWindow.Activate();
        }

        private static FileSearchMode GetUserFileSearchMode()
        {
            FileSearchMode result;
            if (Converts.Try(Settings.Default.FileSearchMode, out result))
                return result;

            return FileSearchMode.StartsWith;
        }

        private static int GetUserFileSearchCount()
        {
            int value = Settings.Default.FileSearchCount;
            if (value == 0)
                value = 10;

            return value;
        }

        #region Handling exceptions

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder message = new StringBuilder();

            string exceptionMessage = e.Exception.ToString();
            if (exceptionMessage.Length > 800)
                exceptionMessage = exceptionMessage.Substring(0, 800);

            message.AppendLine(exceptionMessage);

            MessageBoxResult result = MessageBox.Show(message.ToString(), "Do you want to kill the aplication?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                Shutdown();

            e.Handled = true;
        }

        #endregion

        protected override void OnExit(ExitEventArgs e)
        {
            runHotKey.Dispose();
            TryDestroyTrayIcon();
            base.OnExit(e);
        }

        #region IPinStateService

        private HashSet<string> pinnedFiles;

        private HashSet<string> GetPinnedFiles()
        {
            if (pinnedFiles == null)
            {
                pinnedFiles = new HashSet<string>();

                string rawValue = Settings.Default.PinnedFiles;
                if (!String.IsNullOrEmpty(rawValue))
                {
                    foreach (string filePath in rawValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (File.Exists(filePath))
                            pinnedFiles.Add(filePath);
                    }
                }
            }

            return pinnedFiles;
        }

        private void OnFilePinned(FileViewModel viewModel)
        {
            HashSet<string> pinnedFiles = GetPinnedFiles();
            if (viewModel.IsPinned)
                pinnedFiles.Add(viewModel.Path);
            else
                pinnedFiles.Remove(viewModel.Path);

            Settings.Default.PinnedFiles = String.Join(";", pinnedFiles);
            Settings.Default.Save();
        }

        public IEnumerable<string> GetList()
        {
            return GetPinnedFiles();
        }

        public bool IsPinned(string path)
        {
            Ensure.NotNullOrEmpty(path, "path");
            return GetPinnedFiles().Contains(path);
        }

        #endregion

        #region INavigator & INavigatorState

        private MainWindow mainWindow;
        private ConfigurationWindow configurationWindow;
        private bool isMainWindowViewModelReloadRequired = false;

        public bool IsConfigurationOpened
        {
            get { return configurationWindow != null; }
        }

        public bool IsMainOpened
        {
            get { return mainWindow != null; }
        }

        public void OpenConfiguration()
        {
            if (configurationWindow == null)
            {
                isMainWindowViewModelReloadRequired = true;

                ConfigurationViewModel viewModel = new ConfigurationViewModel(new SaveConfigurationCommandFactory(Settings.Default, runHotKey, shortcutService), this);
                viewModel.SourceDirectoryPath = Settings.Default.SourceDirectoryPath;
                viewModel.PreferedApplicationPath = Settings.Default.PreferedApplicationPath;
                viewModel.FileSearchMode = GetUserFileSearchMode();
                viewModel.FileSearchCount = GetUserFileSearchCount();
                viewModel.IsFileSearchPatternSaved = Settings.Default.IsFileSearchPatternSaved;
                viewModel.IsLastUsedApplicationSavedAsPrefered = Settings.Default.IsLastUsedApplicationSavedAsPrefered;
                viewModel.IsDismissedWhenLostFocus = Settings.Default.IsDismissedWhenLostFocus;
                viewModel.IsHiddentOnStartup = Settings.Default.IsHiddentOnStartup;
                viewModel.IsAutoSelectApplicationVersion = Settings.Default.IsAutoSelectApplicationVersion;
                viewModel.IsFileNameRemovedFromDisplayedPath = Settings.Default.IsFileNameRemovedFromDisplayedPath;
                viewModel.IsDisplayedPathTrimmedToLastFolderName = Settings.Default.IsDisplayedPathTrimmedToLastFolderName;
                viewModel.IsAutoStartup = shortcutService.Exists(Environment.SpecialFolder.Startup);
                viewModel.IsTrayIcon = Settings.Default.IsTrayIcon;
                viewModel.AdditionalApplications = new ObservableCollection<AdditionalApplicationListViewModel>(LoadAdditionalApplications());
                viewModel.RunKey = runHotKey.FindKeyViewModel();

                configurationWindow = new ConfigurationWindow(viewModel, this, String.IsNullOrEmpty(Settings.Default.SourceDirectoryPath));
                configurationWindow.ShowInTaskbar = !runHotKey.IsSet;
                configurationWindow.ResizeMode = !runHotKey.IsSet ? ResizeMode.CanMinimize : ResizeMode.NoResize;
                configurationWindow.Closed += OnConfigurationWindowClosed;
            }
            configurationWindow.Show();
            configurationWindow.Activate();
        }

        private IEnumerable<AdditionalApplicationListViewModel> LoadAdditionalApplications()
        {
            string rawValue = Settings.Default.AdditionalApplications;
            if (String.IsNullOrEmpty(rawValue))
                return Enumerable.Empty<AdditionalApplicationListViewModel>();

            return Converts
                .To<string, AdditionalApplicationCollection>(Settings.Default.AdditionalApplications)
                .Select(a => new AdditionalApplicationListViewModel(a));
        }

        private void OnConfigurationWindowClosed(object sender, EventArgs e)
        {
            configurationWindow.Closed -= OnConfigurationWindowClosed;
            configurationWindow = null;

            if (!runHotKey.IsSet && mainWindow == null)
                Shutdown();
        }

        private AdditionalApplicationModel sourceAdditionalApplicationModel;
        private AdditionalApplicationEditWindow additionalApplicationEditWindow;
        private Action<AdditionalApplicationModel> onAdditionalApplicationSaved;

        public void OpenAdditionalApplicationEdit(AdditionalApplicationModel model, Action<AdditionalApplicationModel> onSaved)
        {
            sourceAdditionalApplicationModel = model;
            onAdditionalApplicationSaved = onSaved;

            AdditionalApplicationEditViewModel viewModel = new AdditionalApplicationEditViewModel(model, OnAdditionalApplicationSaved);
            additionalApplicationEditWindow = new AdditionalApplicationEditWindow(viewModel);
            additionalApplicationEditWindow.Owner = configurationWindow;
            additionalApplicationEditWindow.ShowDialog();
        }

        private void OnAdditionalApplicationSaved(AdditionalApplicationModel model)
        {
            additionalApplicationEditWindow.Close();

            if (onAdditionalApplicationSaved != null)
                onAdditionalApplicationSaved(model);
        }

        private string directoryPath;
        private FileSystemWatcherSearchService fileSearchService;

        private IFileSearchService CreateFileSearchService()
        {
            if (fileSearchService == null || directoryPath != Settings.Default.SourceDirectoryPath)
            {
                directoryPath = Settings.Default.SourceDirectoryPath;
                fileSearchService = new FileSystemWatcherSearchService(directoryPath, this);
            }

            return fileSearchService;
        }

        public void OpenMain()
        {
            if (mainWindow == null)
            {
                mainWindow = new MainWindow(this, Settings.Default, new ProcessService());
                mainWindow.Closing += OnMainWindowClosing;
                mainWindow.Closed += OnMainWindowClosed;
            }

            if (mainWindow.ViewModel == null || isMainWindowViewModelReloadRequired)
            {
                isMainWindowViewModelReloadRequired = false;

                mainWindow.ShowInTaskbar = !runHotKey.IsSet;
                mainWindow.IsAutoSelectApplicationVersion = Settings.Default.IsAutoSelectApplicationVersion;

                MainViewModel viewModel = new MainViewModel(
                    new PinnedForEmptyPatternFileSearchService(
                        //new DelayedFileSearchService(
                        //    Dispatcher,
                        //    CreateFileSearchService()
                        //),
                        //this
                        CreateFileSearchService(),
                        this
                    ),
                    GetUserFileSearchMode,
                    GetUserFileSearchCount
                );

                VsVersionLoader vsLoader = new VsVersionLoader();
                vsLoader.Add(viewModel);

                AdditionalApplicationLoader additionalLoader = new AdditionalApplicationLoader();
                additionalLoader.Add(viewModel);

                IFileCollection files = viewModel;
                foreach (string filePath in GetPinnedFiles())
                    files.Add(Path.GetFileNameWithoutExtension(filePath), filePath, true);

                mainWindow.ViewModel = viewModel;
                mainWindow.TrySelectPreferedApplication();
            }

            mainWindow.IsAutoSelectApplicationVersion = Settings.Default.IsAutoSelectApplicationVersion;

            if (Settings.Default.IsFileSearchPatternSaved)
                mainWindow.ViewModel.SearchPattern = Settings.Default.FileSearchPattern;
            else if (!String.IsNullOrEmpty(mainWindow.ViewModel.SearchPattern))
                mainWindow.ViewModel.SearchPattern = String.Empty;

            mainWindow.Deactivated += OnMainWindowDeactivated;

            if (!startup.IsStartup || !startup.IsHidden)
            {
                mainWindow.Show();
                mainWindow.Activate();
            }
        }

        private void OnMainWindowDeactivated(object sender, EventArgs e)
        {
            if (Settings.Default.IsDismissedWhenLostFocus && mainWindow != null)
            {
                DispatcherHelper.Run(() =>
                {
                    if (Settings.Default.IsDismissedWhenLostFocus && mainWindow != null && !mainWindow.IsActive)
                        mainWindow.Close();
                }, 500);
            }
        }

        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            if (Settings.Default.IsFileSearchPatternSaved)
            {
                Settings.Default.FileSearchPattern = mainWindow.ViewModel.SearchPattern;
                Settings.Default.Save();
            }

            if (runHotKey.IsSet)
            {
                mainWindow.Hide();
                e.Cancel = true;
            }

            mainWindow.Deactivated -= OnMainWindowDeactivated;
        }

        private void OnMainWindowClosed(object sender, EventArgs e)
        {
            mainWindow.Closed -= OnMainWindowClosed;
            mainWindow.Closing -= OnMainWindowClosed;
            mainWindow = null;

            if (!runHotKey.IsSet && configurationWindow == null)
                Shutdown();
        }

        #endregion
    }
}

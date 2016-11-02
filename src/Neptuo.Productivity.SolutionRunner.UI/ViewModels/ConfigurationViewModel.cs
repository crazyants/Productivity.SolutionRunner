﻿using Neptuo.Activators;
using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Productivity.SolutionRunner.Services;
using Neptuo.Productivity.SolutionRunner.Services.Searching;
using Neptuo.Productivity.SolutionRunner.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Neptuo.Productivity.SolutionRunner.ViewModels
{
    public class ConfigurationViewModel : ObservableObject
    {
        private string sourceDirectoryPath;
        public string SourceDirectoryPath
        {
            get { return sourceDirectoryPath; }
            set
            {
                if (sourceDirectoryPath != value)
                {
                    sourceDirectoryPath = value;
                    RaisePropertyChanged();
                    saveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string preferedApplicationPath;
        public string PreferedApplicationPath
        {
            get { return preferedApplicationPath; }
            set
            {
                if (preferedApplicationPath != value)
                {
                    preferedApplicationPath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private KeyViewModel runKey;
        public KeyViewModel RunKey
        {
            get { return runKey; }
            set
            {
                if (runKey != value)
                {
                    runKey = value;
                    RaisePropertyChanged();
                }
            }
        }

        private FileSearchMode fileSearchMode;
        public FileSearchMode FileSearchMode
        {
            get { return fileSearchMode; }
            set
            {
                if (fileSearchMode != value)
                {
                    fileSearchMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int fileSearchCount;
        public int FileSearchCount
        {
            get { return fileSearchCount; }
            set
            {
                if (fileSearchCount != value)
                {
                    fileSearchCount = value;
                    RaisePropertyChanged();
                    saveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isFileSearchPatternSaved;
        public bool IsFileSearchPatternSaved
        {
            get { return isFileSearchPatternSaved; }
            set
            {
                if (isFileSearchPatternSaved != value)
                {
                    isFileSearchPatternSaved = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isLastUsedApplicationSavedAsPrefered;
        public bool IsLastUsedApplicationSavedAsPrefered
        {
            get { return isLastUsedApplicationSavedAsPrefered; }
            set
            {
                if (isLastUsedApplicationSavedAsPrefered != value)
                {
                    isLastUsedApplicationSavedAsPrefered = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isDismissedWhenLostFocus;
        public bool IsDismissedWhenLostFocus
        {
            get { return isDismissedWhenLostFocus; }
            set
            {
                if (isDismissedWhenLostFocus != value)
                {
                    isDismissedWhenLostFocus = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isHiddentOnStartup;
        public bool IsHiddentOnStartup
        {
            get { return isHiddentOnStartup; }
            set
            {
                if (isHiddentOnStartup != value)
                {
                    isHiddentOnStartup = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isAutoSelectApplicationVersion;
        public bool IsAutoSelectApplicationVersion
        {
            get { return isAutoSelectApplicationVersion; }
            set
            {
                if (isAutoSelectApplicationVersion != value)
                {
                    isAutoSelectApplicationVersion = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isFileNameRemovedFromDisplayedPath;
        public bool IsFileNameRemovedFromDisplayedPath
        {
            get { return isFileNameRemovedFromDisplayedPath; }
            set
            {
                if (isFileNameRemovedFromDisplayedPath != value)
                {
                    isFileNameRemovedFromDisplayedPath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isDisplayedPathTrimmedToLastFolderName;
        public bool IsDisplayedPathTrimmedToLastFolderName
        {
            get { return isDisplayedPathTrimmedToLastFolderName; }
            set
            {
                if (isDisplayedPathTrimmedToLastFolderName != value)
                {
                    isDisplayedPathTrimmedToLastFolderName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool isAutoStartup;
        public bool IsAutoStartup
        {
            get { return isAutoStartup; }
            set
            {
                if (isAutoStartup != value)
                {
                    isAutoStartup = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<AdditionalApplicationListViewModel> AdditionalApplications { get; set; }

        public string Version { get; private set; }

        private SaveConfigurationCommand saveCommand;
        public ICommand SaveCommand
        {
            get { return saveCommand; }
        }
        public ICommand RemoveAdditionalApplicationCommand { get; private set; }
        public ICommand EditAdditionalApplicationCommand { get; private set; }
        public ICommand CreateAdditionalApplicationCommand { get; private set; }

        public ConfigurationViewModel(IFactory<SaveConfigurationCommand, ConfigurationViewModel> commandFactory, INavigator navigator)
        {
            string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Version = String.Format("v{0}", version);

            saveCommand = commandFactory.Create(this);
            EditAdditionalApplicationCommand = new EditAdditionalApplicationCommand(this, navigator);
            RemoveAdditionalApplicationCommand = new RemoveAdditionalApplicationCommand(this);
            CreateAdditionalApplicationCommand = new CreateAdditionalApplicationCommand(this, navigator);
        }
    }
}

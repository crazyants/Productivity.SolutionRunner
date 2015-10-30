﻿using Neptuo.Productivity.SolutionRunner.Properties;
using Neptuo.Productivity.SolutionRunner.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SolutionRunner.ViewModels.Commands
{
    public class SaveConfigurationCommand : CommandBase
    {
        private readonly ConfigurationViewModel viewModel;
        private readonly IRunHotKeyService runHotKey;

        public SaveConfigurationCommand(ConfigurationViewModel viewModel, IRunHotKeyService runHotKey)
        {
            Ensure.NotNull(viewModel, "viewModel");
            Ensure.NotNull(runHotKey, "runHotKey");
            this.viewModel = viewModel;
            this.runHotKey = runHotKey;
        }

        protected override bool CanExecute()
        {
            return !String.IsNullOrEmpty(viewModel.SourceDirectoryPath) && Directory.Exists(viewModel.SourceDirectoryPath);
        }

        protected override void Execute()
        {
            Settings.Default.SourceDirectoryPath = viewModel.SourceDirectoryPath;
            Settings.Default.PreferedApplicationPath = viewModel.PreferedApplicationPath;

            string runKeyValue;
            if (Converts.Try(viewModel.RunKey, out runKeyValue))
                Settings.Default.RunKey = runKeyValue;

            if (viewModel.RunKey == null)
                runHotKey.UnBind();
            else
                runHotKey.Bind(viewModel.RunKey.Key, viewModel.RunKey.Modifier);

            Settings.Default.Save();
            EventManager.RaiseConfigurationSaved(viewModel);
        }
    }
}

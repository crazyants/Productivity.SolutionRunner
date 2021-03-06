﻿using Neptuo.Converters;
using Neptuo.Productivity.SolutionRunner.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Neptuo.Productivity.SolutionRunner.Services.Converters
{
    public class KeyViewModelConverter : TwoWayConverter<KeyViewModel, string>
    {
        private static readonly Key[] modifierKeys = new Key[] { Key.System, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.LeftAlt, Key.RightAlt };

        public override bool TryConvertFromOneToTwo(KeyViewModel sourceValue, out string targetValue)
        {
            if (sourceValue == null)
            {
                targetValue = null;
                return true;
            }

            StringBuilder value = new StringBuilder();
            Action<string> append = (part) =>
            {
                if (value.Length > 0)
                    value.Append(" + ");

                value.Append(part);
            };

            if (sourceValue.Modifier.HasFlag(ModifierKeys.Control))
                append("Ctrl");

            if (sourceValue.Modifier.HasFlag(ModifierKeys.Shift))
                append("Shift");

            if (sourceValue.Modifier.HasFlag(ModifierKeys.Windows))
                append("Windows");

            if (sourceValue.Modifier.HasFlag(ModifierKeys.Alt))
                append("Alt");

            bool result = false;
            if (!modifierKeys.Contains(sourceValue.Key))
            {
                append(sourceValue.Key.ToString());
                result = true;
            }

            targetValue = value.ToString();
            return result;
        }

        public override bool TryConvertFromTwoToOne(string sourceValue, out KeyViewModel targetValue)
        {
            if (String.IsNullOrEmpty(sourceValue))
            {
                targetValue = null;
                return true;
            }

            ModifierKeys modifier = ModifierKeys.None;

            bool result = true;
            string[] parts = sourceValue.Split('+');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i].Trim();
                if (part == "Ctrl")
                    modifier |= ModifierKeys.Control;
                else if (part == "Shift")
                    modifier |= ModifierKeys.Shift;
                else if (part == "Windows")
                    modifier |= ModifierKeys.Windows;
                else if (part == "Alt")
                    modifier |= ModifierKeys.Alt;
                else
                    result = false;
            }

            if (result)
            {
                Key key;
                if (Enum.TryParse<Key>(parts[parts.Length - 1], out key))
                {
                    targetValue = new KeyViewModel(key, modifier);
                    return true;
                }
            }

            targetValue = null;
            return false;
        }
    }
}

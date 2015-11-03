﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Neptuo.Productivity.SolutionRunner.Views.Controls
{
    public static class WindowDrag
    {
        private readonly static Type[] activeControlTypes = new Type[] { typeof(ListViewItem), typeof(Button), typeof(TextBox), typeof(ComboBox), typeof(ComboBoxItem) };

        public static bool TryMove(MouseButtonEventArgs e)
        {
            FrameworkElement element = e.OriginalSource as FrameworkElement;
            if (element != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    FrameworkElement parent = element.Parent as FrameworkElement;
                    if (parent == null)
                        parent = element.TemplatedParent as FrameworkElement;

                    if (parent == null)
                        break;

                    Type parentType = parent.GetType();
                    if (activeControlTypes.Contains(parentType))
                        return false;

                    element = parent;
                }
            }

            return true;
        }
    }
}

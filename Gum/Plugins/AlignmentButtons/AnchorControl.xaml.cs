﻿using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AnchorControl.xaml
    /// </summary>
    public partial class AnchorControl : UserControl
    {
        StateSave CurrentState
        {
            get
            {
                if(SelectedState.Self.SelectedStateSave != null)
                {
                    return SelectedState.Self.SelectedStateSave;
                }
                else
                {
                    return SelectedState.Self.SelectedElement?.DefaultState;
                }
            }
        }

        public AnchorControl()
        {
            InitializeComponent();
        }

        private void TopLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);
                
            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void TopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void TopRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop);

            RefreshAndSave();
        }

        private void MiddleLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void MiddleMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void MiddleRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            RefreshAndSave();
        }

        private void BottomLeftButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void BottomMiddleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void BottomRightButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight);

            SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom);

            RefreshAndSave();
        }

        private void SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment alignment, PositionUnitType xUnits)
        {
            var state = CurrentState;
            string prefix = GetVariablePrefix();

            state.SetValue(prefix + "X", 0.0f, "float");
            state.SetValue(prefix + "X Origin",
                alignment, "HorizontalAlignment");
            state.SetValue(prefix + "X Units",
               xUnits, typeof(Gum.Managers.PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                state.SetValue(prefix + "HorizontalAlignment", alignment, "HorizontalAlignment");
            }

        }

        private static string GetVariablePrefix()
        {
            string prefix = "";
            var instance = SelectedState.Self.SelectedInstance;
            if (instance != null)
            {
                prefix = instance.Name + ".";
            }
            return prefix;
        }

        private void SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment alignment, PositionUnitType yUnits)
        {
            var state = CurrentState;
            string prefix = GetVariablePrefix();

            state.SetValue(prefix + "Y", 0.0f, "float");
            state.SetValue(prefix + "Y Origin",
                alignment, typeof(global::RenderingLibrary.Graphics.VerticalAlignment).Name);
            state.SetValue(prefix + "Y Units",
                yUnits, typeof(PositionUnitType).Name);

            if (SelectedState.Self.SelectedInstance?.BaseType == "Text")
            {
                state.SetValue(prefix + "VerticalAlignment", alignment, "VerticalAlignment");
            }

        }

        private static void RefreshAndSave()
        {
            GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}

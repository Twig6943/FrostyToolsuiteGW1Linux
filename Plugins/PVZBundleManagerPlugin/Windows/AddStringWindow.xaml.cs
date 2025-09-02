using System;
using System.Windows;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk.Managers;

namespace GW2BundleManagerPlugin.Windows
{
    /// <summary>
    /// Interaction logic for AddProfileWindow.xaml
    /// </summary>
    public partial class AddStringWindow : FrostyDockableWindow
    {
        public string AssetPath { get; set; } = "";
        public string Bundle { get; set; } = "";

        public AddStringWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            ResAssetEntry entry = App.AssetManager.GetResEntry(AssetPath);
            if(entry == null)
            {
                App.Logger.LogError($"Resource {AssetPath} does not exist.");
                return;
            }

            int bundleId = App.AssetManager.GetBundleId(Bundle);
            if(bundleId == -1)
            {
                App.Logger.LogError($"Bundle {Bundle} does not exist.");
                return;
            }
            
            if(entry.IsInBundle(bundleId))
            {
                App.Logger.LogError($"Resource {entry.Name} is already in {Bundle}.");
                return;
            }

            entry.AddToBundle(bundleId);
            App.Logger.Log($"(Manually Added) Resource {entry.Name} was added to bundle {Bundle}");
            Close();
        }

        private void assetPathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            AssetPath = assetPathTextBox.Text;
        }

        private void bundleNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Bundle = bundleNameTextBox.Text;
        }
    }
}

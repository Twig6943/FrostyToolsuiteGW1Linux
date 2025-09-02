using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GW2BundleManagerPlugin.Extensions.DECMEs
{
    public class ManageBundles : DataExplorerContextMenuExtension
    {
        public override string ContextItemName => "Manage Bundles";

        public override ImageSource Icon => (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/GW2BundleManagerPlugin;component/Resources/Icons/Manage.png");

        public override RelayCommand ContextItemClicked => new RelayCommand(delegate (object execute)
        {
            // We can expect it to be EBX since DECMEs are only for common data explorers
            EbxAssetEntry selectedEbx = (EbxAssetEntry)App.EditorWindow.VisibleExplorer.SelectedAsset;

            FrostyTaskWindow.Show("Managing Asset", selectedEbx.Filename, delegate (FrostyTaskWindow inTask)
            {
                Plugin.BundleManager.BundleManageAsset(selectedEbx);
            });
        });
    }
}

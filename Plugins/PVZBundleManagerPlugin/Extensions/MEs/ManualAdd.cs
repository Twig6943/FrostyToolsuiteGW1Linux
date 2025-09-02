using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using GW2BundleManagerPlugin.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Extensions.MEs
{
    public class ManualAdd : MenuExtension
    {
        public override string MenuItemName => "Manual Add";

        public override string TopLevelMenuName => "GW2 Bundle Manager";

        public override RelayCommand MenuItemClicked => new RelayCommand(delegate (object execute)
        {
            AddStringWindow window = new AddStringWindow();
            window.Show();
        });
    }
}

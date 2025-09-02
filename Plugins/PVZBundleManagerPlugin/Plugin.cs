using Frosty.Core;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin
{
    /// <summary>
    /// Serves as a host for singleton instances of classes essential to the functionality of this plugin, akin to Frosty's <see cref="App"/> class.
    /// </summary>
    internal static class Plugin
    {
        public static BundleManager BundleManager
        {
            get;
        } = new BundleManager(App.AssetManager, App.FileSystem);
    }
}

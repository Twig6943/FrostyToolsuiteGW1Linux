using Frosty.Core;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Extensions.StartupActions
{
    public class BundleManagerInitialization : StartupAction
    {
        public override Action<ILogger> Action => delegate (ILogger inLogger)
        {
            Plugin.BundleManager.SetLogger(inLogger);
            Plugin.BundleManager.Initialize();

            // Now, revert to the App's logger
            Plugin.BundleManager.SetLogger(App.Logger);
        };
    }
}

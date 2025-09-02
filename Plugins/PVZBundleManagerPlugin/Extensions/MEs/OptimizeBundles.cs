using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.IO;
using FrostySdk.Managers;
using GW2BundleManagerPlugin.Ports.Classes;

namespace GW2BundleManagerPlugin.Extensions.MEs
{
    public class OptimizeBundles : MenuExtension
    {
        public override string MenuItemName => "Optimize Bundles";

        public override string TopLevelMenuName => "GW2 Bundle Manager";

        public override ImageSource Icon => (ImageSource)new ImageSourceConverter().ConvertFromString("pack://application:,,,/GW2BundleManagerPlugin;component/Resources/Icons/Manage.png");

        private HashSet<EbxAssetEntry> mEbxEntries => App.AssetManager.EnumerateEbx().ToHashSet();

        public override RelayCommand MenuItemClicked => new RelayCommand(delegate (object execute)
        {
            // Try to reduce the usage of massive bundles like CharactersShared as much as possibl

            int charssharedBundleId = App.AssetManager.GetBundleId("win32/gameplay/kits/bundling/characterssharedbundleasset");
            BundleEntry charsSharedBE = App.AssetManager.GetBundleEntry(charssharedBundleId);
            List<EbxAssetEntry> unoptimizedAssets = new List<EbxAssetEntry>();
            foreach (EbxAssetEntry modifiedEntry in mEbxEntries.Where(e => e.IsModified || e.IsAdded))
            {
                if (modifiedEntry.IsInBundle(charssharedBundleId))
                {
                    // See if this really needs to be in charactersshared
                    List<EbxAssetEntry> refsTo = GetReferencesTo(modifiedEntry);

                    bool needsToBeInCharsShared = true;
                    foreach (EbxAssetEntry refEntry in refsTo)
                    {
                        if (refEntry.IsInBundle(charssharedBundleId))
                        {
                            needsToBeInCharsShared = true;
                            break;
                        }
                        needsToBeInCharsShared = false;
                    }

                    if (!needsToBeInCharsShared)
                    {
                        unoptimizedAssets.Add(modifiedEntry);
                        //App.Logger.Log("{0} does not need to be in charsshared", modifiedEntry.Name);
                        /*BaseAssetHandler handler = AssetHandlerDB.GetAssetHandler(modifiedEntry.Type);
                        handler.RemoveFromBundle(modifiedEntry, charsSharedBE);

                        foreach (EbxAssetEntry refEntry in refsTo)
                        {
                            foreach (int bunId in refEntry.Bundles.Concat(refEntry.AddedBundles))
                            {
                                handler.AddToBundle(modifiedEntry, App.AssetManager.GetBundleEntry(bunId));
                            }
                        }

                        App.Logger.Log("Optimized bundles of {0}", modifiedEntry.Name);*/
                    }
                }
            }

            if (unoptimizedAssets.Count > 0)
            {
                using (NativeWriter writer =
                       new NativeWriter(new FileStream(@"C:\bundlereport.txt", FileMode.Create)))
                {
                    foreach (EbxAssetEntry entry in unoptimizedAssets)
                    {
                        writer.WriteLine($"{entry.Name} does not need to be in CharsShared");
                    }
                }
            }
        });

        public List<EbxAssetEntry> GetReferencesTo(EbxAssetEntry inEntry)
        {
            List<EbxAssetEntry> refs = new List<EbxAssetEntry>();
            foreach (EbxAssetEntry entry in mEbxEntries)
            {
                if (entry.ContainsDependency(inEntry.Guid))
                {
                    refs.Add(entry);
                }
            }

            return refs;
        }

        public List<EbxAssetEntry> GetTopLevelReferences(EbxAssetEntry inEntry)
        {
            List<EbxAssetEntry> topLevelReferences = new List<EbxAssetEntry>();

            Guid fileGuid = inEntry.Guid;

            foreach (EbxAssetEntry entry in mEbxEntries)
            {
                if (entry.ContainsDependency(fileGuid))
                {
                    // Check if the current entry is referenced by any other entry
                    bool isReferenced = false;
                    foreach (EbxAssetEntry otherEntry in mEbxEntries)
                    {
                        if (otherEntry.ContainsDependency(entry.Guid) && otherEntry.Guid != fileGuid)
                        {
                            isReferenced = true;
                            break;
                        }
                    }

                    if (!isReferenced)
                    {
                        topLevelReferences.Add(App.AssetManager.GetEbxEntry(entry.Guid));
                    }
                }
            }

            return topLevelReferences;
        }


    }
}

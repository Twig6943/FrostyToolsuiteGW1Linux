using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Ports.Classes
{
    public class BaseAssetHandler
    {
        // Originally AddToBundleExtension in PluginPorts, now BaseAssetHandler with improvements

        /// <summary>
        /// The name of the asset type that this is intended for.
        /// </summary>
        public virtual string AssetType => null;

        /// <summary>
        /// An array of <see cref="ProfileVersion"/> enumerations that indicate the supported profiles of this <see cref="BaseAssetHandler"/>.
        /// </summary>
        public virtual List<ProfileVersion> SupportedProfiles => null;

        /// <summary>
        /// An array of <see cref="ProfileVersion"/> enumerations that indicate the unsupported profiles of this <see cref="BaseAssetHandler"/>.
        /// </summary>
        public virtual List<ProfileVersion> UnsupportedProfiles => null;

        /// <summary>
        /// Adds a provided asset entry to a specified bundle.
        /// </summary>
        /// <param name="entry">The asset entry to be used.</param>
        /// <param name="bentry">The bundle that the asset entry will be added to.</param>
        public virtual bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            // If the asset doesn't exist, this can be considered a failure
            if (entry != null)
            {
                Parallel.ForEach(App.AssetManager.GetEbx(entry).Objects, obj =>
                {
                    Type objType = obj.GetType();
                    if (objType.Name == "SyncedSequenceEntityData" || objType.Name == "SequenceEntityData")
                    {
                        foreach (PointerRef trackPr in ((dynamic)obj).PropertyTracks)
                        {
                            object resolvedTrackPr = trackPr.Resolve();
                            if (resolvedTrackPr.GetType().Name == "TransformPartPropertyTrackData")
                            {
                                ResAssetEntry res = App.AssetManager.GetResEntry(((dynamic)trackPr.Internal).Resource);
                                res.AddToBundle(App.AssetManager.GetBundleId(bentry));
                            }
                        }
                    }
                });
            }
            return (entry?.BetterAddToBundle(App.AssetManager.GetBundleId(bentry)) ?? false);
        }

        /// <summary>
        /// Creates an asset instance of this handler's type.
        /// </summary>
        /// <param name="name">The name of the new asset.</param>
        /// <param name="basedOnEntry">Optional. If provided, existing data from the specified asset will be used.</param>
        /// <param name="newType">Optional. If specified, an empty, unmodified asset of the specified type will be created.</param>
        /// <returns>The <see cref="EbxAssetEntry"/> of the duplicated asset.</returns>
        public virtual EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAsset ebx = basedOnEntry != null ? App.AssetManager.GetEbx(basedOnEntry) : null;
            EbxAsset ebxAsset = null;

            if (newType != null)
            {
                ebxAsset = new EbxAsset(TypeLibrary.CreateObject(newType.Name));
            }
            else
            {
                // Ported from 1.0.5.10 (old decompilation) with alterations due to method changes, original source can be retrieved from clean decompilation if needed
                // Fixes sudden crashing when duplicating from certain assets and types

                // If newType is null and a basedOnEntry was not specified, return null to indicate missing data
                if (basedOnEntry == null)
                {
                    return null;
                }

                // Get the asset stream of the base asset
                Stream baseAssetEntry = App.AssetManager.GetEbxStream(basedOnEntry);

                // Create an ebx reader based on the selected profile
                using (EbxReader ebxReader = EbxReader.CreateReader(baseAssetEntry, App.FileSystem, true))
                {
                    // Set the asset to the ebx reader's resolved data
                    ebxAsset = ebxReader.ReadAsset<EbxAsset>();
                }
            }
            ebxAsset.SetFileGuid(Guid.NewGuid());
            dynamic rootObject = ebxAsset.RootObject;
            rootObject.Name = name;
            AssetClassGuid assetClassGuid = new AssetClassGuid(Utils.GenerateDeterministicGuid(ebx?.Objects ?? new List<object>(), (Type)rootObject.GetType(), ebx?.FileGuid ?? Guid.Empty), -1);
            rootObject.SetInstanceGuid(assetClassGuid);
            EbxAssetEntry ebxAssetEntry = App.AssetManager.AddEbx(name, ebxAsset);
            ebxAssetEntry.AddedBundles.AddRange(basedOnEntry?.EnumerateBundles() ?? new List<int>());
            ebxAssetEntry.ModifiedEntry.DependentAssets.AddRange(ebxAsset.Dependencies);
            return ebxAssetEntry;
        }

        /// <summary>
        /// Removes an asset from a specified bundle.
        /// </summary>
        /// <param name="entry">The asset entry to be used.</param>
        /// <param name="bundle">The bundle that the asset entry will be removed from.</param>
        public virtual bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            return (entry?.RemoveFromBundle(App.AssetManager.GetBundleId(bundle)) ?? false);
        }
    }
}

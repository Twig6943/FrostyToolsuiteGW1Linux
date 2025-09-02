using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class HavokAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "HavokAsset";

        /// <summary>
        /// Initializes a new instance of the <see cref="HavokAssetHandler"/> class.
        /// </summary>
        public HavokAssetHandler()
        {
        }

        public override bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            if (!base.AddToBundle(entry, bentry))
                return false;

            int bundleId = App.AssetManager.GetBundleId(bentry);

            // HavokAssets list external ebx, but we can rely on the bundle editor to handle that

            dynamic rootObj = App.AssetManager.GetEbx(entry).RootObject;
            ResAssetEntry refRes = App.AssetManager.GetResEntry(rootObj.Resource);

            if (refRes == null)
                return true;

            // All we need to do with Havok physics resources is add them, they have no chunks
            refRes.BetterAddToBundle(bundleId);
            entry.LinkAsset(refRes);

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAssetEntry newAstEntry = base.Create(name, basedOnEntry, newType);
            EbxAsset newAst = App.AssetManager.GetEbx(newAstEntry);

            dynamic nARoot = newAst.RootObject;

            // Duplicate physics resource
            ResAssetEntry bOEPhysRes = App.AssetManager.GetResEntry(nARoot.Resource);
            byte[] bOEPRData = (bOEPhysRes != null ?
                NativeReader.ReadInStream(App.AssetManager.GetRes(bOEPhysRes))
                :
                null
            );

            ResAssetEntry newPhysRes = App.AssetManager.AddRes(
                name.ToLowerInvariant(),
                ResourceType.HavokPhysicsData,
                bOEPhysRes?.ResMeta,
                bOEPRData,
                newAstEntry.AddedBundles.ToArray()
            );
            newAstEntry.LinkAsset(newPhysRes);

            nARoot.Resource = newPhysRes.ResRid;
            App.AssetManager.ModifyEbx(newAstEntry.Name, newAst);

            return newAstEntry;
        }

        public override bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            if (!base.RemoveFromBundle(entry, bundle))
                return false;

            int bundleId = App.AssetManager.GetBundleId(bundle);

            dynamic rootObj = App.AssetManager.GetEbx(entry).RootObject;
            ResAssetEntry refRes = App.AssetManager.GetResEntry(rootObj.Resource);

            if (refRes == null)
                return true;

            refRes.RemoveFromBundle(bundleId);
            entry.LinkAsset(refRes);

            return true;
        }
    }
}

using Frosty.Core;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class MovieTextureAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "MovieTextureAsset";

        public override bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            if (!base.AddToBundle(entry, bentry))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;
            ChunkAssetEntry chunkAssetEntry = App.AssetManager.GetChunkEntry(rootObject.ChunkGuid);

            chunkAssetEntry?.BetterAddToBundle(App.AssetManager.GetBundleId(bentry));
            entry.LinkAsset(chunkAssetEntry);

            chunkAssetEntry = App.AssetManager.GetChunkEntry(rootObject.SubtitleChunkGuid);

            chunkAssetEntry?.BetterAddToBundle(App.AssetManager.GetBundleId(bentry));
            entry.LinkAsset(chunkAssetEntry);

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            // Allow the essential creation functionality of the BaseAssetHandler to run first; we may add our own alterations afterwards
            EbxAssetEntry newEntry = base.Create(name, basedOnEntry, newType);

            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);
            byte[] rootChunkData = new byte[0];
            byte[] subtitleChunkData = new byte[0];

            // If available, use existing data from the basedOnEntry
            if (newType == null)
            {
                dynamic basedOnObject = App.AssetManager.GetEbx(basedOnEntry, false).RootObject;
                ChunkAssetEntry rootChunk = App.AssetManager.GetChunkEntry(basedOnObject.ChunkGuid);
                ChunkAssetEntry subtitleChunk = App.AssetManager.GetChunkEntry(basedOnObject.SubtitleChunkGuid);

                if (rootChunk != null)
                {
                    rootChunkData = NativeReader.ReadInStream(App.AssetManager.GetChunk(rootChunk));
                }

                if (subtitleChunk != null)
                {
                    subtitleChunkData = NativeReader.ReadInStream(App.AssetManager.GetChunk(subtitleChunk));
                }
            }

            // Create a new chunk for the created MovieTexture2 asset
            ChunkAssetEntry newRootChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(rootChunkData, null, null, newEntry.AddedBundles.ToArray()));
            ChunkAssetEntry newSubtitleChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(subtitleChunkData, null, null, newEntry.AddedBundles.ToArray()));

            newEntry.LinkAsset(newRootChunk);
            newEntry.LinkAsset(newSubtitleChunk);

            // Finally, assign to the new asset's ChunkId and SubtitleChunkId properties with the new chunk ids
            dynamic newAssetObject = newAsset.RootObject;

            newAssetObject.ChunkGuid = newRootChunk.Id;
            newAssetObject.ChunkSize = (uint)rootChunkData.Length;
            newAssetObject.SubtitleChunkGuid = newSubtitleChunk.Id;
            newAssetObject.SubtitleChunkSize = (uint)subtitleChunkData.Length;

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);

            return newEntry;
        }

        public override bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            if (!base.RemoveFromBundle(entry, bundle))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;

            ChunkAssetEntry rootChunk = App.AssetManager.GetChunkEntry(rootObject.ChunkGuid);
            ChunkAssetEntry subtitleChunk = App.AssetManager.GetChunkEntry(rootObject.SubtitleChunkGuid);

            int bundleId = App.AssetManager.GetBundleId(bundle);

            rootChunk?.RemoveFromBundle(bundleId);
            entry.LinkAsset(rootChunk);

            subtitleChunk?.RemoveFromBundle(bundleId);
            entry.LinkAsset(subtitleChunk);

            return true;
        }
    }
}

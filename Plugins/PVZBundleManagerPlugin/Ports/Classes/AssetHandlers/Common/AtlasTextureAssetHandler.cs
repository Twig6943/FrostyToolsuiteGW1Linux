using Frosty.Core;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using GW2BundleManagerPlugin.Ports.Classes.Resources;
using System;
using System.Collections.Generic;
using System.IO;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class AtlasTextureAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "AtlasTextureAsset";

        public override bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            // Adjusted to check for null references

            if (!base.AddToBundle(entry, bentry))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;
            int bundleId = App.AssetManager.GetBundleId(bentry);

            ResAssetEntry textureRes = App.AssetManager.GetResEntry(rootObject.Resource);

            // Technically the bundle edit hasn't failed; the res just doesn't exist
            if (textureRes == null)
                return true;

            textureRes.BetterAddToBundle(bundleId);
            entry.LinkAsset(textureRes);

            AtlasTexture resAs = App.AssetManager.GetResAs<AtlasTexture>(textureRes);

            ChunkAssetEntry textureChunk = App.AssetManager.GetChunkEntry(resAs.ChunkId);
            textureChunk?.BetterAddToBundle(App.AssetManager.GetBundleId(bentry));
            textureRes?.LinkAsset(textureChunk);

            // TODO: Handle FirstMips for FB 2017

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAssetEntry newEntry = base.Create(name, basedOnEntry, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);

            dynamic newAssetObject = newAsset.RootObject;
            AtlasTexture newTexture = new AtlasTexture();

            byte[] textureChunkData = new byte[0];

            // If newType is null, the new entry is based off of an existing entry (basedOnEntry); if it isn't, the new entry should be treated as a completely new asset
            if (newType == null)
            {
                dynamic basedOnObject = App.AssetManager.GetEbx(basedOnEntry).RootObject;
                ResAssetEntry textureRes = App.AssetManager.GetResEntry(basedOnObject.Resource);

                if (textureRes != null)
                {
                    newTexture.Read(new NativeReader(App.AssetManager.GetRes(textureRes)), App.AssetManager, textureRes, null);
                    textureChunkData = NativeReader.ReadInStream(newTexture.Data);
                }
            }

            // Create the chunk first, as the res depends on the chunk
            ChunkAssetEntry newTextureChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(textureChunkData, null, null, newEntry.AddedBundles.ToArray()));
            newTexture.SetData(newTexture.Width, newTexture.Height, newTextureChunk.Id, App.AssetManager); // Width/height will be set if basedOnEntry is used

            // Finally, create the res
            ResAssetEntry newTextureRes = App.AssetManager.AddRes(newEntry.Name.ToLowerInvariant(), ResourceType.AtlasTexture, newTexture.ResourceMeta, newTexture.SaveBytes(), newEntry.AddedBundles.ToArray());
            newTextureRes.LinkAsset(newTextureChunk);
            newEntry.LinkAsset(newTextureRes);

            newAssetObject.Resource = newTextureRes.ResRid;

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);
            return newEntry;
        }

        public override bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            if (!base.RemoveFromBundle(entry, bundle))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;
            int bundleId = App.AssetManager.GetBundleId(bundle);

            ResAssetEntry textureRes = App.AssetManager.GetResEntry(rootObject.Resource);

            if (textureRes == null)
                return true;

            textureRes.RemoveFromBundle(bundleId);
            entry.LinkAsset(textureRes);

            // If the associated resource entry is not null, then the resource data exists as well
            AtlasTexture texture = App.AssetManager.GetResAs<AtlasTexture>(textureRes);

            ChunkAssetEntry textureChunk = App.AssetManager.GetChunkEntry(texture.ChunkId);
            textureChunk?.RemoveFromBundle(bundleId);
            textureRes?.LinkAsset(textureChunk);

            return true;
        }
    }
}

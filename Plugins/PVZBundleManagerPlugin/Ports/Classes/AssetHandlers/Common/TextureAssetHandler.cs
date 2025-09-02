using Frosty.Core;
using Frosty.Core.Viewport;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class TextureAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "TextureAsset";

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

            Texture texture = App.AssetManager.GetResAs<Texture>(textureRes);
            ChunkAssetEntry textureChunk = App.AssetManager.GetChunkEntry(texture.ChunkId);

            if (textureChunk == null)
                return true;

            // Calculate range start, logical size, etc
            texture.CalculateMipData(
                texture.MipCount,
                TextureUtils.GetFormatBlockSize(texture.PixelFormat),
                TextureUtils.IsCompressedFormat(texture.PixelFormat),
                (uint)texture.Data.Length
            );

            textureChunk.BetterAddToBundle(App.AssetManager.GetBundleId(bentry));
            textureRes.LinkAsset(textureChunk);

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAssetEntry newEntry = base.Create(name, basedOnEntry, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);

            dynamic newAssetObject = newAsset.RootObject;
            Texture newTexture = new Texture();

            byte[] textureChunkData = new byte[0];

            // If newType is null, the new entry is based off of an existing entry (basedOnEntry); if it isn't, the new entry should be treated as a completely new asset
            if (newType == null)
            {
                ResAssetEntry textureRes = App.AssetManager.GetResEntry(newAssetObject.Resource);

                if (textureRes != null)
                {
                    newTexture = App.AssetManager.GetResAs<Texture>(textureRes);

                    // Recalculate mips
                    newTexture.CalculateMipData(
                        newTexture.MipCount,
                        TextureUtils.GetFormatBlockSize(newTexture.PixelFormat),
                        TextureUtils.IsCompressedFormat(newTexture.PixelFormat),
                        (uint)newTexture.Data.Length
                    );

                    textureChunkData = NativeReader.ReadInStream(newTexture.Data);
                }
            }

            string resName = name.ToLowerInvariant();

            newTexture.AssetNameHash = (uint)Fnv1.HashString(resName);

            // Create the chunk first, as the res depends on the chunk
            ChunkAssetEntry newTextureChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(textureChunkData, null, newTexture, newEntry.AddedBundles.ToArray()));
            newTexture.SetData(newTextureChunk.Id, App.AssetManager);

            // Finally, create the res
            ResAssetEntry newTextureRes = App.AssetManager.AddRes(resName, ResourceType.Texture, newTexture.ResourceMeta, newTexture.SaveBytes(), newEntry.AddedBundles.ToArray());

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
            Texture texture = App.AssetManager.GetResAs<Texture>(textureRes);
            ChunkAssetEntry textureChunk = App.AssetManager.GetChunkEntry(texture.ChunkId);

            if (textureChunk == null)
                return true;

            texture.CalculateMipData(
                texture.MipCount,
                TextureUtils.GetFormatBlockSize(texture.PixelFormat),
                TextureUtils.IsCompressedFormat(texture.PixelFormat),
                (uint)texture.Data.Length
            );

            textureChunk.RemoveFromBundle(bundleId);
            textureRes.LinkAsset(textureChunk);

            return true;
        }
    }
}

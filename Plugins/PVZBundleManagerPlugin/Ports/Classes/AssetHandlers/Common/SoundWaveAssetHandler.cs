using Frosty.Core;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class SoundWaveAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "SoundWaveAsset";

        public override bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            if (!base.AddToBundle(entry, bentry))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;
            foreach (dynamic item in rootObject.Chunks)
            {
                ChunkAssetEntry chunkAssetEntry = App.AssetManager.GetChunkEntry(item.ChunkId);

                chunkAssetEntry?.BetterAddToBundle(App.AssetManager.GetBundleId(bentry));
                entry.LinkAsset(chunkAssetEntry);
            }

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAssetEntry newEntry = base.Create(name, basedOnEntry, newType);
            EbxAsset newAsset = App.AssetManager.GetEbx(newEntry);

            dynamic newAssetObject = newAsset.RootObject;

            // With sounds, chunk dependencies are stored within a list, and so we must iterate rather than directly reference properties
            if (newType == null)
            {
                dynamic basedOnObject = App.AssetManager.GetEbx(basedOnEntry).RootObject;

                // Within this iteration, we will create an equivalent chunk for the new asset, and so create the necessary variables for such a task
                ChunkAssetEntry currentExistingChunk;
                ChunkAssetEntry currentNewChunk;

                byte[] currentSoundData = null;

                for (int i = 0; i < basedOnObject.Chunks.Count; i++)
                {
                    currentExistingChunk = App.AssetManager.GetChunkEntry(basedOnObject.Chunks[i].ChunkId);

                    // Create empty data if the chunk doesn't exist (in the event that this actually occurs)
                    if (currentExistingChunk != null)
                        currentSoundData = NativeReader.ReadInStream(App.AssetManager.GetChunk(currentExistingChunk));

                    // Now, create our new chunk
                    currentNewChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(currentSoundData, null, null, newEntry.AddedBundles.ToArray()));
                    newEntry.LinkAsset(currentNewChunk);

                    newAssetObject.Chunks[i].ChunkId = currentNewChunk.Id;
                    newAssetObject.Chunks[i].ChunkSize = (uint)currentSoundData.Length;
                }
            }
            else
            {
                // If the sound is a completely new asset, create a chunk for it manually
                ChunkAssetEntry newChunk = App.AssetManager.GetChunkEntry(App.AssetManager.AddChunk(new byte[0], null, null, newEntry.AddedBundles.ToArray()));
                newEntry.LinkAsset(newChunk);

                dynamic soundDataChunk = TypeLibrary.CreateObject("SoundDataChunk");
                soundDataChunk.ChunkId = newChunk.Id;

                newAssetObject.Chunks.Add(soundDataChunk);
            }

            App.AssetManager.ModifyEbx(newEntry.Name, newAsset);
            return newEntry;
        }

        public override bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            if (!base.RemoveFromBundle(entry, bundle))
                return false;

            dynamic rootObject = App.AssetManager.GetEbx(entry).RootObject;
            ChunkAssetEntry currentSoundChunk;
            int bundleId = App.AssetManager.GetBundleId(bundle);

            foreach (dynamic soundDataChunk in rootObject.Chunks)
            {
                currentSoundChunk = App.AssetManager.GetChunkEntry(soundDataChunk.ChunkId);

                currentSoundChunk?.RemoveFromBundle(bundleId);
                entry.LinkAsset(currentSoundChunk);
            }

            return true;
        }
    }
}

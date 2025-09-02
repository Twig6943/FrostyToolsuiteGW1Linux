using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Viewport;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using GW2BundleManagerPlugin.Ports.Classes.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Ports.Classes.AssetHandlers.Common
{
    public class MeshAssetHandler : BaseAssetHandler
    {
        public override string AssetType => "MeshAsset";

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshAssetHandler"/> class.
        /// </summary>
        public MeshAssetHandler()
        {
        }

        private EbxAssetEntry GetBundleMeshVarDb(BundleEntry inBundle)
        {
            EbxAssetEntry result = null;

            // Find any database in the bundle that the mesh is now in, so that we can add to it
            foreach (EbxAssetEntry curDb in App.AssetManager.EnumerateEbx(
                "MeshVariationDatabase",
                bundleSubPath: inBundle.Name
            ))
            {
                result = curDb;
                break;
            }

            // If no database was found, create a new one
            if (result == null)
            {
                string bdlName = inBundle.Name.Substring(
                    (inBundle.Name.IndexOf('/') + 1)
                );

                // Blueprint bundles should go under Win32, while everything else is at the bundle path
                string newDbPath = string.Format("{0}{1}/MeshVariationDb_Win32", new object[]
                {
                    (inBundle.Type == BundleType.BlueprintBundle ? "Win32/" : null),
                    bdlName.ToLowerInvariant()
                });

                result = AssetHandlerDB.GetAssetHandler(null).Create(
                    newDbPath,
                    newType: TypeLibrary.GetType("MeshVariationDatabase")
                );

                result.AddedBundles.Add(App.AssetManager.GetBundleId(inBundle));
            }

            return result;
        }

        private void GetExistingMeshVarInfo(EbxAssetEntry inMeshEntry, out EbxAssetEntry outMeshVarDbEntry, out EbxAsset outMVDEAsset, out dynamic outMVDVariation)
        {
            // Default values
            outMeshVarDbEntry = null;
            outMVDEAsset = null;
            outMVDVariation = null;

            foreach (EbxAssetEntry curDb in App.AssetManager.EnumerateEbx("MeshVariationDatabase"))
            {
                EbxAsset cDAsset = App.AssetManager.GetEbx(curDb);

                // Doesn't contain our asset
                if (!cDAsset.Dependencies.Contains(inMeshEntry.Guid))
                    continue;

                dynamic cDARoot = cDAsset.RootObject;

                foreach (dynamic curEntry in cDARoot.Entries)
                {
                    PointerRef cEMesh = (PointerRef)curEntry.Mesh;

                    // VariationAssetNameHash indicates that it is based on another variation
                    if (cEMesh.External.FileGuid != inMeshEntry.Guid || curEntry.VariationAssetNameHash != 0)
                        continue;

                    outMeshVarDbEntry = curDb;
                    outMVDEAsset = cDAsset;
                    outMVDVariation = curEntry;

                    break;
                }

                if (outMeshVarDbEntry != null)
                    break;
            }
        }

        private List<EbxAssetEntry> GetMeshVarTextures(dynamic inVariation)
        {
            List<EbxAssetEntry> result = new List<EbxAssetEntry>();

            foreach (dynamic curMaterial in inVariation.Materials)
            {
                foreach (dynamic curTexParam in curMaterial.TextureParameters)
                {
                    PointerRef cTPRef = (PointerRef)curTexParam.Value;
                    EbxAssetEntry cTPREntry = App.AssetManager.GetEbxEntry(cTPRef.External.FileGuid);

                    if (cTPREntry == null)
                        continue;

                    result.Add(cTPREntry);
                }
            }

            return result;
        }

        public override bool AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            if (!base.AddToBundle(entry, bentry))
                return false;

            int bundleId = App.AssetManager.GetBundleId(bentry);

            // Find an existing database that contains the mesh
            EbxAssetEntry existingVarDbEntry = null;
            EbxAsset eVDAsset = null;
            dynamic eVDAVariation = null;

            GetExistingMeshVarInfo(entry, out existingVarDbEntry, out eVDAsset, out eVDAVariation);

            // If we have an existing entry, add it to the new db
            if (eVDAVariation != null)
            {
                // We need to update the registries to allow for the mesh to be used
                EbxAssetEntry firstBdlMeshDb = GetBundleMeshVarDb(bentry);

                // Add the textures of the entry to the bundle
                foreach (EbxAssetEntry curTextureEntry in GetMeshVarTextures(eVDAVariation))
                {
                    if(!Plugin.BundleManager.CanBeLoadedByBundle(curTextureEntry, bundleId))
                    {
                        AssetHandlerDB.GetAssetHandler(curTextureEntry.Type).AddToBundle(curTextureEntry, bentry);
                    }
                }

                // Now, we can clone the variation
                // TODO: Move cloning functionality to another class
                FrostyClipboard.Current.SetData(eVDAVariation);
                dynamic eVDAVClone = FrostyClipboard.Current.GetData(eVDAsset, existingVarDbEntry);

                EbxAsset fBMDAst = App.AssetManager.GetEbx(firstBdlMeshDb);

                ((dynamic)fBMDAst.RootObject).Entries.Add(eVDAVClone);
                App.AssetManager.ModifyEbx(firstBdlMeshDb.Name, fBMDAst);
            }

            // Most of the time, all we need to add is this, OccluderMeshResource isn't too common
            dynamic rootObj = App.AssetManager.GetEbx(entry).RootObject;
            ResAssetEntry refRes = App.AssetManager.GetResEntry(rootObj.MeshSetResource);

            if (refRes == null)
                return true;

            refRes.BetterAddToBundle(bundleId);
            entry.LinkAsset(refRes);

            MeshSet rRMesh = App.AssetManager.GetResAs<MeshSet>(refRes);

            foreach (MeshSetLod rRMLod in rRMesh.Lods)
            {
                ChunkAssetEntry lodChunk = App.AssetManager.GetChunkEntry(rRMLod.ChunkId);
                lodChunk?.BetterAddToBundle(bundleId);

                if(lodChunk != null)
                {
                    // Contains inline mesh data, no lod chunk
                    refRes.LinkAsset(lodChunk);
                }
            }

            // If there isn't an occluder resource, that is all we need to do
            refRes = App.AssetManager.GetResEntry(rootObj.OccluderMeshResource);
            if (refRes == null)
                return true;

            refRes.BetterAddToBundle(bundleId);
            entry.LinkAsset(refRes);

            return true;
        }

        public override EbxAssetEntry Create(string name, EbxAssetEntry basedOnEntry = null, Type newType = null)
        {
            EbxAssetEntry newAstEntry = base.Create(name, basedOnEntry, newType);
            EbxAsset newAst = App.AssetManager.GetEbx(newAstEntry);

            dynamic nARootObj = newAst.RootObject;

            // Since we must link the new LOD chunks to the res, create it beforehand
            ResAssetEntry newMeshResEntry = App.AssetManager.AddRes(
                name.ToLowerInvariant(),
                ResourceType.MeshSet,
                null,
                new byte[0],
                newAstEntry.AddedBundles.ToArray()
            );

            MeshSet nMREData = new MeshSet();

            // If newType is null, this means that data from basedOnEntry will be used
            if (newType == null)
            {
                // Add the duped asset to meshdbs
                EbxAssetEntry existingVarDbEntry;
                EbxAsset eVDAsset;
                dynamic eVDAVariation;

                GetExistingMeshVarInfo(basedOnEntry, out existingVarDbEntry, out eVDAsset, out eVDAVariation);

                if (eVDAVariation != null)
                {
                    // Clone the variation beforehand; it is inefficient to repeat it within the loop
                    FrostyClipboard.Current.SetData(eVDAVariation);
                    dynamic eVDAVClone = FrostyClipboard.Current.GetData(eVDAsset, existingVarDbEntry);

                    eVDAVClone.Mesh = new PointerRef(newAstEntry.Guid);

                    foreach (int curBdlId in newAstEntry.AddedBundles)
                    {
                        BundleEntry cBEntry = App.AssetManager.GetBundleEntry(curBdlId);
                        // We only need to add the mesh to one db within the bundle
                        EbxAssetEntry firstBdlMeshDb = GetBundleMeshVarDb(cBEntry);

                        // Bundles will always be the same as the based on asset, so don't add textures to bundles

                        EbxAsset fBMDAst = App.AssetManager.GetEbx(firstBdlMeshDb);

                        ((dynamic)fBMDAst.RootObject).Entries.Add(eVDAVClone);
                        App.AssetManager.ModifyEbx(firstBdlMeshDb.Name, fBMDAst);
                    }
                }

                ResAssetEntry bOEMeshResEntry = App.AssetManager.GetResEntry(nARootObj.MeshSetResource);

                // If null, the pre-assigned value for nMREData will be used
                if (bOEMeshResEntry != null)
                {
                    nMREData = App.AssetManager.GetResAs<MeshSet>(bOEMeshResEntry);

                    // We can reuse the data, but only if we create new LOD chunks
                    for (int i = 0; i < nMREData.Lods.Count; i++)
                    {
                        MeshSetLod curLod = nMREData.Lods[i];
                        ChunkAssetEntry cLChunk = App.AssetManager.GetChunkEntry(curLod.ChunkId);

                        // In the event that this occurs, remove unknown chunk refs
                        if (cLChunk == null)
                        {
                            nMREData.Lods.Remove(curLod);
                            continue;
                        }

                        // Create the new chunk
                        byte[] cLCData = NativeReader.ReadInStream(App.AssetManager.GetChunk(cLChunk));
                        ChunkAssetEntry newLodChunk = App.AssetManager.GetChunkEntry(
                            App.AssetManager.AddChunk(cLCData, null, null, newAstEntry.AddedBundles.ToArray())
                        );

                        curLod.ChunkId = newLodChunk.Id;
                        newMeshResEntry.LinkAsset(newLodChunk);
                    }
                }

                // Rarely, there will be an occluder mesh that we need to dupe
                ResAssetEntry bOEOccluderResEntry = App.AssetManager.GetResEntry(nARootObj.OccluderMeshResource);

                if (bOEOccluderResEntry != null)
                {
                    // Occluder mesh resources don't have chunk deps, so duping is much more straightforward
                    ResAssetEntry newOccluderResEntry = App.AssetManager.AddRes(
                        string.Format("{0}_occludermesh", name.ToLowerInvariant()),
                        ResourceType.OccluderMesh,
                        bOEOccluderResEntry.ResMeta,
                        NativeReader.ReadInStream(App.AssetManager.GetRes(bOEOccluderResEntry)),
                        newAstEntry.AddedBundles.ToArray()
                    );

                    nARootObj.OccluderMeshResource = newOccluderResEntry.ResRid;
                    newAstEntry.LinkAsset(newOccluderResEntry);
                }
            }

            // If a blank asset is created, it will only have a new MeshResource by default, no occluder

            nARootObj.MeshSetResource = newMeshResEntry.ResRid;
            newAstEntry.LinkAsset(newMeshResEntry);

            // Now, we can give the mesh resource its ResMeta
            newMeshResEntry.ResMeta = nMREData.ResourceMeta;

            App.AssetManager.ModifyRes(name.ToLowerInvariant(), nMREData);
            App.AssetManager.ModifyEbx(name, newAst);

            return newAstEntry;
        }

        public override bool RemoveFromBundle(EbxAssetEntry entry, BundleEntry bundle)
        {
            if (!base.RemoveFromBundle(entry, bundle))
                return false;

            int bundleId = App.AssetManager.GetBundleId(bundle);
            dynamic rootObj = App.AssetManager.GetEbx(entry).RootObject;

            // Most of the time, all we need to add is this, OccluderMeshResource isn't too common
            ResAssetEntry refRes = App.AssetManager.GetResEntry(rootObj.MeshSetResource);

            // We'll have to remove the mesh from all variation databases to minimize conflicts
            foreach (EbxAssetEntry curDb in App.AssetManager.EnumerateEbx(
                "MeshVariationDatabase",
                bundleSubPath: bundle.Name
            ))
            {
                EbxAsset cDAsset = App.AssetManager.GetEbx(curDb);

                if (!cDAsset.Dependencies.Contains(entry.Guid))
                    continue;

                dynamic cDARoot = cDAsset.RootObject;

                // Find the entry itself
                for (int i = 0; i < cDARoot.Entries.Count; i++)
                {
                    dynamic curEntry = cDARoot.Entries[i];
                    PointerRef cEMesh = (PointerRef)curEntry.Mesh;

                    // We want to remove every variation, so don't check VariationAssetNameHash
                    if (cEMesh.External.FileGuid != entry.Guid)
                        continue;

                    cDARoot.Entries.Remove(curEntry);
                    App.AssetManager.ModifyEbx(curDb.Name, cDAsset);
                }
            }

            if (refRes == null)
                return true;

            refRes.RemoveFromBundle(bundleId);
            entry.LinkAsset(refRes);

            MeshSet rRMesh = App.AssetManager.GetResAs<MeshSet>(refRes);

            foreach (MeshSetLod rRMLod in rRMesh.Lods)
            {
                ChunkAssetEntry lodChunk = App.AssetManager.GetChunkEntry(rRMLod.ChunkId);
                lodChunk?.RemoveFromBundle(bundleId);

                refRes.LinkAsset(lodChunk);
            }

            // If there isn't an occluder resource, that is all we need to do
            refRes = App.AssetManager.GetResEntry(rootObj.OccluderMeshResource);
            if (refRes == null)
                return true;

            refRes.RemoveFromBundle(bundleId);
            entry.LinkAsset(refRes);

            return true;
        }
    }
}

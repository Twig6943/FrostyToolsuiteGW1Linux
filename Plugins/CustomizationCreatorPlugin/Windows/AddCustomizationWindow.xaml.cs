using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers.Entries;
using FrostySdk.Resources;

namespace CustomizationCreatorPlugin.Windows
{
    /// <summary>
    /// Interaction logic for AddCustomizationWindow.xaml
    /// </summary>
    public partial class AddCustomizationWindow : FrostyDockableWindow
    {
        private string mBlueprintDirectory = "";
        private string mCustomizationName = "";

        public AddCustomizationWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void varBPDirTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            mBlueprintDirectory = varBPDirTextBox.Text.TrimEnd('/');
        }

        private void varWepNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            mCustomizationName = varWepNameTextBox.Text;
        }

        private string VerifyFileName(string inFilename)
        {
            string filename = inFilename;
            if (filename.Contains("//"))
            {
                filename = filename.Replace("//", "/");
            }

            if (filename.Contains("\\"))
            {
                filename = filename.Replace("\\", "/");
            }

            return filename;
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry entry = App.AssetManager.GetEbxEntry("");

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                entry  = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Body/Default/browncoat_body_default_visualasset_bpb");
            }
            else if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                entry = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Costume/browncoat_costume_default_unlockasset_bpb");
            }

            string baseName = entry.Name.Substring(0, entry.Name.LastIndexOf('/') + 1);
            uint customizationHash = (uint)Utils.HashString(mCustomizationName + DateTime.Now.ToFileTime());
            
            //string newName = $"{baseName}{customizationHash}/{mCustomizationName}_visualasset_bpb";
            string newName = $"";

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                newName = $"{baseName}{customizationHash}/{mCustomizationName}_visualasset_bpb";
            }
            else if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                newName = $"{baseName}{customizationHash}/{mCustomizationName}_unlockasset_bpb";
            }

            VerifyFileName(newName);
            EbxAssetEntry newBpb = CreateAsset(newName, TypeLibrary.GetType("BlueprintBundle"));
            newName = newName.ToLowerInvariant().Replace("win32/", string.Empty);

            // Create the new bundle
            string bundleName = "win32/" + newName;
            BundleEntry newBundleEntry = App.AssetManager.AddBundle(bundleName, BundleType.BlueprintBundle, App.AssetManager.GetBundleEntry(entry.Bundles[0]).SuperBundleId);
            int bundleId = App.AssetManager.GetBundleId(newBundleEntry);

            newBpb.AddedBundles.Add(bundleId);

            dynamic bpbRoot = App.AssetManager.GetEbx(entry).RootObject;

            /*// Get the ObjectBlueprint to dupe
            EbxAssetEntry objectBP = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Body/Default/BrownCoat_Body_Default");

            // Dupe the ObjectBlueprint
            string objectBPName = $"Characters/Zombie/Horde/BrownCoat/Body/Default/{mCustomizationName}";

            EbxAssetEntry newObjectBP = CreateAsset(objectBPName, TypeLibrary.GetType("ObjectBlueprint"));
            newObjectBP.AddedBundles.Clear();
            newObjectBP.AddedBundles.Add(bundleId);
            dynamic newObjectBPRoot = App.AssetManager.GetEbx(newObjectBP).RootObject;*/

            string customizationBPAssetName = $"{mBlueprintDirectory}/{mCustomizationName}";
            
            //Type customizationBPType = TypeLibrary.GetType("VisualCustomizationAsset");
            Type customizationBPType = TypeLibrary.GetType("");

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                customizationBPType = TypeLibrary.GetType("VisualCustomizationAsset");
            }
            else if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                customizationBPType = TypeLibrary.GetType("PVZVisualUnlockAsset");
            }

            EbxAssetEntry newCustomizationBP = CreateAsset(customizationBPAssetName, customizationBPType);
            newCustomizationBP.AddToBundle(bundleId);

            // Update the new BPB
            EbxAsset newBpbAsset = App.AssetManager.GetEbx(newBpb);
            dynamic newBpbRoot = newBpbAsset.RootObject;

            // TODO: Create a new instance of DataContainerCollectionBlueprint
            //dynamic funny = TypeLibrary.CreateObject("DataContainerCollectionBlueprint");
            //newBpbRoot.Blueprint = funny; 

            // Temporary
            //newBpbRoot.Blueprint = bpbRoot.Blueprint;

            newBpbAsset.AddDependency(newCustomizationBP.Guid);
            App.AssetManager.ModifyEbx(newBpb.Name, newBpbAsset);

            // Create the VisualAsset
            //EbxAssetEntry baseVisual = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Body/Default/BrownCoat_Body_Default_VisualAsset");
            EbxAssetEntry baseVisual = App.AssetManager.GetEbxEntry("");

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                baseVisual = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Body/Default/BrownCoat_Body_Default_VisualAsset");
            }
            else if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                baseVisual = App.AssetManager.GetEbxEntry("Characters/Zombie/Horde/BrownCoat/Costume/BrownCoat_Costume_Default_UnlockAsset");
            }

            EbxAssetEntry newVisual = DuplicateAsset(baseVisual, $"{mBlueprintDirectory}/{mCustomizationName}", false);

            EbxAsset newVisualAsset = App.AssetManager.GetEbx(newVisual);
            dynamic newVisualAssetRoot = newVisualAsset.RootObject;

            //newVisualAssetRoot.BlueprintBundleReference.Name = bundleName.Replace("win32/", string.Empty);
            //App.AssetManager.ModifyEbx(newVisual.Name, newVisualAsset);

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                newVisualAssetRoot.BlueprintBundleReference.Name = bundleName.Replace("win32/", string.Empty);
            }
            else if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                newVisualAssetRoot.Identifier = (uint)Utils.HashString($"{newVisual.Name}{newVisual.Guid}", true);
                newVisualAssetRoot.DebugUnlockId = newVisual.Name.Split('/').Last();
                newVisualAssetRoot.BlueprintBundleReference.Name = bundleName.Replace("win32/", string.Empty);
            }
            App.AssetManager.ModifyEbx(newVisual.Name, newVisualAsset);

            App.Logger.Log("Successfully created the customization {0} and bundle {1}.", newCustomizationBP.Name, bundleName);
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private EbxAssetEntry CreateAsset(string newName, Type newType)
        {
            EbxAsset newAsset = null;

            newAsset = new EbxAsset(TypeLibrary.CreateObject(newType.Name));

            newAsset.SetFileGuid(Guid.NewGuid());

            dynamic obj = newAsset.RootObject;
            obj.Name = newName;

            AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
            obj.SetInstanceGuid(guid);

            EbxAssetEntry newEntry = App.AssetManager.AddEbx(newName, newAsset);

            newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);

            return newEntry;
        }

        private EbxAssetEntry DuplicateAsset(EbxAssetEntry entry, string newName, bool createNew, Type newType = null)
        {
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            EbxAsset newAsset = null;

            if (createNew)
            {
                newAsset = new EbxAsset(TypeLibrary.CreateObject(newType.Name));
            }
            else
            {
                using (EbxBaseWriter writer = EbxBaseWriter.CreateWriter(new MemoryStream(), EbxWriteFlags.DoNotSort | EbxWriteFlags.IncludeTransient))
                {
                    writer.WriteAsset(asset);
                    byte[] buf = writer.ToByteArray();
                    using (EbxReader reader = EbxReader.CreateReader(new MemoryStream(buf)))
                        newAsset = reader.ReadAsset<EbxAsset>();
                }
            }

            newAsset.SetFileGuid(Guid.NewGuid());

            dynamic obj = newAsset.RootObject;
            obj.Name = newName;

            AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
            obj.SetInstanceGuid(guid);

            EbxAssetEntry newEntry = App.AssetManager.AddEbx(newName, newAsset);

            newEntry.AddedBundles.AddRange(entry.EnumerateBundles());
            newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);

            return newEntry;
        }

        private ResAssetEntry DuplicateRes(ResAssetEntry entry, string name, ResourceType resType)
        {
            if (App.AssetManager.GetResEntry(name) == null)
            {
                ResAssetEntry newEntry;
                using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(entry)))
                {
                    newEntry = App.AssetManager.AddRes(name, resType, entry.ResMeta, reader.ReadToEnd(), entry.EnumerateBundles().ToArray());
                }
                return newEntry;
            }
            else
            // A resource with this name already exists
            {
                return null;
            }
        }

        private ChunkAssetEntry DuplicateChunk(ChunkAssetEntry entry, Texture texture = null)
        {
            byte[] random = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            // Create a unique GUID for the new chunk
            while (true)
            {
                rng.GetBytes(random);

                random[15] |= 1;

                if (App.AssetManager.GetChunkEntry(new Guid(random)) == null)
                {
                    break;
                }
            }
            Guid newGuid;
            using (NativeReader reader = new NativeReader(App.AssetManager.GetChunk(entry)))
            {
                newGuid = App.AssetManager.AddChunk(reader.ReadToEnd(), new Guid(random), texture, entry.EnumerateBundles().ToArray());
            }

            ChunkAssetEntry newEntry = App.AssetManager.GetChunkEntry(newGuid);

            return newEntry;
        }
    }
}

using Frosty.Core;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GW2BundleManagerPlugin.Ports.Classes
{
    public static class AssetHandlerDB
    {
        private static Dictionary<string, BaseAssetHandler> assetHandlers = GetAssetHandlers();

        /// <summary>
        /// Retrieves the asset handler for a given type.
        /// </summary>
        /// <param name="inType">The type name to be used.</param>
        /// <param name="inIncludeSubclasses">Optional. A bool determining whether or not asset handlers made for subclasses of the given type should be used, if available.</param>
        /// <returns>The retrieved asset handler. If a handler made specifically for the asset of type <paramref name="inType"/> was not found, an instance of <see cref="BaseAssetHandler"/> will be returned.</returns>
        public static BaseAssetHandler GetAssetHandler(string inType, bool inIncludeSubclasses = true)
        {
            // If the given type is null, simply use the base asset handler
            inType ??= "null";

            if (assetHandlers.ContainsKey(inType))
                return assetHandlers[inType];

            BaseAssetHandler result = null;

            if (inIncludeSubclasses)
            {
                // To find a subclass, we must iterate over each KeyValuePair within the AssetHandlers dictionary and check if type is a subclass of the key
                foreach (KeyValuePair<string, BaseAssetHandler> typeHandlerPair in assetHandlers)
                {
                    // We need to ensure that classes at the lowest level of inheritance do not override higher-level classes
                    // Exact types should always have priority
                    if (result != null && TypeLibrary.IsSubClassOf(result.AssetType, typeHandlerPair.Key))
                        continue;

                    if (TypeLibrary.IsSubClassOf(inType, typeHandlerPair.Key))
                        result = typeHandlerPair.Value;
                }
            }

            return result ?? assetHandlers["null"];
        }

        /// <summary>
        /// Gathers a collection of all available asset handlers, optionally filtering them for the active profile.
        /// </summary>
        /// <returns>The collection of retrieved asset handlers.</returns>
        public static Dictionary<string, BaseAssetHandler> GetAssetHandlers()
        {
            BaseAssetHandler currentHandler;
            Dictionary<string, BaseAssetHandler> result = new Dictionary<string, BaseAssetHandler>();

            // To allow for asset handlers to be defined outside of this assembly, we must iterate over every loaded assembly
            foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Added try catch to catch reflection type load exceptions
                try
                {
                    foreach (Type assemblyType in loadedAssembly.GetTypes())
                    {
                        // Filter out anything that is not a subclass of BaseAssetHandler
                        if (!assemblyType.IsSubclassOf(typeof(BaseAssetHandler)))
                            continue;

                        currentHandler = (BaseAssetHandler)Activator.CreateInstance(assemblyType);

                        // If a handler already exists with the same asset type, determine which one is most significant
                        // In this case, handlers from plugins are most significant
                        if (result.ContainsKey(currentHandler.AssetType) && App.PluginManager.GetPluginAssembly(assemblyType.Assembly.GetName().Name) == null)
                        {
                            throw new Exception(string.Format("Conflicting asset handlers for asset type \"{0}\" found between assembly \"{1}\" and assembly \"{2}.\" Assemblies are not a plugin and are most likely Frosty assemblies.", new object[]
                            {
                                currentHandler.AssetType,
                                assemblyType.Assembly.GetName().Name,
                                result[currentHandler.AssetType].GetType().Name
                            }));
                        }

                        // Filter out anything that is unsupported by the asset handler
                        if (currentHandler.SupportedProfiles != null && !currentHandler.SupportedProfiles.Contains((ProfileVersion)ProfilesLibrary.DataVersion))
                            continue;

                        if (currentHandler.UnsupportedProfiles != null && currentHandler.UnsupportedProfiles.Contains((ProfileVersion)ProfilesLibrary.DataVersion))
                            continue;

                        result[currentHandler.AssetType] = currentHandler;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.WriteLine(string.Format("Plugin \"{0}\" returned \"ReflectionTypeLoadException\" when accessing DefinedTypes. This is likely due to a dependency on another plugin, which may fail to retrieve its dependencies due to plugin integration.", loadedAssembly.GetName().Name));
                }
            }

            // For assets that do not have a dedicated instance, use the default handler
            result["null"] = new BaseAssetHandler();

            return result;
        }
    }
}

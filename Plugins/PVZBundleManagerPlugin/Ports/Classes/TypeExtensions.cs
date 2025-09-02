using Frosty.Controls;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GW2BundleManagerPlugin.Ports.Classes
{
    public static class TypeExtensions
    {
        public static bool BetterAddToBundle(this AssetEntry assetEntry, int bid)
        {
            // TOC chunk
            if (assetEntry is ChunkAssetEntry && assetEntry.Bundles.Count == 0 && !assetEntry.IsAdded)
                return false;

            if (assetEntry.IsInBundle(bid))
                return false;

            // If the bundle was previously removed
            if (assetEntry.RemBundles.Contains(bid))
            {
                assetEntry.RemBundles.Remove(bid);

                // RemBundles is intended for base bundles,
                // so if there is no base bundle with that id,
                // just remove it
                // Otherwise, this is where we can return
                if (assetEntry.Bundles.Contains(bid))
                {
                    assetEntry.IsDirty = true;
                    return true;
                }
            }

            assetEntry.AddedBundles.Add(bid);
            assetEntry.IsDirty = true;
            return true;
        }

        /// <summary>
        /// Enumerates through the asset entries of a type provided via an existing <see cref="AssetEntry"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="AssetEntry"/> to enumerate through.</typeparam>
        /// <param name="entryType">The asset entry type to be used.</param>
        /// <param name="modifiedOnly">A bool determining whether or not modified assets should only be enumerated for any applicable enumeration method.</param>
        /// <param name="customAssetBranch">The type to be used in custom asset enumeration.</param>
        /// <returns>A list of the enumerated assets.</returns>
        public static List<AssetEntry> EnumerateByEntryType(this AssetManager assetManager, Type entryType, bool modifiedOnly = false, string customAssetBranch = "legacy")
        {
            // Create a list of elements with the provided type
            List<AssetEntry> enumeratedEntries = new List<AssetEntry>();

            // Begin a switch statement over the source entry's type name
            switch (entryType.Name)
            {
                case "AssetEntry":
                    // Add the results of an enumeration over custom assets to the list of enumerated entries
                    enumeratedEntries.AddRange(assetManager.EnumerateCustomAssets(customAssetBranch, modifiedOnly));

                    // Break this case
                    break;

                case "ChunkAssetEntry":
                    // Add the results of an enumeration over chunks to the list of enumerated entries
                    enumeratedEntries.AddRange(assetManager.EnumerateChunks(modifiedOnly));

                    // Break this case
                    break;

                case "EbxAssetEntry":
                    // Append the elements of an enumeration over the available asset entries to the associated output list
                    enumeratedEntries.AddRange(assetManager.EnumerateEbx("", modifiedOnly));

                    // Break this case
                    break;

                case "ResAssetEntry":
                    // Add the res enumeration results to the associated output list
                    enumeratedEntries.AddRange(assetManager.EnumerateRes(0, modifiedOnly));

                    // Break this case
                    break;
            }

            // Return the list of enumerated entries
            return enumeratedEntries;
        }

        /// <summary>
        /// Exports an asset's dependencies.
        /// </summary>
        /// <param name="inEbx">The <see cref="EbxAsset"/> to be used.</param>
        /// <param name="inOutputPath">The output path to export the assets to.</param>
        /// <param name="inDoRecursively">Determines whether or not recursive export should be used.</param>
        /// <param name="inCreateDirectories">Determines whether or not assets will be exported to a single directory or numerous subdirectories.</param>
        /// <param name="inIODefinition">The </param>
        /// <param name="inExpDefinition">The export <see cref="AssetDefinition"/> to be used. This parameter is deprecated and solely exists for compatibility reasons, use <paramref name="inIODefinition"/> instead.</param>
        /// <param name="inExtension">The file extension to be used for determining the export type.</param>
        public static void ExportAssetDependencies(this AssetManager assetManager, EbxAsset inEbx, string inOutputPath, bool inCreateDirectories = false,
                                                   bool inDoRecursively = false, AssetDefinition inExpDefinition = null, string inExtension = null)
        {
            if (inEbx.Dependencies.Count() == 0)
                return;

            foreach (Guid dependencyGuidEntry in inEbx.Dependencies)
            {
                EbxAssetEntry dependencyAssetEntry = assetManager.GetEbxEntry(dependencyGuidEntry);
                EbxAsset dependencyEbx = assetManager.GetEbx(dependencyAssetEntry);

                Stream dependencyEbxStream = assetManager.GetEbxStream(dependencyAssetEntry);

                string dependencyOutputPath = inCreateDirectories ? Path.Combine(new string[]
                {
                    inOutputPath,
                    string.Concat(new string[]
                    {
                        App.AssetManager.GetEbxEntry(inEbx.FileGuid).Filename,
                        "_Dependencies"
                    })
                }) : inOutputPath;

                Directory.CreateDirectory(dependencyOutputPath);

                bool exportSuccessful = false;

                // If IODefinition does not work, try using a (deprecated) AssetDefinition export
                if (!exportSuccessful && inExpDefinition != null && !string.IsNullOrEmpty(inExtension))
                    exportSuccessful = inExpDefinition.Export(dependencyAssetEntry, dependencyOutputPath, inExtension);

                // Check if the external export process was successful
                if (!exportSuccessful)
                {
                    // Fallback to writing the binary partition
                    using (NativeWriter nativeWriter = new NativeWriter(new FileStream(string.Concat(new string[]
                    {
                        Path.Combine(new string[]
                        {
                            dependencyOutputPath,
                            dependencyAssetEntry.Filename
                        }),
                        ".bin"
                    }), FileMode.Create, FileAccess.Write)))
                    {
                        nativeWriter.Write(NativeReader.ReadInStream(dependencyEbxStream));
                    }
                }

                // Check if recursive export should be used
                if (inDoRecursively)
                {
                    // Recursively export any available dependencies of the current dependency
                    assetManager.ExportAssetDependencies(dependencyEbx, dependencyOutputPath, inCreateDirectories, inDoRecursively, inExpDefinition, inExtension);
                }
            }
        }

        /// <summary>
        /// Navigates through the visual tree of a <see cref="DependencyObject"/> until an object whose <see cref="Type"/> matches <typeparamref name="T"/> is found.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="DependencyObject"/> that should be searched for.</typeparam>
        /// <param name="includeDerivedTypes">A bool determining whether or not the derived types of this <see cref="DependencyObject"/> should be included in the type comparison.</param>
        /// <param name="resultsToSkip">A quantity of results that should be skipped.</param>
        /// <param name="returnSkippedResultIfNoResults">A bool determining whether or not the last skipped result should be returned in the event no other elements are found.</param>
        /// <param name="navigateDownwards">A bool determining whether or not the search should be performed downwards. If false, the search will be conducted upwards.</param>
        /// <returns>A <see cref="DependencyObject"/> whose <see cref="Type"/> matches <typeparamref name="T"/> or null if nothing was found.</returns>
        public static T FindByType<T>(this DependencyObject dependencyObject, bool includeDerivedTypes = true, int resultsToSkip = 0, bool returnSkippedResultIfNoResults = false, bool navigateDownwards = true)
        {
            DependencyObject currentObject = dependencyObject;
            DependencyObject lastSkippedResult = null;
            DependencyObject result = null;

            if (navigateDownwards)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    currentObject = VisualTreeHelper.GetChild(dependencyObject, i);

                    if (includeDerivedTypes ? currentObject is T : currentObject.GetType() == typeof(T))
                    {
                        result = currentObject;
                    }

                    // If the current object is not of the type T or if it was skipped, recursively execute FindByType upon it and assign that to the result only if it is null
                    result ??= (DependencyObject)(object)currentObject.FindByType<T>(includeDerivedTypes, resultsToSkip, returnSkippedResultIfNoResults, navigateDownwards);

                    if (result != null)
                    {
                        if (resultsToSkip != 0)
                        {
                            resultsToSkip--;
                            lastSkippedResult = result;
                            continue;
                        }

                        break;
                    }
                }
            }
            else
            {
                // If navigateDownwards is false, we must navigate upwards

                // Rather than pass the break condition directly to the while loop, we will use an if statement to handle it, as this allows for result skip functionality
                while (true)
                {
                    currentObject = !(currentObject is FrostyDockableWindow) ? VisualTreeHelper.GetParent(currentObject) : ((FrostyDockableWindow)currentObject).WindowParent;

                    if (currentObject == null)
                    {
                        // Break the while loop if there are no leftover DependencyObject parents/children
                        break;
                    }

                    // Compare the current object against the given type based on includeDerivedTypes
                    if (includeDerivedTypes ? currentObject is T : currentObject.GetType() == typeof(T))
                    {
                        if (resultsToSkip == 0)
                        {
                            result = currentObject;
                            break;
                        }

                        resultsToSkip--;
                        lastSkippedResult = currentObject;
                    }
                }
            }

            return (T)(object)(result ?? (returnSkippedResultIfNoResults ? lastSkippedResult : null));
        }

        /// <summary>
        /// Retrieves all dependencies of an asset, including the dependencies of its references.
        /// </summary>
        /// <returns>All dependencies of the asset.</returns>
        public static List<EbxAssetEntry> GetAllDependencies(this EbxAssetEntry ebxAssetEntry)
        {
            List<Guid> depsToProc = ebxAssetEntry.EnumerateDependencies().ToList();
            List<EbxAssetEntry> result = new List<EbxAssetEntry>();

            // Use "for" so that we may add to the collection
            for (int i = 0; i < depsToProc.Count; i++)
            {
                EbxAssetEntry curEntry = App.AssetManager.GetEbxEntry(depsToProc[i]);
                if (curEntry == null)
                    continue;

                if (!result.Contains(curEntry))
                {
                    result.Add(curEntry);
                    // Process its dependencies as well
                    depsToProc.AddRange(curEntry.EnumerateDependencies());
                }
            }

            return result;
        }

        /// <summary>
        /// Iterates over every element in a <see cref="List{CString}"/> and adds a string conversion of such an element to a returned list.
        /// </summary>
        /// <param name="cStringList">The <see cref="CString"/> list to be used.</param>
        /// <returns>A list of strings consisting of <see cref="CString"/> string conversions.</returns>
        public static List<string> NormalizeCStringList(this List<CString> cStringList)
        {
            // Create a list of strings for storing converted strings from the provided cstring list
            List<string> stringList = new List<string>();

            // Begin an iteration over the collection of cstrings
            for (int i = 0; i < cStringList.Count; i++)
            {
                // Convert the iterator's current selection to a string and add it to the list of converted strings
                stringList.Add(cStringList[i].ToString());
            }

            // Return the converted list
            return stringList;
        }

        /// <summary>
        /// Retrieves a <see cref="FrameworkElement"/> with the specified name and casts it to the specified type <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> that the retrieved <see cref="FrameworkElement"/> should be casted to.</typeparam>
        /// <param name="name">The name to be used in the search.</param>
        /// <param name="navigateDownwards">Optional. A bool determining whether or not the search should be performed downwards on the visual tree. If false, the search will be conducted upwards.</param>
        /// <returns>If the search returned a result, the <see cref="FrameworkElement"/> specified by name. Otherwise, null.</returns>
        public static T RecursiveFindName<T>(this FrameworkElement uiElement, string name, bool navigateDownwards = true)
        {
            FrameworkElement currentElement = uiElement;
            T result = default;

            if (navigateDownwards)
            {
                // Get the amount of child elements hosted by the source element
                int childrenCount = VisualTreeHelper.GetChildrenCount(uiElement);

                // Iterate through each child of the provided UIElement via index
                for (int i = 0; i < childrenCount; i++)
                {
                    // Check if an element has been found
                    if (result != null)
                    {
                        break;
                    }

                    currentElement = VisualTreeHelper.GetChild(uiElement, i) as FrameworkElement;

                    // Skip elements that are not of the type FrameworkElement
                    if (currentElement == null)
                    {
                        continue;
                    }

                    if (currentElement.Name == name)
                    {
                        // Attempt to cast the element
                        result = (T)(object)currentElement;
                        break;
                    }

                    // If no result was found, execute this method recursively over the retrieved element and assign the returned value to the result
                    result = currentElement.RecursiveFindName<T>(name);
                }
            }
            else
            {
                while (true)
                {
                    currentElement = (!(currentElement is FrostyDockableWindow) ? VisualTreeHelper.GetParent(currentElement) : ((FrostyDockableWindow)currentElement).WindowParent) as FrameworkElement;

                    // If currentElement is null, this may also indicate that one of the parents is not a FrameworkElement, which would mean there is no Name property
                    if (currentElement == null)
                    {
                        break;
                    }

                    if (currentElement.Name == name)
                    {
                        result = (T)(object)currentElement;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the value of a field within a class regardless of its access modifiers.
        /// </summary>
        /// <param name="targetField">The field name to be used.</param>
        public static object GetFieldValue(this object refObject, string targetField)
        {
            // Create a type for storing the current type selection
            Type currentType = refObject.GetType();

            // Create a FieldInfo instance for storing the retrieved instance
            FieldInfo objectField = null;

            // Begin a loop based on the condition of the object's current type not being null and its field being null
            while (currentType != null && objectField == null)
            {
                // Assign to the object field with the current FieldInfo retrieval attempt
                objectField = currentType.GetField(targetField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                // Assign to the object type with the next base type
                currentType = currentType.BaseType;
            }

            // Return a result based on the object field being null
            return objectField != null ? objectField.GetValue(refObject) : null;
        }

        /// <summary>
        /// Registers key bindings to a <see cref="CommandBindingCollection"/> via a dictionary of <see cref="KeyGesture"/>/<see cref="ExecutedRoutedEventHandler"/> pairings.
        /// </summary>
        /// <param name="gestureHandlerPairings">The dictionary of key/handler pairings to be used.</param>
        public static void RegisterKeyBindings(this CommandBindingCollection commandBindingCollection, Dictionary<KeyGesture, ExecutedRoutedEventHandler> gestureHandlerPairings)
        {
            // Create a RoutedCommand for storing the created command instances of the iteration
            RoutedCommand currentCommand;

            // Begin an iteration over the provided dictionary
            for (int i = 0; i < gestureHandlerPairings.Count; i++)
            {
                // Assign to the iteration's current RoutedCommand
                currentCommand = new RoutedCommand();

                // Register the iteration's selected key to the new command
                currentCommand.InputGestures.Add(gestureHandlerPairings.Keys.ElementAt(i));

                // Register a CommandBinding with the use of the newly-created RoutedCommand
                commandBindingCollection.Add(new CommandBinding(currentCommand, gestureHandlerPairings.Values.ElementAt(i)));
            }
        }

        /// <summary>
        /// Removes this <see cref="AssetEntry"/> from a bundle specified by its ID.
        /// </summary>
        /// <param name="bundleId">The ID of the bundle.</param>
        /// <returns>A bool indicating the success of this operation.</returns>
        public static bool RemoveFromBundle(this AssetEntry assetEntry, int bundleId)
        {
            if (assetEntry is ChunkAssetEntry && assetEntry.Bundles.Count == 0 && !assetEntry.IsAdded)
                return false;

            if (!assetEntry.IsInBundle(bundleId))
            {
                return false;
            }

            // Since RemBundles is specifically dedicated for the removal of existing bundles, we must check if the bundle is within AddedBundles as to avoid adding a non-default bundle to RemBundles
            if (assetEntry.AddedBundles.Contains(bundleId))
            {
                assetEntry.AddedBundles.Remove(bundleId);
                assetEntry.IsDirty = true;
                return true;
            }

            assetEntry.RemBundles.Add(bundleId);
            assetEntry.IsDirty = true;
            return true;
        }

        /// <summary>
        /// Gets the value of a <see cref="PointerRef"/>.
        /// </summary>
        /// <param name="pointerRef">The <see cref="PointerRef"/> to be used.</param>
        /// <returns>The resolved value.</returns>
        public static object Resolve(this PointerRef pointerRef)
        {
            // Create a variable for storing the returned pointerref value
            object pointerRefValue = null;

            // Check if it's external
            if (pointerRef.Type == PointerRefType.External)
            {
                // Get the pointerref's external ebximportreference
                EbxImportReference importReference = pointerRef.External;

                // Get the associated asset and asset entry
                EbxAssetEntry importEntry = App.AssetManager.GetEbxEntry(importReference.FileGuid);

                // If the importEntry is null, this is an invalid ref
                if (importEntry == null)
                    return null;

                if (importReference.ClassGuid == Guid.Empty)
                    return importEntry;

                EbxAsset importAsset = App.AssetManager.GetEbx(importEntry);

                // Set the pointerref's value to its import reference's referenced object
                pointerRefValue = importAsset.GetObject(importReference.ClassGuid);
            }
            else if (pointerRef.Type == PointerRefType.Internal)
            {
                // Set the pointerref's value to its internal value
                pointerRefValue = pointerRef.Internal;
            }

            // If it isn't either of these, it is a null pointerref, so nothing has to be set since the pointerref's value is defaulted to null
            // Return the value
            return pointerRefValue;
        }

        /// <summary>
        /// Initiates a search on an object via a provided list of properties and search operations, returning its findings once the operation is complete.
        /// </summary>
        /// <param name="refObject">The object to be used in the search.</param>
        /// <param name="targetProperties">The properties to search through until the last array element is reached.</param>
        /// <param name="returnNullIfNoResults">A bool determining whether or not null should be returned if no results have been found.</param>
        /// <param name="shouldResolveRefs">A bool determining whether or not <see cref="PointerRef"/> instances should be resolved. This may not apply to all scenarios, as resolving a <see cref="PointerRef"/> will be necessary at times.</param>
        /// <returns>The value of the last property in the <paramref name="targetProperties"/> array. If one of the properties is a <see cref="IEnumerable"/>, a list of the results will be returned.</returns>
        public static object Search(this object refObject, List<string> targetProperties, bool returnNullIfNoResults = false, bool shouldResolveRefs = true)
        {
            // Ported from 1.0.5.10, now with alterations to improve efficiency and to adapt to changes

            // Create an object for storing the search result
            object searchResult = null;

            // Check the provided parameters list either for being null or having no elements (which can be the case for the target properties list)
            if (refObject != null && targetProperties != null && targetProperties.Count != 0)
            {
                // Store the object's type
                Type objectType = refObject.GetType();

                // Store the current target property's name
                string currentPropertyName = targetProperties[0];

                // Store a variation of the current property's name that's been split by ":", which would indicate there's a search operation if there's elements
                string[] splittedPropertyName = currentPropertyName.Split(':');

                // Create a string for storing a potential search operation
                string searchOperation = "";

                // Check if there's more than one element in the results
                if (splittedPropertyName.Length > 1)
                {
                    // Store the search operation as a lowercase variant
                    searchOperation = splittedPropertyName[0].ToLower();

                    // Begin a switch statement on the potential search operation
                    switch (searchOperation)
                    {
                        // Check if the search operation is an includes operation
                        case "includes":
                            // Remove the search operation from the property name
                            currentPropertyName = splittedPropertyName[splittedPropertyName.Length - 1];

                            // Do nothing, as the remaining steps of the operation will now be passed after the currentProperty != null check
                            break;

                        // Check if the search operation is a type check with subclasses operation
                        case "type_subclasses":
                            // Store a splitted variation of the splitted property name's second element to gain a potential array of types
                            string[] types = splittedPropertyName[splittedPropertyName.Length - 1].Split(',');

                            // Create a bool that indicates the success of this operation
                            bool isSuccess = false;

                            // Begin an iteration over the types
                            for (int i = 0; i < types.Length; i++)
                            {
                                // Check if the input object's type is a subclass of the selected type or the same type
                                if (TypeLibrary.IsSubClassOf(refObject, types[i]))
                                {
                                    // Set the is success boolean to true
                                    isSuccess = true;

                                    // Break the iteration if it is
                                    break;
                                }
                            }

                            // Return a result based on the success of the iteration
                            return isSuccess ? refObject : null;
                    }
                }

                // Create a property info variable for storing the secondary iteration's current property
                PropertyInfo currentProperty = objectType.GetProperty(currentPropertyName);

                // Create an object for storing the current property's value
                object propertyValue;

                // Remove the first element from the properties list to allow for it to be passed to recursive operations if one should be removed
                targetProperties.RemoveAt(0);

                // Check if the current property's info isn't null
                if (currentProperty != null)
                {
                    // Store the value of the current property
                    propertyValue = currentProperty.GetValue(refObject);

                    // Check if the type of search method is an includes operation
                    if (searchOperation == "includes")
                    {
                        // The property has already had a null check, so the operation is already known to be successful

                        // Set the search result to a recursive search on the input object
                        searchResult = refObject.Search(targetProperties);

                        // Return the result of a recursive operation on the input object
                        return searchResult;
                    }

                    // Check if the value is of a list
                    if (propertyValue is IList)
                    {
                        // Create a list for storing the potential of multiple results
                        List<object> enumerationResults = new List<object>();
                        // Cast the property's value to an IEnumerable
                        IEnumerable propertyEnumerable = (IEnumerable)propertyValue;
                        // Get the enumerator
                        IEnumerator propertyEnumerator = propertyEnumerable.GetEnumerator();
                        // Create an object for storing the enumerator's current selection
                        object enumeratorSelection;
                        // Create a list for storing a copy of the target properties
                        List<string> subTargetProperties;

                        // Create an object for storing the sub-recursive search results
                        object secondarySearchResult;
                        // Get a bool representing whether or not pointerrefs should be resolved by checking if the amount of properties is greater than or equal to 2
                        bool resolveRefs = targetProperties.Count >= 2;

                        // Begin the enumeration
                        while (propertyEnumerator.MoveNext())
                        {
                            // Set the enumerator's current selection
                            enumeratorSelection = propertyEnumerator.Current;
                            // Set the sub target properties to the target properties
                            subTargetProperties = new List<string>(targetProperties);

                            // Check if the selection is a pointerref and if a property will be searched for in the resolved object
                            if (enumeratorSelection is PointerRef && targetProperties.Count >= 1)
                            {
                                // Set the selection to the resolved value
                                enumeratorSelection = ((PointerRef)enumeratorSelection).Resolve();
                            }

                            // Get the enumerator's current selection, perform a recursive search over it, and set the secondary search results to its results
                            secondarySearchResult = enumeratorSelection.Search(subTargetProperties, true, resolveRefs);

                            // Check if this search result isn't null
                            if (secondarySearchResult != null)
                            {
                                // Add it to the enumeration results
                                enumerationResults.Add(secondarySearchResult);
                            }
                        }

                        // Clear the target properties
                        targetProperties.Clear();

                        // Set the search result to the enumeration results
                        searchResult = enumerationResults;
                    }
                    else
                    {
                        // Check if the property's value is a pointerref
                        if (propertyValue is PointerRef && shouldResolveRefs)
                        {
                            // Set the search result to the result of a recursive operation on the resolved value
                            searchResult = ((PointerRef)propertyValue).Resolve().Search(targetProperties);
                        }
                        else
                        {
                            // Pass the property's value and the property list of this operation to a secondary operation and set the result
                            searchResult = propertyValue.Search(targetProperties);
                        }
                    }
                }
                else
                {
                    // Set the search result to the input object if the property was null
                    searchResult = returnNullIfNoResults ? null : refObject;
                }
            }
            else
            {
                // Set the search result to the input object if anything was null or an empty list
                searchResult = returnNullIfNoResults ? null : refObject;
            }

            // Return the search result of this operation
            return searchResult;
        }

        /// <summary>
        /// Splits a string by a given character only once.
        /// </summary>
        /// <param name="splitChar">The character to split the string by.</param>
        /// <returns>The splitted string.</returns>
        public static string[] SplitOnce(this string refString, char splitChar)
        {
            int charIndex = refString.IndexOf(splitChar);

            if (charIndex == -1)
            {
                // Return an array that simply consists of the given string as a means of indicating a failed operation
                return new string[] { refString };
            }

            return new string[]
            {
                refString.Remove(charIndex),
                refString.Remove(0, charIndex + 1)
            };
        }
    }
}

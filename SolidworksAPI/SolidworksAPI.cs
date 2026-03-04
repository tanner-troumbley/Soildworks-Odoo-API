// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using SolidWorks.Interop.sldworks;
// using SolidWorks.Interop.swconst;

// namespace SwNestedBOMExtractor
// {
//     // Represents a single property
//     public class CustomProperty
//     {
//         public string Name { get; set; }
//         public string Value { get; set; }
//         public string RawValue { get; set; }
//         public string Scope { get; set; } // "General" or configuration name
//     }

//     // Represents a single part in the BOM
//     public class PartEntry
//     {
//         public string FileName { get; set; }
//         public string FilePath { get; set; }
//         public int Quantity { get; set; }
//         public List<CustomProperty> Properties { get; set; } = new List<CustomProperty>();
//     }

//     // Represents the BOM for an assembly (can contain sub-assemblies)
//     public class AssemblyBOM : PartEntry
//     {
//         public List<PartEntry> Parts { get; set; } = new List<PartEntry>();
//         public List<AssemblyBOM> SubAssemblies { get; set; } = new List<AssemblyBOM>();
//     }

//     class Program
//     {
//         static void Main(string[] args)
//         {
//             try
//             {
//                 // Connect to SOLIDWORKS
//                 var swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
//                 if (swApp == null)
//                 {
//                     Console.WriteLine("Unable to connect to SOLIDWORKS.");
//                     return;
//                 }

//                 swApp.Visible = true;

//                 // Get active document
//                 var swModel = swApp.ActiveDoc as ModelDoc2;
//                 if (swModel == null)
//                 {
//                     Console.WriteLine("No active document found.");
//                     return;
//                 }
                
//                 // Build BOM recursively
//                 AssemblyBOM rootBOM = ProcessAssembly(swModel, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

//                 // Export to JSON
//                 string json = JsonSerializer.Serialize(rootBOM, new JsonSerializerOptions
//                 {
//                     WriteIndented = true
//                 });

//                 string outputPath = $"C:/Users/tanner.troumbley/PycharmProjects/Solidworks API/{Path.GetFileNameWithoutExtension(swModel.GetTitle())}.json";
//                 File.WriteAllText(outputPath, json);
//                 Console.WriteLine($"BOM exported to: {outputPath}");    
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Error: " + ex.Message);
//             }
//         }

//         /// <summary>
//         /// Recursively processes an assembly and returns its BOM object.
//         /// </summary>
//         static AssemblyBOM ProcessAssembly(ModelDoc2 swModel, HashSet<string> processedParts)
//         {
//             var swAssembly = (AssemblyDoc)swModel;
//             var bom = new AssemblyBOM
//             {
//                 FileName = Path.GetFileNameWithoutExtension(swModel.GetTitle()),
//                 FilePath = swModel.GetPathName()
//             };

//             // General properties
//             var bomgeneralProps = swModel.Extension.get_CustomPropertyManager("");
//             bom.Properties.AddRange(GetProperties(bomgeneralProps, "General"));

//             // Configuration-specific properties
//             string[] bomconfigNames = (string[])swModel.GetConfigurationNames();
//             if (bomconfigNames != null)
//             {
//                 foreach (string configName in bomconfigNames)
//                 {
//                     var configProps = swModel.Extension.get_CustomPropertyManager(configName);
//                     bom.Properties.AddRange(GetProperties(configProps, configName));
//                 }
//             }
//             object[] components = (object[])swAssembly.GetComponents(true);
//             if (components == null) return bom;

//             foreach (Component2 comp in components)
//             {
//                 if (comp == null) continue;

//                 var compModel = comp.GetModelDoc2() as ModelDoc2;
//                 if (compModel == null)
//                 {
//                     var partEntry = new PartEntry
//                     {
//                         FileName = comp.Name2
//                     };
//                     bom.Parts.Add(partEntry);
//                     continue;
//                 }

//                 int docType = compModel.GetType();

//                 if (docType == (int)swDocumentTypes_e.swDocPART)
//                 {
//                     string partPath = compModel.GetPathName();
//                     if (string.IsNullOrEmpty(partPath))
//                     {
//                         // Console.WriteLine($"Part path is null for {compModel} for swDocumentTypes_e.swDocPART");
//                         continue;
//                     }

//                     if (processedParts.Contains(partPath)) continue;
//                     processedParts.Add(partPath);

//                     var partEntry = new PartEntry
//                     {
//                         FileName = Path.GetFileNameWithoutExtension(partPath),
//                         FilePath = partPath
//                     };

//                     // General properties
//                     var generalProps = compModel.Extension.get_CustomPropertyManager("");
//                     partEntry.Properties.AddRange(GetProperties(generalProps, "General"));

//                     // Configuration-specific properties
//                     string[] configNames = (string[])compModel.GetConfigurationNames();
//                     if (configNames != null)
//                     {
//                         foreach (string configName in configNames)
//                         {
//                             var configProps = compModel.Extension.get_CustomPropertyManager(configName);
//                             partEntry.Properties.AddRange(GetProperties(configProps, configName));
//                         }
//                     }
//                     // else
//                     // {
//                     //     Console.WriteLine($"Configname is null for {compModel}");
//                     // }

//                     bom.Parts.Add(partEntry);
//                 }
//                 else if (docType == (int)swDocumentTypes_e.swDocASSEMBLY)
//                 {
//                     // Recursively process sub-assembly
//                     var subBOM = ProcessAssembly(compModel, processedParts);
//                     bom.SubAssemblies.Add(subBOM);
//                 }
//             }

//             return bom;
//         }

//         /// <summary>
//         /// Extracts properties from a CustomPropertyManager.
//         /// </summary>
//         static List<CustomProperty> GetProperties(CustomPropertyManager propMgr, string scope)
//         {
//             var props = new List<CustomProperty>();
//             string[] propNames = (string[])propMgr.GetNames();
//             if (propNames == null) return props;

//             foreach (string propName in propNames)
//             {
//                 string valOut;
//                 string resolvedVal;
//                 bool wasResolved;
//                 bool linkToProp;

//                 propMgr.Get6(propName, false, out valOut, out resolvedVal, out wasResolved, out linkToProp);
//                 if (valOut != resolvedVal)
//                 {
//                     Console.WriteLine($"valOut: {valOut}, reslovedVal: {resolvedVal}");
//                 }
//                 props.Add(new CustomProperty
//                 {
//                     Name = propName,
//                     Value = resolvedVal,
//                     RawValue = valOut,
//                     Scope = scope
//                 });
//             }

//             return props;
//         }
//     }
// }

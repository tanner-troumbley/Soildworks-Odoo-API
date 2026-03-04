// using System;

// using System.Collections.Generic;

// using System.IO;

// using Newtonsoft.Json; // Install via NuGet: Newtonsoft.Json

// using SolidWorks.Interop.sldworks;

// using SolidWorks.Interop.swconst;
 
// namespace SWMultiLevelBOMExport

// {

//     public class BomNode

//     {

//         public string Name { get; set; }

//         public string Path { get; set; }

//         public int Quantity { get; set; }

//         public bool IsAssembly { get; set; }

//         public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

//         public List<BomNode> Children { get; set; } = new List<BomNode>();

//     }
 
//     class Program

//     {

//         static SldWorks swApp;
 
//         static void Main(string[] args)

//         {

//             try

//             {

//                 swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;

//                 if (swApp == null)

//                 {

//                     Console.WriteLine("Unable to connect to SOLIDWORKS.");

//                     return;

//                 }
 
//                 ModelDoc2 swModel = swApp.ActiveDoc;

//                 if (swModel == null || swModel.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)

//                 {

//                     Console.WriteLine("Please open an assembly document.");

//                     return;

//                 }
 
//                 AssemblyDoc swAssembly = (AssemblyDoc)swModel;

//                 Component2 rootComp = swAssembly.GetComponents(false)[0];
 
//                 BomNode rootNode = BuildBomTree(rootComp, 1);
 
//                 string outputPath = Path.Combine(

//                     Environment.GetFolderPath(Environment.SpecialFolder.Desktop),

//                     "BOM_MultiLevel_WithProps.json"

//                 );
 
//                 File.WriteAllText(outputPath, JsonConvert.SerializeObject(rootNode, Formatting.Indented));
 
//                 Console.WriteLine($"Multi-level BOM with properties exported to:\n{outputPath}");

//             }

//             catch (Exception ex)

//             {

//                 Console.WriteLine("Error: " + ex.Message);

//             }

//         }
 
//         static BomNode BuildBomTree(Component2 comp, int quantity)

//         {

//             if (comp == null || comp.IsSuppressed()) return null;
 
//             string compPath = comp.GetPathName();

//             if (string.IsNullOrEmpty(compPath)) return null;
 
//             bool isAssembly = comp.GetModelDoc2() is AssemblyDoc;
 
//             BomNode node = new BomNode

//             {

//                 Name = Path.GetFileName(compPath),

//                 Path = compPath,

//                 Quantity = quantity,

//                 IsAssembly = isAssembly

//             };
 
//             ExtractCustomProperties(comp, node.Properties);
 
//             if (isAssembly)

//             {

//                 Dictionary<string, (Component2 comp, int qty)> childMap =

//                     new Dictionary<string, (Component2, int)>(StringComparer.OrdinalIgnoreCase);
 
//                 object[] children = comp.GetChildren();

//                 if (children != null)

//                 {

//                     foreach (Component2 child in children)

//                     {

//                         if (child == null || child.IsSuppressed()) continue;
 
//                         string childPath = child.GetPathName();

//                         if (string.IsNullOrEmpty(childPath)) continue;
 
//                         if (!childMap.ContainsKey(childPath))

//                             childMap[childPath] = (child, 0);
 
//                         childMap[childPath] = (child, childMap[childPath].qty + 1);

//                     }
 
//                     foreach (var kvp in childMap)

//                     {

//                         BomNode childNode = BuildBomTree(kvp.Value.comp, kvp.Value.qty);

//                         if (childNode != null)

//                             node.Children.Add(childNode);

//                     }

//                 }

//             }
 
//             return node;

//         }
 
//         static void ExtractCustomProperties(Component2 comp, Dictionary<string, string> props)

//         {

//             try

//             {

//                 ModelDoc2 model = (ModelDoc2)comp.GetModelDoc2();

//                 if (model == null) return;
 
//                 CustomPropertyManager propMgr = model.Extension.CustomPropertyManager[""];
 
//                 string[] keys = { "Part Number", "Description", "Material" };
 
//                 foreach (string key in keys)

//                 {

//                     string valOut;

//                     string resolvedVal;

//                     int res = propMgr.Get4(key, false, out valOut, out resolvedVal);

//                     if (res == (int)swCustomInfoGetResult_e.swCustomInfoGetResult_ResolvedValue)

//                         props[key] = resolvedVal;

//                     else if (!string.IsNullOrEmpty(valOut))

//                         props[key] = valOut;

//                     else

//                         props[key] = "";

//                 }

//             }

//             catch

//             {

//                 // Ignore property extraction errors

//             }

//         }

//     }

// }

 
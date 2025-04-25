Console.WriteLine("Write dependency version start");

Directory.SetCurrentDirectory("../"); // move to repository root
const string DEPENDENCY_PACKAGE_DOT_JSON = "../TexTransTool/package.json";
var thisPackageJsonPath = @"package.json";


var dependencyPackageJson = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(DEPENDENCY_PACKAGE_DOT_JSON));
if (dependencyPackageJson is null) { throw new NullReferenceException(); }
var dependencyVersion = dependencyPackageJson["version"]!.GetValue<string>();
var dependencyCode = dependencyPackageJson["name"]!.GetValue<string>();


var thisPackageJson = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(thisPackageJsonPath));
if (thisPackageJson is null) { throw new NullReferenceException(); }

thisPackageJson["dependencies"]![dependencyCode] = dependencyVersion;
thisPackageJson["vpmDependencies"]![dependencyCode] = "^" + dependencyVersion;

var outOpt = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General) { WriteIndented = true };
File.WriteAllText(thisPackageJsonPath, thisPackageJson.ToJsonString(outOpt) + "\n");
Console.WriteLine("Write version exit!");

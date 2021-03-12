using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Reactor.BepInAutoPlugin
{
    public class ReplaceBepInPlugin : Task
    {
        [Required]
        public string Input { get; set; }

        [Required]
        public string[] ReferencedAssemblies { get; set; }

        public override bool Execute()
        {
            var resolver = new DefaultAssemblyResolver();

            var referencedAssemblies = ReferencedAssemblies.Select(AssemblyDefinition.ReadAssembly).ToArray();

            resolver.ResolveFailure += (_, reference) =>
            {
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (referencedAssembly.Name.Name == reference.Name)
                    {
                        return referencedAssembly;
                    }
                }

                return null;
            };

            using var moduleDefinition = ModuleDefinition.ReadModule(Input, new ReaderParameters { AssemblyResolver = resolver, ReadWrite = true });

            var nameAttribute = moduleDefinition.Assembly.CustomAttributes.Single(x => x.AttributeType.Name == nameof(AssemblyTitleAttribute));
            var name = (string) nameAttribute.ConstructorArguments.Single().Value;

            var versionAttribute = moduleDefinition.Assembly.CustomAttributes.Single(x => x.AttributeType.Name == nameof(AssemblyInformationalVersionAttribute));
            var version = (string) versionAttribute.ConstructorArguments.Single().Value;

            var bepInEx = resolver.Resolve(
                moduleDefinition.AssemblyReferences.SingleOrDefault(x => x.Name == (x.Version.Major >= 6 ? "BepInEx.Core" : "BepInEx"))
            );
            var bepInPlugin = bepInEx.MainModule.GetType("BepInEx.BepInPlugin");

            var stringReference = moduleDefinition.ImportReference(typeof(string));

            foreach (var typeDefinition in moduleDefinition.Types)
            {
                var attribute = typeDefinition.CustomAttributes.SingleOrDefault(x => x.AttributeType.FullName == "BepInEx.BepInAutoPluginAttribute");

                if (attribute != null)
                {
                    var guid = (string) attribute.ConstructorArguments.Single().Value;

                    typeDefinition.CustomAttributes.Remove(attribute);

                    typeDefinition.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(bepInPlugin.GetConstructors().Single()))
                    {
                        ConstructorArguments =
                        {
                            new CustomAttributeArgument(stringReference, guid),
                            new CustomAttributeArgument(stringReference, name),
                            new CustomAttributeArgument(stringReference, version),
                        }
                    });
                }
            }

            moduleDefinition.Write();

            return true;
        }
    }
}

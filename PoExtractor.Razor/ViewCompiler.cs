using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace PoExtractor.Razor
{
    public static class ViewCompiler
    {
        public static IList<RazorPageGeneratorResult> CompileViews(string projectDirectory)
        {
            var projectEngine = CreateProjectEngine("PoExtractor.GeneratedCode", projectDirectory);

            var results = new List<RazorPageGeneratorResult>();

            var allDirectories = Directory.EnumerateDirectories(projectDirectory, "*.*", SearchOption.AllDirectories)
                .Distinct()
                .OrderBy(dirName => dirName);
            foreach (var dir in allDirectories)
            {
                var dirPath = dir.Substring(projectDirectory.Length).Replace('\\', '/');
                var razorFiles = projectEngine.FileSystem.EnumerateItems(dirPath).OrderBy(rzrProjItem => rzrProjItem.FileName);

                foreach (var item in razorFiles.Where(o => o.Extension == ".cshtml" || o.Extension == ".razor"))
                {
                    results.Add(GenerateCodeFile(projectEngine, item));
                }
            }

            return results;
        }

        public static RazorProjectEngine CreateProjectEngine(string rootNamespace, string projectDirectory)
        {
            var fileSystem = RazorProjectFileSystem.Create(projectDirectory);
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder =>
            {

                builder
                    .SetNamespace(rootNamespace)
                    .ConfigureClass((document, @class) => {
                        @class.ClassName = Path.GetFileNameWithoutExtension(document.Source.FilePath);
                    });
#if NETSTANDARD2_0
                FunctionsDirective.Register(builder);
                InheritsDirective.Register(builder);
                SectionDirective.Register(builder);
#endif
            });

            return projectEngine;
        }

        public static RazorPageGeneratorResult GenerateCodeFile(RazorProjectEngine projectEngine, RazorProjectItem projectItem)
        {
            var codeDocument = projectEngine.Process(projectItem);
            var cSharpDocument = codeDocument.GetCSharpDocument();

            return new RazorPageGeneratorResult
            {
                FilePath = projectItem.PhysicalPath,
                GeneratedCode = cSharpDocument.GeneratedCode,
            };
        }
    }
}

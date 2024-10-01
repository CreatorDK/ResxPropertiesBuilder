using Microsoft.VisualStudio.Shell;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;

namespace ResxPropertiesBuilder
{
    extern alias VSInterop;
    class CodeGeneratorWPF : CodeGenerator
    {
        public new const string Name = "ResxPropertiesBuilderWPF";
        public new const string Description = "Create Custom .Designer file, wrapper .WPF file for XAML, ResourceDictionary if they not exist and .Properties file with Properties from .resx file. Also Create wrapper class for access to code from XAML";

        public override string GetDefaultExtension()
        {
            return base.GetDefaultExtension();
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            if (string.IsNullOrEmpty(inputFileContent) || CodeGenerator.IsLocalizedFile(inputFileName))
            {
                return null;
            }

            string fileNamespace = base.FileNamespace;

            string resourceNameSpace = GetResourcesNamespace();

            string baseName = Path.GetFileNameWithoutExtension(inputFileName);

            string designerFileName = GetDesignerFileName(inputFileName);

            string wpfFileName = GetWPFFileName(inputFileName);

            string resourceDictionaryFileName = GetResourceDictionaryFileName(inputFileName);

            if (!File.Exists(designerFileName))
            {

                CodeCompileUnit designerCodeCompileUnit;

                try
                {
                    designerCodeCompileUnit = CreateDesignerCodeCompileUnit(fileNamespace, resourceNameSpace, baseName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    SaveDesignerFile(designerCodeCompileUnit, designerFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    AttachFileToProjectItem(designerFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
            }
            else
            {
                AttachFileToProjectItem(designerFileName);
            }

            if (!File.Exists(wpfFileName))
            {

                CodeCompileUnit wpfCodeCompileUnit;

                try
                {
                    wpfCodeCompileUnit = CreateWPFCodeCompileUnit(fileNamespace, baseName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    SaveWPFFile(wpfCodeCompileUnit, wpfFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    AttachFileToProjectItem(wpfFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
            }
            else
            {
                AttachFileToProjectItem(wpfFileName);
            }

            if (!File.Exists(resourceDictionaryFileName))
            {

                string resourceDictionaryContent;

                try
                {
                    resourceDictionaryContent = CreateResourceDictionaryContent(fileNamespace, baseName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    SaveResourceDictionary(resourceDictionaryContent, resourceDictionaryFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
                try
                {
                    AttachFileToProjectItem(resourceDictionaryFileName);
                }
                catch (Exception ex)
                {
                    GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                    return null;
                }
            }
            else
            {
                AttachFileToProjectItem(resourceDictionaryFileName);
            }

            CodeCompileUnit propertiesCodeCompileUnit;

            try
            {
                propertiesCodeCompileUnit = CreatePropertiesCodeCompilerUnit(fileNamespace, baseName, inputFileName, inputFileContent);
            }
            catch (Exception ex)
            {
                GeneratorErrorCallback(true, 5, ex.Message, 1, 1);
                return null;
            }

            if (propertiesCodeCompileUnit == null)
                return null;

            StreamWriter streamWriter = new StreamWriter((Stream)new MemoryStream(), Encoding.UTF8);
            CodeProvider.GenerateCodeFromCompileUnit(propertiesCodeCompileUnit, (TextWriter)streamWriter, (CodeGeneratorOptions)null);
            streamWriter.Flush();
            return StreamToBytes(streamWriter.BaseStream);
        }
        private protected string GetWPFFileName(string inputFile)
        {
            string fileBaseName = Path.GetDirectoryName(inputFile) + @"\" + Path.GetFileNameWithoutExtension(inputFile);

            string templateFileName = fileBaseName + ".WPF." + CodeProvider.FileExtension;

            return templateFileName;
        }
        private protected CodeCompileUnit CreateWPFCodeCompileUnit(string nameSpace, string baseName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ResxWPFBuilder.CreateDesignerCodeCompiledUnit(baseName, nameSpace, CodeProvider, DesignerClassDeclaration);
        }
        private protected void SaveWPFFile(CodeCompileUnit wpfCodeCompileUnit, string wpfFileName)
        {
            using (var fileStream = new FileStream(wpfFileName, FileMode.OpenOrCreate))
            {
                StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                CodeProvider.GenerateCodeFromCompileUnit(wpfCodeCompileUnit, (TextWriter)streamWriter, (CodeGeneratorOptions)null);
                streamWriter.Flush();
            }
        }
        private protected string GetResourceDictionaryFileName(string inputFile)
        {
            string fileBaseName = Path.GetDirectoryName(inputFile) + @"\" + Path.GetFileNameWithoutExtension(inputFile);

            string templateFileName = fileBaseName + "ResourceDictionary.xaml";

            return templateFileName;
        }
        private protected string CreateResourceDictionaryContent(string nameSpace, string baseName)
        {
            return ResxDictionaryBuilder.CreateDictionaryContent(baseName, nameSpace);
        }
        private protected void SaveResourceDictionary(string content, string fileName)
        {
            File.WriteAllText(fileName, content);
        }
    }
}

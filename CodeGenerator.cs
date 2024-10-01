using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace ResxPropertiesBuilder
{
    extern alias VSInterop;
    class CodeGenerator : BaseCodeGeneratorWithSite
    {
        public const string Name = "ResxPropertiesBuilder";
        public const string Description = "Create Custom .Designer file if not exist and .Properties file with Properties from .resx file";

        private protected System.CodeDom.Compiler.CodeDomProvider codeDomProvider;
        private protected DesignerClassDeclaration DesignerClassDeclaration
        {
            get
            {
                return GetDefaultDesignerDeclaration();
            }
        }
        private protected System.CodeDom.Compiler.CodeDomProvider CodeProvider
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                if (this.codeDomProvider == null)
                {
                    IVSMDCodeDomProvider provider = SiteServiceProvider.GetService(typeof(SVSMDCodeDomProvider)) as IVSMDCodeDomProvider;
                    if (provider != null)
                    {
                        this.codeDomProvider = provider.CodeDomProvider as System.CodeDom.Compiler.CodeDomProvider;
                    }
                }
                return this.codeDomProvider;
            }
            set => this.codeDomProvider = value != null ? value : throw new ArgumentNullException();
        }
        public override string GetDefaultExtension()
        {
             return ".Properties." + CodeProvider.FileExtension;
        }
        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            if (string.IsNullOrEmpty(inputFileContent) || CodeGenerator.IsLocalizedFile(inputFileName))
            {
                return null;
            }

            string fileNamespace = base.FileNamespace;

            string resourcesNameSpace = GetResourcesNamespace();

            string baseName = Path.GetFileNameWithoutExtension(inputFileName);

            string designerFileName = GetDesignerFileName(inputFileName);

            if (!File.Exists(designerFileName))
            {

                CodeCompileUnit designerCodeCompileUnit;

                try
                {
                    designerCodeCompileUnit = CreateDesignerCodeCompileUnit(fileNamespace, resourcesNameSpace, baseName);
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

            CodeCompileUnit propertiesCodeCompileUnit;

            try
            {
                propertiesCodeCompileUnit = CreatePropertiesCodeCompilerUnit(fileNamespace, baseName, inputFileName, inputFileContent);
            }
            catch(Exception ex)
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
        private protected CodeCompileUnit CreatePropertiesCodeCompilerUnit(string nameSpace, string baseName, string inputFileName, string inputFileContent)
        {
            IDictionary resourceList = (IDictionary)new Hashtable((IEqualityComparer)StringComparer.OrdinalIgnoreCase);
            IDictionary dictionary = (IDictionary)new Hashtable((IEqualityComparer)StringComparer.OrdinalIgnoreCase);

            using (IResourceReader resourceReader = (IResourceReader)ResXResourceReader.FromFileContents(inputFileContent))
            {
                if (resourceReader is ResXResourceReader resXresourceReader2)
                {
                    resXresourceReader2.UseResXDataNodes = true;
                    string directoryName = Path.GetDirectoryName(inputFileName);
                    resXresourceReader2.BasePath = Path.GetFullPath(directoryName);
                }
                foreach (DictionaryEntry dictionaryEntry in resourceReader)
                {
                    ResXDataNode resXdataNode = (ResXDataNode)dictionaryEntry.Value;
                    resourceList.Add(dictionaryEntry.Key, (object)resXdataNode);
                    dictionary.Add(dictionaryEntry.Key, (object)resXdataNode.GetNodePosition());
                }
            }

            string[] unmatchable;
            DesignerClassDeclaration declaration = GetDefaultDesignerDeclaration();

            CodeCompileUnit propertiesCodeCompileUnit = ResxPropertiesBuilder.CreatePropertiesCodeCompiledUnit(baseName, nameSpace, resourceList, CodeProvider, declaration, out unmatchable);

            foreach (string str in unmatchable)
            {
                Point point = (Point)dictionary[(object)str];
                GeneratorErrorCallback(true, 1, RM.GetString("CannotCreatePropertyForResource", (object)str), point.Y, point.X);
            }

            return propertiesCodeCompileUnit;
        }
        private protected static bool IsLocalizedFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            {
                string str = fileName.Substring(0, fileName.Length - 5);
                if (!string.IsNullOrEmpty(str))
                {
                    int num = str.LastIndexOf('.');
                    if (num > 0)
                    {
                        string name = str.Substring(num + 1);
                        if (!string.IsNullOrEmpty(name))
                        {
                            try
                            {
                                if (new CultureInfo(name) != null)
                                    return true;
                            }
#pragma warning disable CS0168 // Переменная объявлена, но не используется
                            catch (ArgumentException ex)
#pragma warning restore CS0168 // Переменная объявлена, но не используется
                            {
                            }
                        }
                    }
                }
            }
            return false;
        }
        private protected string GetDesignerFileName(string inputFile)
        {
            string fileBaseName = Path.GetDirectoryName(inputFile) + @"\" + Path.GetFileNameWithoutExtension(inputFile);

            string templateFileName = fileBaseName + ".Designer." + CodeProvider.FileExtension;

            return templateFileName;
        }
        private protected static DesignerClassDeclaration GetDefaultDesignerDeclaration()
        {
            return new DesignerClassDeclaration()
            {
                ResMgrFieldName = "resourceMan",
                ResMgrPropertyName = "ResourceManager",
                CultureInfoFieldName = "resourceCulture",
                CultureInfoPropertyName = "Culture",
                ClassModifier = TypeAttributes.Public
            };
        }
        protected byte[] StreamToBytes(Stream stream)
        {
            if (stream.Length == 0L)
                return new byte[0];
            long position = stream.Position;
            stream.Position = 0L;
            byte[] buffer = new byte[(int)stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            stream.Position = position;
            return buffer;
        }
        private protected VSInterop.EnvDTE.ProjectItem GetProjectItem()
        {
            VSInterop.EnvDTE.Project project;
            ThreadHelper.ThrowIfNotOnUIThread();
            VSInterop.EnvDTE.DTE dte = (VSInterop.EnvDTE.DTE)Package.GetGlobalService(typeof(VSInterop.EnvDTE.DTE));
            Array ary = (Array)dte.ActiveSolutionProjects;
            if (ary.Length > 0)
            {
                project = (VSInterop.EnvDTE.Project)ary.GetValue(0);
            }
            else
            {
                return null;
            }

            Microsoft.VisualStudio.Shell.Interop.IVsProject VsProject = VsHelper.ToVsProject(project);
            VSInterop.EnvDTE.ProjectItem item;
            VSDOCUMENTPRIORITY[] pdwPriority = new VSDOCUMENTPRIORITY[1];


            int iFound;
            uint itemId;
            VsProject.IsDocumentInProject(InputFilePath, out iFound, pdwPriority, out itemId);

            if (iFound != 0 && itemId != 0)
            {
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleSp;
                VsProject.GetItemContext(itemId, out oleSp);
                if (oleSp != null)
                {
                    ServiceProvider sp = new ServiceProvider(oleSp);
                    // convert our handle to a ProjectItem
                    item = sp.GetService(typeof(VSInterop.EnvDTE.ProjectItem))
                                as VSInterop.EnvDTE.ProjectItem;
                }
                else
                {
                    throw new ApplicationException ("Unable to retrieve Visual Studio ProjectItem");
                }

            }
            else
            {
                throw new ApplicationException("Unable to retrieve Visual Studio ProjectItem");
            }

            return item;
        }
        private protected CodeCompileUnit CreateDesignerCodeCompileUnit(string fileNamespace, string resourceNamespace, string baseName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ResxDesignerBuilder.CreateDesignerCodeCompiledUnit(baseName, fileNamespace, resourceNamespace, CodeProvider, DesignerClassDeclaration);
        }
        private protected void SaveDesignerFile(CodeCompileUnit designerCodeCompileUnit, string designerFileName)
        {
            using (var fileStream = new FileStream(designerFileName, FileMode.OpenOrCreate))
            {
                StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                CodeProvider.GenerateCodeFromCompileUnit(designerCodeCompileUnit, (TextWriter)streamWriter, (CodeGeneratorOptions)null);
                streamWriter.Flush();
            }
        }
        private protected void AttachFileToProjectItem(string designerFileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VSInterop.EnvDTE.ProjectItem ResxItem = GetProjectItem();
            ResxItem.ProjectItems.AddFromFile(designerFileName);
        }
        protected string GetResourcesNamespace()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string str1 = (string)null;
            try
            {
                Guid guid = typeof(IVsBrowseObject).GUID;
                IntPtr ppvSite;
                this.GetSite(ref guid, out ppvSite);
                if (ppvSite != IntPtr.Zero)
                {
                    IVsBrowseObject objectForIunknown = Marshal.GetObjectForIUnknown(ppvSite) as IVsBrowseObject;
                    Marshal.Release(ppvSite);
                    if (objectForIunknown != null)
                    {
                        IVsHierarchy pHier;
                        uint pItemid;
                        objectForIunknown.GetProjectItem(out pHier, out pItemid);
                        if (pHier != null)
                        {
                            object pvar;
                            pHier.GetProperty(pItemid, -2049, out pvar);
                            if (pvar is string str10)
                                str1 = str10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ProjectUtilities.IsCriticalException(ex))
                    throw;
            }
            return str1;
        }
    }
}

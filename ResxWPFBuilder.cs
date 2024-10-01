using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources.Tools;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Helper = ResxPropertiesBuilder.CodeCompileHelper;

namespace ResxPropertiesBuilder
{
    public static class ResxWPFBuilder
    {
        public static CodeCompileUnit CreateDesignerCodeCompiledUnit
            (string baseName,
            string generatedCodeNamespace,
            CodeDomProvider codeProvider,
            DesignerClassDeclaration declaration)
        {
            if (baseName == null)
                throw new ArgumentNullException(nameof(baseName));
            if (codeProvider == null)
                throw new ArgumentNullException(nameof(codeProvider));

            string str1 = "WPF" + baseName;

            if (!codeProvider.IsValidIdentifier(str1))
            {
                string str2 = Helper.VerifyResourceName(str1, codeProvider);
                if (str2 != null)
                    str1 = str2;
            }
            if (!codeProvider.IsValidIdentifier(str1))
                throw new ArgumentException(RM.GetString("InvalidIdentifier", (object)str1));
            if (!string.IsNullOrEmpty(generatedCodeNamespace) && !codeProvider.IsValidIdentifier(generatedCodeNamespace))
            {
                string str2 = Helper.VerifyResourceName(generatedCodeNamespace, codeProvider, true);
                if (str2 != null)
                    generatedCodeNamespace = str2;
            }

            bool internalClass = declaration.ClassModifier == TypeAttributes.NotPublic;
            bool useStatic = declaration.ClassModifier == TypeAttributes.NotPublic || codeProvider.Supports(GeneratorSupport.PublicStaticMembers);

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.ReferencedAssemblies.Add("System.dll");
            codeCompileUnit.UserData.Add((object)"AllowLateBound", (object)false);
            codeCompileUnit.UserData.Add((object)"RequireVariableDeclaration", (object)true);
            CodeNamespace codeNamespace = new CodeNamespace(generatedCodeNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Text"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Globalization"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Windows.Data"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
            codeCompileUnit.Namespaces.Add(codeNamespace);
            CodeTypeDeclaration srClass = new CodeTypeDeclaration(str1);
            codeNamespace.Types.Add(srClass);
            AddGeneratedCodeAttributeforMember((CodeTypeMember)srClass);
            srClass.TypeAttributes = declaration.ClassModifier;

            srClass.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            srClass.Comments.Add(new CodeCommentStatement(RM.GetString("ClassWPFComment"), true));
            srClass.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            srClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DebuggerNonUserCodeAttribute))
            {
                Options = CodeTypeReferenceOptions.GlobalReference
            }));
            srClass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(CompilerGeneratedAttribute))
            {
                Options = CodeTypeReferenceOptions.GlobalReference
            }));

            bool supportsTryCatch = codeProvider.Supports(GeneratorSupport.TryCatchStatements);
            bool useTypeInfo = codeProvider is ITargetAwareCodeDomProvider awareCodeDomProvider && !awareCodeDomProvider.SupportsProperty(typeof(Type), "Assembly", false);
            if (useTypeInfo)
                codeNamespace.Imports.Add(new CodeNamespaceImport("System.Reflection"));
            EmitBasicClassMembers(declaration, srClass, generatedCodeNamespace, baseName, null, internalClass, useStatic, supportsTryCatch, useTypeInfo);
            System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers((CodeObject)codeCompileUnit);
            return codeCompileUnit;
        }

        private static void EmitBasicClassMembers(
          DesignerClassDeclaration declaration,
          CodeTypeDeclaration srClass,
          string nameSpace,
          string baseName,
          string resourcesNamespace,
          bool internalClass,
          bool useStatic,
          bool supportsTryCatch,
          bool useTypeInfo)
        {
            string str = resourcesNamespace == null ? (nameSpace == null || nameSpace.Length <= 0 ? baseName : nameSpace + "." + baseName) : (resourcesNamespace.Length <= 0 ? baseName : resourcesNamespace + "." + baseName);

            CodeAttributeDeclaration attributeDeclaration1 = new CodeAttributeDeclaration(new CodeTypeReference(typeof(SuppressMessageAttribute)));
            attributeDeclaration1.AttributeType.Options = CodeTypeReferenceOptions.GlobalReference;
            attributeDeclaration1.Arguments.Add(new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"Microsoft.Performance")));
            attributeDeclaration1.Arguments.Add(new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"CA1811:AvoidUncalledPrivateCode")));

            var codeConstructorSnippet =
                "            if (!bFoundInstalledCultures)\n" +
                "            {\n" +
                "                CultureInfo tCulture;\n" +
                "                foreach (string dir in Directory.GetDirectories(Application.StartupPath))\n" +
                "                {\n" +
                "                    try\n" +
                "                    {\n" +
                "                        DirectoryInfo dirinfo = new DirectoryInfo(dir);\n" +
                "                        tCulture = CultureInfo.GetCultureInfo(dirinfo.Name);\n\n" +
                "                        if (dirinfo.GetFiles(Path.GetFileNameWithoutExtension(Application.ExecutablePath) + \".resources.dll\").Length > 0)\n" +
                "                        {\n" +
                "                            pSupportedCultures.Add(tCulture);\n" +
                "                        }\n" +
                "                    }\n" +
                "                    catch(ArgumentException)\n" +
                "                    {\n" +
                "                    }\n" +
                "                }\n" +
                "                bFoundInstalledCultures = true;\n" +
                "            }\n";

            CodeTypeConstructor codeConstructor = new CodeTypeConstructor();
            codeConstructor.CustomAttributes.Add(attributeDeclaration1);
            codeConstructor.Attributes = MemberAttributes.Static;
            codeConstructor.Statements.Add(new CodeSnippetStatement(codeConstructorSnippet));

            srClass.Members.Add((CodeTypeMember)codeConstructor);

            CodeTypeReference codeTypeReference1 = new CodeTypeReference(typeof(bool), CodeTypeReferenceOptions.GlobalReference);
            CodeMemberField codeMemberField1 = new CodeMemberField(codeTypeReference1, "bFoundInstalledCultures");
            codeMemberField1.Attributes = MemberAttributes.Static;
            codeMemberField1.InitExpression = new CodePrimitiveExpression(false);
            codeMemberField1.Comments.Add(new CodeCommentStatement(RM.GetString("FoundInstalledCulturesComment"), true));

            CodeTypeReference codeTypeReference2 = new CodeTypeReference(typeof(List<CultureInfo>), CodeTypeReferenceOptions.GenericTypeParameter);
            CodeMemberField codeMemberField2 = new CodeMemberField(codeTypeReference2, "pSupportedCultures");
            codeMemberField2.Attributes = MemberAttributes.Static;
            codeMemberField2.InitExpression =
                new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(List<CultureInfo>)));
            codeMemberField2.Comments.Add(new CodeCommentStatement(RM.GetString("FoundInstalledCulturesComment"), true));

            CodeTypeReference codeTypeReference3 = new CodeTypeReference(typeof(ObjectDataProvider), CodeTypeReferenceOptions.GlobalReference);
            CodeMemberField codeMemberField3 = new CodeMemberField(codeTypeReference3, "m_provider");
            codeMemberField3.Attributes = MemberAttributes.Static | MemberAttributes.Private;

            srClass.Members.Add((CodeTypeMember)codeMemberField1);
            srClass.Members.Add((CodeTypeMember)codeMemberField2);
            srClass.Members.Add((CodeTypeMember)codeMemberField3);

            CodeMemberProperty codeMemberPropertySupportedCultures = new CodeMemberProperty();
            codeMemberPropertySupportedCultures.Name = "SupportedCultures";
            codeMemberPropertySupportedCultures.HasGet = true;
            codeMemberPropertySupportedCultures.HasSet = false;
            codeMemberPropertySupportedCultures.Type = new CodeTypeReference(typeof(List<CultureInfo>));
            codeMemberPropertySupportedCultures.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            codeMemberPropertySupportedCultures.GetStatements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("pSupportedCultures")));
            codeMemberPropertySupportedCultures.Comments.Add(new CodeCommentStatement(RM.GetString("ListOfAvailableCultures"), true));

            srClass.Members.Add((CodeTypeMember)codeMemberPropertySupportedCultures);

            CodeMemberProperty codeMemberPropertyResourceProvider = new CodeMemberProperty();
            codeMemberPropertyResourceProvider.Name = "ResourceProvider";
            codeMemberPropertyResourceProvider.HasGet = true;
            codeMemberPropertyResourceProvider.HasSet = false;
            codeMemberPropertyResourceProvider.Type = new CodeTypeReference(typeof(ObjectDataProvider));
            codeMemberPropertyResourceProvider.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            var codeMemberPropertyResourceProviderReturnStatementSpippet = string.Format(
                "                if (m_provider == null)\n" +
                "                    m_provider = (ObjectDataProvider)App.Current.FindResource(\"{0}\");\n" +
                "                return m_provider;", baseName);
            codeMemberPropertyResourceProvider.GetStatements.Add(new CodeSnippetStatement(codeMemberPropertyResourceProviderReturnStatementSpippet));
            codeMemberPropertyResourceProvider.Comments.Add(new CodeCommentStatement(RM.GetString("ListOfAvailableCultures"), true));

            srClass.Members.Add((CodeTypeMember)codeMemberPropertyResourceProvider);

            CodeMemberMethod methodGetResourceInstance = new CodeMemberMethod();
            methodGetResourceInstance.Name = "GetResourceInstance";
            methodGetResourceInstance.Attributes = MemberAttributes.Public;
            methodGetResourceInstance.ReturnType = new CodeTypeReference(nameSpace + "." + baseName);
            methodGetResourceInstance.Statements.Add(new CodeSnippetStatement(string.Format("            return new {0}.{1}();", nameSpace, baseName)));
            methodGetResourceInstance.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodGetResourceInstance.Comments.Add(new CodeCommentStatement(RM.GetString("GetResourceInstanceComment"), true));
            methodGetResourceInstance.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            srClass.Members.Add((CodeTypeMember)methodGetResourceInstance);

            CodeMemberMethod methodChangeCulture = new CodeMemberMethod();
            methodChangeCulture.Name = "ChangeCulture";
            methodChangeCulture.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            methodChangeCulture.ReturnType = new CodeTypeReference(typeof(void));
            methodChangeCulture.Parameters.Add(new CodeParameterDeclarationExpression(typeof(CultureInfo), "culture"));
            var snippet = string.Format(
                "            if (pSupportedCultures.Contains(culture))\n" +
                "            {{\n" +
                "                {0}.Culture = culture;\n" +
                "                ResourceProvider.Refresh();\n" +
                "            }}", baseName);
            methodChangeCulture.Statements.Add(new CodeSnippetStatement(snippet));
            methodChangeCulture.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodChangeCulture.Comments.Add(new CodeCommentStatement(RM.GetString("ChangeCultureComment"), true));
            methodChangeCulture.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            srClass.Members.Add((CodeTypeMember)methodChangeCulture);
        }

        private static void AddGeneratedCodeAttributeforMember(CodeTypeMember typeMember)
        {
            CodeAttributeDeclaration attributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(GeneratedCodeAttribute)));
            attributeDeclaration.AttributeType.Options = CodeTypeReferenceOptions.GlobalReference;
            CodeAttributeArgument attributeArgument1 = new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)typeof(ResxWPFBuilder).FullName));
            CodeAttributeArgument attributeArgument2 = new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"1.0.0.0"));
            attributeDeclaration.Arguments.Add(attributeArgument1);
            attributeDeclaration.Arguments.Add(attributeArgument2);
            typeMember.CustomAttributes.Add(attributeDeclaration);
        }
    }
}

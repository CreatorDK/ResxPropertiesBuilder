using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Resources.Tools;
using System.Runtime.CompilerServices;

using Helper = ResxPropertiesBuilder.CodeCompileHelper;

namespace ResxPropertiesBuilder
{
    public static class ResxDesignerBuilder
    {
        public static CodeCompileUnit CreateDesignerCodeCompiledUnit
            (string baseName,
            string fileNamespace,
            string resourceNamespace,
            CodeDomProvider codeProvider,
            DesignerClassDeclaration declaration)
        {
            if (baseName == null)
                throw new ArgumentNullException(nameof(baseName));
            if (codeProvider == null)
                throw new ArgumentNullException(nameof(codeProvider));

            string str1 = baseName;
            if (!codeProvider.IsValidIdentifier(str1))
            {
                string str2 = Helper.VerifyResourceName(str1, codeProvider);
                if (str2 != null)
                    str1 = str2;
            }
            if (!codeProvider.IsValidIdentifier(str1))
                throw new ArgumentException(RM.GetString("InvalidIdentifier", (object)str1));
            if (!string.IsNullOrEmpty(fileNamespace) && !codeProvider.IsValidIdentifier(fileNamespace))
            {
                string str2 = Helper.VerifyResourceName(fileNamespace, codeProvider, true);
                if (str2 != null)
                    fileNamespace = str2;
            }

            bool internalClass = declaration.ClassModifier == TypeAttributes.NotPublic;
            bool useStatic = declaration.ClassModifier == TypeAttributes.NotPublic || codeProvider.Supports(GeneratorSupport.PublicStaticMembers);

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.ReferencedAssemblies.Add("System.dll");
            codeCompileUnit.UserData.Add((object)"AllowLateBound", (object)false);
            codeCompileUnit.UserData.Add((object)"RequireVariableDeclaration", (object)true);
            CodeNamespace codeNamespace = new CodeNamespace(fileNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeCompileUnit.Namespaces.Add(codeNamespace);
            CodeTypeDeclaration srClass = new CodeTypeDeclaration(str1);
            srClass.IsPartial = true;
            codeNamespace.Types.Add(srClass);
            AddGeneratedCodeAttributeforMember((CodeTypeMember)srClass);
            srClass.TypeAttributes = declaration.ClassModifier;
            
            srClass.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            srClass.Comments.Add(new CodeCommentStatement(RM.GetString("ClassDocComment"), true));
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
            EmitBasicClassMembers(declaration, srClass, fileNamespace, baseName, resourceNamespace, internalClass, useStatic, supportsTryCatch, useTypeInfo);
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
            CodeCommentStatement commentStatement1 = new CodeCommentStatement(RM.GetString("ClassComments1"));
            srClass.Comments.Add(commentStatement1);
            CodeCommentStatement commentStatement2 = new CodeCommentStatement(RM.GetString("ClassComments2"));
            srClass.Comments.Add(commentStatement2);
            CodeCommentStatement commentStatement3 = new CodeCommentStatement(RM.GetString("ClassComments3"));
            srClass.Comments.Add(commentStatement3);
            CodeCommentStatement commentStatement4 = new CodeCommentStatement(RM.GetString("ClassComments4"));
            srClass.Comments.Add(commentStatement4);
            CodeAttributeDeclaration attributeDeclaration1 = new CodeAttributeDeclaration(new CodeTypeReference(typeof(SuppressMessageAttribute)));
            attributeDeclaration1.AttributeType.Options = CodeTypeReferenceOptions.GlobalReference;
            attributeDeclaration1.Arguments.Add(new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"Microsoft.Performance")));
            attributeDeclaration1.Arguments.Add(new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"CA1811:AvoidUncalledPrivateCode")));
            CodeConstructor codeConstructor = new CodeConstructor();
            codeConstructor.CustomAttributes.Add(attributeDeclaration1);
            if (useStatic | internalClass)
                codeConstructor.Attributes = MemberAttributes.FamilyAndAssembly;
            else
                codeConstructor.Attributes = MemberAttributes.Public;
            srClass.Members.Add((CodeTypeMember)codeConstructor);
            CodeTypeReference codeTypeReference = new CodeTypeReference(typeof(ResourceManager), CodeTypeReferenceOptions.GlobalReference);
            CodeMemberField codeMemberField1 = new CodeMemberField(codeTypeReference, declaration.ResMgrFieldName);
            codeMemberField1.Attributes = MemberAttributes.Private;
            if (useStatic)
                codeMemberField1.Attributes |= MemberAttributes.Static;
            srClass.Members.Add((CodeTypeMember)codeMemberField1);
            CodeTypeReference type = new CodeTypeReference(typeof(CultureInfo), CodeTypeReferenceOptions.GlobalReference);
            CodeMemberField codeMemberField2 = new CodeMemberField(type, declaration.CultureInfoFieldName);
            codeMemberField2.Attributes = MemberAttributes.Private;
            if (useStatic)
                codeMemberField2.Attributes |= MemberAttributes.Static;
            srClass.Members.Add((CodeTypeMember)codeMemberField2);
            CodeMemberProperty codeMemberProperty1 = new CodeMemberProperty();
            srClass.Members.Add((CodeTypeMember)codeMemberProperty1);
            codeMemberProperty1.Name = declaration.ResMgrPropertyName;
            codeMemberProperty1.HasGet = true;
            codeMemberProperty1.HasSet = false;
            codeMemberProperty1.Type = codeTypeReference;
            if (internalClass)
                codeMemberProperty1.Attributes = MemberAttributes.Assembly;
            else
                codeMemberProperty1.Attributes = MemberAttributes.Public;
            if (useStatic)
                codeMemberProperty1.Attributes |= MemberAttributes.Static;
            CodeAttributeDeclaration attributeDeclaration2 = new CodeAttributeDeclaration("System.ComponentModel.EditorBrowsableAttribute", new CodeAttributeArgument[1]
            {
        new CodeAttributeArgument((CodeExpression) new CodeFieldReferenceExpression((CodeExpression) new CodeTypeReferenceExpression(new CodeTypeReference(typeof (EditorBrowsableState))
        {
          Options = CodeTypeReferenceOptions.GlobalReference
        }), "Advanced"))
            });
            attributeDeclaration2.AttributeType.Options = CodeTypeReferenceOptions.GlobalReference;
            codeMemberProperty1.CustomAttributes.Add(attributeDeclaration2);
            CodeMemberProperty codeMemberProperty2 = new CodeMemberProperty();
            srClass.Members.Add((CodeTypeMember)codeMemberProperty2);
            codeMemberProperty2.Name = declaration.CultureInfoPropertyName;
            codeMemberProperty2.HasGet = true;
            codeMemberProperty2.HasSet = true;
            codeMemberProperty2.Type = type;
            if (internalClass)
                codeMemberProperty2.Attributes = MemberAttributes.Assembly;
            else
                codeMemberProperty2.Attributes = MemberAttributes.Public;
            if (useStatic)
                codeMemberProperty2.Attributes |= MemberAttributes.Static;
            codeMemberProperty2.CustomAttributes.Add(attributeDeclaration2);
            CodeFieldReferenceExpression referenceExpression1 = new CodeFieldReferenceExpression((CodeExpression)null, declaration.ResMgrFieldName);
            CodeMethodInvokeExpression invokeExpression = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression((CodeExpression)new CodeTypeReferenceExpression(typeof(object)), "ReferenceEquals"), new CodeExpression[2]
            {
        (CodeExpression) referenceExpression1,
        (CodeExpression) new CodePrimitiveExpression((object) null)
            });
            CodePropertyReferenceExpression referenceExpression2 = !useTypeInfo ? new CodePropertyReferenceExpression((CodeExpression)new CodeTypeOfExpression(new CodeTypeReference(srClass.Name)), "Assembly") : new CodePropertyReferenceExpression((CodeExpression)new CodeMethodInvokeExpression((CodeExpression)new CodeTypeOfExpression(new CodeTypeReference(srClass.Name)), "GetTypeInfo", new CodeExpression[0]), "Assembly");
            CodeObjectCreateExpression createExpression = new CodeObjectCreateExpression(codeTypeReference, new CodeExpression[2]
            {
        (CodeExpression) new CodePrimitiveExpression((object) str),
        (CodeExpression) referenceExpression2
            });
            CodeStatement[] codeStatementArray = new CodeStatement[2]
            {
        (CodeStatement) new CodeVariableDeclarationStatement(codeTypeReference, "temp", (CodeExpression) createExpression),
        (CodeStatement) new CodeAssignStatement((CodeExpression) referenceExpression1, (CodeExpression) new CodeVariableReferenceExpression("temp"))
            };
            codeMemberProperty1.GetStatements.Add((CodeStatement)new CodeConditionStatement((CodeExpression)invokeExpression, codeStatementArray));
            codeMemberProperty1.GetStatements.Add((CodeStatement)new CodeMethodReturnStatement((CodeExpression)referenceExpression1));
            codeMemberProperty1.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            codeMemberProperty1.Comments.Add(new CodeCommentStatement(RM.GetString("ResMgrPropertyComment"), true));
            codeMemberProperty1.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));
            CodeFieldReferenceExpression referenceExpression3 = new CodeFieldReferenceExpression((CodeExpression)null, declaration.CultureInfoFieldName);
            codeMemberProperty2.GetStatements.Add((CodeStatement)new CodeMethodReturnStatement((CodeExpression)referenceExpression3));
            CodePropertySetValueReferenceExpression referenceExpression4 = new CodePropertySetValueReferenceExpression();
            codeMemberProperty2.SetStatements.Add((CodeStatement)new CodeAssignStatement((CodeExpression)referenceExpression3, (CodeExpression)referenceExpression4));
            codeMemberProperty2.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            codeMemberProperty2.Comments.Add(new CodeCommentStatement(RM.GetString("CulturePropertyComment1"), true));
            codeMemberProperty2.Comments.Add(new CodeCommentStatement(RM.GetString("CulturePropertyComment2"), true));
            codeMemberProperty2.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            CodeMemberMethod methodGetStringWithName = new CodeMemberMethod();
            methodGetStringWithName.Name = "GetString";
            methodGetStringWithName.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            methodGetStringWithName.ReturnType = new CodeTypeReference(typeof(string));
            methodGetStringWithName.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            methodGetStringWithName.Statements.Add(new CodeSnippetStatement("            return ResourceManager.GetString(name, resourceCulture);"));
            methodGetStringWithName.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodGetStringWithName.Comments.Add(new CodeCommentStatement(RM.GetString("MethodGetStringWithNameComment"), true));
            methodGetStringWithName.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            CodeMemberMethod methodGetStringWithNameCulture = new CodeMemberMethod();
            methodGetStringWithNameCulture.Name = "GetString";
            methodGetStringWithNameCulture.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            methodGetStringWithNameCulture.ReturnType = new CodeTypeReference(typeof(string));
            methodGetStringWithNameCulture.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            methodGetStringWithNameCulture.Parameters.Add(new CodeParameterDeclarationExpression(typeof(CultureInfo), "culture"));
            methodGetStringWithNameCulture.Statements.Add(new CodeSnippetStatement("            return ResourceManager.GetString(name, culture);"));
            methodGetStringWithNameCulture.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodGetStringWithNameCulture.Comments.Add(new CodeCommentStatement(RM.GetString("MethodGetStringWithNameCultureComment"), true));
            methodGetStringWithNameCulture.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            CodeParameterDeclarationExpression objectArrayParamsDelaration = new CodeParameterDeclarationExpression(typeof(object[]), "args");
            objectArrayParamsDelaration.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.ParamArrayAttribute))));

            CodeMemberMethod methodGetStringWithNameArgs = new CodeMemberMethod();
            methodGetStringWithNameArgs.Name = "GetString";
            methodGetStringWithNameArgs.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            methodGetStringWithNameArgs.ReturnType = new CodeTypeReference(typeof(string));
            methodGetStringWithNameArgs.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            methodGetStringWithNameArgs.Parameters.Add(objectArrayParamsDelaration);
            string methodGetStringWithNameArgsSnippet =
                "            string format = ResourceManager.GetString(name, resourceCulture);\n\n" +
                "            if (args == null || args.Length == 0)\n" +
                "                return format;\n\n" +
                "            for (int index = 0; index < args.Length; ++index)\n" +
                "            {\n" +
                "                if (args[index] is string str1 && str1.Length > 1024)\n" +
                "                    args[index] = (object)(str1.Substring(0, 1021) + \"...\");\n" +
                "            }\n\n" +
                "            return string.Format((IFormatProvider)resourceCulture, format, args);";
            methodGetStringWithNameArgs.Statements.Add(new CodeSnippetStatement(methodGetStringWithNameArgsSnippet));
            methodGetStringWithNameArgs.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodGetStringWithNameArgs.Comments.Add(new CodeCommentStatement(RM.GetString("MethodGetStringWithNameArgsComment"), true));
            methodGetStringWithNameArgs.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            CodeMemberMethod methodGetStringWithNameCultureArgs = new CodeMemberMethod();
            methodGetStringWithNameCultureArgs.Name = "GetString";
            methodGetStringWithNameCultureArgs.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            methodGetStringWithNameCultureArgs.ReturnType = new CodeTypeReference(typeof(string));
            methodGetStringWithNameCultureArgs.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            methodGetStringWithNameCultureArgs.Parameters.Add(new CodeParameterDeclarationExpression(typeof(CultureInfo), "culture"));
            methodGetStringWithNameCultureArgs.Parameters.Add(objectArrayParamsDelaration);
            string methodGetStringWithNameCultureArgsSnippet =
                "            string format = ResourceManager.GetString(name, culture);\n\n" +
                "            if (args == null || args.Length == 0)\n" +
                "                return format;\n\n" +
                "            for (int index = 0; index < args.Length; ++index)\n" +
                "            {\n" +
                "                if (args[index] is string str1 && str1.Length > 1024)\n" +
                "                    args[index] = (object)(str1.Substring(0, 1021) + \"...\");\n" +
                "            }\n\n" +
                "            return string.Format((IFormatProvider)culture, format, args);";
            methodGetStringWithNameCultureArgs.Statements.Add(new CodeSnippetStatement(methodGetStringWithNameCultureArgsSnippet));
            methodGetStringWithNameCultureArgs.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            methodGetStringWithNameCultureArgs.Comments.Add(new CodeCommentStatement(RM.GetString("MethodGetStringWithNameCultureArgsComment"), true));
            methodGetStringWithNameCultureArgs.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            srClass.Members.Add(methodGetStringWithName);
            srClass.Members.Add(methodGetStringWithNameCulture);
            srClass.Members.Add(methodGetStringWithNameArgs);
            srClass.Members.Add(methodGetStringWithNameCultureArgs);

        }

        private static void AddGeneratedCodeAttributeforMember(CodeTypeMember typeMember)
        {
            CodeAttributeDeclaration attributeDeclaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(GeneratedCodeAttribute)));
            attributeDeclaration.AttributeType.Options = CodeTypeReferenceOptions.GlobalReference;
            CodeAttributeArgument attributeArgument1 = new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)typeof(ResxDesignerBuilder).FullName));
            CodeAttributeArgument attributeArgument2 = new CodeAttributeArgument((CodeExpression)new CodePrimitiveExpression((object)"1.0.0.0"));
            attributeDeclaration.Arguments.Add(attributeArgument1);
            attributeDeclaration.Arguments.Add(attributeArgument2);
            typeMember.CustomAttributes.Add(attributeDeclaration);
        }
    }
}

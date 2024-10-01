using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Resources.Tools;

using Helper = ResxPropertiesBuilder.CodeCompileHelper;

namespace ResxPropertiesBuilder
{
    public static class ResxPropertiesBuilder
    {
        public static CodeCompileUnit CreatePropertiesCodeCompiledUnit(
            string baseName,
            string generatedCodeNamespace,
            IDictionary resourceList, 
            CodeDomProvider codeProvider,
            DesignerClassDeclaration declaration,
            out string[] unmatchable)
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
            if (!string.IsNullOrEmpty(generatedCodeNamespace) && !codeProvider.IsValidIdentifier(generatedCodeNamespace))
            {
                string str2 = Helper.VerifyResourceName(generatedCodeNamespace, codeProvider, true);
                if (str2 != null)
                    generatedCodeNamespace = str2;
            }

            if (resourceList == null)
                throw new ArgumentNullException(nameof(resourceList));
            Dictionary<string, ResourceData> resourceList1 = new Dictionary<string, ResourceData>((IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase);
            foreach (DictionaryEntry resource in resourceList)
            {
                ResourceData resourceData;
                if (resource.Value is ResXDataNode resXdataNode2)
                {
                    string key = (string)resource.Key;
                    if (key != resXdataNode2.Name)
                        throw new ArgumentException(RM.GetString("MismatchedResourceName", (object)key, (object)resXdataNode2.Name));
                    resourceData = new ResourceData(Type.GetType(resXdataNode2.GetValueTypeName((AssemblyName[])null)), resXdataNode2.GetValue((AssemblyName[])null).ToString());
                }
                else
                    resourceData = new ResourceData(resource.Value == null ? typeof(object) : resource.Value.GetType(), resource.Value == null ? (string)null : resource.Value.ToString());
                resourceList1.Add((string)resource.Key, resourceData);
            }
            ArrayList errors = new ArrayList(0);
            Hashtable reverseFixupTable;
            SortedList sortedList = Helper.VerifyResourceNames(resourceList1, codeProvider, errors, out reverseFixupTable);

            bool internalClass = declaration.ClassModifier == TypeAttributes.NotPublic;
            bool useStatic = declaration.ClassModifier == TypeAttributes.NotPublic || codeProvider.Supports(GeneratorSupport.PublicStaticMembers);

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.ReferencedAssemblies.Add("System.dll");
            codeCompileUnit.UserData.Add((object)"AllowLateBound", (object)false);
            codeCompileUnit.UserData.Add((object)"RequireVariableDeclaration", (object)true);
            CodeNamespace codeNamespace = new CodeNamespace(generatedCodeNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeCompileUnit.Namespaces.Add(codeNamespace);
            CodeTypeDeclaration srClass = new CodeTypeDeclaration(str1);
            srClass.IsPartial = true;
            codeNamespace.Types.Add(srClass);
            srClass.TypeAttributes = declaration.ClassModifier;

            srClass.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            srClass.Comments.Add(new CodeCommentStatement(RM.GetString("ClassDocComment"), true));
            srClass.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));

            bool useTypeInfo = codeProvider is ITargetAwareCodeDomProvider awareCodeDomProvider && !awareCodeDomProvider.SupportsProperty(typeof(Type), "Assembly", false);
            if (useTypeInfo)
                codeNamespace.Imports.Add(new CodeNamespaceImport("System.Reflection"));

            foreach (DictionaryEntry dictionaryEntry in sortedList)
            {
                string key = (string)dictionaryEntry.Key;
                string resourceName = (string)reverseFixupTable[(object)key] ?? key;

                if (!DefineResourceFetchingProperty(key, resourceName, (ResourceData)dictionaryEntry.Value, srClass, internalClass, useStatic, declaration))
                    errors.Add(dictionaryEntry.Key);
            }

            System.CodeDom.Compiler.CodeGenerator.ValidateIdentifiers((CodeObject)codeCompileUnit);
            unmatchable = (string[])errors.ToArray(typeof(string));
            return codeCompileUnit;
        }
        private static bool DefineResourceFetchingProperty(
          string propertyName,
          string resourceName,
          ResourceData data,
          CodeTypeDeclaration srClass,
          bool internalClass,
          bool useStatic,
          DesignerClassDeclaration declaration)
        {
            CodeMemberProperty codeMemberProperty = new CodeMemberProperty();
            codeMemberProperty.Name = propertyName;
            codeMemberProperty.HasGet = true;
            codeMemberProperty.HasSet = false;
            Type type = data.Type;
            if (type == (Type)null)
                return false;
            if (type == typeof(MemoryStream))
                type = typeof(UnmanagedMemoryStream);
            while (!type.IsPublic)
                type = type.BaseType;
            CodeTypeReference targetType = new CodeTypeReference(type);
            codeMemberProperty.Type = targetType;
            if (internalClass)
                codeMemberProperty.Attributes = MemberAttributes.Assembly;
            else
                codeMemberProperty.Attributes = MemberAttributes.Public;
            if (useStatic)
                codeMemberProperty.Attributes |= MemberAttributes.Static;

            CodePropertyReferenceExpression referenceExpression1 = new CodePropertyReferenceExpression((CodeExpression)null, declaration.ResMgrPropertyName);
            CodeFieldReferenceExpression referenceExpression2 = new CodeFieldReferenceExpression(useStatic ? (CodeExpression)null : (CodeExpression)new CodeThisReferenceExpression(), "resourceCulture");
            bool flag1 = type == typeof(string);
            bool flag2 = type == typeof(UnmanagedMemoryStream) || type == typeof(MemoryStream);
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            string b = Helper.TruncateAndFormatCommentStringForOutput(data.ValueAsString);
            string a = string.Empty;
            if (!flag1)
                a = Helper.TruncateAndFormatCommentStringForOutput(type.ToString());
            string methodName = !flag1 ? (!flag2 ? "GetObject" : "GetStream") : "GetString";
            string text;
            if (flag1)
                text = RM.GetString("StringPropertyComment", (object)b);
            else if (b == null || string.Equals(a, b))
                text = RM.GetString("NonStringPropertyComment", (object)a);
            else
                text = RM.GetString("NonStringPropertyDetailedComment", (object)a, (object)b);
            codeMemberProperty.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryStart, true));
            codeMemberProperty.Comments.Add(new CodeCommentStatement(text, true));
            codeMemberProperty.Comments.Add(new CodeCommentStatement(Helper.DocCommentSummaryEnd, true));
            CodeExpression codeExpression = (CodeExpression)new CodeMethodInvokeExpression((CodeExpression)referenceExpression1, methodName, new CodeExpression[2]
            {
        (CodeExpression) new CodePrimitiveExpression((object) resourceName),
        (CodeExpression) referenceExpression2
            });
            CodeMethodReturnStatement methodReturnStatement;
            if (flag1 | flag2)
            {
                methodReturnStatement = new CodeMethodReturnStatement(codeExpression);
            }
            else
            {
                CodeVariableDeclarationStatement declarationStatement = new CodeVariableDeclarationStatement(typeof(object), "obj", codeExpression);
                codeMemberProperty.GetStatements.Add((CodeStatement)declarationStatement);
                methodReturnStatement = new CodeMethodReturnStatement((CodeExpression)new CodeCastExpression(targetType, (CodeExpression)new CodeVariableReferenceExpression("obj")));
            }
            codeMemberProperty.GetStatements.Add((CodeStatement)methodReturnStatement);
            srClass.Members.Add((CodeTypeMember)codeMemberProperty);
            return true;
        }
    }
}

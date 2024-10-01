namespace ResxPropertiesBuilder
{
    public static class ResxDictionaryBuilder
    {
        public static string CreateDictionaryContent(string baseName, string namespase)
        {
            string className = "WPF" + baseName;
            string comment1 = RM.GetString("ResourceDictionaryResourcesComment1");
            string comment2 = RM.GetString("ResourceDictionaryResourcesComment2");
            string comment3 = RM.GetString("ResourceDictionaryCultureResourcesListComment");

            return string.Format("" +
                "<ResourceDictionary\n" +
                "	xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                "	xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                "  xmlns:cultures=\"clr-namespace:{0}\">\n\n" +
                "  <!-- " +
                "  {2}\n" +
                "  {3}\n" +
                "  -->\n" +
                "  <ObjectDataProvider x:Key=\"Resources\" ObjectType=\"{{x:Type cultures:{1}}}\" MethodName=\"GetResourceInstance\"/>\n\n" +
                "  <!-- \n" +
                "   {4}\n" +
                "  -->\n" +
                "  <ObjectDataProvider x:Key=\"CultureResourcesList\" ObjectType=\"{{x:Type cultures:{1}}}\"/>\n\n" +
                "</ResourceDictionary>", namespase, className, comment1, comment2, comment3);
        }
    }
}

using System.Reflection;

namespace ResxPropertiesBuilder
{
    public class DesignerClassDeclaration
    {
        public string ResMgrFieldName { get; set; }
        public string ResMgrPropertyName { get; set; }
        public string CultureInfoFieldName { get; set; }
        public string CultureInfoPropertyName { get; set; }
        public TypeAttributes ClassModifier { get; set; }
    }
}

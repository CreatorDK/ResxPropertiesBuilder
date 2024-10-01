using System;

namespace ResxPropertiesBuilder
{
    public class ResourceData
    {
        private Type _type;
        private string _valueAsString;

        internal ResourceData(Type type, string valueAsString)
        {
            this._type = type;
            this._valueAsString = valueAsString;
        }

        internal Type Type => this._type;

        internal string ValueAsString => this._valueAsString;
    }
}

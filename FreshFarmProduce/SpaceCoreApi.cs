using System;

namespace SpaceCore
{
    public interface IApi
    {
        /// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
        void RegisterSerializerType(Type type);
    }
}

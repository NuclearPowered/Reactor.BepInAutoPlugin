using System;

// ReSharper disable once CheckNamespace
namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BepInAutoPluginAttribute : Attribute
    {
        public BepInAutoPluginAttribute(string guid)
        {
        }
    }
}

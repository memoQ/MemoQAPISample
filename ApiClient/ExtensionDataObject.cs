﻿namespace System.Runtime.Serialization
{
    // An apparently unnecessary interface and class
    // But if they're missing, the files generated by SvcUtil.exe do not compile
    // http://tattoocoder.com/asp-net-core-getting-clean-with-soap/

    public class ExtensionDataObject { }
    internal interface IExtensibleDataObject { }
}
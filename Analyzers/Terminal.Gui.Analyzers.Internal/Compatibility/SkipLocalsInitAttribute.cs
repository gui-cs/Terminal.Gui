namespace System.Runtime.CompilerServices;

[AttributeUsage (
                    AttributeTargets.Class
                    | AttributeTargets.Constructor
                    | AttributeTargets.Event
                    | AttributeTargets.Interface
                    | AttributeTargets.Method
                    | AttributeTargets.Module
                    | AttributeTargets.Property
                    | AttributeTargets.Struct,
                    Inherited = false)]

internal sealed class SkipLocalsInitAttribute : Attribute;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Terminal.Gui.Analyzers.Internal.Attributes;
using Terminal.Gui.Analyzers.Internal.Generators.EnumExtensions;

namespace Terminal.Gui.Analyzers.Internal.Tests.Generators.EnumExtensions;

[TestFixture]
[Category ("Source Generators")]
[TestOf (typeof (EnumExtensionMethodsIncrementalGenerator))]
[Parallelizable (ParallelScope.Children)]
public class EnumExtensionMethodsIncrementalGeneratorTests
{
    private static bool _isInitialized;

    /// <summary>All enum types declared in the test assembly.</summary>
    private static readonly ObservableCollection<Type> AllEnumTypes = [];

    /// <summary>
    ///     All enum types without a <see cref="GenerateEnumExtensionMethodsAttribute"/>, <see cref="AllEnumTypes"/>
    /// </summary>
    private static readonly HashSet<Type> BoringEnumTypes = [];

    /// <summary>All extension classes generated for enums with our attribute.</summary>
    private static readonly ObservableCollection<Type> EnumExtensionClasses = [];

    private static readonly ConcurrentDictionary<Type, EnumData> ExtendedEnumTypeMappings = [];
    private static IEnumerable<Type> ExtendedEnumTypes => ExtendedEnumTypeMappings.Keys;

    private static readonly ReaderWriterLockSlim InitializationLock = new ();

    private static IEnumerable<AssemblyExtendedEnumTypeAttribute> GetAssemblyExtendedEnumTypeAttributes () =>
        Assembly.GetExecutingAssembly ()
                .GetCustomAttributes<AssemblyExtendedEnumTypeAttribute> ();

    private static IEnumerable<TestCaseData> Get_AssemblyExtendedEnumTypeAttribute_EnumHasGeneratorAttribute_Cases ()
    {
        return GetAssemblyExtendedEnumTypeAttributes ()
            .Select (
                     static attr => new TestCaseData (attr)
                     {
                         TestName = $"{nameof (AssemblyExtendedEnumTypeAttribute_EnumHasGeneratorAttribute)}({attr.EnumType.Name},{attr.ExtensionClass.Name})",
                         HasExpectedResult = true,
                         ExpectedResult = true
                     });
    }

    [Test]
    [Category ("Attributes")]
    [TestCaseSource (nameof (Get_AssemblyExtendedEnumTypeAttribute_EnumHasGeneratorAttribute_Cases))]
    public bool AssemblyExtendedEnumTypeAttribute_EnumHasGeneratorAttribute (AssemblyExtendedEnumTypeAttribute attr)
    {
        Assume.That (attr, Is.Not.Null);
        Assume.That (attr.EnumType, Is.Not.Null);
        Assume.That (attr.EnumType!.IsEnum);

        return attr.EnumType.IsDefined (typeof (GenerateEnumExtensionMethodsAttribute));
    }

    private const string AssemblyExtendedEnumTypeAttributeEnumPropertyName =
        $"{nameof (AssemblyExtendedEnumTypeAttribute)}.{nameof (AssemblyExtendedEnumTypeAttribute.EnumType)}";

    [Test]
    [Category("Attributes")]
    public void AssemblyExtendedEnumTypeAttribute_ExtensionClassHasExpectedReverseMappingAttribute ([ValueSource(nameof(GetAssemblyExtendedEnumTypeAttributes))]AssemblyExtendedEnumTypeAttribute attr)
    {
        Assume.That (attr, Is.Not.Null);
        Assume.That (attr.ExtensionClass, Is.Not.Null);
        Assume.That (attr.ExtensionClass!.IsClass);
        Assume.That (attr.ExtensionClass!.IsSealed);

        Assert.That (attr.ExtensionClass.IsDefined (typeof (ExtensionsForEnumTypeAttribute<>)));
    }

    [Test]
    [Category("Attributes")]
    public void ExtendedEnum_AssemblyHasMatchingAttribute ([ValueSource(nameof(GetExtendedEnum_EnumData))]EnumData enumData)
    {
        Assume.That (enumData, Is.Not.Null);
        Assume.That (enumData.EnumType, Is.Not.Null);
        Assume.That (enumData.EnumType!.IsEnum);

        Assert.That (enumData.EnumType, Has.Attribute<GenerateEnumExtensionMethodsAttribute> ());
    }

    [Test]
    public void BoringEnum_DoesNotHaveExtensions ([ValueSource (nameof (BoringEnumTypes))] Type enumType)
    {
        Assume.That (enumType.IsEnum);

        Assert.That (enumType, Has.No.Attribute<GenerateEnumExtensionMethodsAttribute> ());
    }

    [Test]
    public void ExtendedEnum_FastIsDefinedFalse_DoesNotHaveFastIsDefined ([ValueSource (nameof (GetExtendedEnumTypes_FastIsDefinedFalse))] EnumData enumData)
    {
        Assume.That (enumData.EnumType.IsEnum);
        Assume.That (enumData.EnumType, Has.Attribute<GenerateEnumExtensionMethodsAttribute> ());
        Assume.That (enumData.GeneratorAttribute, Is.Not.Null);
        Assume.That (enumData.GeneratorAttribute, Is.EqualTo (enumData.EnumType.GetCustomAttribute<GenerateEnumExtensionMethodsAttribute> ()));
        Assume.That (enumData.GeneratorAttribute, Has.Property ("FastIsDefined").False);
        Assume.That (enumData.ExtensionClass, Is.Not.Null);

        Assert.That (enumData.ExtensionClass!.GetMethod ("FastIsDefined"), Is.Null);
    }

    [Test]
    public void ExtendedEnum_StaticExtensionClassExists ([ValueSource (nameof (ExtendedEnumTypes))] Type enumType)
    {
        Assume.That (enumType.IsEnum);
        Assume.That (enumType, Has.Attribute<GenerateEnumExtensionMethodsAttribute> ());
        ITypeInfo enumTypeInfo = new TypeWrapper (enumType);
        Assume.That (enumType, Has.Attribute<GenerateEnumExtensionMethodsAttribute> ());
    }

    [Test]
    public void ExtendedEnum_FastIsDefinedTrue_HasFastIsDefined ([ValueSource (nameof (GetExtendedEnumTypes_FastIsDefinedTrue))] EnumData enumData)
    {
        Assume.That (enumData.EnumType, Is.Not.Null);
        Assume.That (enumData.EnumType.IsEnum);
        Assume.That (enumData.EnumType, Has.Attribute<GenerateEnumExtensionMethodsAttribute> ());
        Assume.That (enumData.ExtensionClass, Is.Not.Null);
        ITypeInfo extensionClassTypeInfo = new TypeWrapper (enumData.ExtensionClass!);
        Assume.That (extensionClassTypeInfo.IsStaticClass);
        Assume.That (enumData.GeneratorAttribute, Is.Not.Null);
        Assume.That (enumData.GeneratorAttribute, Is.EqualTo (enumData.EnumType.GetCustomAttribute<GenerateEnumExtensionMethodsAttribute> ()));
        Assume.That (enumData.GeneratorAttribute, Has.Property ("FastIsDefined").True);

        MethodInfo? fastIsDefinedMethod = enumData.ExtensionClass!.GetMethod ("FastIsDefined");

        Assert.That (fastIsDefinedMethod, Is.Not.Null);
        Assert.That (fastIsDefinedMethod, Has.Attribute<ExtensionAttribute> ());
        IMethodInfo[] extensionMethods = extensionClassTypeInfo.GetMethodsWithAttribute<ExtensionAttribute> (false);


    }

    private static IEnumerable<EnumData> GetExtendedEnum_EnumData ()
    {
        InitializationLock.EnterUpgradeableReadLock ();

        try
        {
            if (!_isInitialized)
            {
                Initialize ();
            }

            return ExtendedEnumTypeMappings.Values;
        }
        finally
        {
            InitializationLock.ExitUpgradeableReadLock ();
        }
    }

    private static IEnumerable<Type> GetBoringEnumTypes ()
    {
        InitializationLock.EnterUpgradeableReadLock ();

        try
        {
            if (!_isInitialized)
            {
                Initialize ();
            }

            return BoringEnumTypes;
        }
        finally
        {
            InitializationLock.ExitUpgradeableReadLock ();
        }
    }

    private static IEnumerable<EnumData> GetExtendedEnumTypes_FastIsDefinedFalse ()
    {
        InitializationLock.EnterUpgradeableReadLock ();

        try
        {
            if (!_isInitialized)
            {
                Initialize ();
            }

            return ExtendedEnumTypeMappings.Values.Where (static t => t.GeneratorAttribute?.FastIsDefined is false);
        }
        finally
        {
            InitializationLock.ExitUpgradeableReadLock ();
        }
    }

    private static IEnumerable<EnumData> GetExtendedEnumTypes_FastIsDefinedTrue ()
    {
        InitializationLock.EnterUpgradeableReadLock ();

        try
        {
            if (!_isInitialized)
            {
                Initialize ();
            }

            return ExtendedEnumTypeMappings.Values.Where (static t => t.GeneratorAttribute?.FastIsDefined is true);
        }
        finally
        {
            InitializationLock.ExitUpgradeableReadLock ();
        }
    }

    private static void Initialize ()
    {
        if (!InitializationLock.IsUpgradeableReadLockHeld || !InitializationLock.TryEnterWriteLock (5000))
        {
            return;
        }

        try
        {
            if (_isInitialized)
            {
                return;
            }

            AllEnumTypes.CollectionChanged += AllEnumTypes_CollectionChanged;
            EnumExtensionClasses.CollectionChanged += EnumExtensionClasses_OnCollectionChanged;

            Type [] allAssemblyTypes = Assembly
                                       .GetExecutingAssembly ()
                                       .GetTypes ();

            IEnumerable<Type> allEnumTypes = allAssemblyTypes.Where (IsDefinedEnum);

            foreach (Type type in allEnumTypes)
            {
                AllEnumTypes.Add (type);
            }

            foreach (Type type in allAssemblyTypes.Where (static t => t.IsClass && t.IsDefined (typeof (ExtensionsForEnumTypeAttribute<>))))
            {
                EnumExtensionClasses.Add (type);
            }

            _isInitialized = true;
        }
        finally
        {
            InitializationLock.ExitWriteLock ();
        }

        return;

        static bool IsDefinedEnum (Type t) { return t is { IsEnum: true, IsGenericType: false, IsConstructedGenericType: false, IsTypeDefinition: true }; }

        static void AllEnumTypes_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is not NotifyCollectionChangedAction.Add and not NotifyCollectionChangedAction.Replace || e.NewItems is null)
            {
                return;
            }

            foreach (Type enumType in e.NewItems.OfType<Type> ())
            {
                if (enumType.GetCustomAttribute<GenerateEnumExtensionMethodsAttribute> () is not { } generatorAttribute)
                {
                    BoringEnumTypes.Add (enumType);

                    continue;
                }

                ExtendedEnumTypeMappings.AddOrUpdate (
                                               enumType,
                                               CreateNewEnumData,
                                               UpdateGeneratorAttributeProperty,
                                               generatorAttribute);
            }
        }

        static EnumData CreateNewEnumData (Type tEnum, GenerateEnumExtensionMethodsAttribute attr) { return new (tEnum, attr); }

        static EnumData UpdateGeneratorAttributeProperty (Type tEnum, EnumData data, GenerateEnumExtensionMethodsAttribute attr)
        {
            data.GeneratorAttribute ??= attr;

            return data;
        }

        static void EnumExtensionClasses_OnCollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }

            foreach (Type extensionClassType in e.NewItems!.OfType<Type> ())
            {
                if (extensionClassType.GetCustomAttribute (typeof (ExtensionsForEnumTypeAttribute<>), false) is not IExtensionsForEnumTypeAttributes
                        {
                            EnumType.IsEnum: true
                        } extensionForAttribute)
                {
                    continue;
                }

                ExtendedEnumTypeMappings [extensionForAttribute.EnumType].ExtensionClass ??= extensionClassType;
            }
        }
    }

    public sealed record EnumData (
        Type EnumType,
        GenerateEnumExtensionMethodsAttribute? GeneratorAttribute = null,
        Type? ExtensionClass = null,
        IExtensionsForEnumTypeAttributes? ExtensionForEnumTypeAttribute = null)
    {
        public Type? ExtensionClass { get; set; } = ExtensionClass;

        public IExtensionsForEnumTypeAttributes? ExtensionForEnumTypeAttribute { get; set; } = ExtensionForEnumTypeAttribute;
        public GenerateEnumExtensionMethodsAttribute? GeneratorAttribute { get; set; } = GeneratorAttribute;
    }
}

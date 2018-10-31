﻿using Cauldron.Interception.Cecilator.Coders;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Cauldron.Interception.Cecilator
{
    public sealed partial class BuilderType : CecilatorBase, IEquatable<BuilderType>
    {
        [EditorBrowsable(EditorBrowsableState.Never), DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly TypeDefinition typeDefinition;

        [EditorBrowsable(EditorBrowsableState.Never), DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly TypeReference typeReference;

        private BuilderType childType;
        public Builder Builder { get; private set; }

        public BuilderType ChildType
        {
            get
            {
                if (this.childType == null)
                    this.childType = (new BuilderType(this.Builder, this.moduleDefinition.GetChildrenType(this.typeReference).childType));

                return this.childType.Import();
            }
        }

        public BuilderCustomAttributeCollection CustomAttributes => new BuilderCustomAttributeCollection(this.Builder, this.typeDefinition);

        public object DefaultValue
        {
            get
            {
                if (this.typeReference.IsGenericParameter || this.typeDefinition == null /* This is probably a generic parameter */)
                    return CodeBlocks.DefaultValueOf(this);

                switch (this.Fullname)
                {
                    case "System.Int16": return default(short);
                    case "System.Int32": return default(int);
                    case "System.Int64": return default(long);
                    case "System.UInt16": return default(ushort);
                    case "System.UInt32": return default(uint);
                    case "System.UInt64": return default(ulong);
                    case "System.Single": return default(float);
                    case "System.Double": return default(double);
                    case "System.Boolean": return default(bool);
                    case "System.Byte": return default(byte);
                    case "System.Char": return default(char);
                    case "System.SByte": return default(sbyte);
                    case "System.IntPtr": return default(IntPtr);
                }

                if (this.typeDefinition.IsValueType)
                    return CodeBlocks.DefaultOfStruct(this.typeReference);

                if (this.typeDefinition.FullName == "System.Threading.Tasks.Task")
                    return CodeBlocks.DefaultOfTask(this.typeReference);

                if (this.typeDefinition.FullName == "System.Threading.Tasks.Task`1")
                    return CodeBlocks.DefaultTaskOfT(this.typeReference);

                return null;
            }
        }

        public BuilderType EnumUnderlyingType => new BuilderType(this.Builder, this.typeDefinition.GetEnumUnderlyingType());
        public string Fullname => this.typeReference.FullName;

        public bool HasUnresolvedGenericParameters
        {
            get
            {
                if (this.typeReference.HasGenericParameters || this.typeReference.ContainsGenericParameter ||
                    this.typeReference.GenericParameters.Any(x => x.IsGenericParameter) || ((this.typeReference as GenericInstanceType)?.GenericArguments.Any(x => x.IsGenericParameter) ?? false))
                    return true;

                return false;
            }
        }

        public bool IsAbstract => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Abstract);
        public bool IsAsyncStateMachine => this.Implements("System.Runtime.CompilerServices.IAsyncStateMachine", false);
        public bool IsByReference { get; private set; }
        public bool IsEnumerable => BuilderTypes.IEnumerable.BuilderType.AreReferenceAssignable(this) || this.IsArray;
        public bool IsForeign => this.moduleDefinition.Assembly == this.typeDefinition.Module.Assembly;
        public bool IsGenerated => this.typeDefinition.FullName.IndexOf('<') >= 0 || this.typeDefinition.FullName.IndexOf('>') >= 0;

        public bool IsGenericType => this.typeDefinition == null || this.typeReference.Resolve() == null;
        public bool IsInterface => this.typeDefinition == null ? false : this.typeDefinition.Attributes.HasFlag(TypeAttributes.Interface);

        public bool IsInternal
        {
            get => this.typeDefinition.Attributes.HasFlag(TypeAttributes.NotPublic);
            set
            {
                if (this.typeDefinition.Attributes.HasFlag(TypeAttributes.Public))
                {
                    this.typeDefinition.Attributes = this.typeDefinition.Attributes & ~TypeAttributes.Public;
                    this.typeDefinition.Attributes |= TypeAttributes.NotPublic;
                }

                if (this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedPublic))
                {
                    this.typeDefinition.Attributes = this.typeDefinition.Attributes & ~TypeAttributes.NestedPublic;
                    this.typeDefinition.Attributes |= TypeAttributes.NestedFamily;
                }
            }
        }

        public bool IsNested => this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedFamily) ||
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedAssembly) ||
            this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedPrivate) ||
            this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedPublic);

        public bool IsNestedFamily => this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedFamily);

        public bool IsNestedPrivate => this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedPrivate);
        public bool IsNestedPublic => this.typeDefinition.Attributes.HasFlag(TypeAttributes.NestedPublic);

        public bool IsNullable => this.typeDefinition.AreEqual(BuilderTypes.Nullable1.BuilderType.typeDefinition);

        public bool IsPrivate => !this.IsPublic && this.IsNestedPrivate;

        public bool IsPublic => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Public) && !this.IsNestedPrivate;

        public bool IsSealed => this.typeDefinition.Attributes.HasFlag(TypeAttributes.Sealed);
        public bool IsStatic => this.IsAbstract && this.IsSealed;

        public bool IsVoid => this.typeDefinition.FullName == "System.Void";

        public string Namespace
        {
            get
            {
                if (this.typeDefinition.IsNested)
                {
                    var parent = this.typeDefinition.DeclaringType;
                    while (parent != null && string.IsNullOrEmpty(parent.Namespace))
                    {
                        parent = parent.DeclaringType;
                    }

                    return parent?.Namespace;
                }

                return this.typeDefinition.Namespace;
            }
        }

        public AssemblyDefinition Assembly => this.typeDefinition.Module.Assembly;

        public BuilderType DeclaringType => this.typeReference.DeclaringType.ToBuilderType();

        public Collection<TypeReference> GenericArguments => (this.typeReference as GenericInstanceType)?.GenericArguments;

        public Collection<GenericParameter> GenericParameters => this.typeDefinition?.GenericParameters;

        public bool HasGenericArguments => (this.typeReference as GenericInstanceType)?.HasGenericArguments ?? false;

        public bool HasGenericParameters => this.typeReference.HasGenericParameters;

        public bool IsArray => this.typeDefinition != null && (this.typeDefinition.IsArray || this.typeReference.FullName.EndsWith("[]") || this.typeDefinition.FullName.EndsWith("[]"));

        public bool IsDelegate => this.typeDefinition.IsDelegate();

        public bool IsEnum => this.typeDefinition.IsEnum;

        public bool IsGenericInstance => this.typeReference.IsGenericInstance;

        public bool IsGenericParameter => this.typeReference.IsGenericParameter;
        public bool IsPrimitive => this.typeDefinition?.IsPrimitive ?? this.typeReference?.IsPrimitive ?? false;
        public bool IsUsed => InstructionBucket.IsUsed(this.typeReference);

        public bool IsValueType => this.typeDefinition == null ? this.typeReference == null ? false : this.typeReference.IsValueType : this.typeDefinition.IsValueType;

        public string Name => this.typeDefinition == null ? this.typeReference.Name : this.typeDefinition.Name;

        /// <summary>
        /// Writes the names of the methods in the current type to the build output.
        /// </summary>
        public void DisplayMethodNames()
        {
            foreach (var item in this.Methods)
                this.Log(LogTypes.Info, $"           {item.Fullname}");
        }

        public IEnumerable<AttributedField> GetAttributedFields()
        {
            foreach (var field in this.Fields)
            {
                if (field.fieldDef.HasCustomAttributes)
                    foreach (var attrib in field.fieldDef.CustomAttributes)
                        yield return new AttributedField(field, attrib);
            }
        }

        public BuilderType GetGenericArgument(int index)
        {
            if (this.typeReference.IsGenericInstance)
                return new BuilderType(this.Builder, (this.typeReference as GenericInstanceType).GenericArguments[index]);

            throw new IndexOutOfRangeException("There is generic argument with index " + index);
        }

        public IEnumerable<BuilderType> GetGenericArguments()
        {
            if (this.typeReference.IsGenericInstance)
            {
                var genericInstanceType = this.typeReference as GenericInstanceType;
                for (int i = 0; i < genericInstanceType.GenericArguments.Count; i++)
                    yield return new BuilderType(this.Builder, genericInstanceType.GenericArguments[i]);
            }
        }

        public bool Implements(Type interfaceType, bool getAll = true) => this.Implements(this.moduleDefinition.ImportReference(interfaceType).ToBuilderType(), getAll);

        public bool Implements(BuilderType builderType, bool getAll = true)
        {
            if (getAll)
                return this.Interfaces.ToArray().Any(x => x.typeReference != null && x.typeReference.AreEqual(builderType.typeReference));
            else
                return this.typeDefinition.Interfaces.Any(x => (x.InterfaceType.AreEqual(builderType.typeReference)));
        }

        public bool Implements(string interfaceName, bool getAll = true) => this.Implements(this.Builder.GetType(interfaceName));

        public BuilderType Import()
        {
            var type = this.typeReference ?? this.typeDefinition;

            try
            {
                return new BuilderType(this.Builder, this.moduleDefinition.ImportReference(type) ?? throw new ArgumentNullException("Unable to resolve."));
            }
            catch (Exception e)
            {
                throw new Exception($"An error has occured while trying to import the type '{type.FullName}'", e);
            }
        }

        public bool Inherits(Type type) => this.Inherits(this.typeDefinition.FullName);

        public bool Inherits(string typename) => this.BaseClasses.Any(x =>
            (x.typeReference != null && x.typeReference.FullName.GetHashCode() == typename.GetHashCode() && x.typeReference.FullName == typename) ||
            (x.typeDefinition != null && x.typeDefinition.FullName.GetHashCode() == typename.GetHashCode() && x.typeDefinition.FullName == typename));

        public BuilderType ToGenericInstance(params BuilderType[] genericArguments) =>
            this.ToGenericInstance(genericArguments.Select(x => x.typeReference ?? x.typeDefinition));

        public BuilderType ToGenericInstance(IEnumerable<BuilderType> genericArguments) =>
            this.ToGenericInstance(genericArguments.Select(x => x.typeReference ?? x.typeDefinition));

        public BuilderType ToGenericInstance(params TypeReference[] genericArguments) =>
            this.ToGenericInstance(genericArguments.Select(x => x));

        public BuilderType ToGenericInstance(IEnumerable<TypeReference> genericArguments)
        {
            var newType = new GenericInstanceType(this.typeReference);

            foreach (var item in genericArguments)
                newType.GenericArguments.Add(item);

            return newType.ToBuilderType();
        }

        public GenericParameter ToGenericParameter() => this.typeReference as GenericParameter;

        #region Constructors

        internal BuilderType(Builder builder, ArrayType arrayType) : base(builder)
        {
            this.IsByReference = arrayType.IsByReference;
            this.typeReference = arrayType;
            this.typeDefinition = this.typeReference.BetterResolve();
            this.Builder = builder;
        }

        internal BuilderType(Builder builder, TypeDefinition typeDefinition) : base(builder)
        {
            this.IsByReference = typeDefinition.IsByReference;
            this.typeDefinition = typeDefinition;
            this.typeReference = typeDefinition.ResolveType(typeDefinition);
            this.Builder = builder;
        }

        internal BuilderType(Builder builder, TypeReference typeReference) : base(builder)
        {
            this.IsByReference = typeReference.IsByReference;
            this.typeDefinition = typeReference.BetterResolve();
            this.typeReference = typeReference;
            this.Builder = builder;
        }

        internal BuilderType(BuilderType builderType, TypeDefinition typeDefinition) : base(builderType)
        {
            this.IsByReference = typeDefinition.IsByReference;
            this.typeDefinition = typeDefinition;
            this.typeReference = typeDefinition.ResolveType(typeDefinition);
            this.Builder = builderType.Builder;
        }

        internal BuilderType(BuilderType builderType, TypeReference typeReference) : base(builderType)
        {
            this.IsByReference = typeReference.IsByReference;
            this.typeReference = typeReference;
            this.typeDefinition = typeReference.BetterResolve();
            this.Builder = builderType.Builder;
        }

        #endregion Constructors

        #region Interfaces Base classes and nested types

        public IEnumerable<BuilderType> BaseClasses =>
            this.typeReference.GetBaseClasses().Select(x => new BuilderType(this, x)).Distinct(new BuilderTypeEqualityComparer());

        public IEnumerable<BuilderType> Interfaces =>
            this.typeReference.GetInterfaces().Select(x => new BuilderType(this, x)).Distinct(new BuilderTypeEqualityComparer());

        public IEnumerable<BuilderType> NestedTypes =>
            this.typeReference.GetNestedTypes().Select(x => new BuilderType(this, x)).Distinct(new BuilderTypeEqualityComparer());

        public BuilderType BaseType => new BuilderType(this, this.typeDefinition.BaseType);

        #endregion Interfaces Base classes and nested types

        #region Actions

        public Method ParameterlessContructor
        {
            get
            {
                if (!this.typeDefinition.HasMethods)
                    return null;

                var ctor = this.typeDefinition.Methods.FirstOrDefault(x => x.Name == ".ctor" && x.Parameters.Count == 0);

                if (ctor == null)
                    return null;

                if (this.typeReference is GenericInstanceType genericInstance)
                    return new Method(this, ctor.MakeHostInstanceGeneric(genericInstance.GenericArguments.ToArray()), ctor);

                return new Method(this, ctor, ctor);
            }
        }

        public Method StaticConstructor
        {
            get
            {
                if (!this.typeDefinition.HasMethods)
                    return null;

                var ctor = this.typeDefinition.Methods.FirstOrDefault(x => x.Name == ".cctor");

                if (ctor == null)
                    return null;

                return new Method(this, ctor);
            }
        }

        public void AddInterface(Type interfaceType) => this.typeDefinition.Interfaces.Add(new InterfaceImplementation(this.moduleDefinition.ImportReference(interfaceType)));

        public void AddInterface(BuilderType interfaceType) => this.typeDefinition.Interfaces.Add(new InterfaceImplementation(this.moduleDefinition.ImportReference(interfaceType.typeReference)));

        public Method CreateConstructor(params BuilderType[] parameters)
        {
            var method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, this.moduleDefinition.TypeSystem.Void);

            foreach (var item in parameters)
                method.Parameters.Add(new ParameterDefinition(item.typeDefinition));

            this.typeDefinition.Methods.Add(method);

            var result = new Method(this, method);
            // result.NewCoder().Call(BuilderTypes.Object.BuilderType.ParameterlessContructor.Import()).Return().Replace();
            result.NewCoder().Call(this.BaseType.ParameterlessContructor.Import()).Return().Replace();
            return result;
        }

        public Method CreateStaticConstructor()
        {
            var cctor = this.StaticConstructor;

            if (cctor != null)
                return cctor;

            var method = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, this.moduleDefinition.TypeSystem.Void);

            var processor = method.Body.GetILProcessor();
            processor.Append(processor.Create(OpCodes.Ret));
            this.typeDefinition.Methods.Add(method);

            var result = new Method(this, method);
            result.NewCoder().Return().Replace();
            return result;
        }

        /// <summary>
        /// Returns all constructors that calls the base class constructor. All constructors that
        /// calls this() are excluded
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Method> GetRelevantConstructors()
        {
            if (this.typeDefinition.HasMethods)
            {
                var ctors = this.typeDefinition.Methods.Where(ctor =>
                {
                    if (ctor.Name != ".ctor")
                        return false;

                    var body = ctor.Body;
                    var first = body.Instructions.FirstOrDefault(x => x.OpCode == OpCodes.Call && (x.Operand as MethodReference).Name == ".ctor");

                    if (first == null)
                        return false;

                    var operand = first.Operand as MethodReference;

                    if (operand.DeclaringType.FullName == this.typeDefinition.BaseType.FullName)
                        return true;

                    return false;
                });

                foreach (var item in ctors)
                    yield return new Method(this, item);

                var cctor = this.StaticConstructor;

                if (cctor != null)
                    yield return cctor;
            }
        }

        public BuilderType MakeArray() => new BuilderType(this.Builder, new ArrayType(this.moduleDefinition.ImportReference(this.typeReference)));

        public BuilderType MakeGeneric(params BuilderType[] typeReference) => new BuilderType(this.Builder, this.typeDefinition.MakeGenericInstanceType(typeReference.Select(x => x.typeReference).ToArray()));

        public BuilderType MakeGeneric(params Type[] type) => this.MakeGeneric(type.Select(x => this.Builder.GetType(x)).ToArray());

        public BuilderType MakeGeneric(params TypeReference[] typeReference) => new BuilderType(this.Builder, this.typeDefinition.MakeGenericInstanceType(typeReference));

        public void Remove()
        {
            InstructionBucket.Reset();

            if (this.typeDefinition.IsNested)
                this.typeDefinition.DeclaringType.NestedTypes.Remove(this.typeDefinition);
            else
                this.moduleDefinition.Types.Remove(this.typeDefinition);
        }

        #endregion Actions

        #region Fields

        public FieldCollection Fields => new FieldCollection(this, this.typeDefinition.Fields);

        public Field CreateField(Modifiers modifier, Type fieldType, string name) => this.CreateField(modifier, fieldType.ToBuilderType(), name);

        public Field CreateField(Modifiers modifier, Field field, string name) => this.CreateField(modifier, field.fieldRef.FieldType, name);

        public Field CreateField(Modifiers modifier, BuilderType fieldType, string name) => this.CreateField(modifier, fieldType.typeReference, name);

        public Field CreateField(Modifiers modifier, TypeReference typeReference, string name)
        {
            var attributes = FieldAttributes.CompilerControlled;

            if (modifier.HasFlag(Modifiers.Constant)) attributes |= FieldAttributes.Literal | FieldAttributes.Static;
            if (modifier.HasFlag(Modifiers.Private)) attributes |= FieldAttributes.Private;
            if (modifier.HasFlag(Modifiers.Static)) attributes |= FieldAttributes.Static;
            if (modifier.HasFlag(Modifiers.Public)) attributes |= FieldAttributes.Public;
            if (modifier.HasFlag(Modifiers.Protected)) attributes |= FieldAttributes.Family;

            var field = new FieldDefinition(name, attributes, this.moduleDefinition.ImportReference(typeReference));
            this.typeDefinition.Fields.Add(field);

            return new Field(this, field);
        }

        public Field GetField(string name, bool throwException = true)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var fields = this.typeDefinition.Fields;
            for (int i = 0; i < fields.Count; i++)
                if (fields[i].Name.GetHashCode() == name.GetHashCode() && fields[i].Name == name)
                    return new Field(this, fields[i]);

            if (throwException)
                throw new MissingFieldException($"Unable to find field '{name}' in type '{this.typeReference.FullName}'");
            else
                return null;
        }

        #endregion Fields

        #region Properties

        public IEnumerable<Property> Properties
        {
            get
            {
                if (this.IsInterface)
                    return this.typeDefinition.Properties.Concat(this.typeDefinition.GetInterfaces().SelectMany(x => x.Resolve().Properties)).Select(x => new Property(this, x));
                else
                    return this.typeDefinition.Properties.Select(x => new Property(this, x));
            }
        }

        public bool ContainsProperty(string name) => this.GetProperties().Any(x => x.Name == name);

        public Property CreateProperty(Modifiers modifier, Type propertyType, string name, PropertySetterCreationOption setterCreationOption = PropertySetterCreationOption.AlwaysCreate) =>
                    this.CreateProperty(modifier, this.Builder.GetType(propertyType), name, setterCreationOption);

        public Property CreateProperty(Modifiers modifier, BuilderType propertyType, string name, PropertySetterCreationOption setterCreationOption = PropertySetterCreationOption.AlwaysCreate)
        {
            var contain = this.GetProperties().Get(name);

            if (contain != null)
                return new Property(this, contain);

            var createSetter = setterCreationOption == PropertySetterCreationOption.AlwaysCreate;
            var attributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName;

            if (modifier.HasFlag(Modifiers.Private)) attributes |= MethodAttributes.Private;
            if (modifier.HasFlag(Modifiers.Static)) attributes |= MethodAttributes.Static;
            if (modifier.HasFlag(Modifiers.Public)) attributes |= MethodAttributes.Public;
            if (modifier.HasFlag(Modifiers.Protected)) attributes |= MethodAttributes.Family;

            var returnType = this.moduleDefinition.ImportReference(propertyType.typeReference);
            var property = new PropertyDefinition(name, PropertyAttributes.None, returnType);
            var backingField = this.CreateField(modifier, returnType, $"<{name}>k__BackingField");

            // This should be here to insure that the field does not have these attributes... Is that even possible?
            if (modifier.HasFlag(Modifiers.Overrides)) attributes |= MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot;

            property.GetMethod = new MethodDefinition("get_" + name, attributes, returnType);
            this.typeDefinition.Properties.Add(property);
            this.typeDefinition.Methods.Add(property.GetMethod);

            if (createSetter)
            {
                property.SetMethod = new MethodDefinition("set_" + name, attributes, this.moduleDefinition.TypeSystem.Void);
                property.SetMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, returnType));
                this.typeDefinition.Methods.Add(property.SetMethod);
            }

            var result = new Property(this, property);

            result.Getter.NewCoder().Load(backingField).Return().Replace();

            if (createSetter)
                result.Setter.NewCoder().SetValue(backingField, CodeBlocks.GetParameter(0)).Return().Replace();

            result.RefreshBackingField();
            return result;
        }

        public Property CreateProperty(Field field, PropertySetterCreationOption setterCreationOption = PropertySetterCreationOption.AlwaysCreate)
        {
            var name = $"<{field.Name}>_fieldProperty";

            var contain = this.GetProperties().Get(name);

            if (contain != null)
                return new Property(this, contain);

            var createSetter = setterCreationOption == PropertySetterCreationOption.AlwaysCreate;
            var attributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Private;

            if (field.Modifiers.HasFlag(Modifiers.Private)) attributes |= MethodAttributes.Private;
            if (field.Modifiers.HasFlag(Modifiers.Static)) attributes |= MethodAttributes.Static;
            if (field.Modifiers.HasFlag(Modifiers.Public)) attributes |= MethodAttributes.Public;
            if (field.Modifiers.HasFlag(Modifiers.Protected)) attributes |= MethodAttributes.Family;
            if (field.Modifiers.HasFlag(Modifiers.Overrides)) attributes |= MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot;

            var property = new PropertyDefinition(name, PropertyAttributes.None, field.FieldType.typeReference)
            {
                GetMethod = new MethodDefinition("get_" + name, attributes, field.FieldType.typeReference)
            };
            this.typeDefinition.Properties.Add(property);
            this.typeDefinition.Methods.Add(property.GetMethod);

            if (createSetter)
            {
                property.SetMethod = new MethodDefinition("set_" + name, attributes, this.moduleDefinition.TypeSystem.Void);
                property.SetMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType.typeReference));
                this.typeDefinition.Methods.Add(property.SetMethod);
            }

            var result = new Property(this, property);

            result.Getter.NewCoder().Load(field).Return().Replace();

            if (createSetter)
                result.Setter.NewCoder().SetValue(field, CodeBlocks.GetParameter(0)).Return().Replace();

            result.RefreshBackingField();
            return result;
        }

        public IEnumerable<Property> GetAllProperties() => this.GetProperties().Select(x => new Property(this, x));

        public Property GetProperty(string name)
        {
            var result = this.GetProperties().Get(name);

            if (result == null)
                throw new MethodNotFoundException($"Unable to proceed. The type '{this.typeDefinition.FullName}' does not contain a property '{name}'");

            return new Property(this, result);
        }

        private IEnumerable<PropertyDefinition> GetProperties() => this.typeDefinition.Properties.Concat(this.BaseClasses.SelectMany(x => x.typeDefinition.Properties).Where(x => !x.IsPrivate()));

        #endregion Properties

        #region Methods

        public IEnumerable<Method> Methods
        {
            get
            {
                return
                    this.typeDefinition.IsInterface ?
                    this.typeReference.GetMethodReferencesByInterfaces().Select(x => new Method(this, x.reference, x.definition)) :
                    this.typeDefinition.Methods.Where(x => x.Body != null).Select(x => new Method(this, x));
            }
        }

        public Method CreateMethod(Modifiers modifier, Type returnType, string name, params Type[] parameters) =>
            this.CreateMethod(modifier, this.Builder.GetType(returnType), name, parameters.Select(x => this.Builder.GetType(x)).ToArray());

        public Method CreateMethod(Modifiers modifier, BuilderType returnType, string name, params BuilderType[] parameters) =>
            this.CreateMethodInternal(modifier, returnType, name, parameters);

        public Method CreateMethod(Modifiers modifier, string name, params Type[] parameters) =>
            this.CreateMethod(modifier, name, parameters.Select(x => x.ToBuilderType()).ToArray());

        public Method CreateMethod(Modifiers modifier, string name, params BuilderType[] parameters) =>
            this.CreateMethodInternal(modifier, null, name, parameters);

        public Method CreateMethodImplicitInterface(Method methodToOverride)
        {
            if (!methodToOverride.OriginType.IsInterface)
                throw new Exception("CreateMethodImplicitInterface requires a method from an interface.");

            var method = new MethodDefinition(
                $"{methodToOverride.DeclaringType.typeReference.FullName}.{methodToOverride.Name}",
                MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Private,
                this.moduleDefinition.ImportReference(methodToOverride.methodReference.ReturnType));

            method.Overrides.Add(this.moduleDefinition.ImportReference(methodToOverride.methodReference));

            if (methodToOverride.methodReference.HasGenericParameters)
                foreach (var item in methodToOverride.methodReference.GenericParameters)
                    method.GenericParameters.Add(item);

            foreach (var item in methodToOverride.Parameters)
            {
                var parameterType = item.typeReference.ResolveType(methodToOverride.methodReference.DeclaringType, methodToOverride.methodReference);
                method.Parameters.Add(new ParameterDefinition(parameterType.IsGenericParameter ? parameterType : this.moduleDefinition.ImportReference(parameterType)));
            }

            if (methodToOverride.methodReference is GenericInstanceMethod genericInstanceMethod && genericInstanceMethod.HasGenericArguments)
            {
                var instancedMethod = new GenericInstanceMethod(method);
                foreach (var item in genericInstanceMethod.GenericArguments)
                    instancedMethod.GenericArguments.Add(item);
            }

            this.typeDefinition.Methods.Add(method);

            var result = new Method(this, method);

            if (!result.IsAbstract)
                result.NewCoder().ThrowNew(typeof(NotImplementedException), $"The method '{method.Name}' in type '{this.typeReference}' is not implemented.").Return().Replace();

            return result;
        }

        public Method CreateMethodInternal(Modifiers modifier, BuilderType returnType, string name, params BuilderType[] parameters)
        {
            try
            {
                var attributes = modifier.ToMethodAttributes();

                var method = returnType == null ?
                    new MethodDefinition(name, attributes, this.moduleDefinition.TypeSystem.Void) :
                    new MethodDefinition(name, attributes, this.moduleDefinition.ImportReference(returnType.typeReference));

                foreach (var item in parameters)
                {
                    var parameterType = item.typeReference.ResolveType(this.typeReference) ?? item.typeDefinition.ResolveType(this.typeReference);
                    method.Parameters.Add(new ParameterDefinition(parameterType.IsGenericParameter ? parameterType : this.moduleDefinition.ImportReference(parameterType)));
                }

                this.typeDefinition.Methods.Add(method);
                var result = new Method(this, method);

                if (!attributes.HasFlag(MethodAttributes.Abstract))
                    result.NewCoder().ThrowNew(typeof(NotImplementedException), $"The method '{method.Name}' in type '{this.typeReference}' is not implemented.").Return().Replace();

                return result;
            }
            catch (NullReferenceException e)
            {
                throw new NullReferenceException(
                  string.Join("\r\n", new string[] {
                    "Object reference not set to an instance of an object.",
                    "Return Type: " + returnType.typeReference,
                    "Parameters: " + parameters == null? "0" : parameters.Length.ToString(),
                    string.Join("             Parameter 1: ", parameters?.Select(x=>(x.typeReference ?? x.typeDefinition.ResolveType(this.typeReference))?.ToString() ?? "").ToArray() ?? new string[0])
                    }), e);
            }
            catch
            {
                throw;
            }
        }

        public Method GetAddHandlerEvent(string eventName, bool throwException = true)
        {
            var @event = this.typeDefinition.Events.FirstOrDefault(x => x.Name == eventName);
            if (@event == null)
                throw new MethodNotFoundException($"Unable to proceed. The type '{this.typeDefinition.FullName}' does not contain an event '{eventName}'");

            return new Method(this, @event.AddMethod);
        }

        public Method GetMethod(string name, bool throwException = true)
            => this.GetMethodInternal(Modifiers.All, null, name, throwException, 0, null);

        public Method GetMethod(string name, int parameterCount, bool throwException = true)
            => this.GetMethodInternal(Modifiers.All, null, name, throwException, parameterCount, null);

        public Method GetMethod(string name, bool throwException = true, params Type[] parameters)
            => this.GetMethodInternal(Modifiers.All, null, name, throwException, parameters.Length, parameters.Select(x => x.ToBuilderType().typeReference).ToArray());

        public Method GetMethod(string name, bool throwException = true, params BuilderType[] parameters)
            => this.GetMethodInternal(Modifiers.All, null, name, throwException, parameters.Length, parameters.Select(x => x.typeReference).ToArray());

        public Method GetMethod(string name, bool throwException = true, params TypeReference[] parameters)
            => this.GetMethodInternal(Modifiers.All, null, name, throwException, parameters.Length, parameters);

        public Method GetMethod(Modifiers modifier, BuilderType returnType, string name, params BuilderType[] parameters)
            => this.GetMethodInternal(modifier, returnType, name, false, parameters.Length, parameters.Select(x => x.typeReference).ToArray());

        public Method GetMethod(string name, bool throwException = true, params string[] parametersTypeNames)
        {
            var result = this.GetMethodsInternal(Modifiers.All, null, name, parametersTypeNames.Length, null)
                .FirstOrDefault(x => x.methodReference.Parameters.Select(y => y.ParameterType.FullName).SequenceEqual(parametersTypeNames));

            if (result == null && throwException)
                throw new MethodNotFoundException($"Unable to proceed. The type '{this.typeDefinition.FullName}' does not contain a method '{name}'");

            return result;
        }

        public IEnumerable<Method> GetMethods() => this.GetMethodsInternal(Modifiers.All, null, null, null, null);

        public IEnumerable<Method> GetMethods(Func<MethodReference, bool> predicate)
            => this.GetMethodsInternal(Modifiers.All, null, null, null, null).Where(x => predicate(x.methodReference));

        public IEnumerable<Method> GetMethods(string name, int parameterCount, bool throwException = true)
            => this.GetMethodsInternal(Modifiers.All, null, name, parameterCount, null);

        public Method GetOrCreateMethod(Modifiers modifier, BuilderType returnType, string name, params BuilderType[] parameters)
        {
            var method = this.GetMethod(modifier, returnType, name, parameters);
            if (method != null)
                return method;

            return this.CreateMethod(modifier, returnType, name, parameters);
        }

        private Method GetMethodInternal(
            Modifiers modifier,
            BuilderType returnType,
            string name,
            bool throwException,
            int? parameterCount,
            TypeReference[] parameterTypes)
        {
            foreach (var method in this.typeReference.GetMethodReferences())
            {
                var result = this.TryFilterMethod(method, modifier, returnType, name, parameterCount, parameterTypes);

                if (result.HasValue)
                    return new Method(this, result.Value.reference, result.Value.definition);
            }

            if (throwException)
            {
                foreach (var method in this.typeReference.GetMethodReferences())
                    Builder.Current.Log(LogTypes.Info, $"-------> {method}");

                throw new MethodNotFoundException($"Unable to proceed. The type '{this.typeDefinition.FullName}' does not contain a method '{name}'");
            }

            return null;
        }

        private IEnumerable<Method> GetMethodsInternal(
            Modifiers modifier,
            BuilderType returnType,
            string name,
            int? parameterCount,
            TypeReference[] parameterTypes)
        {
            foreach (var method in this.typeReference.GetMethodReferences())
            {
                var result = this.TryFilterMethod(method, modifier, returnType, name, parameterCount, parameterTypes);

                if (result.HasValue)
                    yield return new Method(this, result.Value.reference, result.Value.definition);
            }
        }

        private MethodDefinitionAndReference? TryFilterMethod(
            MethodDefinitionAndReference method,
            Modifiers modifier,
            BuilderType returnType,
            string name,
            int? parameterCount,
            TypeReference[] parameterTypes)
        {
            if (returnType != null && !method.reference.ReturnType.AreEqual(returnType))
                return null;

            if (modifier != Modifiers.All)
            {
                if (modifier.HasFlag(Modifiers.Private) && !method.definition.Attributes.HasFlag(MethodAttributes.Private))
                    return null;

                if (modifier.HasFlag(Modifiers.Public) && !method.definition.Attributes.HasFlag(MethodAttributes.Public))
                    return null;

                if (modifier.HasFlag(Modifiers.Protected) && !method.definition.Attributes.HasFlag(MethodAttributes.Family))
                    return null;

                if (modifier.HasFlag(Modifiers.Static) && !method.definition.Attributes.HasFlag(MethodAttributes.Static))
                    return null;

                if (modifier.HasFlag(Modifiers.Internal) && !(!method.definition.Attributes.HasFlag(MethodAttributes.Private) & !method.definition.Attributes.HasFlag(MethodAttributes.Public) & !method.definition.Attributes.HasFlag(MethodAttributes.Family)))
                    return null;

                if (modifier.HasFlag(Modifiers.Overrides) && !(method.definition.Attributes.HasFlag(MethodAttributes.Final) & method.definition.Attributes.HasFlag(MethodAttributes.Virtual) & method.definition.Attributes.HasFlag(MethodAttributes.NewSlot)))
                    return null;
            }

            if (parameterCount != null && method.definition.Parameters != null && parameterCount != method.definition.Parameters.Count)
                return null;

            if (parameterTypes != null && parameterTypes.Length > 0 && method.reference.Parameters != null)
            {
                if (parameterTypes.Length != method.reference.Parameters.Count)
                    return null;

                for (int i = 0; i < parameterTypes.Length; i++)
                    if (!parameterTypes[i].AreEqual(method.definition.Parameters[i].ParameterType))
                        return null;
            }

            if (string.IsNullOrEmpty(name) || method.definition.Name == name || method.reference.Name == name)
                return new MethodDefinitionAndReference
                {
                    reference = method.reference,
                    definition = method.definition
                };

            return null;
        }

        #endregion Methods

        #region Equitable stuff

        public static explicit operator TypeDefinition(BuilderType value) => value?.typeDefinition;

        public static explicit operator TypeReference(BuilderType value) => value?.typeReference;

        public static implicit operator string(BuilderType value) => value?.ToString();

        public static bool operator !=(BuilderType a, BuilderType b) => !(a == b);

        public static bool operator ==(BuilderType a, BuilderType b)
        {
            if (object.Equals(a, null) && object.Equals(b, null))
                return true;

            if (object.Equals(a, null))
                return false;

            return a.Equals(b);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (object.Equals(obj, null))
                return false;

            if (object.ReferenceEquals(obj, this))
                return true;

            switch (obj)
            {
                case BuilderType builderType:
                    return this.Equals(builderType);

                case TypeDefinition typeDefinition when this.typeDefinition != null:
                    return this.typeDefinition.AreEqual(typeDefinition);

                case TypeReference typeReference:
                    return this.typeReference.AreEqual(typeReference);
            }

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(BuilderType other)
        {
            if (object.Equals(other, null))
                return false;

            if (object.ReferenceEquals(other, this))
                return true;

            if (this.IsGenericInstance)
                return this.typeDefinition.AreEqual(other.typeDefinition) || this.typeReference.AreEqual(other.typeReference);

            if (this.IsGenericType)
                return this.typeReference.AreEqual(other.typeReference);

            return this.typeDefinition.AreEqual(other.typeDefinition);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => this.typeDefinition.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => this.typeReference.FullName;

        #endregion Equitable stuff
    }
}
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Concurrent;

namespace SignalR.E2ETesting;

internal class HubAssertBuilder<T>
{
    private static readonly Lazy<Type> typeT = new(() => CreateType());


    /// <summary>
    /// Builds an instance of the specified type using the given method and parameters.
    /// </summary>
    /// <param name="methodAndParams">A blocking collection of messages that represents the method and parameters to use for building the instance.</param>
    /// <returns>An instance of the specified type.</returns>
    internal static T Build(BlockingCollection<Message> methodAndParams)
    {
        return (T)Activator.CreateInstance(typeT.Value, methodAndParams)!;
    }


    /// <summary>
    /// Creates a dynamic assembly and module that generates a new type, which is returned. 
    /// </summary>
    /// <returns>The newly generated type.</returns>
    private static Type CreateType()
    {
        // Call the necessary methods to create and define the new type.
        AssemblyName assemblyName = new("SignalR.E2ETesting");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("SignalR.E2ETesting");
        TypeBuilder typeBuilder = moduleBuilder.DefineType("AssertThat", TypeAttributes.Public | TypeAttributes.Class);

        // Add the interface implementation to type builder.
        typeBuilder.AddInterfaceImplementation(typeof(T));

        // Add the field to type builder.
        FieldBuilder fieldBuilder = typeBuilder.DefineField("message", typeof(BlockingCollection<Message>), FieldAttributes.Private | FieldAttributes.InitOnly);

        // Add the constructor.
        CreateConstructor(typeBuilder, fieldBuilder);

        // Add the method implementations to the type.
        // Use TakeAndCompare to compare the method name and parameters.
        foreach (var method in typeof(T).GetMethods())
        {
            CreateMethod(typeBuilder, method, fieldBuilder);
        }

        // Create the type and return it.
        return typeBuilder.CreateType()!;
    }

    /// <summary>
    /// Creates a method implementation for the current type based on the given method name and parameters.
    /// </summary>
    /// <param name="typeBuilder">A TypeBuilder instance to which the new method will be added.</param>
    /// <param name="method">The MethodInfo object that represents the method being implemented.</param>
    /// <param name="fieldBuilder">A FieldBuilder instance representing the field to be used in the method.</param>
    private static void CreateMethod(TypeBuilder typeBuilder, MethodInfo method, FieldBuilder fieldBuilder)
    {
        // Define the method attributes.
        var methodAttributes = MethodAttributes.Public
                            | MethodAttributes.Virtual
                            | MethodAttributes.Final
                            | MethodAttributes.HideBySig
                            | MethodAttributes.NewSlot;

        // Get method information.
        var methodName = method.Name;
        var parameters = method.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType).ToArray();
        var returnType = method.ReturnType;

        // Define the MethodBuilder instance.
        MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                                                               methodAttributes,
                                                               returnType,
                                                               paramTypes);

        // Get the TakeAndCompare.Invoke method
        var invokeMethod = typeof(TakeAndCompare).GetMethod(nameof(TakeAndCompare.Invoke))!;

        // Sets the number of generic type parameters
        var genericTypeNames =
            paramTypes.Where(p => p.IsGenericParameter).Select(p => p.Name).Distinct().ToArray();

        if (genericTypeNames.Length > 0)
        {
            methodBuilder.DefineGenericParameters(genericTypeNames);
        }

        // Check to see if the last parameter of the method is a CancellationToken.
        bool hasCancellationToken = paramTypes.LastOrDefault() == typeof(CancellationToken);
        if (hasCancellationToken)
        {
            // Remove CancellationToken from the list of input parameters.
            paramTypes = paramTypes.Take(paramTypes.Length - 1).ToArray();
        }

        var generator = methodBuilder.GetILGenerator();
        LocalBuilder localBuilder = generator.DeclareLocal(typeof(object[]));

        // Create a new object array to hold all the parameters to this method.
        generator.Emit(OpCodes.Ldc_I4, paramTypes.Length); // Stack:
        generator.Emit(OpCodes.Newarr, typeof(object)); // allocate object array
        generator.Emit(OpCodes.Stloc, localBuilder);

        // Store each parameter in the object array.
        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(OpCodes.Ldloc, localBuilder); // Object array loaded
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Ldarg, i + 1); // i + 1
            generator.Emit(OpCodes.Box, paramTypes[i]);
            generator.Emit(OpCodes.Stelem_Ref);
        }

        // Load the BlockingCollection<MethodAndParam> field onto the stack.
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, fieldBuilder);

        // Load this method's name.
        generator.Emit(OpCodes.Ldstr, methodName);

        // Load parameter array onto the stack.
        generator.Emit(OpCodes.Ldloc, localBuilder);

        // Call the TakeAndCompare.Invoke method.
        generator.EmitCall(OpCodes.Call, invokeMethod, null);

        // Return Task.CompletedTask.
        generator.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
        generator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Creates a constructor method that initializes the instance of this type.
    /// </summary>
    /// <param name="typeBuilder">A TypeBuilder instance to which the new constructor will be added.</param>
    /// <param name="fieldBuilder">A FieldBuilder instance representing the field to be used in the constructor.</param>
    private static void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
    {
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[]
        {
            typeof(BlockingCollection<Message>)
        });
        ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
        ctorGenerator.Emit(OpCodes.Ldarg_0);
        ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorGenerator.Emit(OpCodes.Ldarg_0);
        ctorGenerator.Emit(OpCodes.Ldarg_1);
        ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        ctorGenerator.Emit(OpCodes.Ret);
    }
}

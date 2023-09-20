using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Concurrent;

namespace SignalR.E2ETesting;

internal class HubAssertBuilder<T>
{
    private static Lazy<Type> typeT = new(() => CreateType());

    internal static T Build(BlockingCollection<Message> methodAndParams)
    {
        return (T)Activator.CreateInstance(typeT.Value, methodAndParams)!;
    }

    private static Type CreateType()
    {
        // Create a dynamic assembly and module.
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

    private static void CreateMethod(TypeBuilder typeBuilder, MethodInfo method, FieldBuilder fieldBuilder)
    {
        var methodAttributes = MethodAttributes.Public
                            | MethodAttributes.Virtual
                            | MethodAttributes.Final
                            | MethodAttributes.HideBySig
                            | MethodAttributes.NewSlot;
        var methodName = method.Name;
        var parameters = method.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType).ToArray();
        var returnType = method.ReturnType;
        MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                                                               methodAttributes,
                                                               returnType,
                                                               paramTypes);
        // 用 反射 獲取 TakeAndCompare.Invoke
        var invokeMethod = typeof(TakeAndCompare).GetMethod(nameof(TakeAndCompare.Invoke))!;
        // Sets the number of generic type parameters
        var genericTypeNames =
            paramTypes.Where(p => p.IsGenericParameter).Select(p => p.Name).Distinct().ToArray();

        if (genericTypeNames.Length > 0)
        {
            methodBuilder.DefineGenericParameters(genericTypeNames);
        }

        // Check to see if the last parameter of the method is a CancellationToken
        bool hasCancellationToken = paramTypes.LastOrDefault() == typeof(CancellationToken);
        if (hasCancellationToken)
        {
            // remove CancellationToken from input paramTypes
            paramTypes = paramTypes.Take(paramTypes.Length - 1).ToArray();
        }

        var generator = methodBuilder.GetILGenerator();
        LocalBuilder localBuilder = generator.DeclareLocal(typeof(object[]));

        // Create an new object array to hold all the parameters to this method
        generator.Emit(OpCodes.Ldc_I4, paramTypes.Length); // Stack:
        generator.Emit(OpCodes.Newarr, typeof(object)); // allocate object array
        
        generator.Emit(OpCodes.Stloc, localBuilder);
        
        // Store each parameter in the object array
        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(OpCodes.Ldloc, localBuilder); // Object array loaded
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Ldarg, i + 1); // i + 1
            generator.Emit(OpCodes.Box, paramTypes[i]);
            generator.Emit(OpCodes.Stelem_Ref);
        }

        // Load the BlockingCollection<MethodAndParam> field onto the stack
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldfld, fieldBuilder);
        
        // this method's name
        generator.Emit(OpCodes.Ldstr, methodName);

        // Load parameter array on to the stack.
        generator.Emit(OpCodes.Ldloc, localBuilder);

        //generator.Emit(OpCodes.Callvirt, invokeMethod);
        generator.EmitCall(OpCodes.Call, invokeMethod, null);

        // return Task.CompletedTask;
        generator.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
        generator.Emit(OpCodes.Ret);
    }

    private static void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
    {
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[]
        {
            typeof(BlockingCollection<Message>)
        });
        ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
        ctorGenerator.Emit(OpCodes.Ldarg_0);
        ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorGenerator.Emit(OpCodes.Ldarg_0); //this
        ctorGenerator.Emit(OpCodes.Ldarg_1);
        ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        ctorGenerator.Emit(OpCodes.Ret);
    }
}
public class TakeAndCompare
{
    public static void Invoke(BlockingCollection<Message> messages, string methodName, object[] parameters)
    {
        CancellationTokenSource cancellationTokenSource = new(1000);
        var message = messages.Take(cancellationTokenSource.Token);
        if (message.MethodName != methodName)
        {
            throw new Exception();
        }
        if (!message.Parameters.SequenceEqual(parameters))
        {
            throw new Exception();
        }
    }
}

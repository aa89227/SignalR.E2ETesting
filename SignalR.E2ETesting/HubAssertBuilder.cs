using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Concurrent;

namespace SignalR.E2ETesting;

internal class HubAssertBuilder<T>
{
    private static Lazy<Type> typeT = new(() => CreateType());

    internal static T Build(BlockingCollection<MethodAndParam> methodAndParams)
    {
        return (T)Activator.CreateInstance(typeT.Value, methodAndParams)!;
    }

    private static Type CreateType()
    {
        // Create a dynamic assembly and module.
        AssemblyName assemblyName = new("SignalR.E2ETesting");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        TypeBuilder typeBuilder = moduleBuilder.DefineType("AssertThat", TypeAttributes.Public | TypeAttributes.Class);

        // Add the interface implementation to type builder.
        typeBuilder.AddInterfaceImplementation(typeof(T));

        // Add the field to type builder.
        FieldBuilder fieldBuilder = typeBuilder.DefineField("methodAndParams", typeof(BlockingCollection<MethodAndParam>), FieldAttributes.Private);

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
        var compare = TakeAndCompare.Invoke;
        MethodInfo invokeMethod = compare.GetMethodInfo();

        var print = (object o) => Console.WriteLine(o);

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

        // Load the BlockingCollection<MethodAndParam> field onto the stack
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldflda, fieldBuilder);
        generator.Emit(OpCodes.Nop);
        generator.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
        generator.Emit(OpCodes.Ret);

        // this method's name
        generator.Emit(OpCodes.Ldstr, methodName);

        // Create an new object array to hold all the parameters to this method
        generator.Emit(OpCodes.Ldc_I4, paramTypes.Length); // Stack:
        generator.Emit(OpCodes.Newarr, typeof(object)); // allocate object array
        generator.Emit(OpCodes.Stloc_0);

        // Store each parameter in the object array
        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(OpCodes.Ldloc_0); // Object array loaded
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Ldarg, i + 1); // i + 1
            generator.Emit(OpCodes.Box, paramTypes[i]);
            generator.Emit(OpCodes.Stelem_Ref);
        }

        // Load parameter array on to the stack.
        generator.Emit(OpCodes.Ldloc_0);

        generator.Emit(OpCodes.Callvirt, invokeMethod);

        // return Task.CompletedTask;
        generator.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
        generator.Emit(OpCodes.Ret);
    }

    private static void CreateConstructor(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
    {
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(BlockingCollection<MethodAndParam>) });
        ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
        ctorGenerator.Emit(OpCodes.Ldarg_0);
        ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorGenerator.Emit(OpCodes.Ldarg_0); //this
        ctorGenerator.Emit(OpCodes.Ldarg_1);
        ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        ctorGenerator.Emit(OpCodes.Ret);
    }

    
}
internal class TakeAndCompare
{
    internal static void Invoke(BlockingCollection<MethodAndParam> methodAndParams, string methodName, object[] parameters)
    {
        CancellationTokenSource cancellationTokenSource = new(1000);
        //var methodAndParam = methodAndParams.Take(cancellationTokenSource.Token);
        //if (methodAndParam.MethodName != methodName)
        //{
        //    throw new Exception();
        //}
        //if (!methodAndParam.Parameters.SequenceEqual(parameters))
        //{
        //    throw new Exception();
        //}
    }
}

interface IExampleHubResponses
{
    Task BroadcastMessage(string message);
    Task Fun1(string message1, string message2);
}

class TestImpl : IExampleHubResponses
{
    private readonly BlockingCollection<MethodAndParam> methodAndParams;

    public TestImpl(BlockingCollection<MethodAndParam> methodAndParams)
    {
        this.methodAndParams = methodAndParams;
    }

    /// <summary>
    /// 拿出 methodAndParams 中的第一個 MessageAndParam，並檢查 methodName 及 parameters 是否相同
    /// </summary>
    /// <param name="message"></param>
    public Task BroadcastMessage(string message)
    {
        var expected = new MethodAndParam("BroadcastMessage", new object[] { message });
        TakeAndCompare(methodAndParams, expected);
        return Task.CompletedTask;
    }

    public Task Fun1(string message1, string message2)
    {
        var expected = new MethodAndParam("Fun1", new object[] { message1, message2 });
        TakeAndCompare(methodAndParams, expected);
        return Task.CompletedTask;
    }

    private static void TakeAndCompare(BlockingCollection<MethodAndParam> methodAndParams, MethodAndParam expected)
    {
        CancellationTokenSource cancellationTokenSource = new(1000);
        var methodAndParam = methodAndParams.Take(cancellationTokenSource.Token);
        var methodName = methodAndParam.MethodName;
        var parameters = methodAndParam.Parameters;
        if (expected.MethodName != methodName)
        {
            throw new Exception();
        }
        if (!expected.Parameters.SequenceEqual(parameters))
        {
            throw new Exception();
        }
    }
}

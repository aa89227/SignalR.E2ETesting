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
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(BlockingCollection<MethodAndParam>) });
        ILGenerator ctorGenerator = constructorBuilder.GetILGenerator();
        ctorGenerator.Emit(OpCodes.Ldarg_0);
        ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorGenerator.Emit(OpCodes.Ldarg_0); //this
        ctorGenerator.Emit(OpCodes.Ldarg_1);
        ctorGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        ctorGenerator.Emit(OpCodes.Ret);

        // Add the method implementations to the type.
        // Use TakeAndCompare to compare the method name and parameters.
        foreach (var method in typeof(T).GetMethods())
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot, method.ReturnType, parameterTypes);
            var methodGenerator = methodBuilder.GetILGenerator();

            // get method name
            // get parameters
            methodGenerator.Emit(OpCodes.Ldstr, method.Name);
            methodGenerator.Emit(OpCodes.Ldc_I4, method.GetParameters().Length);

            methodGenerator.Emit(OpCodes.Newarr, typeof(object));
            methodGenerator.Emit(OpCodes.Stloc_0);
            for (int index = 0; index < method.GetParameters().Length; ++index)
            {
                methodGenerator.Emit(OpCodes.Dup);
                methodGenerator.Emit(OpCodes.Ldc_I4, index);
                methodGenerator.Emit(OpCodes.Ldarg, index + 1);
                //ilGenerator.Emit(OpCodes.Box, method.GetParameters()[index].ParameterType);
                methodGenerator.Emit(OpCodes.Stelem_Ref);
            }
            methodGenerator.Emit(OpCodes.Newobj, typeof(MethodAndParam).GetConstructor(new Type[] { typeof(string), typeof(object[]) })!);
            methodGenerator.Emit(OpCodes.Stloc_0);

            methodGenerator.Emit(OpCodes.Ldarg_0);
            methodGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            methodGenerator.Emit(OpCodes.Ldloc_0);
            methodGenerator.Emit(OpCodes.Call, typeof(HubAssertBuilder<T>).GetMethod(nameof(TakeAndCompare), BindingFlags.NonPublic | BindingFlags.Static)!);
            methodGenerator.Emit(OpCodes.Nop);
            methodGenerator.EmitCall(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!, null);
            methodGenerator.Emit(OpCodes.Stloc_1);

            methodGenerator.Emit(OpCodes.Ldloc_1);
            
            methodGenerator.Emit(OpCodes.Ret);
        }

        // Create the type and return it.
        return typeBuilder.CreateType()!;
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

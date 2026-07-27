using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Toolshed.Commands;

// Nope, we can't use IVewVariable tree whatever cause it uses instances,
// we need to create static navigation tree on types for auto completions

/*
ЗАМЕТКИ: Работает только Something/, иначе Out детектится как object (при Something), но это только у первых
 */

public sealed class StaticReflectionTypeContext
{
    public required IComponentFactory ComponentFactory;
    public required IEntityManager EntityManager;
}

public interface IStaticReflectionType
{
    Type ObjectType { get; }

    public interface IMember
    {
        IStaticReflectionType ValueType { get; }
        string Name { get; }
        object? GetValueFromObject(object parent);
        void SetValueOnObject(object parent, object? value);
    }

    static IStaticReflectionType From(Type type, StaticReflectionTypeContext ctx)
    {
        if (type == typeof(EntityUid))
            return new StaticReflectionEntity(ctx);

        if (typeof(IComponent).IsAssignableFrom(type))
            return new StaticReflectionComponent(type, ctx);

        return new StaticReflectionAny(type, ctx);
    }


    IEnumerable<IStaticReflectionType.IMember> GetMembers();
    bool TryMember(string name, [NotNullWhen(true)] out IStaticReflectionType.IMember? member);
}

public abstract class StaticReflectionType<T> : IStaticReflectionType
{
    public abstract Type ObjectType { get; }
    public abstract class Member(/*StaticReflectionType<T> type*/) : IStaticReflectionType.IMember
    {
        // public IStaticReflectionType Type { get; } = type;
        public abstract IStaticReflectionType ValueType { get; }
        public abstract string Name { get; }

        public object? GetValueFromObject(object parent)
        {
            // TODO: Add error check for conversion
            return GetValue((T)parent);
        }

        public abstract object? GetValue(T obj);

        public void SetValueOnObject(object parent, object? value)
        {
            SetValue((T)parent, value);
        }

        public abstract void SetValue(T obj, object? value);
    }

    public abstract IEnumerable<IStaticReflectionType.IMember> GetMembers();

    public virtual bool TryMember(string name, [NotNullWhen(true)] out IStaticReflectionType.IMember? member)
    {
        return GetMembers()
            .TryFirstOrDefault(member => member.Name == name, out member);
    }
}

public sealed class StaticReflectionEntity(StaticReflectionTypeContext ctx) : StaticReflectionType<EntityUid>
{
    private sealed class ComponentMember(Type componentType, StaticReflectionTypeContext ctx) : Member
    {
        // TODO: StaticReflectionComponent : StaticReflectionAny for Owner and maybe some other properties
        public override IStaticReflectionType ValueType { get; } = IStaticReflectionType.From(componentType, ctx);
        public override string Name => ctx.ComponentFactory.GetComponentName(componentType);
        public override object? GetValue(EntityUid obj)
        {
            return ctx.EntityManager.TryGetComponent(obj, componentType, out var component) ? component : null;
        }

        public override void SetValue(EntityUid obj, object? value)
        {
            // TODO: On set component recreate component and copy all values from value component
        }
    }

    // TODO: metadata members?

    public override Type ObjectType { get; } = typeof(EntityUid);

    public override IEnumerable<IStaticReflectionType.IMember> GetMembers()
    {
        return ctx.ComponentFactory.AllRegisteredTypes
            .Select(comp => new ComponentMember(comp, ctx));
    }
}

public sealed class StaticReflectionComponent(Type type, StaticReflectionTypeContext ctx) : StaticReflectionType<IComponent>
{
    public override Type ObjectType { get; } = type;

    private static object? GetValueFromMember(MemberInfo any, object parent)
    {
        return any switch
        {
            PropertyInfo property => property.GetValue(parent),
            FieldInfo field => field.GetValue(parent),
            _ => null,
        };
    }

    private static Type GetMemberType(MemberInfo any)
    {
        return any switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object),
        };
    }

    private void SetValueForMember(MemberInfo any, object parent, object? value)
    {
        if (!ViewVariablesUtility.TryGetViewVariablesAccess(any, out var access) || access < VVAccess.ReadWrite)
        {
            // No access
            return;
        }

        if (any is PropertyInfo { CanWrite: true } property)
        {
            property.SetValue(parent, value);
        }
        else if (any is FieldInfo field)
        {
            field.SetValue(parent, value);
        }
        else { return; }

        if (parent is IComponent comp && type.HasCustomAttribute<Robust.Shared.GameStates.NetworkedComponentAttribute>())
        {
            ctx.EntityManager.Dirty(comp.Owner, comp);
        }
    }

    private sealed class ComponentMember(MemberInfo member, StaticReflectionComponent parentComponent, StaticReflectionTypeContext ctx) : Member
    {
        public override IStaticReflectionType ValueType { get; } = IStaticReflectionType.From(GetMemberType(member), ctx);
        public override string Name { get; } = member.Name;
        public override object? GetValue(IComponent obj)
        {
            return GetValueFromMember(member, obj);
        }

        public override void SetValue(IComponent obj, object? value)
        {
            parentComponent.SetValueForMember(member, obj, value);
        }
    }

    public override IEnumerable<IStaticReflectionType.IMember> GetMembers()
    {
        return ObjectType.GetMembers()
            .Where(member => ViewVariablesUtility.TryGetViewVariablesAccess(member, out var access) && access >= VVAccess.ReadOnly)
            .Select(member => new ComponentMember(member, this, ctx));
    }
}

public sealed class StaticReflectionAny(Type type, StaticReflectionTypeContext ctx) : StaticReflectionType<object>
{
    public override Type ObjectType { get; } = type;

    private static object? GetValueFromMember(MemberInfo any, object parent)
    {
        return any switch
        {
            PropertyInfo property => property.GetValue(parent),
            FieldInfo field => field.GetValue(parent),
            _ => null,
        };
    }

    private static Type GetMemberType(MemberInfo any)
    {
        return any switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object), // Fail safe, should never happen tho
        };
    }

    private static void SetValueForMember(MemberInfo any, object parent, object? value)
    {
        if (!ViewVariablesUtility.TryGetViewVariablesAccess(any, out var access) || access < VVAccess.ReadWrite)
        {
            // No access
            return;
        }

        if (any is PropertyInfo { CanWrite: true } property)
        {
            property.SetValue(parent, value);
            return;
        }

        if (any is FieldInfo field)
        {
            field.SetValue(parent, value);
            return;
        }

        // Do nothing
    }

    private sealed class AnyMember(MemberInfo member, StaticReflectionTypeContext ctx) : Member
    {
        public override IStaticReflectionType ValueType { get; } = IStaticReflectionType.From(GetMemberType(member), ctx);
        public override string Name { get; } = member.Name;
        public override object? GetValue(object obj)
        {
            return GetValueFromMember(member, obj);
        }

        public override void SetValue(object obj, object? value)
        {
            SetValueForMember(member, obj, value);
        }
    }

    public override IEnumerable<IStaticReflectionType.IMember> GetMembers()
    {
        return ObjectType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(member => ViewVariablesUtility.TryGetViewVariablesAccess(member, out var access) && access >= VVAccess.ReadOnly)
            .Select(member => new AnyMember(member, ctx));
    }
}

public record struct ReflectionMemberPath<TIn>(IEnumerable<IStaticReflectionType.IMember> Path)
{
    private static bool IsPathValidRune(Rune rune)
    {
        return Rune.IsLetterOrDigit(rune) || rune.IsBmp && "/-_".Contains((char) rune.Value);
    }

    public static string? ReadFullPath(ParserContext ctx)
    {
        return ctx.GetWord(IsPathValidRune);
    }

    public static bool TryGetOutputPath(string path, StaticReflectionTypeContext ctx, [NotNullWhen(true)] out Type? type)
    {
        if (TryParse(path, ctx, out var result, out var last) && string.IsNullOrEmpty(last) && path.Length > 0)
        {
            type = result.Path.Last().ValueType.ObjectType;
            return true;
        }

        type = null;
        return false;
    }

    public static bool TryParse(string path, StaticReflectionTypeContext ctx, out ReflectionMemberPath<TIn> result, out string? last)
    {
        var baseType = IStaticReflectionType.From(typeof(TIn), ctx);

        var parts = path.Split("/");
        var currentType = baseType;

        List<IStaticReflectionType.IMember> members = new();
        for (var pathI = 0; pathI < parts.Length; pathI++)
        {
            var part = parts[pathI];
            if (!currentType.TryMember(part, out var member))
            {
                result = new(members);
                last = part;
                // If it was last element then we successfully parsed
                // up to latest element
                return pathI == parts.Length - 1;
            }

            members.Add(member);
            currentType = member.ValueType;
        }

        result = new(members);
        last = null;
        return true;
    }
}

public sealed class ReflectionMemberPathTypeParser<TIn> : TypeParser<ReflectionMemberPath<TIn>>
{
    private StaticReflectionTypeContext? _context;
    private StaticReflectionTypeContext Context => _context ??= new()
    {
        ComponentFactory = _componentFactory,
        EntityManager = _entityManager
    };

    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override bool TryParse(ParserContext ctx, out ReflectionMemberPath<TIn> result)
    {
        var word = ReflectionMemberPath<TIn>.ReadFullPath(ctx);
        if (word is null)
        {
            result = new([]);
            return false;
        }

        if (ReflectionMemberPath<TIn>.TryParse(word, Context, out result, out var last) && last is null)
        {
            return true;
        }

        result = new([]);
        return false;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var word = ReflectionMemberPath<TIn>.ReadFullPath(ctx) ?? "";

        if (!ReflectionMemberPath<TIn>.TryParse(word, Context, out var result, out var last))
        {
            return CompletionResult.Empty;
        }
        if (last is null)
        {
            return CompletionResult.FromOptions([new CompletionOption(word + "/", null, CompletionOptionFlags.PartialCompletion)]);
        }

        var parentType = result.Path.LastOrDefault()?.ValueType
            ?? IStaticReflectionType.From(typeof(TIn), Context);

        var prefix = string.Join("/", result.Path.Select(m => m.Name)) + (result.Path.Any() ? "/" : "");

        var submembers = parentType.GetMembers()
            .Select(member => member.Name)
            .Where(member => member.StartsWith(last))
            .Select(member => new CompletionOption(prefix + member, null, CompletionOptionFlags.PartialCompletion));
        return CompletionResult.FromOptions(submembers);
    }
}

[ToolshedCommand(Name = "refl"), AdminCommand(AdminFlags.VarEdit)]
public sealed class ReflectionCommand : ToolshedCommand
{
    // TOut deduction based on what command returns
    public override Type[] TypeParameterParsers { get; } = [/*TOut: */typeof(ReflectionCommandOutTypeParser)];

    //
    [CommandImplementation("read"), TakesPipedTypeAsGeneric]
    public TOut? Read<TOut, TIn>(
        IInvocationContext ctx,
        [PipedArgument] TIn input,
        ReflectionMemberPath<TIn> path
    )
    {
        object? obj = input;

        foreach (var member in path.Path)
        {
            if (obj is null) { return default; }
            obj = member.GetValueFromObject(obj);
        }

        if (obj is TOut result)
            return result;

        return default;
    }

    [CommandImplementation("read"), TakesPipedTypeAsGeneric]
    public IEnumerable<TOut?> Read<TOut, TIn>(IInvocationContext ctx, [PipedArgument] IEnumerable<TIn> input, ReflectionMemberPath<TIn> path)
    {
        foreach (var item in input)
        {
            yield return Read<TOut, TIn>(ctx, item, path);
        }
    }

    [CommandImplementation("write"), TakesPipedTypeAsGeneric]
    public TIn? Write<TOut, TIn>(IInvocationContext ctx, [PipedArgument] TIn input, ReflectionMemberPath<TIn> path, TOut arg)
    {
        if (input is null)
            return default;

        object? preLast = input;

        var baked = path.Path.ToArray();

        if (baked.Length <= 0)
            return input;

        foreach (var member in baked.SkipLast(1))
        {
            if (preLast is null)
                return input;
            preLast = member.GetValueFromObject(preLast);
        }

        if (preLast is null)
            return input;

        baked.Last().SetValueOnObject(preLast, arg);

        return input;
    }

    [CommandImplementation("write"), TakesPipedTypeAsGeneric]
    public IEnumerable<TIn?> Write<TOut, TIn>(IInvocationContext ctx, [PipedArgument] IEnumerable<TIn> input, ReflectionMemberPath<TIn> path, TOut arg)
    {
        foreach (var item in input)
        {
            yield return Write(ctx, item, path, arg);
        }
    }
}

public sealed class ReflectionCommandOutTypeParser : CustomTypeParser<Type>
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private StaticReflectionTypeContext? _context;
    private StaticReflectionTypeContext Context => _context ??= new()
    {
        ComponentFactory = _componentFactory,
        EntityManager = _entityManager
    };

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        var save = ctx.Save();
        var word = ReflectionMemberPath<object>.ReadFullPath(ctx);
        if (word is null)
        {
            result = typeof(object);
            ctx.Restore(save);
            return true;
        }

        // TIn
        var pipedType = ctx.Bundle.PipedType;

        if (pipedType is null)
        {
            result = typeof(object);
            ctx.Restore(save);
            return true;
        }

        if (pipedType.IsGenericType && pipedType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            pipedType = pipedType.GenericTypeArguments[0];

        // WARNING: DIRTY STUFF
        var constructedType = typeof(ReflectionMemberPath<>).MakeGenericType(pipedType);
        var method = constructedType.GetMethod("TryGetOutputPath", BindingFlags.Public | BindingFlags.Static);
        if (method != null)
        {
            var args = new object?[] { word, Context, null };
            if ((bool)method.Invoke(null, args)!)
            {
                result = (Type)args[2]!;
                ctx.Restore(save);
                return true;
            }
        }
        // END OF WARNING

        result = typeof(object);
        ctx.Restore(save);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return ctx.Completions;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server._Maid.Toolshed.Commands;

/// <summary>
/// Path argument used when piping an EntityUid (format: "VvComponentName/FieldName" or "FieldName").
/// </summary>
public readonly record struct RlComponentFieldPath(string Path)
{
    public override string ToString() => Path;
}

/// <summary>
/// Path argument used when piping a Component directly (format: "FieldName").
/// </summary>
public readonly record struct RlFieldPath(string Path)
{
    public override string ToString() => Path;
}

public sealed class RlFieldPathTypeParser : TypeParser<RlFieldPath>
{
    [Dependency] private readonly IViewVariablesManager _vvm = default!;

    private static bool IsPath(Rune c)
    {
        return Rune.IsLetterOrDigit(c);
    }

    public override bool TryParse(ParserContext ctx, out RlFieldPath result)
    {
        ctx.ConsumeWhitespace();
        string? pathStr = null;
        result = new();

        if (ctx.PeekRune() == new Rune('"'))
            Toolshed.TryParse(ctx, out pathStr);
        else
            pathStr = ctx.GetWord(IsPath);

        if (pathStr == null)
            return false;

        result = new(pathStr);
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        ctx.ConsumeWhitespace();
        if (ctx.PeekRune() == new Rune('"'))
        {
            return CompletionResult.FromHintOptions(["\""], "close string");
        }

        var word = ctx.GetWord(IsPath);
        word ??= "";

        var type = ctx.Bundle.PipedType;
        if (type is null)
            return CompletionResult.Empty;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            type = type.GenericTypeArguments[0];

        var members = GetVvMembers(type)
            .Where(member => member.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .Select(member => new CompletionOption(member.Name));

        return CompletionResult.FromOptions(members);
    }

    public const BindingFlags DefaultBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static IEnumerable<MemberInfo> GetVvMembers(Type type, BindingFlags bindings = DefaultBindings, VVListPathOptions? options = null)
    {
        options ??= new VVListPathOptions();
        var minAccess = options.Value.MinimumAccess;
        return type.GetMembers(bindings)
            .Where(m => ViewVariablesUtility.TryGetViewVariablesAccess(m, out var memberAccess) && memberAccess >= minAccess);
    }
}

public sealed class RlComponentFieldPathTypeParser : TypeParser<RlComponentFieldPath>
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IViewVariablesManager _vvm = default!;

    private static bool IsPath(Rune c)
    {
        return Rune.IsLetterOrDigit(c) || c == new Rune('/');
    }

    public override bool TryParse(ParserContext ctx, out RlComponentFieldPath result)
    {
        ctx.ConsumeWhitespace();
        string? pathStr = null;
        result = new();

        if (ctx.PeekRune() == new Rune('"'))
            Toolshed.TryParse(ctx, out pathStr);
        else
            pathStr = ctx.GetWord(IsPath);

        if (pathStr == null)
            return false;

        result = new(pathStr);
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        ctx.ConsumeWhitespace();
        if (ctx.PeekRune() == new Rune('"'))
        {
            return CompletionResult.FromHintOptions(["\""], "close string");
        }

        var word = ctx.GetWord(IsPath);
        word ??= "";

        if (RlCommand.TrySplitComponentFieldPath(word, out var component, out var field))
        {
            if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => _factory.GetComponentName(comp) == component, out var type))
            {
                return CompletionResult.FromHint($"Unknown type {component}");
            }

            var members = RlFieldPathTypeParser.GetVvMembers(type)
                .Where(member => member.Name.StartsWith(field, StringComparison.OrdinalIgnoreCase))
                .Select(member => new CompletionOption($"{component}/{member.Name}"));

            return CompletionResult.FromOptions(members);
        }
        else
        {
            var compPartial = word.EndsWith('/') ? word[..^1] : word;
            var types = _factory.AllRegisteredTypes
                .Select(type => _factory.GetComponentName(type))
                .Where(name => name.StartsWith(compPartial, StringComparison.OrdinalIgnoreCase))
                .Select(name => new CompletionOption($"{name}/", null, CompletionOptionFlags.PartialCompletion));

            return CompletionResult.FromOptions(types);
        }
    }
}

[ToolshedCommand(Name = "reflect"), AdminCommand(AdminFlags.VarEdit)]
public sealed class RlCommand : ToolshedCommand
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private static readonly Type[] Parsers = [typeof(RlOutputParser)];
    public override Type[] TypeParameterParsers => Parsers;

    [CommandImplementation]
    public TOut? Read<TOut>(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        RlComponentFieldPath path)
    {
        return EvaluateRl<TOut>(ctx, input, path.Path);
    }

    [CommandImplementation]
    public IEnumerable<TOut?> Read<TOut>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        RlComponentFieldPath path)
    {
        foreach (var ent in input)
        {
            if (ctx.HasErrors)
                yield break;

            yield return Read<TOut>(ctx, ent, path);
        }
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? Read<TOut, TComponent>(
        IInvocationContext ctx,
        [PipedArgument] TComponent input,
        RlFieldPath path)
        where TComponent : class, IComponent
    {
        return EvaluateRl<TOut>(ctx, input, path.Path);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut?> Read<TOut, TComponent>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<TComponent> input,
        RlFieldPath path)
        where TComponent : class, IComponent
    {
        foreach (var comp in input)
        {
            if (ctx.HasErrors)
                yield break;

            yield return Read<TOut, TComponent>(ctx, comp, path);
        }
    }

    internal static bool TrySplitComponentFieldPath(string path, [NotNullWhen(true)] out string? componentName, [NotNullWhen(true)] out string? fieldName)
    {
        componentName = null;
        fieldName = null;

        var parts = path.Split('/', 2);
        if (parts.Length != 2)
            return false;

        componentName = parts[0];
        fieldName = parts[1];
        return true;
    }

    private TOut? EvaluateRl<TOut>(IInvocationContext ctx, EntityUid entity, string fullPath)
    {
        if (!TrySplitComponentFieldPath(fullPath, out var componentName, out var fieldName))
        {
            ctx.ReportError(new InvalidRlComponentError(fullPath));
            return default;
        }

        return EvaluateRl<TOut>(ctx, entity, componentName, fieldName);
    }

    private TOut? EvaluateRl<TOut>(IInvocationContext ctx, EntityUid entity, string componentName, string fieldName)
    {
        if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => _factory.GetComponentName(comp) == componentName, out var componentType))
        {
            ctx.ReportError(new InvalidRlComponentError(componentName));
            return default;
        }

        if (!_entityManager.TryGetComponent(entity, componentType, out var component))
        {
            ctx.ReportError(new InvalidRlComponentError(componentName));
            return default;
        }

        return EvaluateRl<TOut>(ctx, component, fieldName);
    }

    private static object? GetValue(MemberInfo member, object instance)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => throw new ArgumentOutOfRangeException(nameof(member))
        };
    }

    private TOut? EvaluateRl<TOut>(IInvocationContext ctx, IComponent component, string fieldName)
    {
        var members = RlFieldPathTypeParser.GetVvMembers(component.GetType());

        if (!members.TryFirstOrDefault(m => m.Name == fieldName, out var member))
        {
            ctx.ReportError(new InvalidRlFieldError(fieldName));
            return default;
        }

        if (GetValue(member, component) is TOut outVal)
            return outVal;

        return default;
    }

    public sealed class RlOutputParser : CustomTypeParser<Type>
    {
        [Dependency] private readonly IComponentFactory _factory = default!;

        private static Type GetValueType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => throw new ArgumentOutOfRangeException(nameof(member)),
            };
        }

        public override bool ShowTypeArgSignature => false;

        public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
        {
            result = null;
            var save = ctx.Save();

            var type = ctx.Bundle.PipedType;
            if (type is null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                type = type.GenericTypeArguments[0];

            if (typeof(IComponent).IsAssignableFrom(type))
            {
                if (Toolshed.TryParse(ctx, out RlFieldPath fieldPath))
                {
                    var members = RlFieldPathTypeParser.GetVvMembers(type);

                    if (members.TryFirstOrDefault(m => m.Name == fieldPath.Path, out var member))
                    {
                        result = GetValueType(member);
                        ctx.Restore(save);
                        return true;
                    }

                    if (ctx.GenerateCompletions)
                    {
                        var prefixMatch = members.FirstOrDefault(m => m.Name.StartsWith(fieldPath.Path, StringComparison.OrdinalIgnoreCase));
                        result = prefixMatch != null ? GetValueType(prefixMatch) : typeof(object);
                        ctx.Restore(save);
                        return true;
                    }
                }
            }
            else if (Toolshed.TryParse(ctx, out RlComponentFieldPath compPath))
            {
                if (!RlCommand.TrySplitComponentFieldPath(compPath.Path, out var componentName, out var fieldName))
                {
                    if (ctx.GenerateCompletions)
                    {
                        result = typeof(object);
                        ctx.Restore(save);
                        return true;
                    }
                    ctx.Restore(save);
                    return false;
                }

                if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => _factory.GetComponentName(comp) == componentName, out var componentType))
                {
                    if (ctx.GenerateCompletions)
                    {
                        result = typeof(object);
                        ctx.Restore(save);
                        return true;
                    }
                    ctx.Restore(save);
                    return false;
                }

                var members = RlFieldPathTypeParser.GetVvMembers(componentType);

                if (!members.TryFirstOrDefault(m => m.Name == fieldName, out var member))
                {
                    if (ctx.GenerateCompletions)
                    {
                        var prefixMatch = members.FirstOrDefault(m => m.Name.StartsWith(fieldName, StringComparison.OrdinalIgnoreCase));
                        result = prefixMatch != null ? GetValueType(prefixMatch) : typeof(object);
                        ctx.Restore(save);
                        return true;
                    }
                    ctx.Restore(save);
                    return false;
                }

                result = GetValueType(member);
                ctx.Restore(save);
                return true;
            }

            ctx.Restore(save);
            return false;
        }

        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            return ctx.Completions;
        }
    }
}

public record struct InvalidRlComponentError(string Name) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Component '{Name}' not found.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct InvalidRlFieldError(string Name) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Field '{Name}' not found.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

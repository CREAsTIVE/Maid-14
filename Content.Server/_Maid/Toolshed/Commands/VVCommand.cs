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

    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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

        var options = new VVListPathOptions();

        var members = type.GetMembers(MembersBindings)
            .Where(member => !ViewVariablesUtility.TryGetViewVariablesAccess(member, out var memberAccess) && options.MinimumAccess >= memberAccess) // Limit by VV access
            .Where(member => member.Name.StartsWith(word))
            .Select(member => new CompletionOption(member.Name));

        return CompletionResult.FromOptions(members);
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

    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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

        var wordParts = word.Split("/", 2);

        if (wordParts.Length <= 1 && !word.Contains('/'))
        {
            var component = wordParts.Length >= 1 ? wordParts[0] : "";
            var types = _factory.AllRegisteredTypes
                .Select(type => $"{new CompletionOption(type.Name)}/");

            return CompletionResult.FromOptions(types);
        }
        else
        {
            var component = wordParts[0];
            if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => comp.Name == component, out var type))
            {
                return CompletionResult.FromHint($"Unknown type {component}");
            }

            var field = wordParts[1]; // Should exist atp

            var options = new VVListPathOptions();

            var members = type.GetMembers(MembersBindings)
                .Where(member => !ViewVariablesUtility.TryGetViewVariablesAccess(member, out var memberAccess) && options.MinimumAccess >= memberAccess) // Limit by VV access
                .Where(member => member.Name.StartsWith(word))
                .Select(member => new CompletionOption(member.Name));

            return CompletionResult.FromOptions(members);
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

    private const BindingFlags MembersBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
            // if (ctx.HasErrors)
            //     yield break;

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
            // if (ctx.HasErrors)
            //     yield break;

            yield return Read<TOut, TComponent>(ctx, comp, path);
        }
    }

    private TOut? EvaluateRl<TOut>(IInvocationContext ctx, EntityUid entity, string fullPath)
    {
        var parts = fullPath.Split('/', 2);
        if (parts.Length != 2)
        {
            ctx.ReportError(new InvalidRlComponentError(fullPath));
        }

        return EvaluateRl<TOut>(ctx, entity, parts[0], parts[1]);
    }

    private TOut? EvaluateRl<TOut>(IInvocationContext ctx, EntityUid entity, string componentName, string fieldName)
    {
        if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => comp.Name == componentName, out var componentType))
        {
            ctx.ReportError(new InvalidRlComponentError(componentName));
            return default;
        }

        if (!_entityManager.TryGetComponent(entity, componentType, out var component))
        {
            ctx.ReportError(new InvalidRlComponentError(componentName)); // separate error? ignore?
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
        var options = new VVListPathOptions();

        // TODO move into separate method
        var members = component.GetType()
            .GetMembers(MembersBindings)
            .Where(m => !ViewVariablesUtility.TryGetViewVariablesAccess(m, out var memberAccess) && options.MinimumAccess >= memberAccess);

        // TODO: optimize for iterable, get type and member once
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
        [Dependency] private readonly IViewVariablesManager _vvm = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

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

                }
            }
            else if (Toolshed.TryParse(ctx, out RlComponentFieldPath compPath))
            {
                // TODO: More shared logic, probably should be in one method, something like "(string component, string field) getParts"
                var parts = compPath.Path.Split('/', 2);
                if (parts.Length != 2)
                    return false;

                var componentName = parts[0];
                var fieldName = parts[1];

                if (!_factory.AllRegisteredTypes.TryFirstOrDefault(comp => comp.Name == componentName, out var componentType))
                    return false;

                var options = new VVListPathOptions();

                var members = componentType
                    .GetMembers(MembersBindings)
                    .Where(m => !ViewVariablesUtility.TryGetViewVariablesAccess(m, out var memberAccess) && options.MinimumAccess >= memberAccess);

                // TODO: optimize for iterable, get type and member once
                if (!members.TryFirstOrDefault(m => m.Name == fieldName, out var member))
                    return false;

                result = GetValueType(member);
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

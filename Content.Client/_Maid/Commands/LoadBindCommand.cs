using Content.Shared.Administration;
using Robust.Client.Input;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using System;
using System.Linq;

namespace Content.Client._Maid.Commands
{
    [AnyCommand]
    public sealed class LoadBindCommand : LocalizedCommands
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IResourceManager _resourceMan = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;

        public override string Command => "ldbind";
        public override string Description => "Loads user keybindings from keybinds.yml";
        public override string Help => "ldbind";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var path = new ResPath("/keybinds.yml");
            if (!_resourceMan.UserData.Exists(path))
            {
                shell.WriteError("No custom keybindings file found in UserData.");
                return;
            }

            try
            {
                using var reader = _resourceMan.UserData.OpenText(path);
                var documents = DataNodeParser.ParseYamlStream(reader).First();
                var mapping = (MappingDataNode) documents.Root;

                _inputManager.ResetAllBindings();

                if (mapping.TryGet("binds", out var baseKeyRegsNode))
                {
                    var baseKeyRegs = _serialization.Read<KeyBindingRegistration[]>(baseKeyRegsNode, notNullableOverride: true);

                    foreach (var reg in baseKeyRegs)
                    {
                        var invalid = false;

                        if (reg.Type != KeyBindingType.Command && !_inputManager.NetworkBindMap.FunctionExists(reg.Function.FunctionName))
                        {
                            invalid = true;
                        }

                        foreach (var existing in _inputManager.GetKeyBindings(reg.Function).ToArray())
                        {
                            _inputManager.RemoveBinding(existing, markModified: false);
                        }

                        _inputManager.RegisterBinding(reg, markModified: true, invalid: invalid);
                    }
                }

                if (mapping.TryGet("leaveEmpty", out var node))
                {
                    var leaveEmpty = _serialization.Read<BoundKeyFunction[]>(node, notNullableOverride: true);

                    foreach (var bind in leaveEmpty)
                    {
                        foreach (var existing in _inputManager.GetKeyBindings(bind).ToArray())
                        {
                            _inputManager.RemoveBinding(existing, markModified: true);
                        }
                    }
                }

                shell.WriteLine("Successfully loaded keybindings from UserData.");
            }
            catch (Exception e)
            {
                shell.WriteError($"Failed to load keybindings: {e.Message}");
            }
        }
    }
}

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Chemistry.Dispenser
{
    /// <summary>
    /// Is simply a list of reagents defined in yaml. This can then be set as a
    /// reagent despenser <c>pack</c> value (also in yaml),
    /// to define which reagents it's able to dispense. Based off of how vending
    /// machines define their inventory.
    /// </summary>
    [Prototype("reagentDispenserInventory")]
    [DataDefinition]
    public sealed partial class ReagentDispenserInventoryPrototype : IPrototype
    {
        [ViewVariables, IdDataField]
        public string ID { get; private set; } = default!;

        // TODO use ReagentId
        [DataField("inventory", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
        public List<string> Inventory = new();
    }
}

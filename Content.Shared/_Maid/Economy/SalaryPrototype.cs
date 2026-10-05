using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Maid.Economy;

/// <summary>
/// Salaries paid to crew members' bank accounts, per job.
/// </summary>
[Prototype]
public sealed partial class SalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, int> Salaries = new();
}

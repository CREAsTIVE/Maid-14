using System.Collections.Generic;

namespace Content.Server._Maid.AdaptiveGameMode;

/// <summary>
/// Interface implemented by systems that dynamically calculate adaptive game mode score contributions.
/// </summary>
public interface IAdaptiveBalanceInfoProvider
{
    /// <summary>
    /// Gets the balance score information for this system.
    /// </summary>
    IEnumerable<AdaptiveBalanceInfo> GetBalanceInfo();
}

/// <summary>
/// Information about a balance contribution (PVP and Chaos scores) for a given entity and condition.
/// </summary>
public struct AdaptiveBalanceInfo
{
    public string Entity;
    public string Condition;
    public float? PvpFrom;
    public float? PvpTo;
    public float? PvpDuration;
    public float? ChaosFrom;
    public float? ChaosTo;
    public float? ChaosDuration;

    public AdaptiveBalanceInfo(
        string entity,
        string condition,
        float? pvpFrom = null,
        float? pvpTo = null,
        float? pvpDuration = null,
        float? chaosFrom = null,
        float? chaosTo = null,
        float? chaosDuration = null)
    {
        Entity = entity;
        Condition = condition;
        PvpFrom = pvpFrom;
        PvpTo = pvpTo;
        PvpDuration = pvpDuration;
        ChaosFrom = chaosFrom;
        ChaosTo = chaosTo;
        ChaosDuration = chaosDuration;
    }
}

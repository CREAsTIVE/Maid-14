using Content.Server._Maid.AdaptiveGameMode;
using Content.Server._Maid.AdaptiveGameMode.ScoreCounters;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System;

namespace Content.Server._Maid.AdaptiveGameMode.ScoreCounters.PlayerFree;

/// <summary>
/// Component that provides a score contribution to the Adaptive game mode while a player is active, alive, uncuffed, and unbuckled.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AdaptiveScorePlayerFreeComponent : Component, IAdaptiveScoreComponent
{
    [DataField(required: true)]
    public ScoreSlope ChaosScore { get; set; } = new();

    [DataField(required: true)]
    public ScoreSlope CombatScore { get; set; } = new();
    [DataField]
    public bool OnStation = true;

    [DataField, ViewVariables]
    [AutoPausedField]
    public TimeSpan ComponentCreated;
}

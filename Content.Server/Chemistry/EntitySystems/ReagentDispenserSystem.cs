// SPDX-FileCopyrightText: 2021 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2021 Clyybber <darkmine956@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 Ygg01 <y.laughing.man.y@gmail.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 ElectroJr <leonsfriedrich@gmail.com>
// SPDX-FileCopyrightText: 2023 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 deltanedas <deltanedas@laptop>
// SPDX-FileCopyrightText: 2023 deltanedas <user@zenith>
// SPDX-FileCopyrightText: 2024 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2024 AWF <you@example.com>
// SPDX-FileCopyrightText: 2024 Brandon Li <48413902+aspiringLich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Cojoke <83733158+Cojoke-dot@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 GitHubUser53123 <110841413+GitHubUser53123@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Jake Huxell <JakeHuxell@pm.me>
// SPDX-FileCopyrightText: 2024 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2024 Kira Bridgeton <161087999+Verbalase@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Charges;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using System.Diagnostics;
using System.Linq;

// Maid START PR №21

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="ReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly ChargesSystem _chargesSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState, after: [typeof(SharedStorageSystem)]);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);

            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit, before: new[] { typeof(ItemSlotsSystem) });
        }

        private float _updateTimer = 0f;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;
            if (_updateTimer >= 1f)
            {
                _updateTimer = 0f;
                var query = EntityQueryEnumerator<ReagentDispenserComponent, UserInterfaceComponent, LimitedChargesComponent>();

                while (query.MoveNext(out var uid, out var reagentDispenserComp, out var uiComp, out var charges))
                {
                    UpdateUiState(uid, reagentDispenserComp);
                }
            }
        }

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent.Owner, ent.Comp);
        }

        private void UpdateUiState(EntityUid reagentDispenserEnt, ReagentDispenserComponent reagentDispenserComp)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenserEnt, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenserEnt, reagentDispenserComp).ToList(); // TODO: Another copy, optimize

            int? charges = null;
            if (TryComp(reagentDispenserEnt, out LimitedChargesComponent? comp))
            {
                charges = _chargesSystem.GetCurrentCharges(reagentDispenserEnt);
            }

            var state = new ReagentDispenserBoundUserInterfaceState(
                outputContainerInfo,
                GetNetEntity(outputContainer),
                inventory,
                reagentDispenserComp.DispenseAmount,
                charges
            );

            _userInterfaceSystem.SetUiState(reagentDispenserEnt, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        private IEnumerable<(ReagentId reagent, int cost)> GetInventory(EntityUid dispenserEnt, ReagentDispenserComponent dispenserComp)
        {
            var inventory = new Dictionary<string, int>();

            // Collect reagents from items provided by SolutionContainerManager; TODO: include parent
            if (TryComp<StorageFillComponent>(dispenserEnt, out var storageFillComp))
            {
                foreach (var item in storageFillComp.Contents)
                {
                    if (!_prototypeManager.TryIndex(item.PrototypeId, out EntityPrototype? entityPrototype))
                        continue;

                    if (!entityPrototype.Components.TryGetValue("SolutionContainerManager", out var data))
                        continue;

                    if (!data.Mapping.TryGet<MappingDataNode>("solutions", out var solutions))
                        continue;

                    foreach (var maybeSolution in solutions.Values)
                    {
                        if (!(maybeSolution is MappingDataNode solution))
                            continue;

                        if (!solution.TryGet<SequenceDataNode>("reagents", out var reagents))
                            continue;

                        foreach (var maybeReagent in reagents)
                        {
                            if (!(maybeReagent is MappingDataNode reagent))
                                continue;

                            if (!reagent.TryGet<ValueDataNode>("ReagentId", out var reagentId))
                                continue;

                            // Finded!
                            inventory[reagentId.Value] = dispenserComp.DefaultReagentCost;
                        }
                    }
                }

            }

            // Collect reagents from packs:

            if (
                 dispenserComp.PackPrototypeId is not null
                && _prototypeManager.TryIndex(dispenserComp.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype)
            )
            {
                foreach (var reagentId in packPrototype.Inventory)
                {
                    inventory[reagentId.Key] = reagentId.Value;
                }
            }

            if (
                HasComp<EmaggedComponent>(dispenserEnt)
                &&  dispenserComp.EmagPackPrototypeId is not null
                && _prototypeManager.TryIndex(dispenserComp.EmagPackPrototypeId, out ReagentDispenserInventoryPrototype? emagPackPrototype)
            )
            {
                foreach (var reagentId in emagPackPrototype.Inventory)
                {
                    inventory[reagentId.Key] = reagentId.Value;
                }
            }

            return inventory.Select(pair => (new ReagentId(pair.Key, null), pair.Value));
        }

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser.Owner, reagentDispenser.Comp);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            var reagentId = message.ReagentId;
            var reagentDispenserComp = reagentDispenser.Comp;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            // Check if we even can dispense that
            var inventory = GetInventory(reagentDispenser.Owner, reagentDispenser.Comp);
            if (!inventory.TryFirstOrNull(data => data.reagent == reagentId, out var inventoryElement))
                return;

            var singleCost = inventoryElement?.cost ?? 1; // "?? 1" should be unreachable

            float requestedDispenseAmount = (int) reagentDispenserComp.DispenseAmount;

            // How much would be dispensed (less then required, if there not enough charges)
            float totalDispenseAmount = requestedDispenseAmount;

            bool hasCharges;
            if (hasCharges = TryComp(reagentDispenser.Owner, out LimitedChargesComponent? charges))
            {
                var avalaibleCharges = _chargesSystem.GetCurrentCharges(reagentDispenser.Owner);

                totalDispenseAmount = requestedDispenseAmount;
                if (requestedDispenseAmount * singleCost > avalaibleCharges)
                    totalDispenseAmount = avalaibleCharges / singleCost;
            }

            // sollution is not null because [NotNullWhen(true)]
            _solutionContainerSystem.TryAddReagent(solution ?? throw new UnreachableException(), reagentId.Prototype, totalDispenseAmount, out var dispensed);

            if (hasCharges && dispensed > float.Epsilon)
            {
                _chargesSystem.AddCharges(reagentDispenser.Owner, -((int) (MathF.Ceiling((float) dispensed * singleCost) + float.Epsilon))); // Ceil dispensed amount up (should be safe)
            }

            UpdateUiState(reagentDispenser.Owner, reagentDispenser.Comp);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser.Owner, reagentDispenser.Comp);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Initializes the beaker slot
        /// </summary>
        private void OnMapInit(Entity<ReagentDispenserComponent> ent, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(ent.Owner, SharedReagentDispenser.OutputSlotName, ent.Comp.BeakerSlot);
        }
    }
}

// Maid END

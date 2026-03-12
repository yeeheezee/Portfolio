using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardBrawl.Core;
using WizardBrawl.Enemy;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

public class BossAiPlayModeTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<UnityEngine.Object> _trackedObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _trackedObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object tracked = _trackedObjects[i];
            if (tracked == null)
            {
                continue;
            }

            if (tracked is GameObject gameObject)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(tracked);
        }

        _trackedObjects.Clear();
    }

    [UnityTest]
    public IEnumerator BossAi_Loop_SelectsAndCastsSpell()
    {
        using BossAiTestRig rig = CreateRig(includePhase2Entry: false);

        bool sawSelectLog = false;
        bool sawCastLog = false;
        Application.LogCallback logHandler = (condition, _, type) =>
        {
            if (type != LogType.Log)
            {
                return;
            }

            if (condition.Contains("[BossAI] select spell=Test Spell"))
            {
                sawSelectLog = true;
            }

            if (condition.Contains("[BossCast] fire spell=Test Spell"))
            {
                sawCastLog = true;
            }
        };

        Application.logMessageReceived += logHandler;

        try
        {
            yield return WaitUntil(() => sawSelectLog && sawCastLog, 2f);
        }
        finally
        {
            Application.logMessageReceived -= logHandler;
        }

        Assert.That(sawSelectLog, Is.True, "BossAI did not select a spell during the attack loop.");
        Assert.That(sawCastLog, Is.True, "BossAI did not cast a spell during the attack loop.");
    }

    [UnityTest]
    public IEnumerator SpellPoolCache_RespectsPhaseGateTransitions()
    {
        using BossAiTestRig rig = CreateRig(includePhase2Entry: true);

        yield return null;

        InvokePrivate(rig.BossAi, "RebuildPhaseCandidateCacheIfNeeded");
        List<BossSpellEntry> phase1Candidates = GetCachedCandidates(rig.BossAi);

        Assert.That(phase1Candidates, Has.Count.EqualTo(2));
        Assert.That(phase1Candidates.Exists(entry => entry.PhaseGate == BossPhaseGate.Phase2Only), Is.False);

        rig.Health.TakeDamage(60f);
        yield return null;

        InvokePrivate(rig.BossAi, "RebuildPhaseCandidateCacheIfNeeded");
        List<BossSpellEntry> phase2Candidates = GetCachedCandidates(rig.BossAi);

        Assert.That(rig.BossAi.CurrentPhase, Is.EqualTo(BossCombatPhase.Phase2));
        Assert.That(phase2Candidates, Has.Count.EqualTo(2));
        Assert.That(phase2Candidates.Exists(entry => entry.PhaseGate == BossPhaseGate.Phase1Only), Is.False);
        Assert.That(phase2Candidates.Exists(entry => entry.PhaseGate == BossPhaseGate.Phase2Only), Is.True);
    }

    [UnityTest]
    public IEnumerator Phase2Transition_FiresOnlyOnce()
    {
        using BossAiTestRig rig = CreateRig(includePhase2Entry: true);

        int phase2TransitionCount = 0;
        rig.BossAi.OnPhaseChanged += phase =>
        {
            if (phase == BossCombatPhase.Phase2)
            {
                phase2TransitionCount++;
            }
        };

        yield return null;

        rig.Health.TakeDamage(60f);
        yield return null;

        rig.Health.Heal(30f);
        rig.Health.TakeDamage(10f);
        yield return null;

        Assert.That(rig.BossAi.CurrentPhase, Is.EqualTo(BossCombatPhase.Phase2));
        Assert.That(phase2TransitionCount, Is.EqualTo(1));
    }

    private BossAiTestRig CreateRig(bool includePhase2Entry)
    {
        GameObject player = Track(new GameObject("Player"));
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, 10f);

        GameObject boss = Track(new GameObject("Boss"));
        boss.transform.position = Vector3.zero;

        Mana mana = TrackComponent(boss.AddComponent<Mana>());
        Health health = TrackComponent(boss.AddComponent<Health>());
        BossAttackCaster caster = TrackComponent(boss.AddComponent<BossAttackCaster>());
        BossAI bossAi = TrackComponent(boss.GetComponent<BossAI>());

        DummyMagicData allPhasesSpell = CreateMagicData("Test Spell");
        DummyMagicData phase1Spell = CreateMagicData("Phase1 Spell");
        DummyMagicData phase2Spell = CreateMagicData("Phase2 Spell");

        BossSpellPoolTable poolTable = Track(CreateSpellPoolTable(includePhase2Entry, allPhasesSpell, phase1Spell, phase2Spell));
        BossStats stats = Track(CreateBossStats());

        SetPrivateField(bossAi, "_stats", stats);
        SetPrivateField(bossAi, "_spellPoolTable", poolTable);
        SetPrivateField(bossAi, "_attackCaster", caster);
        SetPrivateField(bossAi, "_moveWindowDuration", 0.05f);
        SetPrivateField(bossAi, "_attackWindowDuration", 0.25f);
        SetPrivateField(bossAi, "_maxCastsPerAttackWindow", 1);
        SetPrivateField(bossAi, "_maxNoFireWaitSeconds", 2f);
        SetPrivateField(bossAi, "_maxStuckWaitSeconds", 2f);

        return new BossAiTestRig(bossAi, health, mana);
    }

    private BossSpellPoolTable CreateSpellPoolTable(bool includePhase2Entry, MagicData allPhasesSpell, MagicData phase1Spell, MagicData phase2Spell)
    {
        BossSpellPoolTable table = ScriptableObject.CreateInstance<BossSpellPoolTable>();
        var entries = new List<BossSpellEntry>
        {
            CreateSpellEntry(allPhasesSpell, BossPhaseGate.AllPhases),
            CreateSpellEntry(phase1Spell, BossPhaseGate.Phase1Only)
        };

        if (includePhase2Entry)
        {
            entries.Add(CreateSpellEntry(phase2Spell, BossPhaseGate.Phase2Only));
        }

        SetPrivateField(table, "_entries", entries);
        return table;
    }

    private BossSpellEntry CreateSpellEntry(MagicData spell, BossPhaseGate phaseGate)
    {
        var entry = new BossSpellEntry();
        SetPrivateField(entry, "_spell", spell);
        SetPrivateField(entry, "_tier", BossSpellTier.Standard);
        SetPrivateField(entry, "_parryRule", BossParryRule.Parryable);
        SetPrivateField(entry, "_phaseGate", phaseGate);
        return entry;
    }

    private BossStats CreateBossStats()
    {
        BossStats stats = ScriptableObject.CreateInstance<BossStats>();
        stats.MoveSpeed = 0f;
        stats.RotationSpeed = 30f;
        stats.OptimalDistance = 10f;
        stats.DistanceTolerance = 1f;
        stats.StandardAttackCooldown = 0.05f;
        stats.HeavyAttackCooldown = 0.05f;
        stats.UnparryableAttackCooldown = 0.05f;
        stats.RestBetweenAttacks = 0.05f;
        return stats;
    }

    private DummyMagicData CreateMagicData(string magicName)
    {
        DummyMagicData magicData = ScriptableObject.CreateInstance<DummyMagicData>();
        SetPrivateField(magicData, "_magicName", magicName);
        SetPrivateField(magicData, "_manaCost", 0f);
        SetPrivateField(magicData, "_cooldown", 0f);
        SetPrivateField(magicData, "_castWindupTime", 0f);
        SetPrivateField(magicData, "_castRecoveryTime", 0f);
        return Track(magicData);
    }

    private IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (predicate())
            {
                yield break;
            }

            yield return null;
        }
    }

    private List<BossSpellEntry> GetCachedCandidates(BossAI bossAi)
    {
        var field = typeof(BossAI).GetField("_phaseCandidateCache", InstanceFlags);
        Assert.That(field, Is.Not.Null, "Failed to locate BossAI candidate cache field.");
        return new List<BossSpellEntry>((List<BossSpellEntry>)field.GetValue(bossAi));
    }

    private void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
        Assert.That(method, Is.Not.Null, $"Failed to locate method {methodName}.");
        method.Invoke(target, null);
    }

    private T Track<T>(T obj) where T : UnityEngine.Object
    {
        _trackedObjects.Add(obj);
        return obj;
    }

    private T TrackComponent<T>(T component) where T : Component
    {
        return component;
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        Assert.That(field, Is.Not.Null, $"Failed to locate field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, InstanceFlags);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private sealed class BossAiTestRig : IDisposable
    {
        public BossAiTestRig(BossAI bossAi, Health health, Mana mana)
        {
            BossAi = bossAi;
            Health = health;
            Mana = mana;
        }

        public BossAI BossAi { get; }
        public Health Health { get; }
        public Mana Mana { get; }

        public void Dispose()
        {
        }
    }

    private sealed class DummyMagicData : MagicData
    {
        public override IMagicEffect CreateEffect()
        {
            return new NoOpMagicEffect();
        }
    }

    private sealed class NoOpMagicEffect : IMagicEffect
    {
        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
        {
        }
    }
}

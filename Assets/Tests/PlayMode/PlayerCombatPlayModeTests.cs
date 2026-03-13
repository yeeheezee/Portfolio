using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WizardBrawl.Core;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;
using WizardBrawl.Player;

public class PlayerCombatPlayModeTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [UnityTest]
    public IEnumerator TargetedCast_UsesProvidedTargetPoint()
    {
        GameObject casterObject = new GameObject("Caster");
        Camera camera = null;

        try
        {
            casterObject.AddComponent<Mana>();
            PlayerAttackCaster caster = casterObject.AddComponent<PlayerAttackCaster>();

            camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";

            SetPrivateField(caster, "_mainCamera", camera);

            TargetPointMagicData magicData = ScriptableObject.CreateInstance<TargetPointMagicData>();
            SetPrivateField(magicData, "_magicName", "Target Point Test");
            SetPrivateField(magicData, "_manaCost", 0f);
            SetPrivateField(magicData, "_cooldown", 0f);
            SetPrivateField(magicData, "_castMode", MagicCastMode.Targeted);

            Vector3 requestedTargetPoint = new Vector3(3f, 1f, 7f);
            bool casted = (bool)typeof(PlayerAttackCaster)
                .GetMethod("TryCastSelectedMagic", InstanceFlags)
                .Invoke(caster, new object[] { magicData, false, requestedTargetPoint });

            Assert.That(casted, Is.True);
            Assert.That(TargetPointMagicData.LastTargetPoint, Is.EqualTo(requestedTargetPoint));
        }
        finally
        {
            if (camera != null)
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            Object.DestroyImmediate(casterObject);
        }

        yield return null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().BaseType?.GetField(fieldName, InstanceFlags)
            ?? target.GetType().GetField(fieldName, InstanceFlags);
        Assert.That(field, Is.Not.Null, $"Failed to locate field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private sealed class TargetPointMagicData : MagicData
    {
        public static Vector3 LastTargetPoint { get; private set; }

        public override IMagicEffect CreateEffect()
        {
            return new CaptureEffect();
        }

        private sealed class CaptureEffect : IMagicEffect
        {
            public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
            {
                LastTargetPoint = targetPoint;
            }
        }
    }
}

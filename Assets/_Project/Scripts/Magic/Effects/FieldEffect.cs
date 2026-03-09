using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 원형 범위 장판 즉시 판정 실행 로직.
    /// </summary>
    public class FieldEffect : IMagicEffect
    {
        private readonly FieldMagicData _data;

        public FieldEffect(FieldMagicData data)
        {
            _data = data;
        }

        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
        {
            SpawnZone(targetPoint, caster);
        }

        private void SpawnZone(Vector3 targetPoint, GameObject caster)
        {
            GameObject zoneObject;
            if (_data.FieldVfxPrefab != null)
            {
                zoneObject = Object.Instantiate(_data.FieldVfxPrefab, targetPoint, Quaternion.identity);
            }
            else
            {
                zoneObject = new GameObject("FieldZone");
                zoneObject.transform.position = targetPoint;
            }

            FieldZone zone = zoneObject.GetComponent<FieldZone>();
            if (zone == null)
            {
                zone = zoneObject.AddComponent<FieldZone>();
            }
            zone.Initialize(_data, caster);

            float lifetime = Mathf.Max(0.05f, _data.Duration);
            Object.Destroy(zoneObject, lifetime + 0.1f);
            Debug.Log($"[FieldEffect] zone spawned duration={_data.Duration:0.00}, tick={_data.TickInterval:0.00}, radius={_data.Radius:0.00}");
        }
    }
}

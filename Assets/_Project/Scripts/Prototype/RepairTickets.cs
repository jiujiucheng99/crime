using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    public sealed class RepairTicket
    {
        public string Symptom { get; private set; }
        public string PartId { get; private set; }
        public string PartName => ItemCatalog.NameOf(PartId);

        public RepairTicket(string symptom, string partId)
        {
            Symptom = symptom;
            PartId = partId;
        }
    }

    public static class RepairTickets
    {
        public static readonly RepairTicket[] All =
        {
            new RepairTicket("轮胎漏气", "tire"),
            new RepairTicket("电瓶没电", "battery"),
            new RepairTicket("刹车异响", "brake_pads"),
            new RepairTicket("发动机抖动", "spark_plug")
        };

        public static RepairTicket[] PickThree()
        {
            List<RepairTicket> pool = new List<RepairTicket>(All);
            RepairTicket[] result = new RepairTicket[3];
            for (int i = 0; i < result.Length; i++)
            {
                int index = Random.Range(0, pool.Count);
                result[i] = pool[index];
                pool.RemoveAt(index);
            }
            return result;
        }
    }

    public static class RepairParts
    {
        public static readonly string[] All = { "tire", "battery", "brake_pads", "spark_plug" };

        public static void ClearFrom(PlayerInventory inventory)
        {
            if (inventory == null) return;
            foreach (string id in All)
            {
                int count = inventory.CountOf(id);
                if (count > 0) inventory.TryRemove(id, count);
            }
        }
    }
}

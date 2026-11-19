using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    public sealed class JobDefinition
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Workplace { get; private set; }
        public string SkillId { get; private set; }
        public int BaseWage { get; private set; }
        public string Summary { get; private set; }
        public string Instructions { get; private set; }

        public JobDefinition(string id, string name, string workplace, string skillId,
            int baseWage, string summary, string instructions)
        {
            Id = id;
            Name = name;
            Workplace = workplace;
            SkillId = skillId;
            BaseWage = baseWage;
            Summary = summary;
            Instructions = instructions;
        }
    }

    public static class JobCatalog
    {
        private static readonly Dictionary<string, JobDefinition> Jobs =
            new Dictionary<string, JobDefinition>
            {
                {
                    "store_clerk",
                    new JobDefinition(
                        "store_clerk", "便利店店员", "便利店 D8", "none", 12,
                        "在便利店收银台服务顾客。",
                        "上班地点：便利店 D8。\n" +
                        "流程：\n" +
                        "1. 进入便利店，走到收银台。\n" +
                        "2. 靠近服务员或收银台按 F 开始轮班。\n" +
                        "3. 服务 3 位顾客。\n" +
                        "4. 完成后自动结算 12 块。\n" +
                        "提示：轮班期间不要离开便利店。")
                },
                {
                    "restaurant_waiter",
                    new JobDefinition(
                        "restaurant_waiter", "餐厅服务员", "餐厅 E8", "cooking", 20,
                        "在后厨完成订单并出餐。",
                        "上班地点：餐厅 E8 后厨。\n" +
                        "流程：\n" +
                        "1. 进入餐厅，从员工通道进入厨房。\n" +
                        "2. 查看 HUD 上的订单和所需食材。\n" +
                        "3. 在冰箱按 F 拿取对应食材。\n" +
                        "4. 到灶台按 F 开始做饭。\n" +
                        "5. 指针到绿色区域时按空格停止。\n" +
                        "6. 出餐后获得奖励。\n" +
                        "提示：离开员工通道会清空身上的食材。")
                },
                {
                    "transport_staff",
                    new JobDefinition(
                        "transport_staff", "运输中心职员", "运输中心 F7", "none", 20,
                        "驾驶公交车完成 Brick Row 环线。",
                        "上班地点：运输中心 F7。\n" +
                        "流程：\n" +
                        "1. 进入运输中心，乘员工电梯到公交车停车场。\n" +
                        "2. 靠近公交车按 F 发车。\n" +
                        "3. 按 W 加速、S 倒车、A/D 转向、空格刹车。\n" +
                        "4. 沿路线到黄色停止框内。\n" +
                        "5. 停稳 10 秒完成停靠。\n" +
                        "6. 跑完整条环线后结算 20 块。\n" +
                        "提示：未完成当前站点不会开放下一站。")
                },
                {
                    "auto_mechanic",
                    new JobDefinition(
                        "auto_mechanic", "修车厂技工", "修车厂 H6", "mechanics", 12,
                        "领取维修工单，取零件并完成车辆维修。",
                        "上班地点：修车厂 H6。\n" +
                        "流程：\n" +
                        "1. 在工作台按 F 领取 3 张工单。\n" +
                        "2. 查看每张工单需要的零件。\n" +
                        "3. 到零件架按 F 拿对应零件。\n" +
                        "4. 到举升车辆旁按 F 开始维修。\n" +
                        "5. 按空格完成 4 个螺栓的时机判定。\n" +
                        "6. 完成 3 张工单后结算。\n" +
                        "提示：工作台、零件架和车辆上方有颜色箭头。")
                },
                {
                    "warehouse_worker",
                    new JobDefinition(
                        "warehouse_worker", "仓库工人", "南岸仓库 G8", "logistics", 15,
                        "把木箱搬到对应颜色的区域。",
                        "上班地点：南岸仓库 G8 办公桌。\n" +
                        "流程：\n" +
                        "1. 在办公桌按 F 开始班次。\n" +
                        "2. 靠近木箱按 F 搬起。\n" +
                        "3. 木箱颜色对应目标区域：A 黄、B 蓝、C 绿。\n" +
                        "4. 搬到对应颜色区域后按 F 放下。\n" +
                        "5. 完成 4 个木箱后结算。\n" +
                        "提示：搬箱时移动速度会降低，放错区域会扣表现。")
                }
            };

        public static JobDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Jobs.TryGetValue(id, out JobDefinition job);
            return job;
        }

        public static string Instructions(string id)
        {
            JobDefinition job = Get(id);
            if (job == null) return "这份工作暂时没有工作说明。";
            string skill = job.SkillId == "none" ? string.Empty : "\n对应技能：" + job.SkillId;
            return job.Summary + "\n\n" + job.Instructions + skill;
        }

        public static string SkillName(string id)
        {
            JobDefinition job = Get(id);
            if (job == null || job.SkillId == "none") return string.Empty;
            SkillDefinition skill = SkillCatalog.Get(job.SkillId);
            return skill != null ? skill.DisplayName : job.SkillId;
        }
    }
}

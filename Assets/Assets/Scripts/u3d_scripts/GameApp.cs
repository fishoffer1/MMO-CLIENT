using GameClient.Battle;
using GameClient.Entities;
using Proto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameClient
{
    public class GameApp
    {
        // 全局角色
        public static Character Character;
        // 选择的目标
        public static Actor Target;
        // 当前正在施放的技能
        public static Skill CurrSkill;
        // 是否正输入文字
        public static bool IsInputting
        {
            get => SimpleChatBox.Instance.inputField.isFocused;
        }
        /// <summary>
        /// 进入对应的场景
        /// </summary>
        /// <param name="spaceId"></param>
        public static void LoadSpace(int spaceId)
        {
            var spaceDefine = DataManager.Instance.Spaces[spaceId];
            SceneManager.LoadScene(spaceDefine.Resource);
        }


        public static void SelectTarget()
        {
            Log.Information("选择目标");
            Target = Game.RangeUnit(Character.Position, 12000)
                .OrderBy(e => Vector3.Distance(Character.Position, e.Position))
                .FirstOrDefault(e => e.entityId != Character.entityId && !e.IsDeath);
        }

        public static void Spell(Skill skill)
        {
            if (skill.IsUnitTarget && Target == null)
            {
                SelectTarget();
                if (Target == null)
                {
                    Log.Information("无效的技能目标");
                    return;
                }
            }

            SpellRequest req = new SpellRequest() { Info = new() };
            req.Info.CasterId = Character.entityId;
            req.Info.SkillId = skill.Def.ID;
            if (skill.IsUnitTarget)
            {
                req.Info.TargetId = Target.entityId;
            }
            else if (skill.IsPointTarget)
            {
                req.Info.TargetLoc = V3.ToVec3(Target.Position);
            }
            NetClient.Send(req);
        }

    }
}


using GameClient.Battle;
using GameClient.Entities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClient.Mgr
{
    /// <summary>
    /// 技能管理器，每个Actor都有独立的技能管理器
    /// </summary>
    public class SkillManager
    {
        //归属的角色
        private Actor owner;
        //技能列表
        public List<Skill> Skills = new();

        public SkillManager(Actor owner)
        {
            this.owner = owner;
            this.InitSkills();
        }

        public void InitSkills()
        {
            foreach (var info in owner.Info.Skills)
            {
                var skill = new Skill(owner, info.Id);
                Skills.Add(skill);
                Log.Information("角色[{0}]加载技能[{1}-{2}]",owner.Define.Name,skill.Def.ID, skill.Def.Name);
            }
        }

        public void OnUpdate(float delta)
        {
            foreach (Skill skill in Skills)
            {
                skill.OnUpdate(delta);
            }
        }

        internal Skill GetSkill(int skillId)
        {
            return Skills.FirstOrDefault(s=>s.Def.ID == skillId);
        }
    }

}


using GameClient.Mgr;
using Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameClient.Entities
{
    public class Actor : Entity
    {
        public NetActor Info;
        public UnitDefine Define;
        public SkillManager SkillMgr;

        public UnitState UnitState; //单位状态
        public bool IsDeath => UnitState == UnitState.Dead; //角色是否死亡

        //实体对应的游戏对象
        public GameObject renderObj;

        public Actor(NetActor info) : base(info.Entity)
        {
            this.Info = info;
            this.Define = DataManager.Instance.Units[info.Tid];
            this.SkillMgr = new SkillManager(this);
        }
        internal void recvDamage(Damage item)
        {
            var _txtPos = renderObj.transform.position;
            //是否闪避
            if (item.IsMiss)
            {
                DynamicTextManager.CreateText(_txtPos, "Miss", DynamicTextManager.missData);
            }
            else
            {
                //伤害飘字
                DynamicTextManager.CreateText(_txtPos, item.Amount.ToString("0"));
                //是否暴击
                if (item.IsCrit)
                {
                    UIManager.ShakeScreen();
                    DynamicTextManager.CreateText(_txtPos, "Crit!", DynamicTextManager.critData);
                }
            }
            
            //伤害特效
            var attacker = Game.GetUnit(item.AttackerId);
            if (attacker == null) return;
            var skill = attacker.SkillMgr.GetSkill(item.SkillId);
            var ps = Resources.Load<ParticleSystem>(skill.Def.HitArt);
            if (ps != null)
            {
                var pos = renderObj.transform.position + Vector3.up;
                var dir = renderObj.transform.rotation;
                ParticleSystem newPs = GameObject.Instantiate(ps, pos, dir);
                GameObject.Destroy(newPs.gameObject, newPs.main.duration);
            }
            else
            {
                Debug.LogWarning("Failed to load particle system!");
            }
        }

        public virtual void OnHpChanged(float old_hp, float new_hp)
        {
            this.Info.Hp = new_hp;
            
        }

        public virtual void OnMpChanged(float old_value, float new_value)
        {
            this.Info.Mp = new_value;
        }

        public virtual void OnStateChanged(UnitState old_value, UnitState new_value)
        {
            this.UnitState = new_value;
            if (IsDeath)
            {
                if (renderObj == null) return;
                var ani = renderObj?.GetComponent<HeroAnimations>();
                ani.PlayDie();
            }
            else
            {
                renderObj?.SetActive(true);
            }
        }
    }
}

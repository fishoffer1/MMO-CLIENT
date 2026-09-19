
using GameClient.Entities;
using GameClient.Mgr;
using Proto;
using Serilog;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GameClient.Battle
{
    public class Skill
    {
        public enum Stage
        {
            None,       //无状态
            Intonate,   //吟唱
            Active,     //已激活
            Coolding,   //冷却中
        }

        public bool IsPassive;          // 是否被动技能
        public SkillDefine Def;         // 技能设定
        public Actor Owner;             // 技能归属者
        public float Cd;          // 冷却计时，0代表技能可用
        private float _time;            // 技能运行时间
        public Stage State;        // 当前技能状态

        private SCObject _sco;     //技能目标

        //聚气进度0-1
        public float IntonateProgress => _time / Def.IntonateTime;

        public bool IsUnitTarget { get => Def.TargetType == "单位"; }
        public bool IsPointTarget { get => Def.TargetType == "点"; }
        public bool IsNoneTarget { get => Def.TargetType == "None"; }


        private Sprite _icon;
        public Sprite Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = Resources.Load<Sprite>(Def.Icon);
                }
                return _icon;
            }
        }

        public Skill(Actor owner, int skid)
        {
            Owner = owner;
            Def = DataManager.Instance.Skills[skid];
        }

        public void OnUpdate(float delta)
        {
            if (State == Stage.None && Cd == 0) return;

            if (Cd > 0) Cd -= delta;
            if(Cd < 0) Cd = 0;
            _time += delta;

            if (State == Stage.Intonate && _time >= Def.IntonateTime)
            {
                State = Stage.Active;
                Cd = Def.CD;
                OnActive();
            }

            if (State == Stage.Active)
            {
                if (_time >= Def.IntonateTime + Def.Duration)
                {
                    State = Stage.Coolding;
                }
            }

            if (State == Stage.Coolding)
            {
                if (Cd == 0)
                {
                    Log.Information("技能结束：Skill[{0}],Time[{1}]", Def.Name, Def.IntonateTime + Def.CD);
                    _time = 0;
                    State = Stage.None;
                    OnFinish();
                }
            }
        }

        

        public void Use(SCObject target)
        {
            if (Owner.entityId == GameApp.Character.entityId)
            {
                GameApp.CurrSkill = this;
            }

            _sco = target;
            _time = 0;
            State = Stage.Intonate;
            OnIntonate();
            
        }

        //开始蓄气
        public void OnIntonate()
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_sco is SCEntity)
                {
                    var actor = _sco.RealObj as Actor;
                    Owner.renderObj.transform.LookAt(actor.renderObj.transform);
                }
                
            });
            //角色播放蓄气动作
            Kaiyun.Event.FireOut("OnSkillIntonate", this);
        }

        //技能激活
        private void OnActive()
        {
            Kaiyun.Event.FireOut("OnSkillActive", this);
            if (Def.IsMissile)
            {
                var target = _sco.RealObj as Actor;
                GameObject myObject = new GameObject("Missile");
                var missile = myObject.AddComponent<Missile>();
                missile.Init(this, Owner.renderObj.transform.position, target.renderObj);
            }

            
        }


        private void OnHit()
        {
            var ps = Resources.Load<ParticleSystem>(Def.HitArt);
            if (ps != null)
            {

                if (_sco is SCEntity)
                {
                    var target = _sco.RealObj as Actor;
                    var pos = target.renderObj.transform.position + Vector3.up * 0.9f;
                    var dir = target.renderObj.transform.rotation;
                    ParticleSystem newPs = GameObject.Instantiate(ps, pos, dir);
                    newPs.Play();
                    GameObject.Destroy(newPs.gameObject, newPs.main.duration);
                }

                if (_sco is SCPosition)
                {
                    ParticleSystem newPs = GameObject.Instantiate(ps, _sco.Position, Quaternion.identity);
                    newPs.Play();
                    GameObject.Destroy(newPs.gameObject, newPs.main.duration);
                }

            }
            else
            {
                Debug.LogError("Failed to load particle system!");
            }
        }

        private void OnFinish()
        {

        }


        public void PlayParticleSystem(GameObject particlePrefab, int playCount)
        {
            for (int i = 0; i < playCount; i++)
            {
                GameObject particleInstance = GameObject.Instantiate(particlePrefab, _sco.Position*0.001f, Quaternion.identity);
                ParticleSystem particleSystem = particleInstance.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Play();
                }
                else
                {
                    Debug.LogWarning("The instantiated object does not contain a ParticleSystem component.");
                }
            }
        }

        
    }
}



using GameClient.Entities;
using Proto;
using Summer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GameClient.Mgr
{
    public class EntityManager : Singleton<EntityManager>
    {
        public EntityManager() { }
        //线程安全的字典
        private ConcurrentDictionary<int, Entity> _dict = new ConcurrentDictionary<int, Entity>();

        public void AddEntity(Entity entity)
        {
            UnityEngine.Debug.Log("AddEntity："+ entity.entityId);
            _dict[entity.entityId] = entity;
        }

        public void RemoveEntity(int entityId)
        {
            UnityEngine.Debug.Log("RemoveEntity：" + entityId);
            _dict.Remove(entityId,out Entity entity);
            Kaiyun.Event.FireOut("CharacterLeave", entityId);
        }
        public void OnEntityEnter(NetActor info)
        {
            //地面掉落物不进角色/怪物流程，另行处理
            if (info.Type == EntityType.Item)
            {
                return;
            }
            if (info.Type == EntityType.Character)
            {
                AddEntity(new Character(info));
            }
            if (info.Type == EntityType.Monster)
            {
                AddEntity(new Monster(info));
            }
            Kaiyun.Event.FireOut("CharacterEnter", info);
        }

        public void OnEntitySync(NetEntitySync entitySync)
        {
            var entity = _dict.GetValueOrDefault(entitySync.Entity.Id);
            if (entity != null)
            {
                entity.State = entitySync.State;
                entity.EntityData = entitySync.Entity;
                Kaiyun.Event.FireOut("EntitySync", entitySync);
            }
            
        }


        public T GetEntity<T>(int entityId) where T : Entity
        {
            return (T)_dict.GetValueOrDefault(entityId);
        }

        public List<T> GetEntities<T>(Predicate<T> match)
        {
            return _dict.Values.OfType<T>().Where(e => match.Invoke(e)).ToList();
        }


        public void Clear()
        {
            foreach (var entity in _dict.Values)
            {
                if(entity is Actor actor)
                {
                    GameObjectManager.Instance.CharacterLeave(actor.entityId);
                }
            }
            _dict.Clear();
        }

        /// <summary>
        /// 此方法由Unity主线程调用
        /// </summary>
        /// <param name="delta"></param>
        public void OnUpdate(float delta)
        {
            foreach(var entity in _dict.Values)
            {
                var actor = entity as Actor;
                actor.SkillMgr?.OnUpdate(delta);
            }
        }
    }
}

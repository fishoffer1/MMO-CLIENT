
using GameClient;
using GameClient.Entities;
using Proto;
using Summer;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameObjectManager : MonoBehaviour
{
    public static GameObjectManager Instance;
    // <EntityId,GameObject>
    private static Dictionary<int,GameObject> dict = new Dictionary<int,GameObject>();

    private void Start()
    {
        Instance = this;
        Kaiyun.Event.RegisterOut("CharacterEnter", this, "CharacterEnter");
        Kaiyun.Event.RegisterOut("CharacterLeave", this, "CharacterLeave");
        Kaiyun.Event.RegisterOut("EntitySync", this, "EntitySync");
    }

    private void OnDestroy()
    {
        Kaiyun.Event.UnregisterOut("CharacterEnter", this, "CharacterEnter");
        Kaiyun.Event.UnregisterOut("CharacterLeave", this, "CharacterLeave");
        Kaiyun.Event.UnregisterOut("EntitySync", this, "EntitySync");
    }

    public void EntitySync(NetEntitySync entitySync)
    {
        int entityId = entitySync.Entity.Id;
        var go = dict.GetValueOrDefault(entityId, null);
        if (go == null) return;
        Vector3 pos = V3.Of(entitySync.Entity.Position) / 1000f;
        if (pos.y == 0)
        {
            pos = GameTools.CalculateGroundPosition(pos,20);
            entitySync.Entity.Position = V3.ToVec3(pos*1000);
            Debug.Log("同步坐标");
            Debug.Log(entitySync.Entity.Position);
        }
        var gameEntity = go.GetComponent<GameEntity>();
        gameEntity.SetData(entitySync.Entity);
        if (entitySync.Force)
        {
            Vector3 target = V3.Of(entitySync.Entity.Position) * 0.001f;
            gameEntity.Move(target);
        }
    }

    public void CharacterLeave(int entityId)
    {
        if(dict.ContainsKey(entityId))
        {
            var obj = dict[entityId];
            if (obj != null && !obj.IsDestroyed())
            {
                Destroy(obj);
            }
            dict.Remove(entityId);
        }
    }


    public void CharacterEnter(NetActor chr)
    {
        //有可能是Character，也可能是Monster
        if (!dict.ContainsKey(chr.Entity.Id))
        {
            Debug.Log("角色加入：" + chr);
            bool isMine = (chr.Entity.Id == GameApp.Character.entityId);
            Vector3 initPos = V3.Of(chr.Entity.Position) / 1000f;
            if (initPos.y == 0)
            {
                initPos = GameTools.CalculateGroundPosition(initPos);
                Debug.Log("pos:");
                Debug.Log(initPos);
            }
            Actor actor = Game.GetUnit(chr.Entity.Id);
            //加载预制体
            UnitDefine def = DataManager.Instance.Units[chr.Tid];
            var prefab = Resources.Load<GameObject>(def.Resource);
            var go = Instantiate(prefab, initPos, Quaternion.identity, this.transform);
            go.layer = 6; //加入Actor图层
            actor.renderObj = go;
            var gameEntity = go.GetComponent<GameEntity>();
            gameEntity.isMine = isMine;
            gameEntity.entityName = chr.Name;
            gameEntity.SetData(chr.Entity);
            if (isMine)
            {
                go.AddComponent<HeroController>();
            }
            
            if(chr.Type == EntityType.Character)
            {
                go.name = "Character_" + chr.Entity.Id;
            }
            if (chr.Type == EntityType.Monster)
            {
                go.name = "Monster_" + chr.Entity.Id;
            }

            dict.Add(chr.Entity.Id, go);
            
        }
        

    }


}

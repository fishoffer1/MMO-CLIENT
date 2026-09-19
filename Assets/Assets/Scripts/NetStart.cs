using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Proto;
using Summer.Network;
using System;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using GameClient.Mgr;
using GameClient.Entities;
using Serilog;
using GameClient;
using GameClient.Battle;

public class NetStart : MonoBehaviour
{

    public List<GameObject> keepAlive;

    [Header("服务器信息")]
    public string host = "127.0.0.1";
    public int port = 32510;

    public Text ycText;

    [Header("登录参数")]
    public InputField usernameInput;
    public InputField passwordInput;

    private GameObject hero; //当前的角色



    // Start is called before the first frame update
    void Start()
    {
        //6号Layer无视碰撞，可以把角色，NPC，怪物，全都放到6号图层
        Physics.IgnoreLayerCollision(6, 6, true);

        NetClient.ConnectToServer(host, port);

        foreach (GameObject go in keepAlive)
        {
            DontDestroyOnLoad(go);
        }
        

        MessageRouter.Instance.Subscribe<GameEnterResponse>(_GameEnterResponse);
        MessageRouter.Instance.Subscribe<SpaceEnterResponse>(_SpaceEnterResponse);
        MessageRouter.Instance.Subscribe<SpaceCharactersEnterResponse>(_SpaceCharactersEnterResponse);
        MessageRouter.Instance.Subscribe<SpaceEntitySyncResponse>(_SpaceEntitySyncResponse);
        MessageRouter.Instance.Subscribe<HeartBeatResponse>(_HeartBeatResponse);
        MessageRouter.Instance.Subscribe<SpaceCharacterLeaveResponse>(_SpaceCharacterLeaveResponse);
        //施法通知
        MessageRouter.Instance.Subscribe<SpellResponse>(_SpellResponse);
        //单位收到伤害，用来做特效展示，crit，miss，damage
        MessageRouter.Instance.Subscribe<DamageResponse>(_DamageResponse);
        //单位属性发生了变化（HP,MP,HPMAX,MPMAX,STATE,LEVEL,NAME...）
        MessageRouter.Instance.Subscribe<PropertyUpdateResponse>(_PropertyUpdateResponse);

        //监听聊天信息
        MessageRouter.Instance.Subscribe<ChatResponse>(_ChatResponse);


        //心跳包任务，每秒1次
        StartCoroutine(SendHeartMessage());

        //var loginPanel = Resources.Load("Prefabs/UI/LoginPanel") as GameObject;
        //Instantiate(loginPanel);
        SceneManager.LoadScene("LoginScene");

        string exeDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        Debug.Log(exeDirectory);


        DataManager.Instance.Init();

        //注册事件
        Kaiyun.Event.RegisterIn("EnterGame", this, "EnterGame");
    }

    //进入场景
    private void _SpaceEnterResponse(Connection sender, SpaceEnterResponse msg)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if(GameApp.Character==null || GameApp.Character.Info.SpaceId != msg.Character.SpaceId)
            {
                //需要加载新场景
                EntityManager.Instance.Clear();
                GameApp.LoadSpace(msg.Character.SpaceId);
                //把其他单位加入游戏
                foreach(var item in msg.List)
                {
                    EntityManager.Instance.OnEntityEnter(item);
                }
                //把主角加入游戏
                EntityManager.Instance.OnEntityEnter(msg.Character);
                GameApp.Character = EntityManager.Instance.GetEntity<Character>(msg.Character.Entity.Id);
            }
        });
    }

    //接收聊天消息
    private void _ChatResponse(Connection sender, ChatResponse msg)
    {
        var chr = Game.GetUnit(msg.SenderId) as Character;
        var text = $"[玩家]{chr.Info.Name}：{msg.TextValue}";
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            SimpleChatBox.Instance.CreateText(text);
        });
    }
    //提交聊天消息
    public void ChatSubmit(string text)
    {
        ChatRequest req = new ChatRequest();
        req.TextValue = text;
        NetClient.Send(req);
        SimpleChatBox.Instance.inputField.text = "";
    }

    private void _PropertyUpdateResponse(Connection sender, PropertyUpdateResponse msg)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (PropertyUpdate item in msg.List)
            {
                var actor = Game.GetUnit(item.EntityId);
                switch (item.Property)
                {
                    case PropertyUpdate.Types.Prop.Hp:
                        actor.OnHpChanged(item.OldValue.FloatValue, item.NewValue.FloatValue);
                        break;
                    case PropertyUpdate.Types.Prop.Mp:
                        actor.OnMpChanged(item.OldValue.FloatValue, item.NewValue.FloatValue);
                        break;
                    case PropertyUpdate.Types.Prop.State:
                        actor.OnStateChanged(item.OldValue.StateValue, item.NewValue.StateValue);
                        break;
                }
            }
        });
    }

    private void _DamageResponse(Connection conn, DamageResponse msg)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (Damage item in msg.List)
            {
                var attacker = Game.GetUnit(item.AttackerId);
                var target = Game.GetUnit(item.TargetId);
                target.recvDamage(item);
            }
        });
    }

    //收到来自服务器的施法通知
    private void _SpellResponse(Connection conn, SpellResponse msg)
    {
        foreach(CastInfo item in msg.CastList)
        {
            Log.Information("施法信息：{0}", item);
            var caster = Game.GetUnit(item.CasterId);
            try
            {
                var skill = caster.SkillMgr.GetSkill(item.SkillId);
                if (skill.IsUnitTarget)
                {
                    var target = Game.GetUnit(item.TargetId);
                    skill.Use(new SCEntity(target));
                }
                if (skill.IsNoneTarget)
                {
                    skill.Use(new SCEntity(caster));
                }
            }
            catch(Exception ex)
            {
                Log.Information("施法异常：{0}",ex.Message);
            }
            
        }
    }





    /// <summary>
    /// 有角色离开地图
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="msg"></param>
    private void _SpaceCharacterLeaveResponse(Connection sender, SpaceCharacterLeaveResponse msg)
    {
        EntityManager.Instance.RemoveEntity(msg.EntityId);
    }

    //来自于服务器的心跳响应
    private void _HeartBeatResponse(Connection sender, HeartBeatResponse msg)
    {
        
        var t = DateTime.Now - lastBeatTime;
        //Debug.Log("来自于服务器的心跳响应:ms="+t.TotalMilliseconds);
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            int ms = Math.Max(1,(int)Math.Round(t.TotalMilliseconds));
            ycText.text = $"网络延迟：{ms}ms";
        });
    }

    //心跳包对象
    private HeartBeatRequest beatRequest = new HeartBeatRequest();
    DateTime lastBeatTime = DateTime.MinValue;

    IEnumerator SendHeartMessage()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            NetClient.Send(beatRequest);
            lastBeatTime = DateTime.Now;
        }
    }

    //收到角色的同步信息
    private void _SpaceEntitySyncResponse(Connection sender, SpaceEntitySyncResponse msg)
    {
        EntityManager.Instance.OnEntitySync(msg.EntitySync);
    }



    //加入游戏的响应结果（Entity肯定是自己）
    private void _GameEnterResponse(Connection conn, GameEnterResponse msg)
    {
        Debug.Log("加入游戏的响应结果:" + msg.Success);
        if (msg.Success)
        {
            Debug.Log("角色信息:" + msg);
            var info = msg.Character;
            info.Entity = msg.Entity;
 
            GameApp.LoadSpace(info.SpaceId);
            EntityManager.Instance.OnEntityEnter(info);
            GameApp.Character = EntityManager.Instance.GetEntity<Character>(msg.Entity.Id);
        }
    }


    //当有角色进入地图时候的通知（肯定不是自己）
    private void _SpaceCharactersEnterResponse(Connection conn, SpaceCharactersEnterResponse msg)
    {
        //msg.SpaceId;
        //msg.EntityList
        //Debug.Log("角色加入：地图=" + msg.SpaceId + ",entityId=" + msg.CharacterList[0].Id);

        foreach (var info in msg.CharacterList)
        {
            Debug.Log("角色加入：地图=" + msg.SpaceId + ",entityId=" + info.Entity.Id);
            EntityManager.Instance.OnEntityEnter(info);
        }
    }



    // Update is called once per frame
    void Update()
    {
        Kaiyun.Event.Tick();

        // 当鼠标左键被按下
        if (Input.GetMouseButtonDown(0))  
        {
            //从鼠标点击位置发出一条射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);  
            RaycastHit hitInfo;  //存储射线投射结果的数据
            LayerMask actorLayer = LayerMask.GetMask("actor");
            //检测射线是否与特定图层的物体相交
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, actorLayer))  
            {
                //获取被点击的物体
                GameObject clickedObject = hitInfo.collider.gameObject;  
                Debug.Log("选择目标: " + clickedObject.name);
                //记录被点击的角色
                int entityId = clickedObject.GetComponent<GameEntity>().entityId;
                GameApp.Target = EntityManager.Instance.GetEntity<Actor>(entityId);

            }
        }

    }

    private void FixedUpdate()
    {
        EntityManager.Instance.OnUpdate(Time.fixedDeltaTime);
    }

    private void OnDestroy()
    {
        Kaiyun.Event.UnregisterIn("EnterGame", this, "EnterGame");
    }

    public void Login()
    {
        
    }

    /// <summary>
    /// 加入游戏
    /// </summary>
    public void EnterGame(int roleId)
    {
        if (hero != null)
        {
            return;
        }
        GameEnterRequest request = new GameEnterRequest();
        request.CharacterId = roleId;
        NetClient.Send(request);
    }

    void OnApplicationQuit()
    {
        NetClient.Close();
    }
}

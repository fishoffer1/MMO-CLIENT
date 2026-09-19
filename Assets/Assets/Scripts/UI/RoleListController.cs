using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Proto;
using Summer.Network;
using System;
using UnityEngine.SceneManagement;

public class RoleListController : MonoBehaviour
{
    public GameObject RoleSelectPanel;
    public GameObject RoleCreatePanel;

    List<GameObject> PanelList = new List<GameObject>();
    List<RoleInfo> roles = new List<RoleInfo>();
    string[] Jobs = new string[] { "", "战士","法师","仙术","游侠" };

    private int SelectedIndex = -1; //选择的角色下标
    private int SelectedJobId = 1;  //选择的职业id

    // Start is called before the first frame update
    void Start()
    {

        MessageRouter.Instance.Subscribe<ChracterCreateResponse>(_ChracterCreateResponse);
        MessageRouter.Instance.Subscribe<CharacterListResponse>(_CharacterListResponse);
        MessageRouter.Instance.Subscribe<CharacterDeleteResponse>(_CharacterDeleteResponse);


        for (int i = 0; i < 4; i++)
        {
            PanelList.Add(GameObject.Find($"HeroPanel ({i})"));
        }
        //先隐藏所有面板
        foreach (var p in PanelList) p.SetActive(false);
        //加载数据
        //LoadRoles();
        CharacterListRequest listReq = new CharacterListRequest();
        NetClient.Send(listReq);
    }

    /// <summary>
    /// 删除角色的响应
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="msg"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void _CharacterDeleteResponse(Connection conn, CharacterDeleteResponse msg)
    {
        CharacterListRequest listReq = new CharacterListRequest();
        NetClient.Send(listReq);
    }

    /// <summary>
    /// 角色列表的响应
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="msg"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void _CharacterListResponse(Connection sender, CharacterListResponse msg)
    {
        roles.Clear();
        foreach(var c in msg.CharacterList)
        {
            roles.Add(new RoleInfo() { Name = c.Name, Job = c.Tid, Level = c.Level, RoleId=c.Id });
        }
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            //先隐藏所有面板
            foreach (var p in PanelList) p.SetActive(false);
            //再根据角色列表逐个显示
            for (int i = 0; i < roles.Count; i++)
            {
                PanelList[i].SetActive(true);
                PanelList[i].transform.Find("Text (名字)").GetComponent<Text>().text = roles[i].Name;
                PanelList[i].transform.Find("Text (职业)").GetComponent<Text>().text = Jobs[roles[i].Job];
                PanelList[i].transform.Find("Text (等级)").GetComponent<Text>().text = roles[i].Level + "";
            }
        });
        
    }

    private void _ChracterCreateResponse(Connection conn, ChracterCreateResponse msg)
    {

        MyDialog.ShowMessage("系统消息", msg.Message);

        if (msg.Success)
        {
            CharacterListRequest listReq = new CharacterListRequest();
            NetClient.Send(listReq);
            //切换UI
            
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                RoleSelectPanel.SetActive(true);
                RoleCreatePanel.SetActive(false);
            });
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    public void LoadRoles()
    {
        
        //roles.Add(new RoleInfo() { Name="对酒当歌1",Job=1,Level=25});
        //roles.Add(new RoleInfo() { Name="对酒当歌2",Job=2,Level=22});
        //roles.Add(new RoleInfo() { Name="对酒当歌3",Job=3,Level=19});
        //roles.Add(new RoleInfo() { Name="对酒当歌4",Job=4,Level=26});

        //先隐藏所有面板
        foreach (var p in PanelList) p.SetActive(false);
        //再根据角色列表逐个显示
        for(int i = 0;i < roles.Count; i++)
        {
            PanelList[i].SetActive(true);
            PanelList[i].transform.Find("Text (名字)").GetComponent<Text>().text = roles[i].Name;
            PanelList[i].transform.Find("Text (职业)").GetComponent<Text>().text = Jobs[roles[i].Job];
            PanelList[i].transform.Find("Text (等级)").GetComponent<Text>().text = roles[i].Level+"";
        }

    }

    public void RoleClick(int num)
    {
        Debug.Log(num);
        SelectedIndex = num;
        RoleInfo info = roles[num];
        var p2 = GameObject.Find("Panel2");
        p2.transform.Find("Name/Text").GetComponent<Text>().text=info.Name;
        p2.transform.Find("Job/Text").GetComponent<Text>().text= Jobs[info.Job];
        p2.transform.Find("Level/Text").GetComponent<Text>().text= info.Level + "";

        for(int i=0;i<PanelList.Count;i++)
        {
            PanelList[i].transform.Find("Image").gameObject.SetActive(i == num);
        }

    }

    public void SelectJob(int jobId)
    {
        SelectedJobId = jobId;
        GameObject.Find("JobText").GetComponent<Text>().text = Jobs[jobId];
    }

    public void ToCreate()
    {
        RoleSelectPanel.SetActive(false);
        RoleCreatePanel.SetActive(true);
    }

    public void ToSelect()
    {
        RoleSelectPanel.SetActive(true);
        RoleCreatePanel.SetActive(false);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public void DeleteRole()
    {
        if (SelectedIndex < 0)
            return;

        var ok = new Chibi.Free.Dialog.ActionButton("确定", () => {
            var role = roles[SelectedIndex];
            Debug.Log($"删除角色：{role.RoleId} , {role.Name}");
            //发送删除角色的请求
            CharacterDeleteRequest delReq = new CharacterDeleteRequest();
            delReq.CharacterId = role.RoleId;
            NetClient.Send(delReq);
        }, new Color(0f, 0.9f, 0.9f));
        var cannel = new Chibi.Free.Dialog.ActionButton("取消", () => { }, new Color(0f, 0.9f, 0.9f));
        Chibi.Free.Dialog.ActionButton[] buttons = { ok, cannel };
        MyDialog.Show("系统提示", "确定删除此角色吗？删除后无法恢复。", buttons);

        
    }
    /// <summary>
    /// 进入游戏
    /// </summary>
    public void StartGame()
    {
        if (SelectedIndex < 0)
            return;
        var role = roles[SelectedIndex];
        Debug.Log("进入游戏：" + role.Name);
        //进入游戏
        /*GameObject.Find("NetStart").GetComponent<NetStart>()
            .EnterGame(role.RoleId);*/
        Kaiyun.Event.FireIn("EnterGame", role.RoleId);
    }
    /// <summary>
    /// 创建角色
    /// </summary>
    public void CreateRole()
    {
        var input = GameObject.Find("InputField (TempName)").GetComponent<InputField>();
        Debug.Log($"Job={SelectedJobId};Name={input.text}");

        CharacterCreateRequest req = new CharacterCreateRequest();
        req.Name = input.text;
        req.JobType = SelectedJobId;
        NetClient.Send(req);

    }


    class RoleInfo
    {
        public GameObject TargetPanel;
        public string Name; //名称
        public int Job;     //1战士，2法师，3仙术，4游侠
        public int Level;   //等级
        public int RoleId;  //角色ID
    }
}

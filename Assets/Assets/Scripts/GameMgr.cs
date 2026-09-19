
using GameClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{

    public Dropdown dropdown;

    public AbilityGroup abilityGroup;
    //主角信息框
    public UnitFrame playerFrame;
    //目标信息框
    public UnitFrame targetFrame;

    // Start is called before the first frame update
    void Start()
    {
        //如果是移动平台
        if(Application.isMobilePlatform)
        {
            return;
        }
        dropdown.onValueChanged.AddListener((value) =>
        {
            Debug.Log(value.ToString());
            if (value == 0)
            {
                Screen.SetResolution(1280, 720, false);
            }
            else if (value == 1)
            {
                Screen.SetResolution(800, 360, false);
            }else if(value == 2)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
        });
        Screen.SetResolution(1280, 720, false);
        dropdown.SetValueWithoutNotify(0);
    }
    // Update is called once per frame
    void Update()
    {
        abilityGroup.gameObject.SetActive(GameApp.Character != null);
        playerFrame.gameObject.SetActive(GameApp.Character != null);
        targetFrame.gameObject.SetActive(GameApp.Target != null);

        playerFrame.actor = GameApp.Character;
        targetFrame.actor = GameApp.Target;
    }
}

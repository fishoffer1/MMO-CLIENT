using Summer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Battle;
using UnityEngine.UI;
using DG.Tweening;
using GameClient;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Slider IntonateSlider;
    public SimpleChatBox ChatBox;


    private void Awake()
    {
        Instance = this;
    }




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var sk = GameApp.CurrSkill;
        if (sk!=null && sk.State == Skill.Stage.Intonate && sk.Def.IntonateTime > 0.1f)
        {
            IntonateSlider.gameObject.SetActive(true);
            IntonateSlider.value = sk.IntonateProgress;
        }
        else
        {
            IntonateSlider.gameObject.SetActive(false);
        }

        if (GameApp.Character != null)
        {
            ChatBox.gameObject.SetActive(true);
        }
        else
        {
            ChatBox.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameApp.SelectTarget();
        }

        if(GameApp.Target != null && GameApp.Target.IsDeath)
        {
            GameApp.Target = null;
        }
    }

    public static void ShakeScreen(float shakeDuration=0.5f, float shakeAmount = 0.1f)
    {
        Camera.main.transform.DOShakePosition(shakeDuration, shakeAmount);
    }


}

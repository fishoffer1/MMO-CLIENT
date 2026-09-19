using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameClient;
using GameClient.Entities;
using Serilog;

public class UnitFrame : MonoBehaviour
{
    public Image HealthBar;
    public Image Manabar;
    public Actor actor;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (actor == null) return;
        transform.Find("Name").GetComponent<Text>().text = actor.Info.Name;
        transform.Find("Level/Text").GetComponent<Text>().text = actor.Info.Level + "";
        HealthBar.fillAmount = actor.Info.Hp / actor.Define.HPMax;
        Manabar.fillAmount = actor.Info.Mp / actor.Define.MPMax;
    }
}

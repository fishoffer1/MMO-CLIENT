using GameClient.Battle;
using GameClient;
using Serilog;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public class Missile : MonoBehaviour
{
    //所属技能
    public Skill Skill { get; private set; }
    //追击目标
    public GameObject Target { get; private set; }
    //初始位置
    public Vector3 InitPos { get; private set; }

    private GameObject child;

    public void Init(Skill skill, Vector3 initPos, GameObject target)
    {
        this.Skill = skill;
        this.Target = target;
        this.InitPos = initPos;
        transform.position = initPos;
        Log.Information("Missile initPos:{0}", initPos);

        var prefab = Resources.Load<GameObject>(Skill.Def.Missile);
        if(prefab != null)
        {
            child = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            
        }
        

    }


    void Start()
    {
        //transform.localScale = Vector3.one * 0.1f;
    }


    private void FixedUpdate()
    {
        OnUpdate(Time.fixedDeltaTime);
    }


    public void OnUpdate(float dt)
    {
        var a = transform.position;
        var b = this.Target.transform.position;
        Vector3 direction = (b - a).normalized;
        var dist = Skill.Def.MissileSpeed * 0.001f * dt;
        if (dist >= Vector3.Distance(a, b))
        {
            transform.position = b;
            Destroy(this.gameObject, 0.6f);
        }
        else
        {
            transform.position += direction * dist;
        }
        //设置粒子跟随父对象
        child.transform.localPosition = Vector3.up;
    }


}

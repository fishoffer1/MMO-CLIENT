using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClient.Entities
{
    public class Character : Actor
    {
        public Character(NetActor info) : base(info)
        {

        }
        public override void OnStateChanged(UnitState old_value, UnitState new_value)
        {
            base.OnStateChanged(old_value, new_value);
            if (IsDeath && GameApp.Character==this)
            {
                var ok = new Chibi.Free.Dialog.ActionButton("确定", () => {
                    ReviveRequest req = new ReviveRequest();
                    req.EntityId = this.entityId;
                    NetClient.Send(req);
                }, new Color(0f, 0.9f, 0.9f));
                Chibi.Free.Dialog.ActionButton[] buttons = { ok };
                MyDialog.Show("战斗失利", "请点击[确定]回城复活。", buttons);
            }
        }
    }
}

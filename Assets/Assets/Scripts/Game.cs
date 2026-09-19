using GameClient.Entities;
using GameClient.Mgr;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameClient
{
    public class Game
    {
        public static Actor GetUnit(int entityId)
        {
            return EntityManager.Instance.GetEntity<Actor>(entityId);
        }

        internal static List<Actor> RangeUnit(Vector3 position, int range)
        {
            Predicate<Actor> match = (e) => {
                float dist = Vector3.Distance(position, e.Position);
                Log.Information("选择：dist={0}", dist);
                return dist <= range;
            };
            return EntityManager.Instance.GetEntities<Actor>(match);
        }
        public static void StartCoroutine(IEnumerator routine)
        {
            UIManager.Instance.StartCoroutine(routine);
        }
    }


}


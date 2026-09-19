using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClient
{
    public class GameTools
    {
        /// <summary>
        /// 计算垂直地面的坐标
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 CalculateGroundPosition(Vector3 position, float up = 1000, int ignoreLayer = 6)
        {
            Vector3 temp = position + new Vector3(0, 1000f, 0);
            // Raycast downwards to find the ground
            RaycastHit hit;
            int layerMask = ~(1 << ignoreLayer); // Ignore layer 6
            if (Physics.Raycast(temp, Vector3.down, out hit, Mathf.Infinity, layerMask))
            {
                return hit.point;
            }
            else
            {
                // If no ground is found, return the original position
                return position;
            }
        }

        public static void RunOnMainThread(Action action)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(action);
        }

    }
}

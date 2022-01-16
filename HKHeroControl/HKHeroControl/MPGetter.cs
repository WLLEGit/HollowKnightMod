using System.Collections;
using System.Linq;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static Modding.Logger;
using System.Collections.Generic;
using ModCommon.Util;

namespace HKHeroControl
{
    public class MPGetter : MonoBehaviour
    {
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<HealthManager>() != null)
            {
                int mp = 12;
                if (PlayerData.instance.equippedCharm_20) mp += 4;
                if (PlayerData.instance.equippedCharm_21) mp += 8;
                HeroController.instance.AddMPCharge(mp);
            }
        }
    }
}

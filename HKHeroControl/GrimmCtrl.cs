using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace HKHeroControl
{
    public class GrimmCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimation animation = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;

        GameObject todo;

        void OnCollisionEnter2D(Collision2D collision) => OnCollisionStay2D(collision);
        void OnCollisionStay2D(Collision2D collision)
        {
        }
    }
}

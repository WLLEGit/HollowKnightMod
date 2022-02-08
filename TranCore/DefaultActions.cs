using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TranCore
{
    public class DefaultActions
    {
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;

        public DefaultActions(tk2dSpriteAnimator animator,Rigidbody2D rigidbody)
        {
            this.animator = animator;
            rig = rigidbody;
        }
        public static bool AlwaysTrue() => true;
        #region HeroControl
        public IEnumerator MoveHeroTo()
        {
            HeroController.instance.transform.position = rig.transform.position;
            yield break;
        }
        public bool MoveHeroToTest() => !HeroController.instance.cState.transitioning && !HeroController.instance.cState.hazardRespawning
            && !HeroController.instance.cState.dead;
        public IEnumerator MoveTranToHero()
        {
            rig.transform.position = HeroController.instance.transform.position;
            yield break;
        }
        public bool MoveTranToHeroTest() => !MoveHeroToTest();
        public IEnumerator Idle()
        {
            animator.Play("Idle");
            yield break;
        }
        public IEnumerator SetTranScale()
        {
            rig.transform.localScale = HeroController.instance.transform.localScale;
            yield break;
        }
        public IEnumerator Turn()
        {
            if (LeftTest())
            {
                HeroController.instance.FaceLeft();
            }
            if (RightTest())
            {
                HeroController.instance.FaceRight();
            }
            yield break;
        }
        public static bool TurnTest() => LeftTest() || RightTest();
        public static bool AttackTest() => InputHandler.Instance.inputActions.attack.IsPressed;
        #endregion
        #region Direction
        public static bool LeftTest() => InputHandler.Instance.inputActions.left.IsPressed;
        public static bool RightTest() => InputHandler.Instance.inputActions.right.IsPressed;
        public static bool UpTest() => InputHandler.Instance.inputActions.up.IsPressed;
        public static bool DownTest() => InputHandler.Instance.inputActions.down.IsPressed;
        #endregion
        #region Dash
        public static bool DashTest() => InputHandler.Instance.inputActions.dash.IsPressed;
        #endregion
        #region Cast
        public static bool CastDownTest() => InputHandler.Instance.inputActions.cast.IsPressed
            || InputHandler.Instance.inputActions.quickCast.IsPressed;
        public static bool CanCast() => PlayerData.instance.MPCharge >= 33;
        public static bool CanCastS() => PlayerData.instance.MPCharge >= 24;
        public static bool CanCastAuto() => PlayerData.instance.equippedCharm_33 ? CanCastS() : CanCast();
        public static void TakeCastMP() => HeroController.instance.TakeMP(33);
        public static void TakeCastSMP() => HeroController.instance.TakeMP(24);
        public static void TakeCastMPAuto()
        {
            if (PlayerData.instance.equippedCharm_33) TakeCastSMP();
            else TakeCastMP();
        }
        #endregion
        #region Run
        public static bool RunTest() => LeftTest() || RightTest();
        
        #endregion
        #region Jump
        
        public static bool JumpTest()
        {
            return InputHandler.Instance.inputActions.jump.IsPressed;
        }
        #endregion
        #region Fall
        bool isFall = false;
        public IEnumerator Fall()
        {
            isFall = true;
            animator.Play("Fall");
            while (rig.velocity.y < -0.1f && isFall) yield return null;
            if (isFall && rig.velocity.y <= 0)
            {
                yield return animator.PlayAnimWait("Land");
            }
        }
        public bool FallTest()
        {
            if (rig.velocity.y < -0.1f) return true;
            else return false;
        }
        public void CancelFall()
        {
            isFall = false;
        }
        #endregion
        public IEnumerator Stop()
        {
            rig.SetVX(0);
            yield return null;
        }
    }
}

using System.Collections;
using System.Linq;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static Modding.Logger;
using System.Collections.Generic;
using ModCommon.Util;
using System;
using ModCommon;
using static UnityEngine.Random;
using Modding;

namespace HKHeroControl
{
    public class SlyCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;

        ColliderGameObject gSlashCollider = null;
        ColliderGameObject cyclonePhysCollider = null;

        ColliderGameObject s1Collider = null;
        ColliderGameObject s3Collider = null;

        void Awake()
        {
            TranAttach.AutoDis = false;
            animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            rig = gameObject.GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);

            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v);
            Destroy(GetComponent<EnemyDeathEffects>());

            foreach (var b in GetComponents<ObjectBounce>())
                Destroy(b);  // 这个组件会让祖师爷触碰到物体时反弹

            gSlashCollider = new ColliderGameObject(gameObject.FindGameObjectInChildren("GS1"));
            var gSharpFlash = gameObject.FindGameObjectInChildren("Sharp Flash");
            var gSlashEffect = gameObject.FindGameObjectInChildren("GSlash Effect");
            gSlashCollider.SetDecorations(new GameObject[] { gSharpFlash, gSlashEffect });

            cyclonePhysCollider = new ColliderGameObject(gameObject.FindGameObjectInChildren("Cyclone Phys"), 1);
            var tink = gameObject.FindGameObjectInChildren("Cyclone Tink");
            cyclonePhysCollider.SetDecorations(new GameObject[] { tink });

            s1Collider = new ColliderGameObject(gameObject.FindGameObjectInChildren("S1"), 5);
            s3Collider = new ColliderGameObject(gameObject.FindGameObjectInChildren("S3"), 5);


            TranAttach.RegisterAction("M1", ActionMoveHeroTo);
            TranAttach.InvokeActionOn("M1", defaultActions.MoveHeroToTest);
            TranAttach.RegisterAction("M2", ActionMoveTranToHero);
            TranAttach.InvokeActionOn("M2", defaultActions.MoveTranToHeroTest);
            TranAttach.RegisterAction("TURN", ActionTurn);
            TranAttach.InvokeActionOn("TURN", DefaultActions.TurnTest);

            TranAttach.RegisterAction("JUMP", ActionJump,
                TranAttach.InvokeWithout("JUMP")
                );
            TranAttach.InvokeActionOn("JUMP", DefaultActions.JumpTest);

            TranAttach.RegisterAction("FALL", ActionFall,
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("FALL", defaultActions.FallTest);

            TranAttach.RegisterAction("STOP", defaultActions.Stop,
                TranAttach.InvokeWithout("STOP"),
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("QUAKE")
                );
            TranAttach.InvokeActionOn("STOP", DefaultActions.AlwaysTrue);

            TranAttach.RegisterAction("RUN", ActionRun,
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("ATTACK"),
                TranAttach.InvokeWithout("RUN"));
            TranAttach.InvokeActionOn("RUN", DefaultActions.RunTest);

            TranAttach.RegisterAction("IDLE", ActionIdle,
                TranAttach.InvokeWithout("IDLE"),
                TranAttach.Or(
                    TranAttach.And(
                        () => TranAttach.InvokeCount == 3,
                        TranAttach.InvokeWith("STOP")
                        ),
                    () => TranAttach.InvokeCount == 2
                ));
            TranAttach.InvokeActionOn("IDLE", DefaultActions.AlwaysTrue);

            //普通攻击
            TranAttach.RegisterAction("ATTACK", ActionAttack,
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("ATTACK"));
            TranAttach.InvokeActionOn("ATTACK", DefaultActions.AttackTest);

            //冲刺攻击
            TranAttach.RegisterAction("DASH", ActionDash,
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("DASH", DefaultActions.DashTest);

            //法球攻击
            TranAttach.RegisterAction("FIREBALL", ActionGSlash,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("FIREBALL", TranAttach.And(
                DefaultActions.CastDownTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.DownTest,
                        DefaultActions.UpTest
                        )
                    )
                ));

            //上吼攻击
            TranAttach.RegisterAction("ROAR", ActionCyclone,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("ROAR", TranAttach.And(
                DefaultActions.CastDownTest,
                DefaultActions.UpTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.DownTest
                        )
                    )
                ));

            //下砸攻击
            TranAttach.RegisterAction("QUAKE", ActionSpinSlash,
                TranAttach.InvokeWithout("FIREBALL"),
                TranAttach.InvokeWithout("ROAR"),
                TranAttach.InvokeWithout("QUAKE"),
                TranAttach.InvokeWithout("DASH")
                //TranAttach.InvokeWithout("JUMP"),
                //TranAttach.InvokeWithout("FALL"),
                //TranAttach.InvokeWithout("RUN")
                );
            TranAttach.InvokeActionOn("QUAKE", TranAttach.And(
                DefaultActions.CastDownTest,
                DefaultActions.DownTest,
                TranAttach.Not(
                    TranAttach.Or(
                        DefaultActions.UpTest
                        )
                    )
                ));

        }

        private IEnumerator ActionDash()
        {
            HeroController.instance.SetDamageMode(1);
            rig.gravityScale = 0;
            rig.SetVX(2 * HeroController.instance.DASH_SPEED * (faceRight?1:-1));
            animator.Play("Dash");
            yield return new WaitForSeconds(0.25f);
            HeroController.instance.SetDamageMode(0);
            rig.gravityScale = 1;
            rig.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ActionCyclone()
        {
            animator.Play("Charge Ground");
            yield return new WaitForSeconds(0.5f);

            cyclonePhysCollider.AttackAntic();
            HeroController.instance.SetDamageMode(1);
            rig.SetVY(20);
            rig.gravityScale = 0.5f;

            animator.Play("Cyclone");
            yield return new WaitForSeconds(2f);

            rig.gravityScale = 1f;
            HeroController.instance.SetDamageMode(0);
            cyclonePhysCollider.CancelAttack();
        }

        bool faceRight = false;
        private IEnumerator ActionTurn()
        {
            faceRight = DefaultActions.RightTest();
            if (faceRight)
                HeroController.instance.FaceRight();
            else
                HeroController.instance.FaceLeft();
            yield break;
        }

        private IEnumerator ActionMoveTranToHero()
        {
            rig.transform.localScale = HeroController.instance.transform.localScale;
            rig.transform.position = HeroController.instance.transform.position;
            yield break;
        }

        private IEnumerator ActionMoveHeroTo()
        {
            rig.transform.localScale = HeroController.instance.transform.localScale;
            HeroController.instance.transform.position = rig.transform.position;
            yield break;
        }

        private IEnumerator ActionIdle()
        {
            animator.Play("Idle");
            yield break;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                Log($"Actions cnt {TranAttach.InvokeCount}: {string.Join(" ", TranAttach.curActs)}");
                Log($"is falling: {defaultActions.FallTest()}");
            }
        }

        private IEnumerator ActionRun()
        {
            if (TranAttach.IsActionInvoking("FALL") || TranAttach.IsActionInvoking("JUMP")
                || TranAttach.IsActionInvoking("ROAR"))
            {
                rig.SetVX(
                    faceRight ?
                    HeroController.instance.RUN_SPEED_CH_COMBO :
                    -HeroController.instance.RUN_SPEED_CH_COMBO
                    );
                yield return null;
            }
            else
            {
                animator.Play("Run");
                rig.SetVX(
                    faceRight ?
                    HeroController.instance.RUN_SPEED_CH_COMBO :
                    -HeroController.instance.RUN_SPEED_CH_COMBO
                    );
                yield return null;
            }
        }

        IEnumerator ActionGSlash()
        {
            rig.gravityScale = 0;
            rig.velocity = new Vector2(0, 0);
            HeroController.instance.SetDamageMode(1);

            animator.Play("Charge Ground");
            yield return new WaitForSeconds(0.3f);
            rig.SetVX(3 * HeroController.instance.DASH_SPEED * (faceRight ? 1 : -1));
            yield return animator.PlayAnimWait("Attack2 S1");

            gSlashCollider.AttackAntic();
            yield return animator.PlayAnimWait("Attack2 S2");

            yield return animator.PlayAnimWait("GSlash End");

            gSlashCollider.CancelAttack();

            rig.SetVX(0);
            HeroController.instance.SetDamageMode(0);
            rig.gravityScale = 1;
            yield return new WaitForSeconds(0.015f);
        }

        IEnumerator ActionAttack()
        {
            string[] anims1 = new string[] { "Antic", "Attack1 S1", "Attack1 S2", "Attack1 S2" };
            string[] anims2 = new string[] { "Attack2 Antic", "Attack2 S1", "Attack2 S2", "Attack2 S3", "Attack2 S4" };
            
            s1Collider.AttackAntic();
            foreach (var anim in anims1)
                yield return animator.PlayAnimWait(anim);
            s1Collider.CancelAttack();

            s3Collider.AttackAntic();
            foreach (var anim in anims2)
                yield return animator.PlayAnimWait(anim);
            s3Collider.CancelAttack();
        }

        IEnumerator ActionSpinSlash()
        {
            rig.gravityScale = 0;
            rig.velocity = new Vector2(10 * (faceRight ? 1 : -1), 10);

            int damage1 = s1Collider.SetAttack(1);
            int damage2 = s3Collider.SetAttack(1);
            s1Collider.AttackAntic();
            s3Collider.AttackAntic();

            HeroController.instance.SetDamageMode(1);

            animator.Play("Spin Slash");
            yield return new WaitForSeconds(0.6f);

            HeroController.instance.SetDamageMode(0);

            rig.gravityScale = 1;
            rig.velocity = Vector2.zero;

            s1Collider.SetAttack(damage1);
            s3Collider.SetAttack(damage2);
            s1Collider.CancelAttack();
            s3Collider.CancelAttack();

            animator.Play("Spin Slash Recover");
            yield return animator.PlayAnimWait("Slash Recover");
        }

        IEnumerator ActionJump()
        {
            yield return animator.PlayAnimWait("Jump Antic");
            animator.Play("Jump");
            rig.SetVY(30);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ActionFall()
        {
            if(!TranAttach.IsActionInvoking("ROAR"))
                animator.Play("Jump");
            yield break;
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
            => false;

        private bool HeroController_CanQuickMap(On.HeroController.orig_CanQuickMap orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private bool HeroController_CanTalk(On.HeroController.orig_CanTalk orig, HeroController self)
            => !HeroController.instance.cState.dead;

        private void _NoDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go,
                        GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
        {
        }

        void OnEnable()
        {
            if (GetComponent<MeshRenderer>() != null)
            {
                GetComponent<MeshRenderer>().enabled = true;    //使实体可见
            }
            rig.isKinematic = false;
            rig.gravityScale = 1;
            foreach (var v in GetComponents<Collider2D>())
            {
                v.enabled = true;
                v.isTrigger = false;
            }
            On.HeroController.CanTalk += HeroController_CanTalk;
            On.HeroController.CanFocus += HeroController_CanFocus;
            On.HeroController.CanQuickMap += HeroController_CanQuickMap;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
        }

        void OnDisable()
        {
            On.HeroController.CanTalk -= HeroController_CanTalk;
            On.HeroController.CanFocus -= HeroController_CanFocus;
            On.HeroController.CanQuickMap -= HeroController_CanQuickMap;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;

            On.HeroController.TakeDamage -= _NoDamage;
        }
    }
}

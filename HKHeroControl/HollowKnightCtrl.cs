using System.Collections;
using System.Linq;
using UnityEngine;
using TranCore;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using static Modding.Logger;
using System.Collections.Generic;

namespace HKHeroControl
{
    public class HollowKnightCtrl : MonoBehaviour
    {
        public TranAttach TranAttach => gameObject.GetTranAttach();
        tk2dSpriteAnimator animator = null;
        Rigidbody2D rig = null;
        DefaultActions defaultActions = null;


        GameObject[] spikes = null;    // 17个地刺

        void OnCollisionEnter2D(Collision2D collision) => OnCollisionStay2D(collision);
        void OnCollisionStay2D(Collision2D collision)
        {
            HealthManager hm = collision.gameObject.GetComponent<HealthManager>() ??
                        collision.otherCollider.GetComponent<HealthManager>();
            if (hm != null)
            {

                if (collisionActions.Select(e => TranAttach.IsActionInvoking(e)).ToList().Any())
                {

                    hm.Hit(new HitInstance()
                    {
                        AttackType = AttackTypes.SharpShadow,
                        Source = gameObject,
                        DamageDealt = PlayerData.instance.nailDamage,
                        Multiplier = 1,
                        MagnitudeMultiplier = 1,
                        CircleDirection = true,
                        IgnoreInvulnerable = false
                    });
                    FSMUtility.SendEventToGameObject(hm.gameObject, "TOOK DAMAGE");
                    FSMUtility.SendEventToGameObject(hm.gameObject, "TAKE DAMAGE");
                }
            }
        }

        void Awake()
        {
            PlayMakerFSM control = gameObject.LocateMyFSM("Control");

            //处理状态机和触发器
            foreach (var v in GetComponents<PlayMakerFSM>()) Destroy(v); //销毁现有状态机组件
            animator = GetComponent<tk2dSpriteAnimator>();
            rig = GetComponent<Rigidbody2D>();
            defaultActions = new DefaultActions(animator, rig);

            TranAttach.AutoDis = false;
            //初始化
            TranAttach.RegisterAction("INITOBJ", InitObj);
            TranAttach.InvokeActionOn("INITOBJ", DefaultActions.AlwaysTrue);

            //基础移动控制
            TranAttach.RegisterAction("M1", defaultActions.MoveHeroTo);
            TranAttach.InvokeActionOn("M1", defaultActions.MoveHeroToTest);
            TranAttach.RegisterAction("M2", defaultActions.MoveTranToHero);
            TranAttach.InvokeActionOn("M2", defaultActions.MoveTranToHeroTest);
            TranAttach.RegisterAction("M3", defaultActions.SetTranScale);
            TranAttach.InvokeActionOn("M3", DefaultActions.AlwaysTrue);
            TranAttach.RegisterAction("TURN", defaultActions.Turn);
            TranAttach.InvokeActionOn("TURN", DefaultActions.TurnTest);

            //跳跃下落控制
            TranAttach.RegisterAction("JUMP", Jump,
                TranAttach.InvokeWithout("JUMP")
                );
            TranAttach.InvokeActionOn("JUMP", DefaultActions.JumpTest);

            TranAttach.RegisterAction("FALL", defaultActions.Fall,
                TranAttach.InvokeWithout("FALL"),
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("FALL", defaultActions.FallTest);

            TranAttach.RegisterAction("STOP", defaultActions.Stop,
                TranAttach.InvokeWithout("STOP"),
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("STOP", DefaultActions.AlwaysTrue);

            TranAttach.RegisterAction("RUN", Run,
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("RUN"));
            TranAttach.InvokeActionOn("RUN", DefaultActions.RunTest);

            //链接技能
            TranAttach.RegisterAction("DASH", Dash,
                TranAttach.InvokeWithout("DASH")
                );
            TranAttach.InvokeActionOn("DASH", DefaultActions.DashTest);

            //普通攻击与空闲状态
            TranAttach.RegisterAction("C", Counter,
                TranAttach.InvokeWithout("C"),
                TranAttach.InvokeWithout("ATTACK")
                );
            TranAttach.InvokeActionOn("C", DefaultActions.AttackTest);
            TranAttach.RegisterAction("ATTACK", Attack,
                TranAttach.InvokeWithout("DASH"),
                TranAttach.InvokeWithout("JUMP"),
                TranAttach.InvokeWithout("RUN"),
                TranAttach.InvokeWith("C"),
                TranAttach.InvokeWithout("ATTACK"));

            TranAttach.RegisterAction("IDLE", Idle,
                TranAttach.Or(
                    TranAttach.And(
                        () => TranAttach.InvokeCount == 7,
                        TranAttach.InvokeWith("STOP")
                        ),
                    () => TranAttach.InvokeCount == 6
                ));
            TranAttach.InvokeActionOn("IDLE", DefaultActions.AlwaysTrue);

            Destroy(GetComponent<EnemyDeathEffects>());
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

        bool isInit = false;
        IEnumerator InitObj()
        {
            if (isInit)
                yield break;
            else
                isInit = true;

        }

        public void CancelCounter()
        {
            isCounter = false;
        }
        bool isCounter = false;
        IEnumerator Counter()
        {
            Log("Counter Start");
            isCounter = true;
            On.HeroController.TakeDamage -= _NoDamage;
            On.HeroController.TakeDamage += _NoDamage;
            yield return DBGPLAY("Counter Antic");
            yield return DBGPLAY("Counter Stance");
            while (InputHandler.Instance.inputActions.attack.IsPressed && isCounter) yield return null;
            if (isCounter)
            {
                TranAttach.InvokeAction("ATTACK");
            }
            On.HeroController.TakeDamage -= _NoDamage;
        }

        IEnumerator Attack()
        {
            yield return DBGPLAY("CS Dir");
            yield return DBGPLAY("CS Antic");
            yield return DBGPLAY("CSlash");
            yield return DBGPLAY("CSlash Recover");
        }

        IEnumerator Run()
        {
            CancelCounter();
            animator.Play("Run");
            rig.SetVX(
                HeroController.instance.cState.facingRight ?
                HeroController.instance.RUN_SPEED_CH_COMBO :
                -HeroController.instance.RUN_SPEED_CH_COMBO
                );
            yield return null;
        }
        IEnumerator Jump()
        {
            Log("Jump Start");
            CancelCounter();
            yield return null;
            rig.velocity = new Vector2(0, 25);
            yield return DBGPLAY("Jump");
            yield return new WaitForSeconds(0.25f);
            DBGANIM("Jump Recover");
        }
        IEnumerator Dash()
        {
            CancelCounter();
            rig.gravityScale = 0;
            On.HeroController.TakeDamage -= _NoDamage;
            On.HeroController.TakeDamage += _NoDamage;
            animator.Play("Dash Antic");
            yield return DBGPLAY("Dash");
            rig.SetVY(0);

            rig.SetVX(HeroController.instance.cState.facingRight ?
                HeroController.instance.DASH_SPEED :
                -HeroController.instance.DASH_SPEED);
            yield return new WaitForSeconds(0.5f);
            rig.SetVX(0);


            yield return DBGPLAY("Dash Recover");
            DBGANIM("Dash End");

            rig.rotation = 0;
            rig.gravityScale = 1;
            On.HeroController.TakeDamage -= _NoDamage;
        }

        IEnumerator Idle()
        {
            DBGANIM("Idle Stance");
            yield return DBGPLAY("Idle");
        }

        private IEnumerator DBGPLAY(string name)
        {
            Log($"animator start: {name}");
            Log($"invoke count: {TranAttach.InvokeCount}");
            yield return animator.PlayAnimWait(name);
            Log("animator end");
        }

        private void DBGANIM(string name)
        {
            Log($"animator start: {name}");
            animator.Play(name);
            Log("animator end");
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

        void OnDisable()
        {
            On.HeroController.CanTalk -= HeroController_CanTalk;
            On.HeroController.CanFocus -= HeroController_CanFocus;
            On.HeroController.CanQuickMap -= HeroController_CanQuickMap;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;

            On.HeroController.TakeDamage -= _NoDamage;
        }

        private List<string> collisionActions = new List<string> { "Dash" };
    }
}

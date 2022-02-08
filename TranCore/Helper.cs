using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ModCommon;

namespace TranCore
{
    public static class Helper
    {
        public static TranAttach GetTranAttach(this GameObject go) => go.GetOrAddComponent<TranAttach>();
        public static void AddAction(this GameObject go, string name, Func<IEnumerator> func, params Func<bool>[] test) =>
            go.GetTranAttach().RegisterAction(name, func, test);
        public static void InvokeAction(this GameObject go, string name) =>
            go.GetTranAttach().InvokeAction(name);
        public static GameObject SetPos(this GameObject go,Vector3 pos)
        {
            go.transform.position = pos;
            return go;
        }
        public static GameObject SetParent(this GameObject go,Transform p)
        {
            go.transform.parent = p;
            return go;
        }
        public static Rigidbody2D SetVX(this Rigidbody2D rig,float x)
        {
            rig.velocity = new Vector2(x, rig.velocity.y);
            return rig;
        }
        public static Rigidbody2D SetVY(this Rigidbody2D rig, float y)
        {
            rig.velocity = new Vector2(rig.velocity.x, y);
            return rig;
        }
        public static GameObject Clone(this GameObject go)
        {
            return UnityEngine.Object.Instantiate(go);
        }
        public static GameObject TranHeroAttack(this GameObject go,AttackTypes type, int damage)
        {
            if (go.GetComponent<DamageHero>() != null) UnityEngine.Object.Destroy(go.GetComponent<DamageHero>());
            if (go.GetComponent<DENoFrameDamage>() != null)
                foreach (var c in go.GetComponents<DENoFrameDamage>())
                    UnityEngine.Object.Destroy(c);
            if (go.GetComponent<DamageEnemies>() != null)
                foreach (var c in go.GetComponents<DamageEnemies>())
                    UnityEngine.Object.Destroy(c);

            DENoFrameDamage damageEnemies = go.AddComponent<DENoFrameDamage>();
            damageEnemies.ignoreInvuln = true;
            damageEnemies.circleDirection = true;
            damageEnemies.magnitudeMult = 1;
            damageEnemies.damageDealt = damage;
            damageEnemies.attackType = type;

            go.layer =(int) GlobalEnums.PhysLayers.HERO_ATTACK;
            return go;
        }

        public static GameObject TranNormalDE(this GameObject go, AttackTypes type, int damage)
        {
            if (go.GetComponent<DamageHero>() != null) UnityEngine.Object.Destroy(go.GetComponent<DamageHero>());
            if (go.GetComponent<DENoFrameDamage>() != null)
                foreach (var c in go.GetComponents<DENoFrameDamage>())
                    UnityEngine.Object.Destroy(c);
            if (go.GetComponent<DamageEnemies>() != null)
                foreach (var c in go.GetComponents<DamageEnemies>())
                    UnityEngine.Object.Destroy(c);

            DamageEnemies damageEnemies = go.AddComponent<DamageEnemies>();
            damageEnemies.ignoreInvuln = true;
            damageEnemies.circleDirection = true;
            damageEnemies.magnitudeMult = 1;
            damageEnemies.damageDealt = damage;
            damageEnemies.attackType = type;

            go.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
            return go;
        }

        public static Func<bool> InvokeWith(this TranAttach t, string name) => () => t.IsActionInvoking(name);
        public static Func<bool> InvokeWithout(this TranAttach t, string name) => () => !t.IsActionInvoking(name);
        public static Func<bool> OnKeyDown(this TranAttach t, KeyCode key) => () => Input.GetKeyDown(key);
        public static Func<bool> OnKeyUp(this TranAttach t, KeyCode key) => () => Input.GetKeyUp(key);
        public static Func<bool> OnKey(this TranAttach t, KeyCode key) => () => Input.GetKey(key);
        public static Func<bool> Or(this TranAttach t, params Func<bool>[] test) => () => test.Any(x => x());
        public static Func<bool> And(this TranAttach t, params Func<bool>[] test) => () => test.All(x => x());
        public static Func<bool> Not(this TranAttach t, Func<bool> test) => () => !test();
        public static Func<bool> EnoughMP(this TranAttach t, int mp) => () => PlayerData.instance.MPCharge >= mp;
    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using static Modding.Logger;

namespace TranCore
{
    public class TranAttach : MonoBehaviour
    {
        Dictionary<string, Action> actions = new Dictionary<string, Action>();

        List<(string, Func<bool>)> events = new List<(string, Func<bool>)>();
        public bool AutoDis { get; set; } = true;
        public int InvokeCount => invokeCount;
        int invokeCount = 0;
        public void RegisterAction(string name, Func<IEnumerator> c,params Func<bool>[] test)
        {
            actions[name] = new Action()
            {
                c = c,
                test = test
            };
        }
        public void InvokeAction(string name)
        {
            if(actions.TryGetValue(name,out var v))
            {
                if (v.test != null && v.test.Length>0)
                {
                    if (!v.test.All(x => x())) return;
                }
                StartCoroutine(_invoke(v));
            }
        }
        public bool IsActionInvoking(string name)
        {
            if(actions.TryGetValue(name,out var v))
            {
                return v.invokeCount > 0;
            }
            return false;
        }
        public int ActionInvokeCount(string name)
        {
            if (actions.TryGetValue(name, out var v))
            {
                return v.invokeCount;
            }
            return 0;
        }
        public IEnumerator InvokeWait(string name)
        {
            if (actions.TryGetValue(name, out var v))
            {
                if (v.test != null && v.test.Length > 0)
                {
                    if (!v.test.All(x => x())) yield break;
                }
                yield return StartCoroutine(_invoke(v));
            }
        }
        public IEnumerator WaitAction(string name)
        {
            if (actions.TryGetValue(name, out var v))
            {
                while (v.invokeCount > 0) yield return null;
            }
        }
        public List<string> curActs = new List<string>();
        IEnumerator _invoke(Action action)
        {
            string funcName = action.c.Method.Name;
            curActs.Add(funcName);

            action.invokeCount++;
            invokeCount++;
            try
            {
                yield return action.c();
            }
            finally
            {
                action.invokeCount--;
                invokeCount--;
                curActs.Remove(funcName);
            }
        }
        public void InvokeActionOn(string name, Func<bool> test)
        {
            if (test != null)
            {
                events.Add((name, test));
            }
        }
        void Awake()
        {
            gameObject.SetActive(false);
            if (GetComponent<ConstrainPosition>() != null) Destroy(GetComponent<ConstrainPosition>());
            try
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
            }
            finally
            {
                foreach (var v in GetComponentsInChildren<Transform>()) v.gameObject.layer = (int)GlobalEnums.PhysLayers.HERO_BOX;
                gameObject.layer = (int)GlobalEnums.PhysLayers.PLAYER;
                foreach (var v in GetComponentsInChildren<DamageHero>()) Destroy(v);
                Destroy(GetComponent<HealthManager>());

                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            }
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (AutoDis)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        void Update()
        {
            if (HeroController.instance == null)
            {
                gameObject.SetActive(false);
                return;
            }
            HeroController.instance.GetComponent<MeshRenderer>().enabled = false;
            HeroController.instance.hero_state = GlobalEnums.ActorStates.no_input;

            foreach(var v in events)
            {
                try
                {
                    if (v.Item2())
                    {
                        InvokeAction(v.Item1);
                    }
                }
                catch (Exception e)
                {
                    Modding.Logger.LogError(e);
                }
            }
        }

        void OnEnable()
        {
            gameObject.transform.position = HeroController.instance.transform.position;
        }
        void OnDisable()
        {
            if (HeroController.instance != null)
            {
                HeroController.instance.EnableRenderer();
                HeroController.instance.hero_state = GlobalEnums.ActorStates.idle;
            }
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            foreach (var v in actions)
            {
                v.Value.invokeCount = 0;
            }
            invokeCount = 0;
        }

        class Action
        {
            public Func<IEnumerator> c;
            public Func<bool>[] test;
            public int invokeCount;
        }
    }
}

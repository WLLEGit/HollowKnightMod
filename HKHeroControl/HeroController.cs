using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine;

#region Acknowledgement
/// <summary>
/// TranCore.dll    作者：空洞实验室    项目地址：https://github.com/HKLab/TranCore   
/// </summary>
#endregion

namespace HKHeroControl
{
    public class HeroController : Mod
    {
        public GameObject FalseKnightGO = null;
        public GameObject GrimmGO = null;
        public override List<(string, string)> GetPreloadNames()
        {
            var res = new List<(string, string)>();
            foreach (var val in configs.Values)
                res.Add((val.HeroScene, val.HeroAssertPath));
            return res;
        }
        public override void Initialize(Dictionary<string, Dictionary<string, UnityEngine.GameObject>> preloadedObjects)
        {
            //InitGameObject<FalseKnightCtrl>(in preloadedObjects, "FalseKnight", out FalseKnightGO);
            InitGameObject<GrimmCtrl>(in preloadedObjects, "Grimm", out GrimmGO);
        }

        private void InitGameObject<T>(in Dictionary<string, Dictionary<string, GameObject>> preloadedObjects, string name, out GameObject go) where T : Component
        {
            var config = configs[name];
            go = UnityEngine.Object.Instantiate(
                preloadedObjects[config.HeroScene][config.HeroAssertPath]
                );

            go.transform.parent = null;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<T>();
        }
        private struct ConfigType
        {
            public string HeroScene;
            public string HeroAssertPath;
            public ConfigType(string scene, string assert)
            {
                HeroScene = scene;
                HeroAssertPath = assert;
            }
        }
        private Dictionary<string, ConfigType> configs = new Dictionary<string, ConfigType>
        {
            {"FalseKnight", new ConfigType("", "") },
            {"Grimm", new ConfigType("", "") }
        };

    }
}

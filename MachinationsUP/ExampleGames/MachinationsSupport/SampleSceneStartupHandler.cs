using MachinationsUP.Engines.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MachinationsUP.ExampleGames.MachinationsSupport
{
    /// <summary>
    /// Handles first-time initialization of a Scene.
    /// </summary>
    public class SampleSceneStartupHandler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            QualitySettings.vSyncCount = 0;  // VSync must be disabled
            Application.targetFrameRate = 20;
            
            Debug.Log("SampleSceneStartupHandler OnBeforeSceneLoadRuntimeMethod.");
            //Get notifications about Scene Loads.
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoadRuntimeMethod()
        {
            Debug.Log("SampleSceneStartupHandler OnAfterSceneLoadRuntimeMethod.");
        }

        static private void SceneManagerOnsceneLoaded (Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "CompleteMainScene")
            {
                Debug.Log("SampleSceneStartupHandler SceneManagerOnsceneLoaded CompleteMainScene.");
                //Provide the MachinationsGameLayer with an IGameLifecycleProvider.
                //This will usually be the Game Engine.
                MachinationsGameLayer.Instance.GameLifecycleProvider = new SampleGameEngine();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeMethodLoad()
        {    
            Debug.Log("SampleSceneStartupHandler OnRuntimeMethodLoad.");
        }
    }
}
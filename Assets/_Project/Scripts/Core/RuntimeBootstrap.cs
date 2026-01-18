using UnityEngine;
using CatTalk2D.Managers;

namespace CatTalk2D.Core
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureManagers()
        {
            EnsureManager<CatEventSystem>("CatEventSystem");
            EnsureManager<CatStateManager>("CatStateManager");
            EnsureManager<InteractionLogger>("InteractionLogger");
        }

        private static void EnsureManager<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindObjectOfType<T>() != null)
            {
                return;
            }

            var go = new GameObject(name);
            go.AddComponent<T>();
            Object.DontDestroyOnLoad(go);
        }
    }
}

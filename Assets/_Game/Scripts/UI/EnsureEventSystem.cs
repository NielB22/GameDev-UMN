using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Makes UI buttons clickable: creates an EventSystem with the Input System UI module
    /// (and its default actions) if the scene has none. Put it on any UI canvas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class EnsureEventSystem : MonoBehaviour
    {
        private void Awake()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}

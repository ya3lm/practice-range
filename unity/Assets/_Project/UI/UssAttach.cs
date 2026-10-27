using UnityEngine;
using UnityEngine.UIElements;

namespace PracticeRange.UI
{
    /// <summary>Attaches MainUI.uss to the document root at runtime (UXML created by bootstrap has no inline style reference).</summary>
    [RequireComponent(typeof(UIDocument))]
    public class UssAttach : MonoBehaviour
    {
        [SerializeField] StyleSheet sheet;
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (sheet != null && root != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
        }
    }
}

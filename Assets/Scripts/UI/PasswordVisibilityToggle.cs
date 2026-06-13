using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 密码框显隐切换（spec 004 Phase 2.5）。挂在眼睛图标的 Button 上，运行期点击切换
    /// 关联 InputField 的 Standard/Password。自身在 Awake 接线，故 prefab 只需序列化两个引用，
    /// 无需持久化 UnityEvent（builder 在运行期程序集，接不了持久监听）。
    /// </summary>
    [DisallowMultipleComponent]
    public class PasswordVisibilityToggle : MonoBehaviour
    {
        public TMP_InputField input;
        public Button button;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(Toggle);
        }

        public void Toggle()
        {
            if (input == null) return;
            input.contentType = input.contentType == TMP_InputField.ContentType.Password
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            input.ForceLabelUpdate();
            input.ActivateInputField();
        }
    }
}

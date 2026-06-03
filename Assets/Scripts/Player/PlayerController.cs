using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 角色平面移动：读取水平/垂直输入，在世界坐标 XZ 平面按可配置速度移动。
    /// 规约见 specs/001-player-movement.md。
    /// 移动数学被抽到纯静态方法 <see cref="ComputeDisplacement"/>，便于 EditMode 单元测试。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("移动速度（单位/秒）")]
        [SerializeField] private float _moveSpeed = 5f;

        private void Update()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            transform.Translate(ComputeDisplacement(h, v, _moveSpeed, Time.deltaTime), Space.World);
        }

        /// <summary>
        /// 纯函数：根据输入轴计算本帧在 XZ 平面的位移向量。
        /// 方向长度上限为 1（避免斜向加速），再乘以速度与帧时长。无副作用，可单元测试。
        /// </summary>
        public static Vector3 ComputeDisplacement(float horizontal, float vertical, float speed, float deltaTime)
        {
            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }
            return direction * (speed * deltaTime);
        }
    }
}

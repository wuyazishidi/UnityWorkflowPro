using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// 2D 俯视角玩家移动。读取 Horizontal/Vertical 输入，在 XY 平面移动。
    /// 移动数学抽到纯静态方法 <see cref="ComputeFrameMove"/>，便于 EditMode 单元测试。
    /// </summary>
    public class PlayerMovement2D : MonoBehaviour
    {
        [Tooltip("移动速度（单位/秒）")]
        [SerializeField] private float _moveSpeed = 6f;

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 delta = ComputeFrameMove(new Vector2(h, v), _moveSpeed, Time.fixedDeltaTime);
            if (_rb != null)
            {
                _rb.MovePosition(_rb.position + delta); // 经物理移动 → 墙体碰撞会阻挡
            }
            else
            {
                transform.Translate(delta.x, delta.y, 0f, Space.World);
            }
        }

        /// <summary>
        /// 纯函数：输入向量归一化（长度上限 1，避免斜向加速），再乘 speed*dt。无副作用。
        /// </summary>
        public static Vector2 ComputeFrameMove(Vector2 input, float speed, float deltaTime)
        {
            Vector2 dir = input;
            if (dir.sqrMagnitude > 1f)
            {
                dir = dir.normalized;
            }
            return dir * (speed * deltaTime);
        }
    }
}

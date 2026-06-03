using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>
    /// 2D 相机平滑跟随目标，锁定相机 Z 深度。目标位置计算抽为纯函数便于单测。
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.15f;
        [SerializeField] private float _zDepth = -10f;

        private Vector3 _velocity;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;
            Vector3 desired = ComputeFollowPosition(_target.position, _zDepth);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
        }

        /// <summary>
        /// 纯函数：相机应处的位置 = 目标 XY + 固定 Z 深度。
        /// </summary>
        public static Vector3 ComputeFollowPosition(Vector3 targetPosition, float zDepth)
        {
            return new Vector3(targetPosition.x, targetPosition.y, zDepth);
        }
    }
}

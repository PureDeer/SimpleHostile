using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace BL767.SimpleHostile
{
    public class Look : MonoBehaviourPunCallbacks
    {
        #region Variables

        // 水平转向就移动玩家
        public Transform player;

        // 垂直转向移动摄像机
        public Transform cams;

        public Transform weapon;

        public float hSensitivity;
        public float vSensitivity;
        public float maxAngle;

        public static bool cursorLocked = true;

        private Quaternion camsCenter;

        #endregion Variables

        #region MonoBehaviour Callbacks

        private void Start()
        {
            // 获得camera作为子组件的Rotation
            camsCenter = cams.localRotation;
        }

        private void Update()
        {
            if (!photonView.IsMine) return;
            SetV();
            SetH();
            UpdateCursorLock();
        }

        #endregion MonoBehaviour Callbacks

        #region Private Methods

        private void SetV()
        {
            // 获得鼠标的V轴移动量
            float _input = Input.GetAxis("Mouse Y") * vSensitivity * Time.deltaTime;
            // 创建一个绕给定轴旋转给定角度的rotation，并通过相乘原始rotation获得新的rotation
            Quaternion _adj = Quaternion.AngleAxis(_input, -Vector3.right);
            Quaternion _delta = cams.localRotation * _adj;

            // 改变摄像机的旋转角度，并限制角度.
            // Angle()返回的是摄像机偏移和摄像机原来位置所产生的角度∠ACB
            cams.localRotation = Quaternion.Angle(camsCenter, _delta) < maxAngle ? _delta : cams.localRotation;
            // 将垂直摄像机的旋转向量赋给武器
            weapon.rotation = cams.rotation;
        }

        private void SetH()
        {
            // 获得鼠标的h轴移动量
            float _input = Input.GetAxis("Mouse X") * hSensitivity * Time.deltaTime;
            Quaternion _adj = Quaternion.AngleAxis(_input, Vector3.up);
            player.localRotation *= _adj;
        }

        private void UpdateCursorLock()
        {
            // 判断是否锁定鼠标
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            // 鼠标可见度与鼠标锁定状态时相反的
            Cursor.visible = !cursorLocked;
            // 判断是否有按键事件
            cursorLocked = !Input.GetKeyDown(KeyCode.Escape);
        }

        #endregion Private Methods
    }
}
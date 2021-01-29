using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BL767.SimpleHostile
{
    public class Sway : MonoBehaviour
    {
        #region Variables

        public float intensity;
        public float smooth;

        internal bool isMine;
        private Quaternion origin_rotation;

        #endregion Variables

        #region MonoBehaviour Callbacks

        private void Start()
        {
            origin_rotation = transform.localRotation;
        }

        private void Update()
        {
            UpdateSway();
        }

        #endregion MonoBehaviour Callbacks

        #region Private Methods

        private void UpdateSway()
        {
            /*
             Controls
             */
            // 鼠标移动量
            float t_x_mouse = Input.GetAxis("Mouse X");
            float t_y_mouse = Input.GetAxis("Mouse Y");

            // 重置武器角度
            if (!isMine)
            {
                t_x_mouse = 0;
                t_y_mouse = 0;
            }

            // 计算移动角度
            Quaternion t_x_adj = Quaternion.AngleAxis(-intensity * t_x_mouse, Vector3.up);
            Quaternion t_y_adj = Quaternion.AngleAxis(intensity * t_y_mouse, Vector3.right);
            Quaternion target_rotation = origin_rotation * t_x_adj * t_y_adj;

            // 让武器往目标角度进行插值，造成延迟效果，因为每帧都会更新，都会进行插值
            transform.localRotation = Quaternion.Lerp(transform.localRotation, target_rotation, Time.deltaTime * smooth);
        }

        #endregion Private Methods
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace BL767.SimpleHostile
{
    public class Player : MonoBehaviourPunCallbacks
    {
        #region Variables

        public float speed;
        public float speedMod = 2;
        public float slideMod;
        public float jumpForce = 400;
        public float lengthOfSlide;
        public int max_health;

        public Camera normalCam;
        public GameObject cameraParent;

        public Transform weaponParent;

        public Transform groundDetector;
        public LayerMask ground;

        private Vector3 weapPareOriPos;
        private Vector3 targetWeaBobPos;
        private Vector3 weaPareCurPos;

        private int cur_health;
        private Transform ui_HPbar;
        private Text ui_ammo;

        private float idleCounter;
        private float movementCounter;
        private Rigidbody rig;

        private float baseFOV;
        private Vector3 originCamPos;
        private readonly float sprintFOVMod = 1.4f;

        private Weapon weapon;
        private Manager manager;

        private bool sliding;
        private float slideTime;
        private Vector3 slideDir;

        /*
         封装motion的中间数据和判断
         用于Update()和FixedUpdate()的互相沟通
         */
        internal bool isJumping;
        internal bool isSprinting;
        internal bool isGrounded;
        internal bool isSliding;

        #endregion Variables

        #region MonoBehaviour Callbacks

        private void Start()
        {
            // 判定操控角色是否是自己，是的话启动摄像机
            cameraParent.SetActive(photonView.IsMine);
            // 如果该玩家不是我们自己，那么就设layer为可被射击的
            if (!photonView.IsMine) gameObject.layer = 11;

            // 原始状态的视野宽度
            baseFOV = normalCam.fieldOfView;
            // 保存人物摄像机原始位置
            originCamPos = normalCam.transform.localPosition;

            // 关闭全局摄像机
            if (Camera.main) Camera.main.enabled = false;
            rig = GetComponent<Rigidbody>();

            // 记住武器原始位置
            // weapon跟Player挂载在同一个物件上
            weapPareOriPos = weaponParent.localPosition;
            weaPareCurPos = weapPareOriPos;
            weapon = GetComponent<Weapon>();

            // 寻找Manager物件，获得挂载的Manager脚本
            manager = GameObject.Find("Manager").GetComponent<Manager>();

            // 初始化HPbar
            cur_health = max_health;
            if (photonView.IsMine)
            {
                ui_HPbar = GameObject.Find("HUD/Health/HPbar").transform;
                ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                RefreshHPbar();
            }
        }

        /// <summary>
        /// not continuous, discrete...
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U)) TakeDamage(25);
            // 通过photonView判断是否是本人操控
            if (!photonView.IsMine) return;

            #region Axles

            float _hMove = Input.GetAxisRaw("Horizontal");
            float _vMove = Input.GetAxisRaw("Vertical");

            #endregion Axles

            #region Controls

            // 判断奔跑，并且限制后退的状况下不可奔跑
            bool _sprint = Input.GetKey(KeyCode.LeftShift);
            bool _jump = Input.GetKeyDown(KeyCode.Space);
            bool _slide = Input.GetKey(KeyCode.C);

            #endregion Controls

            #region States

            // 使用射线判断是否在地上
            // 从transform原点开始，朝着场景中的所有碰撞体，沿点方向，投射一个长度为maxDistance的射线
            // 提供layermask过滤掉不想碰撞的object
            isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            isJumping = _jump && isGrounded;
            isSprinting = _sprint && _vMove > 0 && !isJumping && isGrounded;
            isSliding = isSprinting && _slide && !isSliding;

            // jumping
            // 向刚体施加向上的力
            // 按键响应的判断应该放在Update里，否则会出现掉帧现象，因为Update里每一帧会重置响应，所以才正常
            if (isJumping) rig.AddForce(Vector3.up * jumpForce);

            // HeadBobbing
            // 1.Idle head Bobbing; 2.Motion Head Bobbing
            if (isSliding)
            {
                print("donothing");
            }
            else if (_hMove == 0 && _vMove == 0)
            {
                HeadBob(idleCounter, 0.03f, 0.03f);
                idleCounter += Time.deltaTime;
                weaponParent.localPosition = Vector3.Slerp(weaponParent.localPosition, targetWeaBobPos, Time.deltaTime * 2.0f);
            }
            else if (isSprinting)
            {
                HeadBob(movementCounter, 0.15f, 0.075f);
                movementCounter += Time.deltaTime * 7.0f;
                weaponParent.localPosition = Vector3.Slerp(weaponParent.localPosition, targetWeaBobPos, Time.deltaTime * 10.0f);
            }
            else
            {
                HeadBob(movementCounter, 0.035f, 0.035f);
                movementCounter += Time.deltaTime * 3.0f;
                weaponParent.localPosition = Vector3.Slerp(weaponParent.localPosition, targetWeaBobPos, Time.deltaTime * 6.0f);
            }

            #endregion States

            RefreshHPbar();
            weapon.RefreshAmmo(ui_ammo);
        }

        /// <summary>
        /// FixedUpdate()适合用于解决物理上的问题(RigidBody)，movement
        /// continuous dynamic function.
        /// </summary>
        private void FixedUpdate()
        {
            if (!photonView.IsMine) return;

            #region Axles

            float _hMove = Input.GetAxisRaw("Horizontal");
            float _vMove = Input.GetAxisRaw("Vertical");

            #endregion Axles

            #region Movement

            Vector3 _direction;
            float _adjSpeed = speed;

            if (!sliding)
            {
                // 方向向量
                _direction = new Vector3(_hMove, 0, _vMove);
                // 如果同时按下W和D，那么让向量长度仍然为1而不是根号2
                _direction.Normalize();
                _direction = transform.TransformDirection(_direction);

                // 跑动时候禁止瞄准
                if (Input.GetMouseButton(1) && isSprinting) weapon.Aim(false);
                if (isSprinting) _adjSpeed *= speedMod;
            }
            else
            {
                // 当进入滑行状态时，刚体运动方向就是滑行方向，且滑行时间逐渐减少
                _direction = slideDir;
                _adjSpeed *= slideMod;
                slideTime -= Time.fixedDeltaTime;
                if (slideTime <= 0)
                {
                    sliding = false;
                    weaPareCurPos += Vector3.up * 0.1f;
                    print(weaPareCurPos);
                }
            }

            // 缓存向量；将刚体的速度向量从本地空间转到全局空间, 避免刚体移动的方向和摄像机(鼠标控制的摄像)的方向不相同
            Vector3 _targetVelocity = _direction * _adjSpeed * Time.fixedDeltaTime;
            // 将缓存向量的垂直轴向量设为刚体在跳跃时被施加的向上的力的向量，然后将缓存向量设为刚体最终的移动向量
            _targetVelocity.y = rig.velocity.y;
            // 将刚体的最终运动向量设为缓存向量
            rig.velocity = _targetVelocity;

            // sliding以及FOV
            if (isSliding)
            {
                sliding = true;
                slideDir = _direction;
                slideTime = lengthOfSlide;
                // 调整像机高度
                weaPareCurPos += Vector3.down * 0.1f;
            }

            if (sliding)
            {
                // 给奔跑加上视野加宽效果，使用Lerp线性插值
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVMod * 1.25f, Time.fixedDeltaTime * 8f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, originCamPos + Vector3.down * 0.5f, Time.fixedDeltaTime * 6f);
            }
            else
            {
                if (isSprinting) Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVMod, Time.fixedDeltaTime * 8f);
                else Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.fixedDeltaTime * 8f);

                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, originCamPos, Time.fixedDeltaTime * 6f);
            }

            #endregion Movement
        }

        #endregion MonoBehaviour Callbacks

        #region Private Methods

        /// <summary>
        /// 使用正弦余弦函数移动武器，制造任务bobbing效果
        /// </summary>
        /// <param name="angle">给予正弦余弦公式所需的角度</param>
        /// <param name="p_x_intensity">x缓动系数</param>
        /// <param name="p_y_intensity">y缓动系数</param>
        private void HeadBob(float angle, float p_x_intensity, float p_y_intensity)
        {
            targetWeaBobPos = weaPareCurPos + new Vector3(Mathf.Cos(angle) * p_x_intensity, Mathf.Sin(angle * 2) * p_y_intensity, 0);
        }

        private void RefreshHPbar()
        {
            float t_hp_ratio = (float)cur_health / max_health;
            // 对HPbar的x_scale减少进行插值，deltatime的倍数越大，减少速度越快
            ui_HPbar.localScale = Vector3.Lerp(ui_HPbar.localScale, new Vector3(t_hp_ratio, 1, 1), Time.deltaTime * 6f);
        }

        #endregion Private Methods

        #region Public Methods

        internal void TakeDamage(int p_damage)
        {
            if (photonView.IsMine)
            {
                cur_health -= p_damage;
                RefreshHPbar();
            }

            // 摧毁当前挂载的对象
            if (cur_health <= 0)
            {
                Destroy(gameObject);
                manager.Spawn();
            }
        }

        #endregion Public Methods
    }
}
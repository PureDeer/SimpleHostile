using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace BL767.SimpleHostile
{
    public class Weapon : MonoBehaviourPunCallbacks
    {
        #region Variables

        // Gun Data
        public Gun[] loadout;

        public Transform weaponParent;
        public GameObject bulletHole;
        public LayerMask canBeShot;

        private GameObject currentWeapon;
        private int curWeaponIdx;
        private Player playerMotion;
        private float curCooldown;

        private bool isReloading;

        #endregion Variables

        #region MonoBehaviour Callbacks

        private void Start()
        {
            playerMotion = GetComponent<Player>();
            // 初始化武器的子弹
            foreach (Gun gun in loadout) gun.Initialize();
            Equip(0);
        }

        private void Update()
        {
            // RPC，远程调用函数
            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1)) photonView.RPC("Equip", RpcTarget.All, 0);
            if (currentWeapon && !playerMotion.isSprinting)
            {
                if (photonView.IsMine)
                {
                    Aim(Input.GetMouseButton(1));
                    if (Input.GetMouseButtonDown(0) && curCooldown <= 0)
                    {
                        if (loadout[curWeaponIdx].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                        else StartCoroutine(Reload(loadout[curWeaponIdx].reloadT));
                    }

                    if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(Reload(loadout[curWeaponIdx].reloadT));

                    // 冷却计算
                    if (curCooldown > 0) curCooldown -= Time.deltaTime;
                }

                // 武器位置重置，每次都会尝试将武器的位置重置为0
                currentWeapon.transform.localPosition =
                    Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4.0f);
            }
        }

        #endregion MonoBehaviour Callbacks

        #region Private Methods

        internal IEnumerator Reload(float p_reloadT)
        {
            isReloading = true;
            currentWeapon.SetActive(false);

            // 启动协程，而不冻住脚本剩下的代码，即这里会停止，但update仍会继续
            yield return new WaitForSeconds(p_reloadT);
            loadout[curWeaponIdx].Reload();

            isReloading = false;
            currentWeapon.SetActive(true);
        }

        // 标记为远程调用方程
        [PunRPC]
        internal void Equip(int p_idx)
        {
            // 避免重复出现武器
            if (currentWeapon)
            {
                if (isReloading) StopCoroutine("Reload");
                Destroy(currentWeapon);
            }
            curWeaponIdx = p_idx;

            // 类似于克隆(Duplicate)，动态创建一个object。父对象将被分配给新对象
            GameObject t_newWeapon = Instantiate(loadout[curWeaponIdx].prefab, weaponParent.position, weaponParent.rotation, weaponParent);
            // 将新对象的位置，角度设为0
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;
            t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

            currentWeapon = t_newWeapon;
        }

        [PunRPC]
        internal void TakeDamage(int p_damage)
        {
            GetComponent<Player>().TakeDamage(p_damage);
        }

        internal void Aim(bool p_isAiming)
        {
            // 寻找子物件
            Transform t_anchor = currentWeapon.transform.Find("Anchor");
            Transform t_state_ADS = currentWeapon.transform.Find("States/ADS");
            Transform t_state_Hip = currentWeapon.transform.Find("States/Hip");

            if (p_isAiming)
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ADS.position, Time.deltaTime * loadout[curWeaponIdx].aimSpeed);
            else
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_Hip.position, Time.deltaTime * loadout[curWeaponIdx].aimSpeed);
        }

        // 标记为远程调用方程
        [PunRPC]
        internal void Shoot()
        {
            // 存储视野
            Transform t_spawn = transform.Find("Camera/Normal Camera");

            // 枪支精确度。让子弹分散。
            // 发射点加上发射方向乘距离1000，得到发射向量a
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000.0f;
            // 垂直方向和水平方向的分散点位置，即bloom的位置加上代表分散点的向量
            t_bloom += Random.Range(-loadout[curWeaponIdx].bloom, loadout[curWeaponIdx].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[curWeaponIdx].bloom, loadout[curWeaponIdx].bloom) * t_spawn.right;
            // 转成方向, 分散点的位置减摄像机位置等于摄像机到分散点的向量
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();

            // 武器发射速率。每发射一次更新一次
            curCooldown = loadout[curWeaponIdx].fireRate;

            // raycast, 使用计算好的分散向量代替
            bool isHit = Physics.Raycast(t_spawn.position, t_bloom, out RaycastHit t_hit, 1000.0f, canBeShot);

            if (isHit)
            {
                // normal是垂直于hitpoint面的法向量，加上他，可以让物体也垂直于法向量平铺在表面，lookat使他朝向它的法线
                GameObject t_newHole = Instantiate(bulletHole, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity);
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                // 在给定时间内移除子弹孔
                Destroy(t_newHole, 5.0f);
                // 如果射击到其他玩家，那么要更新所有链接的状态
                if (photonView.IsMine && t_hit.collider.gameObject.layer == 11)
                {
                    // 调用被击中的玩家的函数
                    t_hit.collider.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[curWeaponIdx].damage);
                }
            }

            // gunfix
            // 开火的后座力，上下与前后移动
            if (currentWeapon)
            {
                currentWeapon.transform.Rotate(-loadout[curWeaponIdx].recoil, 0, 0);
                currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[curWeaponIdx].kickback;
                if (!photonView.IsMine) currentWeapon.transform.position = weaponParent.transform.position;
            }
        }

        #endregion Private Methods

        #region Public Methods

        public void RefreshAmmo(Text p_text)
        {
            int t_clip = loadout[curWeaponIdx].Clip;
            int t_stash = loadout[curWeaponIdx].Stash;

            p_text.text = t_clip.ToString("D2") + "/" + t_stash;
        }

        #endregion Public Methods
    }
}
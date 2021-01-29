using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BL767.SimpleHostile
{
    // access the prefabs through Gun class
    // view doc: Manual -> Scripting -> Important Classes -> ScriptableObject
    [CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string gunName;
        public int damage;

        // 一开始所拥有的所有子弹数
        public int ammo;

        // 武器的弹匣最大子弹数量
        public int clipsize;

        public float fireRate;
        public float bloom;
        public float kickback;
        public float recoil;
        public float aimSpeed;
        public float reloadT;
        public GameObject prefab;

        // 目前手上除弹匣之外拥有的所有子弹总数
        private int stash;

        public int Stash { get => stash < 0 ? 0 : stash; set => stash = value; }

        // 目前弹匣内有的子弹数量
        private int clip;

        public int Clip { get => clip < 0 ? 0 : clip; set => clip = value; }

        internal void Initialize()
        {
            Stash = ammo;
            Clip = clipsize;
        }

        internal bool FireBullet() => Clip-- > 0;

        internal void Reload()
        {
            // 总子弹数就等于弹匣内子弹数 + 弹匣外子弹数
            Stash += Clip < 0 ? 0 : Clip;
            // 填充弹匣，取size和所补充的子弹数
            Clip = Mathf.Min(clipsize, Stash);
            // 总子弹数减去弹匣内数 = 弹匣外子弹数
            Stash -= Clip;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace BL767.SimpleHostile
{
    public class Manager : MonoBehaviour
    {
        public string player_prefab;
        public Transform[] spawn_points;

        private void Start()
        {
            Spawn();
        }

        public void Spawn()
        {
            // 选定复活地点
            Transform t_spawn = spawn_points[Random.Range(0, spawn_points.Length)];
            // 初始化物体
            PhotonNetwork.Instantiate(player_prefab, t_spawn.position, t_spawn.rotation);
        }
    }
}
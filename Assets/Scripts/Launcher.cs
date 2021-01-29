using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 联机
using Photon.Pun;
using System;

namespace BL767.SimpleHostile
{
    /// <summary>
    /// 回调函数就是某些特定事件触发后再调用的函数
    /// </summary>
    public class Launcher : MonoBehaviourPunCallbacks
    {
        public void Awake()
        {
            // 与宿主机同步
            PhotonNetwork.AutomaticallySyncScene = true;
            // 初始化之后就开始链接
            Connect();
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected!");
            // 链接到宿主之后立刻加入match
            Join();
            base.OnConnectedToMaster();
        }

        public override void OnJoinedRoom()
        {
            StartGame();
            base.OnJoinedRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // 如果加入失败，就创建房间
            Create();
            base.OnJoinRandomFailed(returnCode, message);
        }

        public void Create()
        {
            PhotonNetwork.CreateRoom("");
        }

        public void Connect()
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Trying to connect...");
        }

        public void Join()
        {
            PhotonNetwork.JoinRandomRoom();
        }

        public void StartGame()
        {
            // levelNumber对应Unity->file->build setting
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                PhotonNetwork.LoadLevel(1);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tset : MonoBehaviour
{
    void Update()
    {
        //测试 后续把这个直接扔到拆轨道那里去 拆完马上开始上浮
        //背包系统还没做 上层漂浮缺乏一层判定 下沉缺一层判定

        if (Input.GetKeyDown(KeyCode.T))
        {
            Player player = FindObjectOfType<Player>();
            player.GameplayStatus.AcquireWrench();
            player.TryStartRailRemovedAscend();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Player player = FindObjectOfType<Player>();
            player.GameplayStatus.PutItemInSlot(PlayerSlotItemKind.FloatingSmallItem);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Player player = FindObjectOfType<Player>();
            player.TryReleaseFloatingItemAndRise();
        }

    }
}

/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * MIT License
 * 
 * Copyright (c) 2019 gpsnmeajp
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
#pragma warning disable 0414,0219
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;
using akr.Unity.Windows;

namespace EVMC4U
{
    public class WindowExtensionsReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("WindowExtensionsReceiver v1.0")]
        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用

        public MeshRenderer BackgroundSphereRenderer;
        public WindowManager windowManager;

        [Header("Receive Status(Read only)")]
        public Color backgroundColor = Color.clear;
        [Header("Window Attribute Info(Read only)")]
        public bool IsTopMost = false;
        public bool IsTransparent = false;
        public bool WindowClickThrough = false;
        public bool HideBorder = false;

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        void Start()
        {
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);
            StatusMessage = "Waiting for Master...";
        }

        //デイジーチェーンを更新
        public void UpdateDaisyChain()
        {
            externalReceiverManager.GetIExternalReceiver(NextReceivers);
        }

        public void MessageDaisyChain(ref uOSC.Message message, int callCount)
        {
            //Startされていない場合無視
            if (externalReceiverManager == null || enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            if (shutdown)
            {
                return;
            }

            StatusMessage = "OK";

            //異常を検出して動作停止
            try
            {
                ProcessMessage(ref message);
            }
            catch (Exception e)
            {
                StatusMessage = "Error: Exception";
                Debug.LogError(" --- Communication Error ---");
                Debug.LogError(e.ToString());
                shutdown = true;
                return;
            }

            if (!externalReceiverManager.SendNextReceivers(message, callCount))
            {
                StatusMessage = "Infinite loop detected!";
                shutdown = true;
            }
        }

        private void ProcessMessage(ref uOSC.Message message)
        {
            //メッセージアドレスがない、あるいはメッセージがない不正な形式の場合は処理しない
            if (message.address == null || message.values == null)
            {
                StatusMessage = "Bad message.";
                return;
            }

            //V2.4 背景色情報
            if (message.address == "/VMC/Ext/Setting/Color"
                && (message.values[0] is float)
                && (message.values[1] is float)
                && (message.values[2] is float)
                && (message.values[3] is float))
            {
                backgroundColor = new Color((float)message.values[0], (float)message.values[1], (float)message.values[2], (float)message.values[3]);
                BackgroundSphereRenderer.materials[0].SetColor(Shader.PropertyToID("_Color"), backgroundColor);
            }
            //V2.4 ウィンドウ情報
            else if (message.address == "/VMC/Ext/Setting/Win"
                && (message.values[0] is int)
                && (message.values[1] is int)
                && (message.values[2] is int)
                && (message.values[3] is int))
            {
                IsTopMost = (int)message.values[0] != 0;
                IsTransparent = (int)message.values[1] != 0;
                WindowClickThrough = (int)message.values[2] != 0;
                HideBorder = (int)message.values[3] != 0;

                windowManager.SetWindowAlwaysTopMost(IsTopMost);
                windowManager.SetWindowBackgroundTransparent(IsTransparent,backgroundColor);
                windowManager.SetThroughMouseClick(WindowClickThrough);
                windowManager.SetWindowBorder(HideBorder);
            

            }
        }
    }
}
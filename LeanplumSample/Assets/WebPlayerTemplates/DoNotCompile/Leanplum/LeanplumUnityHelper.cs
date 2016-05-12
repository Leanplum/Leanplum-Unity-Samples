// Copyright 2013, Leanplum, Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

namespace LeanplumSDK
{
    /// <summary>
    ///     Provides a class that is implemented in a MonoBehaviour so that Unity functions can be
    ///     called through the GameObject.
    ///
    /// </summary>
    public class LeanplumUnityHelper : MonoBehaviour
    {
        private static LeanplumUnityHelper instance;

        internal static List<Action> delayed = new  List<Action>();

        private enum ModalType { Message, MessageWithText };

        private class Modal
        {
            public ModalType Type;
            public Action<string> Callback;
            public string Title;
            public string Message;
            public string TextResponse;
        }
        private Modal activeModal;
        private bool developerModeEnabled;

        public static LeanplumUnityHelper Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                // Object not found - create and store a new one.
                instance = FindObjectOfType(typeof(LeanplumUnityHelper)) as LeanplumUnityHelper;

                GameObject container = new GameObject("LeanplumUnityHelper", typeof(LeanplumUnityHelper));
                instance = container.GetComponent<LeanplumUnityHelper>();
                if (instance == null)
                {
                    LeanplumNative.CompatibilityLayer.LogError("Problem during the creation of LeanplumUnityHelper.");
                }
                return instance;
            }

            private set
            {
                instance = value;
            }
        }

        public void NativeCallback(string message)
        {
            LeanplumFactory.SDK.NativeCallback(message);
        }

        private void Start()
        {
            developerModeEnabled = Leanplum.IsDeveloperModeEnabled;
            activeModal = null;

            // Prevent Unity from destroying this GameObject when a new scene is loaded.
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnGUI()
        { 
            if (activeModal != null)
            {
                Rect box = MakeRectAtCenter(350, 150);
                GUI.ModalWindow(0, box, DrawModal, activeModal.Title);
            }
        }

        private void OnApplicationQuit()
        {
            LeanplumNative.CompatibilityLayer.FlushSavedSettings();
            if (LeanplumNative.calledStart)
            {
                LeanplumNative.Stop();
            }
            LeanplumNative.isStopped = true;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!LeanplumNative.calledStart)
            {
                return;
            }

            if (isPaused)
            {
                LeanplumNative.Pause();
            }
            else
            {
                LeanplumNative.Resume();
            }
        }

        private void Update()
        {
            // Workaround so that CheckVarsUpdate() is invoked on Unity's main thread.
            // This is called by Unity on every frame.
            if (VarCache.VarsNeedUpdate && developerModeEnabled && Leanplum.HasStarted)
            {
                VarCache.CheckVarsUpdate();
            }

            // Run deferred actions.
            List<Action> actions = null;
            lock (delayed)
            {
                if (delayed.Count > 0)
                {
                    actions = new List<Action>(delayed);
                    delayed.Clear();
                }
            }
            if (actions != null)
            {
                foreach (Action action in actions)
                {
                    action();
                }
            }
        }

        internal void StartRequest(string url, WWWForm wwwForm, Action<WebResponse> responseHandler,
                                   int timeout, bool isAsset = false)
        {
            StartCoroutine(RunRequest(url, wwwForm, responseHandler, timeout, isAsset));
        }

        private static IEnumerator RunRequest(string url, WWWForm wwwForm, Action<WebResponse> responseHandler,
                                              int timeout, bool isAsset)
        {
            WWW www;

            // If this is an assetbundle download request, try loading from cache first.
            if (isAsset)
            {
                // Set an arbitrary version number - we identify different versions of assetbundles with
                // different filenames in the url.
                www = WWW.LoadFromCacheOrDownload(url, 1);
            }
            else
            {
                www = wwwForm == null ? new WWW(url) : new WWW(url, wwwForm);
            }

            // Create a timer to check for timeouts.
            var timeoutTimer = new Timer(timeout * 1000);
            timeoutTimer.Elapsed += delegate {
                timeoutTimer.Stop();
                www.Dispose();
                QueueOnMainThread(() =>
                {
                    responseHandler(new UnityWebResponse(Constants.NETWORK_TIMEOUT_MESSAGE, String.Empty, null));
                });
            };
            timeoutTimer.Start();

            yield return www;

            // If the timer is still enabled, the request didn't time out.
            if (timeoutTimer.Enabled)
            {
                timeoutTimer.Stop();
                responseHandler(new UnityWebResponse(www.error,
                                                     String.IsNullOrEmpty(www.error) && !isAsset ? www.text : null,
                                                     String.IsNullOrEmpty(www.error) ? www.assetBundle : null));
                www.Dispose();
            }
        }

        internal void DisplayMessageModal(string title, string message)
        {
            activeModal = new Modal
            {
                Title = title,
                Message = message,
                Type = ModalType.Message
            };
        }

        internal void DisplayTextModal(string title, string message, Action<string> callback)
        {
            activeModal = new Modal
            {
                Title = title,
                Message = message,
                Callback = callback,
                TextResponse = "",
                Type = ModalType.MessageWithText
            };
        }

        private Rect MakeRectAtCenter(int width, int height)
        {
            // Calculate coordinates to draw a box at the center of the screen.
            return new Rect((Screen.width - width) / 2, (Screen.height - height) / 3, width, height);
        }

        private void DrawModal(int windowID)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(activeModal.Message);
            if (activeModal.Type == ModalType.MessageWithText)
            {
                GUILayout.FlexibleSpace();
                activeModal.TextResponse = GUILayout.TextField(activeModal.TextResponse);
            }
            GUILayout.FlexibleSpace();

            // Draw buttons.
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close"))
            {
                activeModal = null;
            }
            if (activeModal != null && activeModal.Type == ModalType.MessageWithText)
            {
                if (GUILayout.Button("Submit") && activeModal.TextResponse != "") {
                    if (activeModal.Callback != null)
                    {
                        activeModal.Callback(activeModal.TextResponse);
                    }
                    activeModal = null;
                }
            }
            GUILayout.EndHorizontal();
        }

        internal static void QueueOnMainThread(Action method)
        {
            lock (delayed)
            {
                delayed.Add(method);
            }
        }
    }
}

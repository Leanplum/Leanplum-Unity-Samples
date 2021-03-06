﻿// Copyright 2014, Leanplum, Inc.

using LeanplumSDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeanplumWrapper : MonoBehaviour
{
    public string AppID;
    public string ProductionKey;
    public string DevelopmentKey;
    public string AppVersion;

	void Awake()
	{
		if (Application.isEditor)
		{
			LeanplumFactory.SDK = new LeanplumNative();
		}
		else
		{
			// NOTE: Currently, the native iOS and Android SDKs do not support Unity Asset Bundles.
			// If you require the use of asset bundles, use LeanplumNative on all platforms.
			#if UNITY_IPHONE
			LeanplumFactory.SDK = new LeanplumIOS();
			#elif UNITY_ANDROID
			LeanplumFactory.SDK = new LeanplumAndroid();
			#else
			LeanplumFactory.SDK = new LeanplumNative();
            #endif
        }
    }
    
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

		SocketUtilsFactory.Utils = new SocketUtils();
        
        if (!string.IsNullOrEmpty(AppVersion))
        {
            Leanplum.SetAppVersion(AppVersion);
        }
        if (string.IsNullOrEmpty(AppID) || string.IsNullOrEmpty(ProductionKey) || string.IsNullOrEmpty(DevelopmentKey))
        {
            Debug.LogError("Please make sure to enter your AppID, Production Key, and " +
                           "Development Key in the Leanplum GameObject inspector before starting.");
        }

        if (Debug.isDebugBuild)
        {
            Leanplum.SetAppIdForDevelopmentMode(AppID, DevelopmentKey);
        }
        else
        {
            Leanplum.SetAppIdForProductionMode(AppID, ProductionKey);
        }

		#if UNITY_IPHONE
		Leanplum.RegisterForIOSRemoteNotifications();
		#elif UNITY_ANDROID
		// Registering for Push in Android - using the Built-in Sender ID
		Leanplum.SetGcmSenderId(Leanplum.LeanplumGcmSenderId);

		// This would be used if using your own sender ID.
//		Leanplum.SetGcmSenderId("123456790abcdef");

		// In this case using both sender IDs, for ex. if you want Leanplum to send notifications through Leanplum's ID
		// and your own server to send notifications through your own ID.
//		Leanplum.SetGcmSenderIds("123456790abcdef", Leanplum.LeanplumGcmSenderId);
		#endif
		
	Leanplum.Started += delegate(bool success) {
		Debug.Log("### Leanplum started");
	};

	Leanplum.VariablesChanged += delegate {
		Debug.Log("### Variables callback ");
	};

        Leanplum.Start();
    }
}

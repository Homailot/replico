using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using System.Reflection;
using Unity.VisualScripting;

/// <summary>
/// Persists the hmd and controller state of the XRDeviceSimulator in PlayerPrefs.
/// Tested on XRInteractionToolkit 2.0.3.
/// </summary>
[RequireComponent(typeof(XRDeviceSimulator))]
public class XRDeviceSimulatorPersist : MonoBehaviour
{
    [Tooltip("Save current state of HMD and controllers to player prefs")]
    public bool SaveCurrentState = false;

    [Tooltip("Apply saved state of HMD and controllers on startup")]
    public bool ApplySavedStateOnStartup = true;

    // player prefs strings
    const string PREF_HMD = "XRDeviceSimulatorPersist.HMD";
    const string PREF_LEFT = "XRDeviceSimulatorPersist.LEFT";
    const string PREF_RIGHT = "XRDeviceSimulatorPersist.RIGHT";

    XRDeviceSimulator deviceSimulator;

    FieldInfo hmdFieldInfo;
    FieldInfo leftFieldInfo;
    FieldInfo rightFieldInfo;

    void Start()
    {
        // disable device simulator on device
        if (!Application.isEditor) gameObject.SetActive(false);

        // get ref to device simulator
		deviceSimulator = GetComponent<XRDeviceSimulator>();

        // field info required to access private member variables
        hmdFieldInfo = typeof(XRDeviceSimulator).GetField("m_HMDState", BindingFlags.NonPublic | BindingFlags.Instance);
        leftFieldInfo = typeof(XRDeviceSimulator).GetField("m_LeftControllerState", BindingFlags.NonPublic | BindingFlags.Instance);
        rightFieldInfo = typeof(XRDeviceSimulator).GetField("m_RightControllerState", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void Update()
	{
        if(ApplySavedStateOnStartup)
        {
            string hmdStr = PlayerPrefs.GetString(PREF_HMD, "");
            string leftStr = PlayerPrefs.GetString(PREF_LEFT, "");
            string rightStr = PlayerPrefs.GetString(PREF_RIGHT, "");
            if (hmdStr != "")
            {
                hmdFieldInfo.SetValue(deviceSimulator, JsonUtility.FromJson<XRSimulatedHMDState>(hmdStr));
                //Debug.Log("Apply hmd state: " + hmdStr);
            }
            if (leftStr != "")
            {
                leftFieldInfo.SetValue(deviceSimulator, JsonUtility.FromJson<XRSimulatedControllerState>(leftStr));
                //Debug.Log("Apply left controller state: " + leftStr);
            }
            if (rightStr != "")
            {
                rightFieldInfo.SetValue(deviceSimulator, JsonUtility.FromJson<XRSimulatedControllerState>(rightStr));
                //Debug.Log("Apply right controller state: " + rightStr);
            }

            ApplySavedStateOnStartup = false;
        }
        if(SaveCurrentState)
        {
            PlayerPrefs.SetString(PREF_HMD, JsonUtility.ToJson(hmdFieldInfo.GetValue(deviceSimulator)));
            PlayerPrefs.SetString(PREF_LEFT, JsonUtility.ToJson(leftFieldInfo.GetValue(deviceSimulator)));
            PlayerPrefs.SetString(PREF_RIGHT, JsonUtility.ToJson(rightFieldInfo.GetValue(deviceSimulator)));
        }
    }
}


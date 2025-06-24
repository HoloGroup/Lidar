using TMPro;
using UnityEngine;

public class DownloadingWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text percentsText;
    [SerializeField] private GameObject successWindow;
    [SerializeField] private GameObject errorWindow;

    private bool _listenForModelReciever;
    private bool _initialized = false;
    private void OnEnable()
    {
        percentsText.text = "0";

        if(!_initialized)
        {
            VRTeleportation_NetworkBehviour.Instance.OnModelReceived += OpenSuccessWindow;
            _initialized = true;
        }
    }

    private void Update()
    {
        //if (VRTeleportation_NetworkBehviour.Instance.ModelReceiver != null)
        //{
        //    if (!_listenForModelReciever)
        //    {
        //        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += OpenSuccessWindow;
        //        VRTeleportation_NetworkBehviour.Instance.ModelReceiver.OnModelReceivedError += OpenErrorWindow;
        //        _listenForModelReciever = true;
        //    }

        //    float x = VRTeleportation_NetworkBehviour.Instance.ModelReceiver.FullModelOffset;
        //    float y = VRTeleportation_NetworkBehviour.Instance.ModelReceiver.FullModelLenght;
        //    float value = (x / y) * 100f;
        //    value = Mathf.RoundToInt(value);

        //    if (value > 0)
        //        percentsText.text = value.ToString();
        //}
        //else
            _listenForModelReciever = false;
    }

    private void OpenSuccessWindow(byte[] d)
    {
        //VRTeleportation_NetworkBehviour.Instance.OnModelReceived -= OpenSuccessWindow;
        //VRTeleportation_NetworkBehviour.Instance.ModelReceiver.OnModelReceivedError -= OpenErrorWindow;
        successWindow.SetActive(true);
        gameObject.SetActive(false);
    }

    private void OpenErrorWindow()
    {
        //VRTeleportation_NetworkBehviour.Instance.OnModelReceived -= OpenSuccessWindow;
        //VRTeleportation_NetworkBehviour.Instance.ModelReceiver.OnModelReceivedError -= OpenErrorWindow;
        errorWindow.SetActive(true);
        gameObject.SetActive(false);
    }
}

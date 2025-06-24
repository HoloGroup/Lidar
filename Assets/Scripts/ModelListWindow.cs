using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelListWindow : MonoBehaviour
{
    [SerializeField] private Transform content;

    public bool ListenForModelReciever { get; set; }

    private bool _isInitialized = false;

    private void OnEnable()
    {
        ClearList();
        EnableButtons(true);
        FillModelList();
    }

    public void OnCloseCLick()
    {
        gameObject.SetActive(false);
    }

    public void EnableButtons(bool value)
    {
        List<Button> buttons = AppManager.Instance.modelListWindow.GetComponentsInChildren<Button>().ToList();

        foreach (var button in buttons)
        {
            button.interactable = value;
        }
    }

    private void ClearList()
    {
        for (int j = 0; j < content.childCount; j++)
        {
            Destroy(content.GetChild(j).gameObject);
        }
        content.DetachChildren();
    }

    private async void FillModelList()
    {
        if(!_isInitialized)
        {
            _isInitialized = true;

            VRTeleportation_NetworkBehviour.Instance.OnModelListReceived += (list) =>
            {
                ClearList();

                var i = 0;
                list.Reverse();
                foreach (var model in list)
                {
                    var go = Instantiate(AppManager.Instance.ModelListItem, content);

                    // Init model item
                    var mli = go.GetComponent<ModelListItem>();
                    mli.Initialize(list[i]);
                    i++;
                }
            };
        }


        VRTeleportation_NetworkBehviour.Instance.SendGetModelList();
    }
}

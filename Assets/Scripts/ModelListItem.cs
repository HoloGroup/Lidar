using GLTFast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelListItem : MonoBehaviour
{
    [SerializeField] private Button modelMainButton;
    [SerializeField] private TMP_Text modelNameText;
    [SerializeField] private Button modelSaveButton;
    [SerializeField] private Button modelDeleteButton;
    [SerializeField] private Image modelDownloadingImage;
    [SerializeField] private TMP_Text modelStatusText;

    public string Name { get { return _name; }}
    private string _name;
    private string _path;

    private bool _listenForModelReciever;

    private ModelInfo _info;

    private void Start()
    {
        modelMainButton.onClick.AddListener(OnModelMainButtonClick);
        modelSaveButton.onClick.AddListener(OnModelSaveButtonClick);
        modelDeleteButton.onClick.AddListener(OnModelDeleteButtonClick);
    }

    private void OnDestroy()
    {
        modelMainButton.onClick.RemoveListener(OnModelMainButtonClick);
        modelSaveButton.onClick.RemoveListener(OnModelSaveButtonClick);
        modelDeleteButton.onClick.RemoveListener(OnModelDeleteButtonClick);
    }

    internal void Initialize(ModelInfo modelInfo)
    {
        _info = modelInfo;
        Initialize(_info.Name);
    }

    private void Initialize(string value)
    {
        // Init name
        _name = value;
        _path = Path.Combine(Application.persistentDataPath, _name + ".glb");
        Debug.Log(_path);
        modelNameText.text = _name;

        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += OnModelSaved;

        // Get model status
        GetModelStatus();
    }

    //private void Update()
    //{
    //    if (VRTeleportation_NetworkBehviour.Instance.ModelReceiver != null && _listenForModelReciever)
    //    {
    //        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += OnModelSaved;
    //        VRTeleportation_NetworkBehviour.Instance.ModelReceiver.OnModelReceivedError += OnModelSavedError;

    //        float x = VRTeleportation_NetworkBehviour.Instance.ModelReceiver.FullModelOffset;
    //        float y = VRTeleportation_NetworkBehviour.Instance.ModelReceiver.FullModelLenght;
    //        float value = x / y;

    //        if (value > 0)
    //            modelDownloadingImage.fillAmount = value;
    //    } 
    //}

    private void GetModelStatus()
    {
        if (File.Exists(_path))
        {
            modelStatusText.text = "Saved to device";
            modelDeleteButton.gameObject.SetActive(true);
            modelSaveButton.gameObject.SetActive(false);
            modelDownloadingImage.fillAmount = 0;
        }
        else
        {
            modelStatusText.text = "On server";
            modelDeleteButton.gameObject.SetActive(false);
            modelSaveButton.gameObject.SetActive(true);
        }
    }

    private void OnModelMainButtonClick()
    {
        AppManager.Instance.modelListWindow.gameObject.SetActive(false);

        // Open saved model or download it
        if (File.Exists(_path))
            GetSavedModel();
        else
        {
            AppManager.Instance.downloadingWindow.SetActive(true);
            DownloadModel();
        }
    }

    private void OnModelSaveButtonClick()
    {
        AppManager.Instance.modelListWindow.EnableButtons(false);
        SaveModel();
    }

    private void OnModelDeleteButtonClick()
    {
        DeleteModel();
    }

    private void SaveModel()
    {
        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += async (byte[] d) =>
        {
            Debug.Log($"Saved model {d.Length}");
            VRTeleportation_NetworkBehviour.Instance.OnModelReceived = null;

            await new WaitForUpdate();

            await StartCoroutine(WriteFileAndNotify(d));
        };

        _listenForModelReciever = true;
        VRTeleportation_NetworkBehviour.Instance.GetModel(_info.ID);
    }

    private IEnumerator WriteFileAndNotify(byte[] _data)
    {
        File.WriteAllBytes(_path, _data);
        AppManager.Instance.modelListWindow.EnableButtons(true);
        GetModelStatus();

        yield return null;
    }

    private void OnModelSaved(byte[] d)
    {
        _listenForModelReciever = false;

        VRTeleportation_NetworkBehviour.Instance.OnModelReceived -= OnModelSaved;
    }

    private void OnModelSavedError()
    {
        modelDownloadingImage.fillAmount = 0;
        _listenForModelReciever = false;

        VRTeleportation_NetworkBehviour.Instance.OnModelReceived -= OnModelSaved;
    }

    private void DeleteModel()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        GetModelStatus();
    }

    private void DownloadModel()
    {
        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += async (byte[] d) =>
        {
            Debug.Log($"Received model {d.Length}");
            VRTeleportation_NetworkBehviour.Instance.OnModelReceived = null;

            await new WaitForUpdate();

            var serializer = new VRTeleportation_VRModelSerializer();
            serializer.MaterialForDeserialize = AppManager.Instance.DeserializeMaterial;

            if (AppManager.Instance.LoadedModel != null)
                Destroy(AppManager.Instance.LoadedModel);

            //var parent = new GameObject("Restored model");

            //GLTFast.GltfImport importer = new GLTFast.GltfImport();
            //await importer.LoadGltfBinary(d);
            //await importer.InstantiateMainSceneAsync(parent.transform);


            AppManager.Instance.LoadedModel = serializer.Deserialize(d, 0);
        };

        VRTeleportation_NetworkBehviour.Instance.GetModel(_info.ID);
    }

    private async void GetSavedModel()
    {
        var serializer = new VRTeleportation_VRModelSerializer();
        serializer.MaterialForDeserialize = AppManager.Instance.DeserializeMaterial;

        if (AppManager.Instance.LoadedModel != null)
            Destroy(AppManager.Instance.LoadedModel);

        var d = File.ReadAllBytes(_path);

        //var parent = new GameObject("Restored model");

        //GLTFast.GltfImport importer = new GLTFast.GltfImport();
        //ImportSettings i = new ImportSettings();

        //await importer.LoadGltfBinary(d);
        //await importer.InstantiateMainSceneAsync(parent.transform);

        AppManager.Instance.LoadedModel =  serializer.Deserialize(d, 0);
    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using TMPro;
using TriLibCore;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ModelListItem : MonoBehaviour
{
    [SerializeField] private Button modelMainButton;
    [SerializeField] private TMP_Text modelNameText;
    [SerializeField] private TMP_Text modelDataText;

    [SerializeField] private Button modelSaveButton;
    [SerializeField] private Button modelDeleteButton;
    [SerializeField] private Image modelDownloadingImage;
    [SerializeField] private TMP_Text modelStatusText;

    public string Name { get { return _name; } }
    private string _name;
    private string _path;

    private bool _listenForModelReciever;

    private ModelInfo _info;

    private Coroutine _downloadingAnimationRoutine;

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
        modelDataText.text = modelInfo.CreationDate.ToString();
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
        //AppManager.Instance.modelListWindow.gameObject.SetActive(false);

        // Open saved model or download it
        if (File.Exists(_path))
        {
            AppManager.Instance.modelListWindow.gameObject.SetActive(false);

            GetSavedModel();
        }
        else
        {
            //AppManager.Instance.downloadingWindow.SetActive(true);
            //DownloadModel();
            AppManager.Instance.modelListWindow.EnableButtons(false);
            SaveModel(true);
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

    private void SaveModel(bool autoopen = false)
    {
        _downloadingAnimationRoutine = StartCoroutine(DownloadingAnimationProcess());

        VRTeleportation_NetworkBehviour.Instance.OnModelReceived += async (byte[] d) =>
        {
            Debug.Log($"Saved model {d.Length}");
            VRTeleportation_NetworkBehviour.Instance.OnModelReceived = null;

            await new WaitForUpdate();

            await StartCoroutine(WriteFileAndNotify(d));

            if (_downloadingAnimationRoutine != null)
            {
                StopCoroutine(_downloadingAnimationRoutine);
                modelDownloadingImage.fillAmount = 0;
            }


            if (autoopen)
                OnModelMainButtonClick();
        };

        _listenForModelReciever = true;
        VRTeleportation_NetworkBehviour.Instance.GetModel(_info.ID);
    }
    private IEnumerator DownloadingAnimationProcess()
    {
        bool clockwise = modelDownloadingImage.fillClockwise;
        while (true)
        {
            if (clockwise)
            {
                if (modelDownloadingImage.fillAmount < 1)
                {
                    modelDownloadingImage.fillAmount += Time.deltaTime * 0.7f;
                }
                else
                {
                    clockwise = !clockwise;
                    modelDownloadingImage.fillClockwise = clockwise;
                }
            }
            else
            {
                if (modelDownloadingImage.fillAmount > 0)
                {
                    modelDownloadingImage.fillAmount -= Time.deltaTime * 0.7f;
                }
                else
                {
                    clockwise = !clockwise;
                    modelDownloadingImage.fillClockwise = clockwise;
                }
            }

            yield return null;
        }
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



    private async void GetSavedModel()
    {
        var serializer = new VRTeleportation_VRModelSerializer();
        serializer.MaterialForDeserialize = AppManager.Instance.DeserializeMaterial;

        if (AppManager.Instance.LoadedModel != null)
            Destroy(AppManager.Instance.LoadedModel);


        if(_info.IsRealGLB)
        {
            var request = UnityWebRequest.Get("file://"+_path);
            await AssetDownloader.LoadModelFromUri(request, OnLoad, OnMaterialsLoad, OnProgress, OnError);
            //var parent = new GameObject("Restored model");

            //GLTFast.GltfImport importer = new GLTFast.GltfImport();
            //importer.defaultMaterial = AppManager.Instance.DeserializeMaterial;

            //await importer.LoadGltfBinary(d);
            //await importer.InstantiateMainSceneAsync(parent.transform);
        }
        else
        {
            var d = File.ReadAllBytes(_path);
            AppManager.Instance.LoadedModel = serializer.Deserialize(d, 0);
        }

        void OnError(IContextualizedError obj)
        {
            Debug.LogError($"An error occurred while loading your Model: {obj.GetInnerException()}");
        }

        void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            Debug.Log($"Loading Model. Progress: {progress:P}");
        }

        void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            //assetLoaderContext.RootGameObject.SetActive(false);
            Debug.Log("Model loaded. Loading materials.");
        }

        void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            //assetLoaderContext.RootGameObject.SetActive(true);
            //CachedPlayable = assetLoaderContext.RootGameObject;
            //IsLoaded = true;

            //LoadedEvent?.Invoke();
        }
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using GLTFast.Export;
using GLTFast;
using System.Threading.Tasks;
using GLTFast.Logging;

public class ARMeshGLBExporter : MonoBehaviour
{
    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "ScannedRoom";
    [SerializeField] private bool exportToStreamingAssets = true;
    [SerializeField] private bool exportToDocuments = false;
    [SerializeField] private GameObject _targetObject;
    public string filePath = "D://GIT//Lidar//Assets//StreamingAssets//ScannedRoom.glb";
    public Material material;
    private string GetExportPath()
    {
        if (exportToStreamingAssets)
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }
            return Path.Combine(streamingAssetsPath, exportFileName + ".glb");
        }
        else if (exportToDocuments)
        {
            // Для iOS
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, exportFileName + ".glb");
        }
        else
        {
            return Path.Combine(Application.persistentDataPath, exportFileName + ".glb");
        }
    }

    [ContextMenu("Export to GLB")]
    public async void ExportToGLB()
    {
        await ExportToGLB(_targetObject);
    }

    [ContextMenu("Import from GLB")]
    public async void ImportGLB()
    {
        await ImportFromFile(filePath);
    }

    public async Task ExportToGLB(GameObject scannedRoom)
    {
        if (scannedRoom == null)
        {
            Debug.LogError("No textured mesh data available for export!");
            return;
        }

        try
        {
            string exportPath = GetExportPath();
            Debug.Log($"Starting GLB export to: {exportPath}");

            // Настройка экспорта
            var exportSettings = new ExportSettings
            {
                Format = GltfFormat.Binary,
                FileConflictResolution = FileConflictResolution.Overwrite,
                ComponentMask = ~(ComponentType.Camera | ComponentType.Light),//, // Исключаем камеры и свет
                ImageDestination = ImageDestination.MainBuffer

                //// Настройки для текстур
                //ImageSettings = new ImageExportSettings
                //{
                //    ImageFormat = ImageFormat.Png,
                //    TextureMaxResolution = 2048,
                //    JpegQuality = 90
                //}
            };



            // Создаём экспортер
            var gltf = new GameObjectExport(exportSettings);

            // Добавляем объект для экспорта
            gltf.AddScene(new GameObject[] { scannedRoom }, "ScannedRoom");

            // Экспортируем
            var success = await gltf.SaveToFileAndDispose(exportPath);

            if (success)
            {
                Debug.Log($"Successfully exported GLB to: {exportPath}");

                // Показываем информацию о файле
                FileInfo fileInfo = new FileInfo(exportPath);
                Debug.Log($"File size: {fileInfo.Length / (1024 * 1024f):F2} MB");

                // Опционально: показываем в проводнике (только в редакторе)
#if UNITY_EDITOR
                UnityEditor.EditorUtility.RevealInFinder(exportPath);
#endif
            }
            else
            {
                Debug.LogError("Failed to export GLB file!");
            }

            // Удаляем временный объект
            if (Application.isPlaying)
            {
                Destroy(scannedRoom);
            }
            else
            {
                DestroyImmediate(scannedRoom);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during GLB export: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    public async Task ImportFromFile(string filePath)
    {
        var gltf = new GLTFast.GltfImport();
        //gltf.defaultMaterial = material;

        // Create a settings object and configure it accordingly
        var settings = new ImportSettings
        {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.OriginalUnique
        };
        // Load the glTF and pass along the settings
        var success = await gltf.Load($"file://{filePath}", settings);

        if (success)
        {
            var gameObject = new GameObject("glTF");
            await gltf.InstantiateMainSceneAsync(gameObject.transform);
        }
        else
        {
            Debug.LogError("Loading glTF failed!");
        }
    }


    private IEnumerator OptimizeTexturesForExport()
    {
        Debug.Log("Optimizing textures for export...");

        // Здесь можно добавить дополнительную оптимизацию текстур
        // например, увеличение разрешения для финального экспорта

        yield return null;
    }

   
    // Метод для получения информации о доступном месте на устройстве
    public void CheckStorageSpace()
    {
        try
        {
            string path = Path.GetDirectoryName(GetExportPath());
            DriveInfo drive = new DriveInfo(path);

            long availableSpace = drive.AvailableFreeSpace;
            long totalSpace = drive.TotalSize;

            Debug.Log($"Available storage space: {availableSpace / (1024 * 1024 * 1024f):F2} GB");
            Debug.Log($"Total storage space: {totalSpace / (1024 * 1024 * 1024f):F2} GB");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not check storage space: {e.Message}");
        }
    }

    // Вспомогательный метод для конвертации текстур в более подходящий формат
    private Texture2D ConvertRenderTextureToTexture2D(RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        return texture2D;
    }
}

//public class MaterialExporter : IMaterialExport
//{
//    public bool ConvertMaterial(Material uMaterial, out GLTFast.Schema.Material material, IGltfWritable gltf, ICodeLogger logger)
//    {
//        throw new System.NotImplementedException();
//    }
//}
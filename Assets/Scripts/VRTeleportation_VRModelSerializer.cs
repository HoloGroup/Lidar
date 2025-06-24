using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
public class VRTeleportation_VRModelSerializer
{
    public Material MaterialForDeserialize;

    private class MeshFilterData
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 Scale;

        public Vector3[] Verts;
        public int[] Tris;
        public Vector3[] Normals;
        public Vector2[] UV;
        public byte[] Texture = new byte[0];
        public string TextureHash = string.Empty;

        public Texture2D Texture2D
        {
            get
            {
                if (Texture.Length == 0)
                    return null;
                else
                {
                    var tex = new Texture2D(1, 1);
                    var res = tex.LoadImage(Texture);
                    tex.Apply();

                    return tex;
                }
            }
        }

        public List<byte> Serialize()
        {
            List<byte> data = new List<byte>();

            data.AddRange(GetBytes(LocalPosition));
            data.AddRange(GetBytes(LocalRotation));
            data.AddRange(GetBytes(Scale));

            data.AddRange(GetBytes(Verts));
            data.AddRange(GetBytes(Tris));
            data.AddRange(GetBytes(Normals));
            data.AddRange(GetBytes(UV));

            data.AddRange(BitConverter.GetBytes(Texture.Length));

            if (Texture.Length > 0)
                data.AddRange(Texture);

            return data;
        }

        public int Deserialize(byte[] data, int offset)
        {
            offset = GetVector3(data, offset, out LocalPosition);
            offset = GetQuaternion(data, offset, out LocalRotation);
            offset = GetVector3(data, offset, out Scale);

            offset = GetVector3Array(data, offset, out Verts);
            offset = GetIntArray(data, offset, out Tris);
            offset = GetVector3Array(data, offset, out Normals);
            offset = GetVector2Array(data, offset, out UV);

            var texLenght = BitConverter.ToInt32(data, offset);
            offset += 4;

            if (texLenght > 0)
            {
                Texture = new byte[texLenght];
                Buffer.BlockCopy(data, offset, Texture, 0, texLenght);


                offset += texLenght;
            }

            return offset;
        }

        private List<byte> GetBytes(Vector3 input)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(input.x));
            data.AddRange(BitConverter.GetBytes(input.y));
            data.AddRange(BitConverter.GetBytes(input.z));

            return data;
        }

        private List<byte> GetBytes(Vector2 input)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(input.x));
            data.AddRange(BitConverter.GetBytes(input.y));

            return data;
        }

        private List<byte> GetBytes(Quaternion input)
        {
            var inp = input.eulerAngles;

            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(inp.x));
            data.AddRange(BitConverter.GetBytes(inp.y));
            data.AddRange(BitConverter.GetBytes(inp.z));

            return data;
        }

        private List<byte> GetBytes(Vector3[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(GetBytes(part));
            }

            return data;
        }

        private List<byte> GetBytes(int[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(BitConverter.GetBytes(part));
            }

            return data;
        }

        private List<byte> GetBytes(Vector2[] input)
        {
            List<byte> data = new List<byte>();


            data.AddRange(BitConverter.GetBytes(input.Length));
            foreach (var part in input)
            {
                data.AddRange(GetBytes(part));
            }

            return data;
        }



        private int GetVector3(byte[] data, int offset, out Vector3 output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            var z = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = new Vector3(x, y, z);

            return offset;
        }


        private int GetVector2(byte[] data, int offset, out Vector2 output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = new Vector2(x, y);

            return offset;
        }

        private int GetQuaternion(byte[] data, int offset, out Quaternion output)
        {
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;

            var y = BitConverter.ToSingle(data, offset);
            offset += 4;

            var z = BitConverter.ToSingle(data, offset);
            offset += 4;

            output = Quaternion.Euler(x, y, z);

            return offset;
        }


        private int GetVector3Array(byte[] data, int offset, out Vector3[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            List<Vector3> result = new List<Vector3>(count);

            for (int i = 0; i < count; ++i)
            {
                Vector3 vector;
                offset = GetVector3(data, offset, out vector);
                result.Add(vector);
            }

            output = result.ToArray();

            return offset;
        }

        private int GetVector2Array(byte[] data, int offset, out Vector2[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            List<Vector2> result = new List<Vector2>(count);

            for (int i = 0; i < count; ++i)
            {
                Vector2 vector;
                offset = GetVector2(data, offset, out vector);
                result.Add(vector);
            }

            output = result.ToArray();

            return offset;
        }

        private int GetIntArray(byte[] data, int offset, out int[] output)
        {
            var count = BitConverter.ToInt32(data, offset);
            offset += 4;

            int[] result = new int[count];

            for (int i = 0; i < count; ++i)
            {
                result[i] = BitConverter.ToInt32(data, offset);
                offset += 4;
            }

            output = result;

            return offset;
        }
    }


    public byte[] Serialize(GameObject model)
    {
        var allMeshFilters = model.GetComponentsInChildren<MeshFilter>();
        List<MeshFilterData> mfDatas = new List<MeshFilterData>(allMeshFilters.Length);

        foreach (var mf in allMeshFilters)
        {
            var mfData = new MeshFilterData();
            var mfTransform = mf.transform;
            mfData.LocalPosition = mfTransform.localPosition;
            mfData.LocalRotation = mfTransform.localRotation;
            mfData.Scale = mfTransform.localScale;

            mfData.Verts = mf.sharedMesh.vertices;
            mfData.Tris = mf.sharedMesh.triangles;
            mfData.Normals = mf.sharedMesh.normals;
            mfData.UV = mf.sharedMesh.uv;

            var tex = mf.GetComponent<MeshRenderer>().material.GetTexture("_BaseMap");
            if (tex != null)
            {
                var tex2d = (tex as Texture2D);
                mfData.Texture = tex2d.EncodeToJPG();
            }

            mfDatas.Add(mfData);
        }


        var resultData = new List<byte>();

        resultData.AddRange(BitConverter.GetBytes(mfDatas.Count));
        foreach (var data in mfDatas)
        {
            resultData.AddRange(data.Serialize());
        }

        return resultData.ToArray();
    }

    public GameObject Deserialize(byte[] data, int offset)
    {
        int meshCount = BitConverter.ToInt32(data, offset);
        offset += 4;

        List<MeshFilterData> mfDatas = new List<MeshFilterData>(meshCount);

        for (int i = 0; i < meshCount; ++i)
        {
            var mfData = new MeshFilterData();
            offset = mfData.Deserialize(data, offset);

            mfDatas.Add(mfData);
        }

        var parent = new GameObject("Restored model");
        var parentTransform = parent.transform;

        parentTransform.position = new Vector3(parentTransform.position.x, parentTransform.position.y + 1.55f, parentTransform.position.z);

        Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        var cryptograph = System.Security.Cryptography.MD5.Create();
        foreach (var mfData in mfDatas)
        {
            var hash = cryptograph.ComputeHash(mfData.Texture);
            var asString = Convert.ToBase64String(hash);
            mfData.TextureHash = asString;

            if (_textures.ContainsKey(asString))
                continue;

            _textures.Add(asString, mfData.Texture2D);
        }

        foreach (var mfData in mfDatas)
        {
            var child = new GameObject();
            child.transform.parent = parentTransform;
            child.transform.localPosition = mfData.LocalPosition;
            child.transform.localRotation = mfData.LocalRotation;
            child.transform.localScale = mfData.Scale;

            var mf = child.AddComponent<MeshFilter>();

            var mesh = new Mesh();
            mesh.vertices = mfData.Verts;
            mesh.triangles = mfData.Tris;
            mesh.normals = mfData.Normals;
            mesh.uv = mfData.UV;

            mf.sharedMesh = mesh;

            var mr = child.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialForDeserialize;

            if (!string.IsNullOrEmpty(mfData.TextureHash))
            {
                var tex = _textures[mfData.TextureHash];
                if (tex != null)
                    mr.material.SetTexture("_BaseMap", tex);
            }
        }

        return parent;
    }
}

public class CustomMaterialGenerator : IMaterialGenerator
{
    public Material GenerateMaterial(GLTFast.Schema.Material gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
    {
        return AppManager.Instance.DeserializeMaterial;
    }

    public Material GetDefaultMaterial(bool pointsSupport = false)
    {
        return AppManager.Instance.DeserializeMaterial;
    }

    public void SetLogger(ICodeLogger logger)
    {

    }
}

public class CustomLogger : ICodeLogger
{
    public void Error(LogCode code, params string[] messages)
    {
        foreach(var msg in messages)
        {
            Debug.LogError(msg);
        }
    }

    public void Error(string message)
    {
        Debug.LogError(message);
    }

    public void Info(LogCode code, params string[] messages)
    {
        foreach (var msg in messages)
        {
            Debug.Log(msg);
        }
    }

    public void Info(string message)
    {
        Debug.Log(message);
    }

    public void Warning(LogCode code, params string[] messages)
    {
        foreach (var msg in messages)
        {
            Debug.LogWarning(msg);
        }
    }

    public void Warning(string message)
    {
        Debug.LogWarning(message);
    }
}